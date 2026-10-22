// Тёмная тема интерфейса: цвета и шрифт. Шрифт — системный (Segoe UI, есть на любой Windows; кириллица и ₽).
using UnityEngine;

public static class Theme
{
    static Color H(string hex, float a = 1f) => Painter.Hex(hex, a);

    public static readonly Color Bg = H("#0d0b16");
    public static readonly Color Panel = H("#14111f", 0.9f);
    public static readonly Color CardDark = H("#1d1930", 0.96f);
    public static readonly Color Hud = H("#110e1c", 0.88f);
    public static readonly Color Chip = H("#231e3a", 0.95f);
    public static readonly Color Button = H("#2a2448");
    public static readonly Color ButtonHover = H("#3b3366");
    public static readonly Color ButtonPress = H("#4c4286");
    public static readonly Color ButtonDisabled = H("#1c192e");
    public static readonly Color Accent = H("#a78bfa");
    public static readonly Color AccentStrong = H("#8b5cf6");
    public static readonly Color Gold = H("#facc15");
    public static readonly Color Good = H("#4ade80");
    public static readonly Color Bad = H("#f87171");
    public static readonly Color Text = H("#f1eefc");
    public static readonly Color TextDim = H("#a8a3c0");
    public static readonly Color SlotBody = H("#5a1f3a");
    public static readonly Color ReelFace = H("#f5f0e6");
    public static readonly Color Overlay = H("#05040a", 0.72f);

    public const int BodySize = 29;
    public const int BodyMinSize = 22;     // меньше — уже прокрутка (UI.FitText)
    public const int ParagraphGap = 13;    // высота пустой строки между абзацами
    public const int ButtonSize = 27;

    static Font font;

    public static Font Font
    {
        get
        {
            if (font != null) return font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 32);
            }
            catch (System.Exception)
            {
                font = null;
            }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }
}
