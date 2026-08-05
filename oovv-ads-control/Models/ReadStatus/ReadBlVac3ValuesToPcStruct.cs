using System.Runtime.InteropServices;

namespace oovv_ads_control.Models.ReadStatus
{
    /// <summary>
    /// 对应 PLC bnct_plc 里的 <c>Read_BLVac3_Values_To_PC_Struct</c>
    /// （DUTs/Read_Status_Struct/Read_BLVac3_Values_To_PC_Struct.TcDUT）。
    /// 那是一个只有一个成员的 UNION，包着 DUTs/System_Status_Struct/S_BLVac3Values_To_PC_Struct.TcDUT，
    /// 字节布局和后者完全一样，所以这里直接照 S_BLVac3Values_To_PC_Struct 的字段顺序写。
    ///
    /// 符号路径：Call_Values_To_PC.S_Read_BLVac3_Values_To_PC
    ///
    /// 字段顺序必须和 PLC 端完全一致，不能调整/按字母排序；每个 BOOL 都要标 [MarshalAs(UnmanagedType.I1)]，
    /// 否则 .NET 会把 bool 按 4 字节的 Win32 BOOL marshal，后面所有字段全部错位。
    /// Pack=8 是 TC3 默认对齐，没在 bnct_plc.plcproj/.tmc 里看到覆盖设置——正式联调时用
    /// MAIN.TEST7（PLC 端 SIZEOF(S_BLVac3Values_To_PC_Struct) 的调试变量）跟这里的
    /// Marshal.SizeOf&lt;ReadBlVac3ValuesToPcStruct&gt;() 对一下，两边应该相等。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct ReadBlVac3ValuesToPcStruct
    {
        [MarshalAs(UnmanagedType.I1)] public bool Param000; // 束流线3低真空自动运行，True代表开
        [MarshalAs(UnmanagedType.I1)] public bool Param001; // 束流线3高真空自动运行，True代表开
        [MarshalAs(UnmanagedType.I1)] public bool Param002; // 束流线角阀3开到位
        [MarshalAs(UnmanagedType.I1)] public bool Param003; // 束流线角阀3关到位
        [MarshalAs(UnmanagedType.I1)] public bool Param004; // 束流线角阀3正在开
        [MarshalAs(UnmanagedType.I1)] public bool Param005; // 束流线角阀3正在关
        [MarshalAs(UnmanagedType.I1)] public bool Param006; // 束流线角阀3开启操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param007; // 束流线角阀3关闭操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param008; // 束流线放气阀3开到位
        [MarshalAs(UnmanagedType.I1)] public bool Param009; // 束流线放气阀3关到位
        [MarshalAs(UnmanagedType.I1)] public bool Param010; // 束流线放气阀3正在开
        [MarshalAs(UnmanagedType.I1)] public bool Param011; // 束流线放气阀3正在关
        [MarshalAs(UnmanagedType.I1)] public bool Param012; // 束流线放气阀3开启操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param013; // 束流线放气阀2关闭操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param014; // 束流线3机械泵开到位
        [MarshalAs(UnmanagedType.I1)] public bool Param015; // 束流线3机械泵关到位
        [MarshalAs(UnmanagedType.I1)] public bool Param016; // 束流线3机械泵正在开
        [MarshalAs(UnmanagedType.I1)] public bool Param017; // 束流线3机械泵正在关
        [MarshalAs(UnmanagedType.I1)] public bool Param018; // 束流线3机械泵开启操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param019; // 束流线3机械泵关闭操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param020; // 束流线3分子泵V7达到频率值
        [MarshalAs(UnmanagedType.I1)] public bool Param021; // 束流线3分子泵V7频率下降到0
        [MarshalAs(UnmanagedType.I1)] public bool Param022; // 束流线3分子泵V7故障
        [MarshalAs(UnmanagedType.I1)] public bool Param023; // 束流线3分子泵V7频率上升中
        [MarshalAs(UnmanagedType.I1)] public bool Param024; // 束流线3分子泵V7频率下降中
        [MarshalAs(UnmanagedType.I1)] public bool Param025; // 束流线3分子泵V7开启操作错误
        [MarshalAs(UnmanagedType.I1)] public bool Param026; // 束流线3分子泵V7关闭操作错误
        public float Param027;  // 束流线3分子泵V7输出频率（Hz），PLC REAL -> C# float
        public double Param028; // 束流线3低真空度（mbar），PLC LREAL -> C# double
        public double Param029; // 束流线3高真空度（mbar），PLC LREAL -> C# double
        [MarshalAs(UnmanagedType.I1)] public bool Param030; // 束流线3低真空已达标
        [MarshalAs(UnmanagedType.I1)] public bool Param031; // 束流线3高真空已达标
    }
}
