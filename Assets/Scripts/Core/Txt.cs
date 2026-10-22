// Текст для экрана: эмодзи из браузерной версии шрифт Unity не рисует — убираем их
// (несколько заменяем на символы или цветные значки), числа и секунды — по-русски.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Txt
{
    static readonly CultureInfo Ru = new CultureInfo("ru-RU");

    // 1800 мс → «1,8»
    public static string Sec(int ms) => (ms / 1000f).ToString("0.##", Ru);

    public static string F2(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    static readonly (string from, string to)[] Swaps =
    {
        ("✅", "✓"), ("❌", "✗"), ("✔️", "✓"), ("✔", "✓"),
        ("🟥", "<color=#e2574c>■</color>"), ("🟦", "<color=#4d8fe0>■</color>"), ("🟩", "<color=#4cbf6b>■</color>"),
        ("❓", "?"),
    };

    static readonly Regex LeadSpaces = new Regex(@"(^|\n)[ \t]+");
    static readonly Regex DoubleSpaces = new Regex(@"(?<=\S) {2,}(?=\S)");

    public static string Clean(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        foreach (var (from, to) in Swaps) text = text.Replace(from, to);

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsHighSurrogate(c)) { i++; continue; }  // эмодзи вне базовой плоскости
            if (char.IsLowSurrogate(c)) continue;
            if (IsSymbolEmoji(c)) continue;
            sb.Append(c);
        }
        var result = LeadSpaces.Replace(sb.ToString(), "$1");
        // В строках с пометкой «(не хватает)» два пробела перед ней оставляем — это отступ
        return result.Contains("<color") ? result : DoubleSpaces.Replace(result, " ");
    }

    static bool IsSymbolEmoji(char c)
    {
        if (c == '✓' || c == '✗' || c == '■' || c == '×' || c == '·' || c == '…') return false;
        return c == '️' || c == '‍' || c == '⃣'
            || (c >= '⌀' && c <= '⏿')
            || (c >= '☀' && c <= '➿')
            || (c >= '⬀' && c <= '⯿');
    }

    // Имя героя: одно слово, без разметки
    public static string CleanName(string value)
    {
        var sb = new StringBuilder();
        foreach (var c in value)
            if (!char.IsWhiteSpace(c) && c != '<' && c != '>' && c != '{' && c != '}' && c != '|') sb.Append(c);
        var s = sb.ToString();
        return s.Length > B.NAME_MAX_LENGTH ? s.Substring(0, B.NAME_MAX_LENGTH) : s;
    }
}
