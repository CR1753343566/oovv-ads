using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using oovv_ads_control.Ads;
using oovv_ads_control.Models;

namespace oovv_ads_control.Services
{
    /// <summary>按需读取的一项：符号路径 + 目标 .NET 类型（标量或 Models 里的 struct 都行）。</summary>
    public sealed record ReadItem(string VariableName, Type ValueType);

    /// <summary>一次读取的结果。失败时 Value 为 null，ErrorCode 有值就是 ADS 报的错，没有就是本地异常（看日志）。</summary>
    public sealed record ReadResult(string VariableName, bool Succeeded, object? Value, AdsErrorCode? ErrorCode);

    /// <summary>按需写入的一项：符号路径 + 要写的值。</summary>
    public sealed record WriteItem(string VariableName, object Value);

    /// <summary>一次写入的结果。</summary>
    public sealed record WriteResult(string VariableName, bool Succeeded, AdsErrorCode? ErrorCode);

    /// <summary>
    /// 系统级 PLC 变量读写服务：和 PlcNotificationService 对称，一个是"推送"，这个是"按需拉取"。
    /// 单个读写走 CreateVariableHandleAsync + ReadAnyAsync/WriteAnyAsync（BaseSamples/ReadWriteAnyType 里验证过的写法），
    /// 标量和 Models 里的 struct 用的是同一套泛型方法，不需要分开写。
    ///
    /// 批量读写（ReadManyAsync/WriteManyAsync）走 TwinCAT.Ads.SumCommand 里的 SumHandleReadAnyType/
    /// SumHandleWriteAnyType——按 handle 打包，不需要 SymbolLoader，N 个变量的读或写打成一个 ADS 包发出去。
    /// handle 本身还是用下面这套惰性缓存拿（可能各自命中缓存、也可能各自现建，现建的部分是并发的，
    /// 不是打包成一个包创建，真正省 ADS 包数量的是"读/写"这一步，不是"创建 handle"那一步）。
    ///
    /// 写入这边 ResultSumCommand.SubErrors 能拿到每一项各自的 AdsErrorCode，读取这边 ResultSumValues
    /// 只确认了整体的 Succeeded/ErrorCode + 按顺序对应的 Values[]——没能在文档里确认到读取的每项错误码
    /// 单独暴露在哪个属性上，所以 ReadManyAsync 目前是"整批一起判断成功/失败"，不是每项独立判断；
    /// 如果以后连上真实 PLC 发现批量读取里某一项失败但整体 Succeeded 还是 true/false 的具体行为和这里的假设
    /// 不一致，把实际现象告诉我，再来改这部分。
    /// </summary>
    public sealed class PlcVariableService : IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<PlcVariableService>();

        private readonly AdsConnectionManager _connectionManager;
        private readonly object _gate = new();
        private readonly Dictionary<string, uint> _handles = new();

        public PlcVariableService(AdsConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            _connectionManager.Disconnected += OnDisconnected;
        }

        /// <summary>读一个变量（标量或 struct 都行），T 必须和 PLC 端类型 blittable 匹配。</summary>
        public async Task<T> ReadAsync<T>(string variableName, CancellationToken ct = default)
        {
            var connection = RequireConnection();
            var handle = await GetOrCreateHandleAsync(connection, variableName, ct);

            // 用 ReadAnyAsync<T> 这个真泛型重载（BaseSamples/ReadWriteFlag 里演示过），
            // 直接拿到强类型的 ResultValue<T>，不用像 ReadAnyAsync(handle, typeof(T), ct) 那样
            // 返回 object 再自己 (T)result.Value! 做一次不受编译器检查的强制转换。
            var result = await connection.ReadAnyAsync<T>(handle, ct);
            if (!result.Succeeded)
                throw new InvalidOperationException($"读取 {variableName} 失败：{result.ErrorCode}");

            return result.Value;
        }

        /// <summary>写一个变量（标量或 struct 都行）。</summary>
        public async Task WriteAsync<T>(string variableName, T value, CancellationToken ct = default) where T : notnull
        {
            var connection = RequireConnection();
            var handle = await GetOrCreateHandleAsync(connection, variableName, ct);

            var result = await connection.WriteAnyAsync(handle, value, ct);
            if (!result.Succeeded)
                throw new InvalidOperationException($"写入 {variableName} 失败：{result.ErrorCode}");
        }

        /// <summary>
        /// 用 PlcVariables 目录里的描述符读取，路径和类型绑在一起声明，不用在调用点手写字符串和类型。
        /// </summary>
        public Task<T> ReadAsync<T>(PlcVariable<T> variable, CancellationToken ct = default)
            => ReadAsync<T>(variable.Path, ct);

        /// <summary>用 PlcVariables 目录里的描述符写入，用法同上。</summary>
        public Task WriteAsync<T>(PlcVariable<T> variable, T value, CancellationToken ct = default) where T : notnull
            => WriteAsync(variable.Path, value, ct);

        /// <summary>
        /// 批量读取：每一项可以是不同的类型（标量混 struct 都行），N 个变量打包成一个 ADS 包读回来。
        /// 整批一起判断成功/失败（原因见类注释），成功时按传入顺序对应 Values。
        /// </summary>
        public async Task<IReadOnlyList<ReadResult>> ReadManyAsync(IReadOnlyList<ReadItem> items, CancellationToken ct = default)
        {
            if (items.Count == 0)
                return Array.Empty<ReadResult>();

            var connection = RequireConnection();

            try
            {
                var handleTasks = items.Select(item => GetOrCreateHandleAsync(connection, item.VariableName, ct));
                var handles = await Task.WhenAll(handleTasks);
                var valueTypes = items.Select(item => item.ValueType).ToArray();

                var sum = new SumHandleReadAnyType(connection, handles, valueTypes);
                var result = await sum.ReadAsync(ct);

                if (!result.Succeeded)
                {
                    Logger.Warning("批量读取（Sum）整体失败：{ErrorCode}", result.ErrorCode);
                    return items.Select(item => new ReadResult(item.VariableName, false, null, result.ErrorCode)).ToArray();
                }

                var results = new ReadResult[items.Count];
                for (int i = 0; i < items.Count; i++)
                    results[i] = new ReadResult(items[i].VariableName, true, result.Values[i], null);

                return results;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "批量读取（Sum）异常");
                return items.Select(item => new ReadResult(item.VariableName, false, null, null)).ToArray();
            }
        }

        /// <summary>
        /// 批量写入：N 个变量打包成一个 ADS 包写下去，ResultSumCommand.SubErrors 能拿到每一项各自的错误码，
        /// 单项失败不影响其它项的判断。
        /// </summary>
        public async Task<IReadOnlyList<WriteResult>> WriteManyAsync(IReadOnlyList<WriteItem> items, CancellationToken ct = default)
        {
            if (items.Count == 0)
                return Array.Empty<WriteResult>();

            var connection = RequireConnection();

            try
            {
                var handleTasks = items.Select(item => GetOrCreateHandleAsync(connection, item.VariableName, ct));
                var handles = await Task.WhenAll(handleTasks);
                var valueTypes = items.Select(item => item.Value.GetType()).ToArray();
                var values = items.Select(item => item.Value).ToArray();

                var sum = new SumHandleWriteAnyType(connection, handles, valueTypes);
                var result = await sum.WriteAsync(values, ct);

                var results = new WriteResult[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    var subError = result.SubErrors[i];
                    if (subError != AdsErrorCode.NoError)
                        Logger.Warning("批量写入失败：{VariableName} -> {ErrorCode}", items[i].VariableName, subError);

                    results[i] = new WriteResult(items[i].VariableName, subError == AdsErrorCode.NoError, subError);
                }

                return results;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "批量写入（Sum）异常");
                return items.Select(item => new WriteResult(item.VariableName, false, null)).ToArray();
            }
        }

        /// <summary>
        /// handle 缓存：CreateVariableHandle 本身是一次 ADS 往返，同一个变量重复读写时复用同一个 handle。
        /// 断线后缓存会被清空（见 OnDisconnected），下次用到时惰性重建——不需要像通知那样主动重建，
        /// 读写是"要用的时候再拿"，没有推送时序问题。
        /// </summary>
        private async Task<uint> GetOrCreateHandleAsync(AdsConnection connection, string variableName, CancellationToken ct)
        {
            lock (_gate)
            {
                if (_handles.TryGetValue(variableName, out var cached))
                    return cached;
            }

            var result = await connection.CreateVariableHandleAsync(variableName, ct);
            if (!result.Succeeded)
                throw new InvalidOperationException($"CreateVariableHandle 失败：{variableName} -> {result.ErrorCode}");

            lock (_gate)
            {
                _handles[variableName] = result.Handle;
            }

            return result.Handle;
        }

        private AdsConnection RequireConnection()
            => _connectionManager.Connection ?? throw new InvalidOperationException("当前未连接，无法读写变量。");

        private void OnDisconnected(object? sender, EventArgs e)
        {
            // 旧 Connection 已经整个 Dispose 了，句柄跟着失效，不需要逐个 DeleteVariableHandle，清缓存就够了
            lock (_gate)
            {
                _handles.Clear();
            }
        }

        public void Dispose()
        {
            _connectionManager.Disconnected -= OnDisconnected;
        }
    }
}
