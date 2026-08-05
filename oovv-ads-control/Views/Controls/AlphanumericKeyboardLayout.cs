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
        public const int ColumnCount = 30;

        public static IReadOnlyList<KeyDefinition> Keys { get; } = new List<KeyDefinition>
        {
            // 第 1 行：数字 + 退格
            new(0, 0, 2, KeyKind.Character, "1", "!"),
            new(0, 2, 2, KeyKind.Character, "2", "@"),
            new(0, 4, 2, KeyKind.Character, "3", "#"),
            new(0, 6, 2, KeyKind.Character, "4", "$"),
            new(0, 8, 2, KeyKind.Character, "5", "%"),
            new(0, 10, 2, KeyKind.Character, "6", "^"),
            new(0, 12, 2, KeyKind.Character, "7", "&"),
            new(0, 14, 2, KeyKind.Character, "8", "*"),
            new(0, 16, 2, KeyKind.Character, "9", "("),
            new(0, 18, 2, KeyKind.Character, "0", ")"),
            new(0, 20, 2, KeyKind.Character, "-", "_"),
            new(0, 22, 2, KeyKind.Character, "=", "+"),
            new(0, 24, 6, KeyKind.Backspace, "⌫"),

            // 第 2 行：Tab + QWERTYUIOP + 括号
            new(1, 0, 3, KeyKind.Tab, "Tab"),
            new(1, 3, 2, KeyKind.Character, "q", null, true),
            new(1, 5, 2, KeyKind.Character, "w", null, true),
            new(1, 7, 2, KeyKind.Character, "e", null, true),
            new(1, 9, 2, KeyKind.Character, "r", null, true),
            new(1, 11, 2, KeyKind.Character, "t", null, true),
            new(1, 13, 2, KeyKind.Character, "y", null, true),
            new(1, 15, 2, KeyKind.Character, "u", null, true),
            new(1, 17, 2, KeyKind.Character, "i", null, true),
            new(1, 19, 2, KeyKind.Character, "o", null, true),
            new(1, 21, 2, KeyKind.Character, "p", null, true),
            new(1, 23, 2, KeyKind.Character, "[", "{"),
            new(1, 25, 2, KeyKind.Character, "]", "}"),
            new(1, 27, 3, KeyKind.Character, "\\", "|"),

            // 第 3 行：Caps + ASDFGHJKL + 分号/引号 + 回车
            new(2, 0, 4, KeyKind.CapsLock, "Caps"),
            new(2, 4, 2, KeyKind.Character, "a", null, true),
            new(2, 6, 2, KeyKind.Character, "s", null, true),
            new(2, 8, 2, KeyKind.Character, "d", null, true),
            new(2, 10, 2, KeyKind.Character, "f", null, true),
            new(2, 12, 2, KeyKind.Character, "g", null, true),
            new(2, 14, 2, KeyKind.Character, "h", null, true),
            new(2, 16, 2, KeyKind.Character, "j", null, true),
            new(2, 18, 2, KeyKind.Character, "k", null, true),
            new(2, 20, 2, KeyKind.Character, "l", null, true),
            new(2, 22, 2, KeyKind.Character, ";", ":"),
            new(2, 24, 2, KeyKind.Character, "'", "\""),
            new(2, 26, 4, KeyKind.Enter, "↵"),

            // 第 4 行：Shift + ZXCVBNM + ,./ + Home/Up/End
            new(3, 0, 5, KeyKind.Shift, "Shift"),
            new(3, 5, 2, KeyKind.Character, "z", null, true),
            new(3, 7, 2, KeyKind.Character, "x", null, true),
            new(3, 9, 2, KeyKind.Character, "c", null, true),
            new(3, 11, 2, KeyKind.Character, "v", null, true),
            new(3, 13, 2, KeyKind.Character, "b", null, true),
            new(3, 15, 2, KeyKind.Character, "n", null, true),
            new(3, 17, 2, KeyKind.Character, "m", null, true),
            new(3, 19, 2, KeyKind.Character, ",", "<"),
            new(3, 21, 2, KeyKind.Character, ".", ">"),
            new(3, 23, 2, KeyKind.Character, "/", "?"),
            new(3, 25, 2, KeyKind.Home, "Home"),
            new(3, 27, 1, KeyKind.ArrowUp, "↑"),
            new(3, 28, 2, KeyKind.End, "End"),

            // 第 5 行：Ctrl/Alt + 空格 + Ins/Del + 左/下/右
            new(4, 0, 3, KeyKind.Ctrl, "Ctrl"),
            new(4, 3, 3, KeyKind.Alt, "Alt"),
            new(4, 6, 12, KeyKind.Space, "Space"),
            new(4, 18, 3, KeyKind.Insert, "Ins"),
            new(4, 21, 3, KeyKind.Delete, "Del"),
            new(4, 24, 2, KeyKind.ArrowLeft, "←"),
            new(4, 26, 2, KeyKind.ArrowDown, "↓"),
            new(4, 28, 2, KeyKind.ArrowRight, "→"),
        };
    }
}
