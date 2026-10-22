// Виджеты: кнопка выбора, карточка команды (матчи), выбор героя и имя, поле мини-игр подработок,
// прилавок лавки, след ракеты.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ===== КНОПКА ВЫБОРА (панель справа) =====
public class ChoiceButton
{
    public Button button;
    Text label;
    Text number;
    Action onClick;

    public static ChoiceButton Create(Transform parent, string text, Action onClick, bool disabled, string iconItem, bool danger, int index)
    {
        var cb = new ChoiceButton { onClick = onClick };
        var rt = UI.Rect(parent, "Choice", new Vector2(0, 1), new Vector2(1, 1));
        cb.button = UI.MakeButton(rt.gameObject, danger ? Painter.Hex("#3a1f2c") : Theme.Button, () => cb.Click(), 16);
        var row = UI.Row(rt.gameObject, 16, new RectOffset(18, 22, 14, 14));
        UI.Layout(rt.gameObject, 62);

        // Номер — горячая клавиша
        var numRt = UI.Rect(rt, "Key", Vector2.zero, Vector2.one);
        UI.Layout(numRt.gameObject, -1, 34, 34);
        var numBg = numRt.gameObject.AddComponent<Image>();
        numBg.sprite = GameArt.Rounded(10);
        numBg.type = Image.Type.Sliced;
        numBg.color = Painter.A(Theme.Accent, 0.22f);
        numBg.raycastTarget = false;
        cb.number = UI.Label(numRt, index < 9 ? (index + 1).ToString() : "", 20, Theme.Accent, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        cb.number.alignByGeometry = true;

        if (iconItem != null)
        {
            var icRt = UI.Rect(rt, "Icon", Vector2.zero, Vector2.one);
            UI.Layout(icRt.gameObject, -1, 52, 52);
            var ic = icRt.gameObject.AddComponent<Image>();
            ic.sprite = GameArt.Item(iconItem);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
        }

        var tRt = UI.Rect(rt, "Text", Vector2.zero, Vector2.one);
        cb.label = UI.MakeText(tRt.gameObject, Theme.ButtonSize, danger ? Painter.Hex("#fca5a5") : Theme.Text, TextAnchor.MiddleLeft);
        tRt.gameObject.AddComponent<TextLift>();
        UI.Layout(tRt.gameObject, -1, -1, -1, 1);
        cb.SetLabel(text);
        cb.Disabled = disabled;
        return cb;
    }

    public void SetLabel(string text) => label.text = Txt.Clean(text);

    public bool Disabled
    {
        get => !button.interactable;
        set
        {
            button.interactable = !value;
            label.color = value ? Painter.A(label.color, 0.45f) : Painter.A(label.color, 1f);
            number.color = value ? Painter.A(Theme.Accent, 0.3f) : Theme.Accent;
        }
    }

    public void Click()
    {
        if (Disabled) return;
        Music.Click();
        onClick?.Invoke();
    }
}

// Подпись кнопки — выше на долю размера шрифта: строка Segoe UI оставляет место под «у», «р», «д»
// и сидит ниже середины, а номер рядом выровнен по самим цифрам (alignByGeometry)
public class TextLift : BaseMeshEffect
{
    const float Share = 0.2f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || !(graphic is Text text)) return;
        int size = text.resizeTextForBestFit ? text.cachedTextGenerator.fontSizeUsedForBestFit : text.fontSize;
        float dy = size * Share;
        var v = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            v.position.y += dy;
            vh.SetUIVertex(v, i);
        }
    }
}

// ===== КАРТОЧКА КОМАНДЫ (ставки на матчи) =====
public class BoardButton
{
    Button button;
    Image bg, outline;
    RectTransform rt;

    static readonly Color[] Emblems =
    {
        Painter.Hex("#4a7ab0"), Painter.Hex("#d6264f"), Painter.Hex("#8a6a4c"), Painter.Hex("#2a9ab0"),
        Painter.Hex("#e0b83a"), Painter.Hex("#2a2a33"), Painter.Hex("#7ad0f0"), Painter.Hex("#f08a24")
    };

    public static BoardButton Create(Transform parent, string team, string odds, int side, Vector2 min, Vector2 max, Action onClick)
    {
        var bb = new BoardButton();
        bb.rt = UI.Rect(parent, "Team" + side, min, max);
        bb.outline = bb.rt.gameObject.AddComponent<Image>();
        bb.outline.sprite = GameArt.Rounded(22);
        bb.outline.type = Image.Type.Sliced;
        bb.outline.color = Painter.A(Theme.Gold, 0f);
        var inner = UI.Stretch(bb.rt, "Inner", 5);
        bb.button = UI.MakeButton(inner.gameObject, Theme.Button, () => { Music.Click(); onClick(); }, 18);
        bb.bg = inner.GetComponent<Image>();

        int idx = Mathf.Abs(team.GetHashCode()) % Emblems.Length;
        var em = UI.Rect(inner, "Emblem", new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f));
        em.sizeDelta = new Vector2(120, 120);
        var emImg = em.gameObject.AddComponent<Image>();
        emImg.sprite = GameArt.Dot();
        emImg.color = Emblems[idx];
        emImg.raycastTarget = false;
        string clean = Txt.Clean(team).Trim();
        UI.Label(em, clean.Length > 0 ? clean.Substring(0, 1) : "?", 64, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one).fontStyle = FontStyle.Bold;
        UI.Label(inner, clean, 28, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.36f)).fontStyle = FontStyle.Bold;
        UI.Label(inner, "×" + odds, 30, Theme.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.03f), new Vector2(1, 0.18f));
        return bb;
    }

    public bool Interactable { set => button.interactable = value; }

    public void SetPicked(bool picked) => outline.color = Painter.A(Theme.Gold, picked ? 1f : 0f);

    public void SetResult(bool won)
    {
        button.interactable = false;
        var colors = button.colors;
        colors.disabledColor = won ? Painter.Hex("#1f6a42") : Painter.Hex("#5a1f2a");
        button.colors = colors;
    }

    public void Pulse() => UI.Bump(rt);
}

// ===== ВЫБОР ГЕРОЯ И ИМЯ =====
public class NameWidget
{
    public Action onBoy, onGirl, onSubmit, onPrevLook, onNextLook;
    public Action<string> onChange;
    Button boy, girl, submit, prevLook, nextLook;
    Text lookLabel;
    InputField field;
    Text placeholder;

    public static NameWidget Create(Transform parent, string boyText, string girlText, string submitText)
    {
        var w = new NameWidget();
        var row = UI.Rect(parent, "Gender", new Vector2(0, 1), new Vector2(1, 1));
        var h = UI.Row(row.gameObject, 14);
        h.childForceExpandWidth = true;
        UI.Layout(row.gameObject, 70, -1, 70);
        w.boy = GenderButton(row, boyText, () => w.onBoy?.Invoke());
        w.girl = GenderButton(row, girlText, () => w.onGirl?.Invoke());

        // Внешность: ‹ Внешность 1 из 3 ›
        var looks = UI.Rect(parent, "Look", new Vector2(0, 1), new Vector2(1, 1));
        UI.Layout(looks.gameObject, 60, -1, 60);
        w.prevLook = LookArrow(looks, "‹", 0, () => w.onPrevLook?.Invoke());
        w.nextLook = LookArrow(looks, "›", 1, () => w.onNextLook?.Invoke());
        w.lookLabel = UI.Label(looks, "", Theme.ButtonSize, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 1));
        w.lookLabel.rectTransform.offsetMin = new Vector2(80, 0);
        w.lookLabel.rectTransform.offsetMax = new Vector2(-80, 0);

        w.field = UI.MakeInput(parent, "", B.NAME_MAX_LENGTH, InputField.ContentType.Standard);
        w.placeholder = (Text)w.field.placeholder;
        w.field.onValueChanged.AddListener(v => w.onChange?.Invoke(v));
        w.field.onEndEdit.AddListener(_ =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) w.onSubmit?.Invoke();
        });

        var sRt = UI.Rect(parent, "Submit", new Vector2(0, 1), new Vector2(1, 1));
        UI.Layout(sRt.gameObject, 64, -1, 64);
        w.submit = UI.MakeButton(sRt.gameObject, Theme.AccentStrong, () => { Music.Click(); w.onSubmit?.Invoke(); });
        UI.Label(sRt, submitText, Theme.ButtonSize, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        return w;
    }

    static Button GenderButton(Transform parent, string text, Action click)
    {
        var rt = UI.Rect(parent, "G", Vector2.zero, Vector2.one);
        UI.Layout(rt.gameObject, 70, -1, 70, 1);
        var b = UI.MakeButton(rt.gameObject, Theme.Button, () => { Music.Click(); click(); });
        UI.Label(rt, text, Theme.ButtonSize, Theme.Text, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        return b;
    }

    static Button LookArrow(Transform parent, string text, float side, Action click)
    {
        var rt = UI.Rect(parent, "Arrow", new Vector2(side, 0), new Vector2(side, 1));
        rt.pivot = new Vector2(side, 0.5f);
        rt.sizeDelta = new Vector2(70, 0);
        var b = UI.MakeButton(rt.gameObject, Theme.Button, () => { Music.Click(); click(); });
        UI.Label(rt, text, 44, Theme.Text, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        return b;
    }

    // Выбор внешности: подпись и доступность стрелок (до выбора пола — недоступно)
    public void SetLook(string text, bool on)
    {
        lookLabel.text = Txt.Clean(text);
        lookLabel.color = on ? Theme.Text : Theme.TextDim;
        prevLook.interactable = nextLook.interactable = on;
    }

    // Подсветка выбранного героя (null — никого)
    public void SetSelected(string gender)
    {
        Tint(boy, gender == "m");
        Tint(girl, gender == "f");
    }

    static void Tint(Button b, bool on)
    {
        var c = b.colors;
        c.normalColor = on ? Theme.AccentStrong : Theme.Button;
        c.highlightedColor = Painter.Shade(c.normalColor, 0.18f);
        c.selectedColor = c.normalColor;
        b.colors = c;
    }

    public void SetEnabled(bool on, string hint)
    {
        field.interactable = on;
        submit.interactable = on;
        placeholder.text = Txt.Clean(hint);
    }

    public string Value
    {
        get => field.text;
        set => field.text = value;
    }

    public void Focus()
    {
        field.Select();
        field.ActivateInputField();
    }
}

// ===== ПОЛЕ МИНИ-ИГРЫ ПОДРАБОТКИ (слева, вместо персонажей) =====
public class MiniView
{
    RectTransform board, center, timerFill, captionRt;
    Text stats, caption, feedback;
    RawImage passer;
    RectTransform passerRt;
    Actor3D passer3d;
    Image prop;
    Text propText;
    RectTransform orderRow;

    public static MiniView Create()
    {
        var v = new MiniView();
        v.board = UI.ShowBoard();
        var card = UI.Panel(v.board, "Card", new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Painter.A(Theme.CardDark, 0.9f), 26);
        v.board = card;
        v.stats = UI.Label(card, "", 26, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.98f));
        v.center = UI.Rect(card, "Center", new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.86f));
        v.caption = UI.Label(card, "", 28, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.3f));
        v.captionRt = v.caption.rectTransform;
        var track = UI.Panel(card, "Timer", new Vector2(0.1f, 0.165f), new Vector2(0.9f, 0.185f), Painter.Hex("#0d0b16"), 8);
        v.timerFill = UI.Panel(track, "Fill", Vector2.zero, Vector2.one, Theme.Accent, 8);
        v.feedback = UI.Label(card, "", 30, Theme.Good, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.15f));
        v.feedback.fontStyle = FontStyle.Bold;
        return v;
    }

    public void Stats(string text) => stats.text = Txt.Clean(text);
    public void Caption(string text) => caption.text = Txt.Clean(text);

    public void Feedback(string text, bool good)
    {
        feedback.text = Txt.Clean(text);
        feedback.color = good ? Theme.Good : Theme.Bad;
        if (text != "") UI.Bump(feedback.rectTransform);
    }

    // Полоска времени: от полной до пустой за seconds
    public void Timer(float seconds)
    {
        var fill = timerFill;
        Run.Tween(seconds, k =>
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(1 - k, 1);
            fill.GetComponent<Image>().color = Color.Lerp(Theme.Accent, Theme.Bad, Mathf.Clamp01((k - 0.6f) / 0.4f));
        });
    }

    // Прохожий идёт через поле, пока его можно окликнуть
    // Прохожие рисуются заранее, пока идёт первый
    public static void PreparePassers()
    {
        foreach (var p in Data.C.FLYER_PASSERS)
        {
            var def = Passers.Def(p.emoji);
            if (Actor3D.Available && def.model != null) Actor3D.Preload(def.model);
            else CharacterArt.Get("passer" + p.emoji, def, p.ok ? "normal" : "angry", default);
        }
    }

    public void Passer(FlyerPasser p, float seconds)
    {
        if (passer == null)
        {
            passerRt = UI.Rect(center, "Passer", new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            passerRt.pivot = new Vector2(0.5f, 0);
            passerRt.sizeDelta = new Vector2(300, 480);
            passer = passerRt.gameObject.AddComponent<RawImage>();
            passer.raycastTarget = false;
        }
        var def = Passers.Def(p.emoji);
        string mood = p.ok ? "normal" : "angry";
        float w = center.rect.width;
        var rt = passerRt;
        var img = passer;
        bool fast = !p.ok;

        // 3D: прохожий в полный рост, боком по ходу движения — видно велосипед, самокат, коляску, наушники
        if (Actor3D.Available && def.model != null)
        {
            if (passer3d == null)
            {
                passer3d = Actor3D.Create(Actor3D.Framing.Full, 1f);
                passerRt.gameObject.AddComponent<Actor3DLink>().actor = passer3d;
                passerRt.sizeDelta = new Vector2(520, 520);
            }
            if (passer3d.SetModel(def.model))
            {
                var actor = passer3d;
                // едет — покачивается плавно; мама с коляской идёт пешком
                bool wheels = def.props != null && (def.props.Contains("bike") || def.props.Contains("scooter"));
                bool run = def.props != null && def.props.Contains("run");
                actor.Turn(-62f);     // идёт слева направо — лицом вправо, чуть к зрителю
                img.texture = actor.Texture;
                img.enabled = true;
                rt.localEulerAngles = Vector3.zero;
                Run.Tween(seconds, k =>
                {
                    if (rt == null) return;
                    rt.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.42f, w * 0.42f, k), 0);
                    actor.Walk(k * seconds, wheels, fast || run, run);
                });
                return;
            }
        }

        passer.texture = CharacterArt.Get("passer" + p.emoji, def, mood, default);
        passer.enabled = passer.texture != null;
        Run.Tween(seconds, k =>
        {
            if (rt == null) return;
            if (img.texture == null) img.texture = CharacterArt.Get("passer" + p.emoji, def, mood, default);
            img.enabled = img.texture != null;
            float x = Mathf.Lerp(-w * 0.42f, w * 0.42f, k);
            float bob = Mathf.Abs(Mathf.Sin(k * seconds * (fast ? 14f : 8f))) * (fast ? 14f : 8f);
            rt.anchoredPosition = new Vector2(x, bob);
            rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(k * seconds * (fast ? 14f : 8f)) * (fast ? 3f : 1.5f));
        });
    }

    public void PasserReact(bool ok)
    {
        if (passer == null) return;
        UI.Bump(passerRt);
    }

    // Посылка с наклейкой
    public void Parcel(string label)
    {
        EnsureProp(GameArt.Parcel());
        propText.text = Txt.Clean(label);
        UI.Bump(prop.rectTransform);
    }

    // Чек покупателя
    public void Receipt(string text, int customer)
    {
        EnsureProp(GameArt.Receipt());
        prop.rectTransform.sizeDelta = new Vector2(470, 470);
        propText.text = Txt.Clean(text);
        propText.color = Painter.Hex("#2a2333");
        propText.fontSize = 26;
        propText.alignByGeometry = true;
        // текст — на бумаге, между пунктирами
        propText.rectTransform.anchorMin = Vector2.zero;
        propText.rectTransform.anchorMax = Vector2.one;
        propText.rectTransform.offsetMin = new Vector2(48, 66);
        propText.rectTransform.offsetMax = new Vector2(-48, -66);
        UI.Bump(prop.rectTransform);
    }

    void EnsureProp(Sprite sprite)
    {
        if (prop != null) return;
        var rt = UI.Rect(center, "Prop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = new Vector2(360, 360);
        prop = rt.gameObject.AddComponent<Image>();
        prop.sprite = sprite;
        prop.preserveAspect = true;
        prop.raycastTarget = false;
        propText = UI.Label(rt, "", 30, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0, -0.18f), new Vector2(1, -0.02f));
        propText.fontStyle = FontStyle.Bold;
    }

    // Лавка: прилавок с товарами, заказ — сверху
    public GroceryGrid Grocery(List<Product> products, Action<Product> onPick)
    {
        center.anchorMin = new Vector2(0.05f, 0.66f);
        captionRt.anchorMin = new Vector2(0.05f, 0.56f);
        captionRt.anchorMax = new Vector2(0.95f, 0.65f);
        var gridRt = UI.Rect(board, "Grid", new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.55f));
        return GroceryGrid.Create(gridRt, products, onPick);
    }

    // Заказ или собранная корзина: значки товаров и «?» на месте недостающих
    public void Order(List<Product> items, int size, bool hidden)
    {
        if (orderRow != null) UnityEngine.Object.Destroy(orderRow.gameObject);
        orderRow = UI.Rect(center, "Order", Vector2.zero, Vector2.one);
        var row = UI.Row(orderRow.gameObject, 18);
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = false;
        row.childControlHeight = false;
        for (int i = 0; i < size; i++)
        {
            var slot = UI.Rect(orderRow, "Slot", Vector2.zero, Vector2.zero);
            slot.sizeDelta = new Vector2(110, 110);
            var bg = slot.gameObject.AddComponent<Image>();
            bg.sprite = GameArt.Rounded(18);
            bg.type = Image.Type.Sliced;
            bg.color = Painter.Hex("#2a2448");
            bg.raycastTarget = false;
            if (i < items.Count)
            {
                var ic = UI.Image(slot, "P", GameArt.Product(items[i]), new Vector2(10, 10), new Vector2(-10, -10));
                ic.preserveAspect = true;
            }
            else if (hidden) UI.Label(slot, "?", 54, Theme.TextDim, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one).fontStyle = FontStyle.Bold;
        }
    }
}

// Прилавок овощной лавки: товары-кнопки
public class GroceryGrid
{
    readonly Dictionary<Product, Button> buttons = new Dictionary<Product, Button>();

    public static GroceryGrid Create(RectTransform parent, List<Product> products, Action<Product> onPick)
    {
        var g = new GroceryGrid();
        var grid = parent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120, 120);
        grid.spacing = new Vector2(16, 16);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        foreach (var p in products)
        {
            var prod = p;
            var rt = UI.Rect(parent, p.name, Vector2.zero, Vector2.zero);
            var b = UI.MakeButton(rt.gameObject, Painter.Hex("#f2e6c8"), () => { Music.Click(); onPick(prod); }, 18);
            var c = b.colors;
            c.disabledColor = Painter.Hex("#8a826e");
            b.colors = c;
            var ic = UI.Image(rt, "Icon", GameArt.Product(p), new Vector2(12, 12), new Vector2(-12, -12));
            ic.preserveAspect = true;
            g.buttons[p] = b;
        }
        return g;
    }

    public void SetEnabled(bool on)
    {
        foreach (var b in buttons.Values)
        {
            b.interactable = on;
            b.transform.localScale = Vector3.one;
        }
    }

    public void MarkPicked(Product p)
    {
        if (!buttons.TryGetValue(p, out var b)) return;
        b.interactable = false;
        b.transform.localScale = Vector3.one * 0.88f;
    }
}

// След ракеты: точки по пути
public class TrailLine
{
    RectTransform parent;
    Vector2 last = new Vector2(-999, -999);

    public static TrailLine Create(RectTransform parent) => new TrailLine { parent = parent };

    public void Add(Vector2 pos)
    {
        if (Vector2.Distance(pos, last) < 14) return;
        last = pos;
        var rt = UI.Rect(parent, "Dot", Vector2.zero, Vector2.zero);
        rt.sizeDelta = new Vector2(12, 12);
        rt.anchoredPosition = pos;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = GameArt.Dot();
        img.color = Painter.A(Theme.Gold, 0.7f);
        img.raycastTarget = false;
        rt.SetAsFirstSibling();
    }
}
