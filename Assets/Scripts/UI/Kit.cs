// Конструкторы элементов интерфейса (uGUI из кода): прямоугольники, картинки, надписи, панели, кнопки, поля ввода.
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static partial class UI
{
    public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    public static RectTransform Stretch(Transform parent, string name, float inset = 0) =>
        Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));

    // Картинка на всю площадь родителя с отступами (offsetMin — слева/снизу, offsetMax — справа/сверху)
    public static Image Image(Transform parent, string name, Sprite sprite, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = Rect(parent, name, Vector2.zero, Vector2.one, offsetMin, offsetMax);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // Панель со скруглёнными углами
    public static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, int radius = 22)
    {
        var rt = Rect(parent, name, anchorMin, anchorMax);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = GameArt.Rounded(radius);
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = Rect(parent, "Label", anchorMin, anchorMax);
        var t = MakeText(rt.gameObject, size, color, anchor);
        t.text = Txt.Clean(text);
        return t;
    }

    public static Text MakeText(GameObject go, int size, Color color, TextAnchor anchor)
    {
        var t = go.AddComponent<Text>();
        t.font = Theme.Font;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.08f;
        t.raycastTarget = false;
        return t;
    }

    // Надпись для вертикальной раскладки (высота — по тексту)
    public static Text FlowText(Transform parent, string name, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var rt = Rect(parent, name, new Vector2(0, 1), new Vector2(1, 1));
        return MakeText(rt.gameObject, size, color, anchor);
    }

    public static VerticalLayoutGroup Column(GameObject go, float spacing, RectOffset padding = null)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = padding ?? new RectOffset();
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return v;
    }

    public static HorizontalLayoutGroup Row(GameObject go, float spacing, RectOffset padding = null)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.padding = padding ?? new RectOffset();
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.childAlignment = TextAnchor.MiddleLeft;
        return h;
    }

    public static LayoutElement Layout(GameObject go, float minH = -1, float prefW = -1, float prefH = -1, float flexW = -1)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight = minH;
        le.preferredWidth = prefW;
        le.preferredHeight = prefH;
        le.flexibleWidth = flexW;
        return le;
    }

    // Кнопка: фон-скругление, цвета по состояниям
    public static Button MakeButton(GameObject go, Color normal, UnityAction onClick, int radius = 16)
    {
        var img = go.AddComponent<Image>();
        img.sprite = GameArt.Rounded(radius);
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.color = Color.white;
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Painter.Shade(normal, 0.18f);
        colors.selectedColor = normal;
        colors.pressedColor = Painter.Shade(normal, 0.32f);
        colors.disabledColor = Theme.ButtonDisabled;
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.08f;
        b.colors = colors;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        if (onClick != null) b.onClick.AddListener(onClick);
        return b;
    }

    // Простая кнопка с надписью по центру (в окнах и на игровом поле)
    public static Button TextButton(Transform parent, string label, Color color, UnityAction onClick, Vector2 anchorMin, Vector2 anchorMax, int size = 28)
    {
        var rt = Rect(parent, "Button", anchorMin, anchorMax);
        var b = MakeButton(rt.gameObject, color, onClick);
        var t = Label(rt, label, size, Theme.Text, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        t.rectTransform.offsetMin = new Vector2(12, 4);
        t.rectTransform.offsetMax = new Vector2(-12, -4);
        return b;
    }

    // Однострочное поле ввода
    public static InputField MakeInput(Transform parent, string placeholder, int limit, InputField.ContentType type)
    {
        var rt = Rect(parent, "Input", new Vector2(0, 1), new Vector2(1, 1));
        Layout(rt.gameObject, 64, -1, 64);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = GameArt.Rounded(14);
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.color = Painter.Hex("#0f0c1a");
        var field = rt.gameObject.AddComponent<InputField>();
        field.targetGraphic = img;

        var textRt = Rect(rt, "Text", Vector2.zero, Vector2.one, new Vector2(20, 6), new Vector2(-20, -6));
        var text = MakeText(textRt.gameObject, Theme.BodySize, Theme.Text, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var phRt = Rect(rt, "Placeholder", Vector2.zero, Vector2.one, new Vector2(20, 6), new Vector2(-20, -6));
        var ph = MakeText(phRt.gameObject, Theme.BodySize, Theme.TextDim, TextAnchor.MiddleLeft);
        ph.fontStyle = FontStyle.Italic;
        ph.text = placeholder;

        field.textComponent = text;
        field.placeholder = ph;
        field.characterLimit = limit;
        field.contentType = type;
        field.lineType = InputField.LineType.SingleLine;
        field.caretColor = Theme.Accent;
        field.selectionColor = Painter.A(Theme.Accent, 0.4f);
        return field;
    }
}
