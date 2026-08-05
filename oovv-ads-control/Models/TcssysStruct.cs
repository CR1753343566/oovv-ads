using System.Runtime.InteropServices;

namespace oovv_ads_control.Models
{
    /// <summary>
    /// 对应 PLC bnct_plc 里的 <c>TCSSys_Struct</c>
    /// （DUTs/System_Status_Struct/TCSSys_Struct.TcDUT）。
    /// 这个不是挂在 Call_Values_To_PC 底下，而是 GVL_TCSSys 这个独立 PROGRAM 的字段，
    /// 符号路径：GVL_TCSSys.TCSSys
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct TcssysStruct
    {
        [MarshalAs(UnmanagedType.I1)] public bool Param000; // 只读，TRUE为加速器准备好
        [MarshalAs(UnmanagedType.I1)] public bool Param001; // 读写，TRUE为TCS准备好
        [MarshalAs(UnmanagedType.I1)] public bool Param002; // 只读，TRUE代表加速器出束
        [MarshalAs(UnmanagedType.I1)] public bool Param003; // 读写，TRUE为TCS系统正在运行
    }
}
