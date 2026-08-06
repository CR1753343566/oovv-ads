using System.Collections.Generic;

namespace oovv_ads_control.Views.Controls
{
    public enum KeyKind
    {
        Character,
        Space,
        Tab,
        Backspace,
        Delete,
        Enter,
        CapsLock,
        Shift,
        Ctrl,
        Alt,
        Insert,
        Home,
        End,
        ArrowLeft,
        ArrowRight,
        ArrowUp,
        ArrowDown,
    }

    /// <summary>
    /// 一个物理按键在网格里的位置/尺寸 + 内容。Row/Column 从 0 开始。
    /// Character 键才有意义区分 Normal/Shifted/IsLetter，其它 Kind 的 Normal 就是按钮上显示的文字。
    /// </summary>
    public sealed record KeyDefinition(
        int Row, int Column, int ColumnSpan,
        KeyKind Kind, string Normal, string? Shifted = null, bool IsLetter = false);

    /// <summary>
    /// 参照 Assets/Icons/source/board.png 设计的标准 QWERTY 布局数据。
    /// 参考图分辨率较低、键位多达 50+，这里的具体字符/位置是按标准美式键盘惯例摆的，不是逐像素照抄参考图——
    /// 渲染出来之后如果比例不满意，改这个文件里的数据就行，不用碰 AlphanumericKeypad 的 XAML/code-behind。
    /// 5 行，每行的 ColumnSpan 加起来都正好是 ColumnCount（30），保证每行等宽对齐。
    /// </summary>
    public static class AlphanumericKeyboardLayout
    {
        public const int ColumnCount = 62;

        public static IReadOnlyList<KeyDefinition> Keys { get; } = new List<KeyDefinition>
        {
            // 第 1 行：数字 + 退格
             new(0, 0, 4, KeyKind.Character, "`", "~"),
             new(0, 4, 4, KeyKind.Character, "1", "!"),
             new(0, 8, 4, KeyKind.Character, "2", "@"),
             new(0, 12, 4, KeyKind.Character, "3", "#"),
             new(0, 16, 4, KeyKind.Character, "4", "$"),
             new(0, 20, 4, KeyKind.Character, "5", "%"),
             new(0, 24, 4, KeyKind.Character, "6", "^"),
             new(0, 28, 4, KeyKind.Character, "7", "&"),
             new(0, 32, 4, KeyKind.Character, "8", "*"),
             new(0, 36, 4, KeyKind.Character, "9", "("),
             new(0, 40, 4, KeyKind.Character, "0", ")"),
             new(0, 44, 4, KeyKind.Character, "-", "_"),
             new(0, 48, 4, KeyKind.Character, "=", "+"),
             new(0, 52, 10, KeyKind.Backspace, "⌫"),

            // 第 2 行：Tab + QWERTYUIOP + 括号
            new(1, 0, 6, KeyKind.Tab, "Tab"),
              new(1, 6, 4, KeyKind.Character, "q", null, true),
              new(1, 10, 4, KeyKind.Character, "w", null, true),
              new(1, 14, 4, KeyKind.Character, "e", null, true),
              new(1, 18, 4, KeyKind.Character, "r", null, true),
              new(1, 22, 4, KeyKind.Character, "t", null, true),
              new(1, 26, 4, KeyKind.Character, "y", null, true),
              new(1, 30, 4, KeyKind.Character, "u", null, true),
              new(1, 34, 4, KeyKind.Character, "i", null, true),
              new(1, 38, 4, KeyKind.Character, "o", null, true),
              new(1, 42, 4, KeyKind.Character, "p", null, true),
              new(1, 46, 4, KeyKind.Character, "[", "{"),
              new(1, 50, 4, KeyKind.Character, "]", "}"),
              new(1, 54, 8, KeyKind.Character, "\\", "|"),

            // 第 3 行：Caps + ASDFGHJKL + 分号/引号 + 回车
            new(2, 0, 8, KeyKind.CapsLock, "Caps"),
            new(2, 8, 4, KeyKind.Character, "a", null, true),
            new(2, 12, 4, KeyKind.Character, "s", null, true),
            new(2, 16, 4, KeyKind.Character, "d", null, true),
            new(2, 20, 4, KeyKind.Character, "f", null, true),
            new(2, 24, 4, KeyKind.Character, "g", null, true),
            new(2, 28, 4, KeyKind.Character, "h", null, true),
            new(2, 32, 4, KeyKind.Character, "j", null, true),
            new(2, 36, 4, KeyKind.Character, "k", null, true),
            new(2, 40, 4, KeyKind.Character, "l", null, true),
            new(2, 44, 4, KeyKind.Character, ";", ":"),
            new(2, 48, 4, KeyKind.Character, "'", "\""),
            new(2, 52, 10, KeyKind.Enter, "↵"),

            // 第 4 行：Shift + ZXCVBNM + ,./ + Home/Up/End
            // Home/方向键/End 都和字母键同宽（span=2），Shift 相应缩到 4 才能让整行凑够 30 列
            new(3, 0, 10, KeyKind.Shift, "Shift"),
            new(3, 10, 4, KeyKind.Character, "z", null, true),
            new(3, 14, 4, KeyKind.Character, "x", null, true),
            new(3, 18, 4, KeyKind.Character, "c", null, true),
            new(3, 22, 4, KeyKind.Character, "v", null, true),
            new(3, 26, 4, KeyKind.Character, "b", null, true),
            new(3, 30, 4, KeyKind.Character, "n", null, true),
            new(3, 34, 4, KeyKind.Character, "m", null, true),
            new(3, 38, 4, KeyKind.Character, ",", "<"),
            new(3, 42, 4, KeyKind.Character, ".", ">"),
            new(3, 46, 4, KeyKind.Character, "/", "?"),
            new(3, 50, 4, KeyKind.Home, "Home"),
            new(3, 54, 4, KeyKind.ArrowUp, "↑"),
            new(3, 58, 4, KeyKind.End, "End"),

            // 第 5 行：Ctrl/Alt + 空格 + Ins/Del + 左/下/右
            new(4, 0, 7, KeyKind.Ctrl, "Ctrl"),
            new(4, 12, 4, KeyKind.Alt, "Alt"),
            new(4, 16, 20, KeyKind.Space, "Space"),
            new(4, 36, 4, KeyKind.Insert, "Ins"),
            new(4, 40, 4, KeyKind.Delete, "Del"),
            new(4, 50, 4, KeyKind.ArrowLeft, "←"),
            new(4, 54, 4, KeyKind.ArrowDown, "↓"),
            new(4, 58, 4, KeyKind.ArrowRight, "→"),
        };
    }
}
