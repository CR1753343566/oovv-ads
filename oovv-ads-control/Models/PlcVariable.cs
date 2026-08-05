namespace oovv_ads_control.Models
{
    /// <summary>
    /// 一个 PLC 变量的描述符：符号路径 + 对应的 C# 类型，两者绑在一起声明。
    /// 相当于 C# 里"带数据的枚举"——enum 本身做不到这点（只是命名整数），
    /// 这里用泛型 record 顶上：T 就是它的类型，PlcVariables 里每个变量各声明一个静态实例，
    /// 调用 ReadAsync(variable)/WriteAsync(variable, value) 时编译器就能保证路径和类型没传错。
    /// </summary>
    public sealed record PlcVariable<T>(string Path);
}
