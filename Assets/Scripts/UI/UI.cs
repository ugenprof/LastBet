// Интерфейс: фон, сцена с персонажами / игровое поле (слева), панель с текстом и выборами (справа), HUD сверху,
// окна (рюкзак, «начать заново», «хочу отыграться», сообщения), эффекты. Здесь нет правил игры — только отображение.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class UI
{
    static RectTransform root, world;
    static RawImage bgA, bgB;
    static string bgName;
    static UnityEngine.UI.Image moodOverlay, brightOverlay;
    static float brightness = 0.85f;
    static RectTransform stageArea, boardArea;
    static StageView stage;

    // Панель текста
    static ScrollRect scroll;
    static RectTransform content, widgetBox, choicesBox, chapterBox;
    static CanvasGroup contentGroup;
    static Text headlineText, bodyText, chapterNum, chapterTitle, chapterEpi;
    static UnityEngine.UI.Image itemBanner;
    static readonly List<ChoiceButton> buttons = new List<ChoiceButton>();
    static bool scrollToTop, fitPending;

    // HUD
    static Text hudDay, hudMoney, hudAddiction, hudSatiety, hudBag;
    static RectTransform addictionFill, satietyFill, bagIcons, hudRestart;
    static UnityEngine.UI.Image musicIcon;

    // Окна
    static RectTransform modalLayer, fxLayer;
    static Action modalClose;
    public static bool ModalOpen => modalLayer != null && modalLayer.gameObject.activeSelf;

    // ===== ПОСТРОЕНИЕ =====
    public static void Build()
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root = (RectTransform)canvasGo.transform;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Фон: две картинки для плавной смены
        var bgBack = Stretch(root, "BgBase").gameObject.AddComponent<UnityEngine.UI.Image>();
        bgBack.color = Theme.Bg;
        bgA = Stretch(root, "BgA").gameObject.AddComponent<RawImage>();
        bgB = Stretch(root, "BgB").gameObject.AddComponent<RawImage>();
        bgA.raycastTarget = bgB.raycastTarget = false;
        bgB.color = new Color(1, 1, 1, 0);
        brightOverlay = Stretch(root, "Brightness").gameObject.AddComponent<UnityEngine.UI.Image>();
        brightOverlay.raycastTarget = false;
        moodOverlay = Stretch(root, "Mood").gameObject.AddComponent<UnityEngine.UI.Image>();
        moodOverlay.color = new Color(0, 0, 0, 0);
        moodOverlay.raycastTarget = false;
        ApplyBrightness();

        world = Stretch(root, "World");
        stageArea = Rect(world, "Stage", new Vector2(0, 0), new Vector2(0.56f, 0.92f));
        stage = new StageView(stageArea);
        boardArea = Rect(world, "Board", new Vector2(0.015f, 0.04f), new Vector2(0.55f, 0.9f));
        boardArea.gameObject.SetActive(false);

        BuildPanel();
        BuildHud();

        modalLayer = Stretch(root, "Modal");
        var dim = modalLayer.gameObject.AddComponent<UnityEngine.UI.Image>();
        dim.color = Theme.Overlay;
        var dimBtn = modalLayer.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(CloseModal);
        modalLayer.gameObject.SetActive(false);

        fxLayer = Stretch(root, "Fx");
        var view = canvasGo.AddComponent<GameView>();
    }

    static void BuildPanel()
    {
        var panel = Panel(world, "Panel", new Vector2(0.565f, 0.03f), new Vector2(0.985f, 0.905f), Theme.Panel, 28);
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var scrollRt = Stretch(panel, "Scroll");
        scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45;

        var viewport = Rect(scrollRt, "Viewport", Vector2.zero, Vector2.one, new Vector2(34, 24), new Vector2(-46, -24));
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImg = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        scroll.viewport = viewport;

        content = Rect(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1));
        content.pivot = new Vector2(0.5f, 1);
        Column(content.gameObject, 22, new RectOffset(0, 0, 10, 24));
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentGroup = content.gameObject.AddComponent<CanvasGroup>();
        scroll.content = content;

        // Полоса прокрутки
        var bar = Rect(scrollRt, "Scrollbar", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-26, 30), new Vector2(-16, -30));
        var barImg = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
        barImg.sprite = GameArt.Rounded(5);
        barImg.type = UnityEngine.UI.Image.Type.Sliced;
        barImg.color = Painter.A(Color.white, 0.05f);
        var sb = bar.gameObject.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        var sliding = Stretch(bar, "Area");
        var handle = Stretch(sliding, "Handle");
        var handleImg = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
        handleImg.sprite = GameArt.Rounded(5);
        handleImg.type = UnityEngine.UI.Image.Type.Sliced;
        handleImg.color = Painter.A(Theme.Accent, 0.45f);
        sb.handleRect = handle;
        sb.targetGraphic = handleImg;
        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Заставка главы
        chapterBox = Rect(content, "Chapter", new Vector2(0, 1), new Vector2(1, 1));
        Column(chapterBox.gameObject, 26, new RectOffset(10, 10, 120, 60)).childAlignment = TextAnchor.UpperCenter;
        chapterNum = FlowText(chapterBox, "Number", 30, Theme.Accent, TextAnchor.UpperCenter);
        chapterTitle = FlowText(chapterBox, "Title", 66, Theme.Text, TextAnchor.UpperCenter);
        chapterTitle.fontStyle = FontStyle.Bold;
        chapterEpi = FlowText(chapterBox, "Epigraph", 31, Theme.TextDim, TextAnchor.UpperCenter);
        chapterEpi.fontStyle = FontStyle.Italic;
        chapterBox.gameObject.SetActive(false);

        var bannerRt = Rect(content, "ItemBanner", new Vector2(0, 1), new Vector2(1, 1));
        Layout(bannerRt.gameObject, 150, -1, 150);
        itemBanner = bannerRt.gameObject.AddComponent<UnityEngine.UI.Image>();
        itemBanner.preserveAspect = true;
        itemBanner.raycastTarget = false;
        bannerRt.gameObject.SetActive(false);

        headlineText = FlowText(content, "Headline", 36, Theme.Accent);
        headlineText.fontStyle = FontStyle.Bold;
        bodyText = FlowText(content, "Body", Theme.BodySize, Theme.Text);

        widgetBox = Rect(content, "Widgets", new Vector2(0, 1), new Vector2(1, 1));
        Column(widgetBox.gameObject, 14);
        choicesBox = Rect(content, "Choices", new Vector2(0, 1), new Vector2(1, 1));
        Column(choicesBox.gameObject, 12, new RectOffset(0, 0, 8, 0));
    }

    static void BuildHud()
    {
        var hud = Rect(root, "Hud", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -84), Vector2.zero);
        var bg = hud.gameObject.AddComponent<UnityEngine.UI.Image>();
        bg.color = Theme.Hud;

        var left = Rect(hud, "Stats", Vector2.zero, new Vector2(0.82f, 1), new Vector2(18, 12), new Vector2(0, -12));
        var row = Row(left.gameObject, 12);
        row.childControlWidth = true;
        row.childForceExpandHeight = true;

        hudDay = Chip(left, "day", 210, out _);
        hudMoney = Chip(left, "money", 200, out _);
        hudAddiction = BarChip(left, "addiction", 330, Painter.Hex("#a855f7"), out addictionFill);
        hudSatiety = BarChip(left, "satiety", 310, Painter.Hex("#4ade80"), out satietyFill);
        hudBag = Chip(left, "bag", 330, out var bagChip, true);
        bagIcons = Rect(bagChip, "Icons", new Vector2(0.48f, 0), new Vector2(1, 1), new Vector2(0, 6), new Vector2(-10, -6));
        var bagRow = Row(bagIcons.gameObject, 2);
        bagRow.childControlWidth = false;
        bagRow.childControlHeight = false;
        bagRow.childAlignment = TextAnchor.MiddleRight;

        var right = Rect(hud, "Controls", new Vector2(0.82f, 0), Vector2.one, new Vector2(0, 12), new Vector2(-18, -12));
        var rrow = Row(right.gameObject, 12);
        rrow.childAlignment = TextAnchor.MiddleRight;
        rrow.childForceExpandHeight = true;

        var musicRt = Rect(right, "Music", Vector2.zero, Vector2.one);
        Layout(musicRt.gameObject, -1, 64);
        MakeButton(musicRt.gameObject, Theme.Chip, () => { Music.Toggle(); UpdateHud(); }, 14);
        musicIcon = Image(musicRt, "Icon", GameArt.Icon("soundOn"), new Vector2(14, 10), new Vector2(-14, -10));
        musicIcon.preserveAspect = true;

        hudRestart = Rect(right, "Restart", Vector2.zero, Vector2.one);
        Layout(hudRestart.gameObject, -1, 170);
        MakeButton(hudRestart.gameObject, Theme.Chip, OpenRestart, 14);
        var ric = Image(hudRestart, "Icon", GameArt.Icon("restart"), new Vector2(10, 12), new Vector2(-124, -12));
        ric.preserveAspect = true;
        var rl = Label(hudRestart, Data.T("hud.restart"), 24, Theme.Text, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one);
        rl.rectTransform.offsetMin = new Vector2(60, 0);
    }

    static Text Chip(Transform parent, string icon, float width, out RectTransform chip, bool button = false)
    {
        chip = Rect(parent, "Chip", Vector2.zero, Vector2.one);
        Layout(chip.gameObject, -1, width);
        if (button) MakeButton(chip.gameObject, Theme.Chip, OpenInventory, 14);
        else
        {
            var img = chip.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = GameArt.Rounded(14);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = Theme.Chip;
        }
        var ic = Image(chip, "Icon", GameArt.Icon(icon), new Vector2(10, 10), Vector2.zero);
        ic.rectTransform.anchorMax = new Vector2(0, 1);
        ic.rectTransform.offsetMax = new Vector2(50, -10);
        ic.preserveAspect = true;
        var t = Label(chip, "", 25, Theme.Text, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one);
        t.rectTransform.offsetMin = new Vector2(60, 0);
        t.rectTransform.offsetMax = new Vector2(-8, 0);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    static Text BarChip(Transform parent, string icon, float width, Color color, out RectTransform fill)
    {
        // Подпись сверху, шкала под ней во всю ширину — текст не наезжает на шкалу при любой ширине окна
        var t = Chip(parent, icon, width, out var chip);
        t.rectTransform.offsetMin = new Vector2(60, 12);
        var track = Panel(chip, "Track", Vector2.zero, new Vector2(1, 0), Painter.Hex("#0d0b16"), 4);
        track.offsetMin = new Vector2(60, 9);
        track.offsetMax = new Vector2(-14, 17);
        fill = Panel(track, "Fill", Vector2.zero, Vector2.one, color, 4);
        return t;
    }

    // ===== ФОН =====
    public static void SetBackground(string name)
    {
        if (string.IsNullOrEmpty(name)) name = "school";
        if (name == bgName) return;
        bgName = name;
        bgPending = true;
        TryApplyBackground();
    }

    static bool bgPending;

    // Фон рисуется в фоновом потоке: ставим его, когда готов (каждый кадр, пока ждём)
    public static void TryApplyBackground()
    {
        if (!bgPending) return;
        var tex = Backgrounds.Get(bgName, true);
        if (tex == null) return;
        bgPending = false;
        if (bgA.texture == null)
        {
            bgA.texture = tex;
            bgA.color = new Color(1, 1, 1, 0);
            Run.UiTween(0.4f, k => bgA.color = new Color(1, 1, 1, k));
            return;
        }
        // Плавная смена: новая картинка проявляется поверх старой
        bgB.texture = tex;
        bgB.color = new Color(1, 1, 1, 0);
        Run.UiTween(0.45f, k =>
        {
            if (bgB.texture != tex) return; // уже следующая смена
            bgB.color = new Color(1, 1, 1, k);
            if (k >= 1)
            {
                bgA.texture = tex;
                bgB.color = new Color(1, 1, 1, 0);
            }
        });
    }

    public static void SetBrightness(float value)
    {
        float from = brightness;
        Run.UiTween(0.3f, k =>
        {
            brightness = Mathf.Lerp(from, value, k);
            ApplyBrightness();
        });
    }

    static void ApplyBrightness()
    {
        if (brightness < 1) brightOverlay.color = new Color(0, 0, 0, (1 - brightness) * 0.95f);
        else brightOverlay.color = new Color(1, 0.95f, 0.8f, (brightness - 1) * 0.45f);
    }

    // Чем больше ставок, тем темнее вокруг
    public static void ApplyMood()
    {
        G.S.mood = Mathf.Min(1f, G.S.betsMade / 8f);
        moodOverlay.color = new Color(0.04f, 0f, 0.08f, G.S.mood * 0.38f);
    }

    // ===== ПЕРСОНАЖИ И ИГРОВОЕ ПОЛЕ =====
    public static void ShowCast(List<string> cast)
    {
        if (boardArea.gameObject.activeSelf) cast = new List<string>();
        stage.Show(cast);
    }

    // Карточка команды на игровом поле (ставки на матчи)
    public static BoardButton TeamButton(Transform parent, string team, string odds, int side, Vector2 min, Vector2 max, Action onClick) =>
        BoardButton.Create(parent, team, odds, side, min, max, onClick);

    public static RectTransform ShowBoard()
    {
        foreach (Transform child in boardArea) UnityEngine.Object.Destroy(child.gameObject);
        boardArea.gameObject.SetActive(true);
        stageArea.gameObject.SetActive(false);
        return boardArea;
    }

    // Точка экрана (доли от левого нижнего угла) — в якорях игрового поля: совместить с рисунком фона
    public static Vector2 BoardPoint(Vector2 screen) => new Vector2(
        (screen.x - boardArea.anchorMin.x) / (boardArea.anchorMax.x - boardArea.anchorMin.x),
        (screen.y - boardArea.anchorMin.y) / (boardArea.anchorMax.y - boardArea.anchorMin.y));

    public static void HideBoard()
    {
        foreach (Transform child in boardArea) UnityEngine.Object.Destroy(child.gameObject);
        boardArea.gameObject.SetActive(false);
        stageArea.gameObject.SetActive(true);
    }

    // ===== ТЕКСТ =====
    // Новая сцена: всё особое убрано, текст проявляется заново
    public static void HideWidgets()
    {
        HideBoard();
        foreach (Transform child in widgetBox) UnityEngine.Object.Destroy(child.gameObject);
        itemBanner.gameObject.SetActive(false);
        chapterBox.gameObject.SetActive(false);
        scrollToTop = true;
        fitPending = true;
        contentGroup.alpha = 0;
        Run.UiTween(0.28f, k => contentGroup.alpha = k);
    }

    public static void SetText(string text, string headline = null, string headlineClass = null)
    {
        chapterBox.gameObject.SetActive(false);
        headlineText.gameObject.SetActive(!string.IsNullOrEmpty(headline));
        if (!string.IsNullOrEmpty(headline))
        {
            headlineText.text = Txt.Clean(headline);
            headlineText.color = headlineClass == "result-win" ? Theme.Good : headlineClass == "result-jackpot" ? Theme.Gold : Theme.Accent;
        }
        bodyText.gameObject.SetActive(true);
        bodyText.text = CompactParagraphs(Txt.Clean(text));
        fitPending = true;
    }

    // Пустая строка между абзацами — пониже обычной: больше текста помещается без прокрутки
    static string CompactParagraphs(string text) =>
        text.Replace("\r", "").Replace("\n\n", "\n<size=" + Theme.ParagraphGap + "> </size>\n");

    // Текст сцены не помещается в панель — шрифт поменьше (до BodyMinSize), чтобы не прокручивать.
    // Если и так не влезает — остаётся прокрутка.
    static void FitText()
    {
        fitPending = false;
        if (!bodyText.gameObject.activeInHierarchy) return;
        float view = scroll.viewport.rect.height;
        for (int size = Theme.BodySize; size >= Theme.BodyMinSize; size--)
        {
            bodyText.fontSize = size;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            if (content.rect.height <= view) break;
        }
    }

    public static void SetChapter(ChapterCard c)
    {
        headlineText.gameObject.SetActive(false);
        bodyText.gameObject.SetActive(false);
        chapterBox.gameObject.SetActive(true);
        chapterNum.text = Txt.Clean(c.number).ToUpper();
        chapterTitle.text = Txt.Clean(c.title);
        chapterEpi.text = Txt.Clean(c.epigraph);
    }

    public static void ShowItemBanner(string itemId)
    {
        itemBanner.sprite = GameArt.Item(itemId);
        itemBanner.gameObject.SetActive(true);
        Bump(itemBanner.rectTransform);
    }

    // ===== КНОПКИ =====
    public static void ClearChoices()
    {
        foreach (Transform child in choicesBox) UnityEngine.Object.Destroy(child.gameObject);
        buttons.Clear();
    }

    public static ChoiceButton AddButton(string label, Action onClick, bool disabled = false, string iconItem = null, bool danger = false)
    {
        var b = ChoiceButton.Create(choicesBox, label, onClick, disabled, iconItem, danger, buttons.Count);
        buttons.Add(b);
        return b;
    }

    // Горячие клавиши 1–9
    public static void PressChoice(int index)
    {
        if (index >= 0 && index < buttons.Count) buttons[index].Click();
    }

    // ===== ОСОБЫЕ ПОЛЯ =====
    public static NameWidget ShowNameInput(string boy, string girl, string submit) =>
        NameWidget.Create(widgetBox, boy, girl, submit);

    public static InputField ShowNumberInput(string placeholder, bool enabled)
    {
        var f = MakeInput(widgetBox, Txt.Clean(placeholder), 7, InputField.ContentType.IntegerNumber);
        f.interactable = enabled;
        if (enabled) Run.UiAfter(0.05f, () => { if (f != null) f.ActivateInputField(); });
        return f;
    }

    // ===== HUD =====
    public static void UpdateHud()
    {
        var S = G.S;
        hudDay.text = Data.T("hud.day") + " " + S.day + " · " + Data.TList("weekdaysShort")[Rules.WeekdayIndex()];
        hudMoney.text = S.money + " ₽";
        hudAddiction.text = Data.T("hud.addiction") + " " + S.addiction;
        hudSatiety.text = Data.T("hud.satiety") + " " + S.satiety;
        addictionFill.anchorMax = new Vector2(S.addiction / 100f, 1);
        satietyFill.anchorMax = new Vector2(S.satiety / 100f, 1);
        satietyFill.GetComponent<UnityEngine.UI.Image>().color = S.satiety < B.SATIETY_MOM_NOTICE ? Theme.Bad : Painter.Hex("#4ade80");
        hudAddiction.color = S.addiction >= B.ADDICTION_NO_EXIT ? Theme.Bad : Theme.Text;

        string progress = S.inventory.Count + "/" + Data.C.ITEMS.Count;
        hudBag.text = S.inventory.Count == 0 ? Tpl.Fill(Data.T("inventory.hudEmpty"), "progress", progress) : progress;
        foreach (Transform child in bagIcons) UnityEngine.Object.Destroy(child.gameObject);
        foreach (var item in S.inventory.Take(7))
        {
            var ic = Rect(bagIcons, "I", Vector2.zero, Vector2.zero);
            ic.sizeDelta = new Vector2(30, 30);
            var img = ic.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = GameArt.Item(item.id);
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        musicIcon.sprite = GameArt.Icon(Music.Muted ? "soundOff" : "soundOn");
        // «Заново» — только когда игра уже идёт (имя введено) и ещё не закончилась
        hudRestart.gameObject.SetActive(!string.IsNullOrEmpty(S.playerName) && !Game.IsEndingScene(S.scene));
    }

    // ===== ОКНА =====
    static RectTransform OpenModal(float width, float height, Action onClose)
    {
        foreach (Transform child in modalLayer) UnityEngine.Object.Destroy(child.gameObject);
        modalLayer.gameObject.SetActive(true);
        modalClose = onClose;
        var card = Panel(modalLayer, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Theme.CardDark, 28);
        card.sizeDelta = new Vector2(width, height);
        card.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        card.gameObject.AddComponent<ClickBlocker>(); // клик по окну не закрывает его
        Bump(card);
        return card;
    }

    // Окно без обработчика закрытия (выбор при запуске) закрывается только своими кнопками
    public static void CloseModal()
    {
        if (!ModalOpen || modalClose == null) return;
        modalLayer.gameObject.SetActive(false);
        var close = modalClose;
        modalClose = null;
        close?.Invoke();
    }

    static Text ModalText(RectTransform card, string text, float top = 0.4f)
    {
        var t = Label(card, text, 30, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0, top), Vector2.one);
        t.rectTransform.offsetMin = new Vector2(50, 0);
        t.rectTransform.offsetMax = new Vector2(-50, -40);
        return t;
    }

    public static void Alert(string message)
    {
        var card = OpenModal(760, 340, () => { });
        ModalText(card, message, 0.36f);
        TextButton(card, Data.T("modal.ok"), Theme.AccentStrong, CloseModal, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.3f));
    }

    static void OpenRestart()
    {
        var card = OpenModal(820, 420, () => { });
        ModalText(card, Data.T("restart.question"), 0.42f);
        TextButton(card, Data.T("restart.yes"), Painter.Hex("#5a1f2c"), () =>
        {
            modalClose = null;
            modalLayer.gameObject.SetActive(false);
            Effects.ConfirmRestart();
        }, new Vector2(0.06f, 0.1f), new Vector2(0.48f, 0.32f));
        TextButton(card, Data.T("restart.no"), Theme.AccentStrong, CloseModal, new Vector2(0.52f, 0.1f), new Vector2(0.94f, 0.32f));
    }

    // Зависимость 100: окно «хочу отыграться» — уйти спать нельзя
    public static void OpenCompulsion()
    {
        var dialog = Data.Scene("compulsion_dialog");
        var card = OpenModal(860, 520, () => { });
        ModalText(card, Tpl.Apply(dialog.text), 0.4f);
        var sleep = TextButton(card, dialog.S("sleepText"), Theme.Button, null, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.34f));
        sleep.interactable = false;
        TextButton(card, dialog.S("continueText"), Theme.AccentStrong, CloseModal, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.19f));
    }

    // Старт игры: есть сохранение — продолжить или начать заново
    public static void OpenContinue(GameState saved, Action onContinue, Action onNew)
    {
        var card = OpenModal(820, 440, null);
        var title = Label(card, Data.T("save.title"), 44, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.68f), new Vector2(1, 0.9f));
        title.fontStyle = FontStyle.Bold;
        Label(card, Tpl.Fill(Data.T("save.info"), "name", saved.playerName, "day", saved.day, "money", saved.money),
            26, Theme.TextDim, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.68f));
        TextButton(card, Tpl.Fill(Data.T("save.continue"), "day", saved.day), Theme.AccentStrong, () =>
        {
            modalLayer.gameObject.SetActive(false);
            onContinue();
        }, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.44f));
        TextButton(card, Data.T("save.newGame"), Theme.Button, () =>
        {
            modalLayer.gameObject.SetActive(false);
            onNew();
        }, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.24f));
    }

    // Рюкзак: что есть и список желаний с прогрессом накоплений
    public static void OpenInventory()
    {
        var S = G.S;
        // С 3D-моделями — примерка: герой слева во весь рост, надетое видно сразу
        bool fitting = Actor3D.Available;
        var card = OpenModal(fitting ? 1560 : 980, 900, () => { });
        Label(card, Data.T("inventory.title"), 38, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0, 0.91f), new Vector2(1, 0.98f)).fontStyle = FontStyle.Bold;

        Actor3D preview = null;
        RectTransform previewRt = null;
        if (fitting)
        {
            var stand = Panel(card, "Fitting", new Vector2(0.025f, 0.13f), new Vector2(0.345f, 0.9f), Painter.Hex("#1f1a33"), 22);
            Image(stand, "Glow", GameArt.Glow(Painter.A(Theme.Accent, 0.5f)), new Vector2(-40, -120), new Vector2(40, 60));
            var floor = Image(stand, "Floor", GameArt.Glow(Painter.A(Theme.Gold, 0.35f)), Vector2.zero, Vector2.zero);
            floor.rectTransform.anchorMin = new Vector2(0.1f, 0.02f);
            floor.rectTransform.anchorMax = new Vector2(0.9f, 0.14f);
            previewRt = Rect(stand, "Hero", Vector2.zero, Vector2.one, new Vector2(10, 14), new Vector2(-10, -10));
            float aspect = 0.32f * 1560 / (0.77f * 900);
            preview = Actor3D.Create(Actor3D.Framing.Full, aspect);
            if (preview.SetModel(Data.HeroKey()))
            {
                var img = previewRt.gameObject.AddComponent<RawImage>();
                img.texture = preview.Texture;
                img.raycastTarget = false;
                var link = previewRt.gameObject.AddComponent<Actor3DLink>();
                link.actor = preview;
                link.showcase = true;
                preview.SetOutfit(Outfit.Parts());
            }
            else
            {
                UnityEngine.Object.Destroy(preview.gameObject);
                preview = null;
            }
            Label(card, Data.T("inventory.tryOn"), 22, Theme.TextDim, TextAnchor.MiddleCenter, new Vector2(0.025f, 0.05f), new Vector2(0.345f, 0.13f));
        }

        float x0 = fitting ? 0.37f : 0.05f, x1 = fitting ? 0.975f : 0.95f;
        var list = Rect(card, "List", new Vector2(x0, 0.18f), new Vector2(x1, 0.9f));
        Column(list.gameObject, 10);
        var wearButtons = new List<(string id, Button button, Text label)>();
        void RefreshWear()
        {
            foreach (var (id, button, label) in wearButtons)
            {
                bool worn = Outfit.IsWorn(id);
                label.text = Data.T(worn ? "inventory.takeOff" : "inventory.wear");
                var c = button.colors;
                c.normalColor = worn ? Theme.Button : Theme.AccentStrong;
                c.highlightedColor = Painter.Shade(c.normalColor, 0.18f);
                c.selectedColor = c.normalColor;
                button.colors = c;
            }
            if (preview != null) preview.SetOutfit(Outfit.Parts());
        }

        foreach (var kv in Data.C.ITEMS)
        {
            string id = kv.Key;
            var item = kv.Value;
            var owned = S.inventory.FirstOrDefault(i => i.id == id);
            bool locked = S.confiscated.Any(i => i.id == id);
            var rowRt = Rect(list, "Row", new Vector2(0, 1), new Vector2(1, 1));
            Layout(rowRt.gameObject, 76, -1, 76);
            var rowImg = rowRt.gameObject.AddComponent<UnityEngine.UI.Image>();
            rowImg.sprite = GameArt.Rounded(14);
            rowImg.type = UnityEngine.UI.Image.Type.Sliced;
            rowImg.color = owned != null ? Painter.Hex("#24304a") : Painter.Hex("#1a1729");
            var ic = Image(rowRt, "Icon", GameArt.Item(id), new Vector2(10, 6), Vector2.zero);
            ic.rectTransform.anchorMax = new Vector2(0, 1);
            ic.rectTransform.offsetMax = new Vector2(74, -6);
            ic.preserveAspect = true;
            if (owned == null) ic.color = new Color(1, 1, 1, 0.45f);

            string status;
            if (owned != null) status = "<color=#4ade80>" + Data.T("inventory.source." + owned.source) + "</color>";
            else if (locked) status = Data.T("inventory.locked");
            else status = S.money >= item.price ? Data.T("inventory.affordable") : Tpl.Fill(Data.T("inventory.saving"), "money", S.money, "price", item.price);
            var nameT = Label(rowRt, "<b>" + item.name + "</b>\n<size=21>" + status + "</size>", 25, Theme.Text, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one);
            nameT.rectTransform.offsetMin = new Vector2(92, 0);
            nameT.rectTransform.offsetMax = new Vector2(-170, 0);

            // Своё и надеваемое — кнопка «Надеть / Снять» вместо цены (примерка видна слева)
            if (owned != null && preview != null && Outfit.Wearable(id))
            {
                var bRt = Rect(rowRt, "Wear", new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                bRt.sizeDelta = new Vector2(150, 50);
                bRt.anchoredPosition = new Vector2(-92, 0);
                string itemId = id;
                var b = MakeButton(bRt.gameObject, Theme.AccentStrong, () =>
                {
                    Music.Click();
                    Outfit.Toggle(itemId);
                    RefreshWear();
                    Bump(previewRt);
                }, 12);
                var bl = Label(bRt, "", 24, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
                wearButtons.Add((id, b, bl));
                continue;
            }
            var price = Label(rowRt, item.price + " ₽", 26, Theme.Gold, TextAnchor.MiddleRight, Vector2.zero, Vector2.one);
            price.rectTransform.offsetMax = new Vector2(-20, 0);
            if (owned == null && !locked)
            {
                var track = Panel(rowRt, "Progress", new Vector2(1, 0), new Vector2(1, 0), Painter.Hex("#0d0b16"), 5);
                track.sizeDelta = new Vector2(130, 10);
                track.anchoredPosition = new Vector2(-85, 16);
                var fill = Panel(track, "Fill", Vector2.zero, Vector2.one, Theme.Gold, 5);
                fill.anchorMax = new Vector2(Mathf.Min(1f, (float)S.money / item.price), 1);
            }
        }
        RefreshWear();
        int resale = S.inventory.Sum(i => i.price / 2);
        string note = S.inventory.Count > 0 ? Tpl.Fill(Data.T("inventory.resale"), "resale", resale) : Data.T("inventory.empty");
        Label(card, note, 24, Theme.TextDim, TextAnchor.MiddleCenter, new Vector2(x0, 0.11f), new Vector2(x1, 0.18f));
        float mid = (x0 + x1) / 2, half = 0.15f * (x1 - x0) / 0.9f;
        TextButton(card, Data.T("inventory.close"), Theme.AccentStrong, CloseModal, new Vector2(mid - half, 0.025f), new Vector2(mid + half, 0.1f));
    }

    // ===== ЭФФЕКТЫ =====
    public static void Bump(RectTransform rt)
    {
        if (rt == null) return;
        Run.UiTween(0.22f, k =>
        {
            if (rt == null) return;
            float s = 1 + Mathf.Sin(k * Mathf.PI) * 0.08f;
            rt.localScale = new Vector3(s, s, 1);
        });
    }

    public static void Shake()
    {
        Run.UiTween(0.35f, k =>
        {
            float a = (1 - k) * 14f;
            world.anchoredPosition = new Vector2(Mathf.Sin(k * 60f) * a, Mathf.Cos(k * 47f) * a * 0.4f);
        });
    }

    // Монеты летят вверх — выигрыш
    public static void Coins()
    {
        var area = boardArea.gameObject.activeSelf ? boardArea : stageArea;
        for (int i = 0; i < 16; i++)
        {
            var rt = Rect(fxLayer, "Coin", new Vector2(0, 0), new Vector2(0, 0));
            rt.sizeDelta = new Vector2(46, 46);
            var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = GameArt.Icon("money");
            img.raycastTarget = false;
            float x = UnityEngine.Random.Range(0.1f, 0.5f) * 1920f, delay = UnityEngine.Random.value * 0.4f, drift = UnityEngine.Random.Range(-80f, 80f);
            Run.UiTween(1.3f + delay, k =>
            {
                if (rt == null) return;
                float t = Mathf.Clamp01((k * (1.3f + delay) - delay) / 1.3f);
                rt.anchoredPosition = new Vector2(x + drift * t, 200 + t * 700 - t * t * 250);
                img.color = new Color(1, 1, 1, t <= 0 ? 0 : 1 - t * t);
                if (k >= 1) UnityEngine.Object.Destroy(rt.gameObject);
            });
        }
    }

    public static void JackpotFlash(string text)
    {
        var rt = Rect(fxLayer, "Jackpot", new Vector2(0, 0.3f), new Vector2(1, 0.7f));
        var glow = Image(rt, "Glow", GameArt.Glow(Painter.Hex("#facc15", 0.8f)), new Vector2(300, -200), new Vector2(-300, 200));
        var t = Label(rt, text, 150, Theme.Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        t.fontStyle = FontStyle.Bold;
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.4f, 0.1f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(4, -6);
        Coins();
        Run.UiTween(2f, k =>
        {
            if (rt == null) return;
            float s = 0.6f + Mathf.SmoothStep(0, 1, Mathf.Min(1, k * 4)) * 0.5f + Mathf.Sin(k * 20f) * 0.03f;
            rt.localScale = new Vector3(s, s, 1);
            float a = k < 0.75f ? 1 : 1 - (k - 0.75f) / 0.25f;
            t.color = Painter.A(Theme.Gold, a);
            glow.color = new Color(1, 1, 1, a);
            if (k >= 1) UnityEngine.Object.Destroy(rt.gameObject);
        });
    }

    // Каждый кадр: прокрутка к началу после смены сцены
    public static void LateTick()
    {
        if (fitPending)
        {
            Canvas.ForceUpdateCanvases();
            FitText();
        }
        // Кнопки и виджеты, появившиеся позже (по таймеру), тоже не должны требовать прокрутки — шрифт ещё на шаг меньше
        else if (bodyText.gameObject.activeInHierarchy && bodyText.fontSize > Theme.BodyMinSize &&
                 content.rect.height > scroll.viewport.rect.height + 1)
            bodyText.fontSize--;
        if (!scrollToTop) return;
        scrollToTop = false;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scroll.verticalNormalizedPosition = 1;
    }

    public static bool InputFocused()
    {
        var go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return go != null && go.GetComponent<InputField>() != null && go.GetComponent<InputField>().isFocused;
    }
}

// Ловит клик, чтобы он не дошёл до затемнения под окном
public class ClickBlocker : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData e) { }
}

// Клавиатура и прокрутка: живёт на Canvas
public class GameView : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) UI.CloseModal();
        if (Input.GetKeyDown(KeyCode.F11)) Screen.fullScreen = !Screen.fullScreen;
        if (UI.ModalOpen || UI.InputFocused()) return;
        for (int i = 0; i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) UI.PressChoice(i);
    }

    void LateUpdate()
    {
        ArtQueue.Pump();
        UI.TryApplyBackground();
        UI.LateTick();
    }
}
