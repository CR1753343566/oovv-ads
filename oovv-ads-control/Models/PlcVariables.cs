using oovv_ads_control.Models.ReadStatus;

namespace oovv_ads_control.Models
{
    /// <summary>
    /// 全项目已知 PLC 变量的目录，符号路径只在这里出现一次。
    /// 目录结构照着 bnct_plc/DUTs 的文件夹分：ReadStatus 对应 DUTs/Read_Status_Struct。
    /// 新增一个变量就在这里加一个静态字段，其它地方引用 PlcVariables.XXX，不要再手写字符串。
    /// </summary>
    public static class PlcVariables
    {
        /// <summary>对应 bnct_plc/DUTs/Read_Status_Struct，挂在 Call_Values_To_PC 这个全局 PROGRAM 底下。</summary>
        public static class ReadStatus
        {
            public static readonly PlcVariable<ReadBlVac3ValuesToPcStruct> BLVac3 =
                new("Call_Values_To_PC.Read_BLVac3_Values_To_PC.S_BLVac3Values_To_PC");
        }

        /// <summary>独立的 GVL_TCSSys 全局 PROGRAM，不挂在 Call_Values_To_PC 底下。</summary>
        public static readonly PlcVariable<TcssysStruct> TcsSys = new("GVL_TCSSys.TCSSys");

        /// <summary>
        /// 对应 bnct_plc/POUs/Com_StripperEleMach.TcPOU——这是个独立 PROGRAM，字段是打平的 REAL/BOOL，
        /// 没有走 DUTs 里的 Com_StripperEleMach_Struct，符号路径直接是 Com_StripperEleMach.字段名。
        /// </summary>
        public static class StripperEleMach
        {
            public static readonly PlcVariable<float> AbPosAxis1 = new("Com_StripperEleMach.fEM_C_AbPos_Axis1");
        }
    }
}
