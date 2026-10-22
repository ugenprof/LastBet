// Сцена с персонажами (слева). Запись в cast: "pasha", "pasha:sad", "*pasha" (* — говорит: шевелит губами),
// "hero" / "hero:happy", "{boss}" — начальник текущей работы, "{friend}" — друг, с которым пошёл в воскресенье,
// "-hero" — героя на сцене нет. Герой появляется слева сам, если на сцене есть кто-то ещё.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StageView
{
    readonly RectTransform area;
    readonly Dictionary<string, StageActor> actors = new Dictionary<string, StageActor>();

    public StageView(RectTransform area) => this.area = area;

    struct Entry { public string id, mood; public bool speaking; }

    static Entry Parse(string entry)
    {
        bool speaking = entry.StartsWith("*");
        var parts = entry.Replace("*", "").Split(':');
        string id = parts[0], mood = parts.Length > 1 ? parts[1] : "normal";
        var S = G.S;
        if (id == "{boss}") id = S.lastJob != null && Jobs.JOB_BOSS_CHARACTERS.TryGetValue(S.lastJob, out var boss) ? boss : "brigadier";
        if (id == "{friend}") id = S.sundayCompany == "alone" ? null : S.sundayCompany;
        return new Entry { id = id, mood = mood, speaking = speaking };
    }

    public void Show(List<string> cast)
    {
        cast = cast ?? new List<string>();
        bool noHero = cast.Contains("-hero");
        var entries = cast.Where(c => c != "-hero").Select(Parse).Where(e => e.id != null && (e.id == "hero" || Data.characters.ContainsKey(e.id))).ToList();
        if (entries.Count > 0 && !noHero && !entries.Any(e => e.id == "hero"))
            entries.Insert(0, new Entry { id = "hero", mood = "normal" });
        // Герой — всегда слева
        entries = entries.OrderBy(e => e.id == "hero" ? 0 : 1).ToList();

        // Кто уже стоит на сцене — остаётся без повторного появления; кто ушёл — исчезает
        var keep = new HashSet<string>(entries.Select(e => e.id));
        foreach (var id in actors.Keys.ToList())
            if (!keep.Contains(id))
            {
                actors[id].Leave();
                actors.Remove(id);
            }

        int n = entries.Count;
        float size = n >= 3 ? 0.66f : 0.76f; // доля высоты площадки для подростка; взрослые выше (scale)
        for (int i = 0; i < n; i++)
        {
            var e = entries[i];
            bool hero = e.id == "hero";
            var def = Data.characters[hero ? Data.HeroKey() : e.id];
            if (!actors.TryGetValue(e.id, out var actor))
            {
                actor = StageActor.Create(area, i * 0.12f);
                actors[e.id] = actor;
            }
            var look = hero
                ? new CharacterArt.Look { tired = G.S.addiction >= B.ADDICTION_RUDE, pale = G.S.satiety < B.SATIETY_MOM_NOTICE }
                : default;
            string key = hero ? Data.HeroKey() : e.id;
            // Пока герой без имени (экран выбора) — без подписи
            string name = hero && string.IsNullOrEmpty(G.S.playerName) ? "" : Tpl.Apply(def.name);
            actor.Set(key, def, e.mood, look, name, e.speaking);

            // Доли площадки, а не пиксели: размер верен с первого кадра и при смене размера окна
            float x = n == 1 ? 0.5f : 0.17f + 0.66f * i / (n - 1);
            float height = size * Mathf.Min(def.scale <= 0 ? 1 : def.scale, 1.18f);
            actor.Place(x, Mathf.Min(1f, height), hero ? 0 : i + 1);
        }
    }
}

// Один персонаж на сцене: картинка, подпись, дыхание, моргание, речь, появление.
// Картинка — 3D-модель (Actor3D), если модели собраны, иначе 2D (CharacterArt).
public class StageActor : MonoBehaviour
{
    RawImage img;
    Actor3D model3d;
    bool use3d;
    RectTransform rt;
    Text nameText;
    RectTransform nameTag;
    CanvasGroup group;

    string key, mood;
    CharacterDef def;
    CharacterArt.Look look;
    float phase, nextBlink, blinkUntil, talkUntil, appearAt, appearT;
    bool leaving;

    public static StageActor Create(RectTransform parent, float delay)
    {
        var rt = UI.Rect(parent, "Actor", Vector2.zero, Vector2.zero);
        rt.pivot = new Vector2(0.5f, 0);
        var a = rt.gameObject.AddComponent<StageActor>();
        a.rt = rt;
        var fit = rt.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fit.aspectRatio = (float)CharacterArt.UW / CharacterArt.UH;
        a.group = rt.gameObject.AddComponent<CanvasGroup>();
        a.group.alpha = 0;
        a.group.blocksRaycasts = false;
        a.img = UI.Stretch(rt, "Body").gameObject.AddComponent<RawImage>();
        a.img.raycastTarget = false;

        a.nameTag = UI.Panel(rt, "Name", new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Painter.A(Theme.CardDark, 0.92f), 14);
        a.nameTag.sizeDelta = new Vector2(240, 44);
        a.nameText = UI.Label(a.nameTag, "", 24, Theme.Text, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        a.phase = Random.value * 10f;
        a.nextBlink = Time.time + 1f + Random.value * 3f;
        a.appearAt = Time.time + delay;
        return a;
    }

    public void Set(string key, CharacterDef def, string mood, CharacterArt.Look look, string name, bool speaking)
    {
        this.key = key;
        this.def = def;
        this.mood = mood;
        this.look = look;
        nameText.text = Txt.Clean(name);
        nameTag.gameObject.SetActive(!string.IsNullOrEmpty(name));
        nameTag.sizeDelta = new Vector2(Mathf.Max(140, nameText.preferredWidth + 40), 44);
        if (speaking) talkUntil = Time.time + 2.2f;
        if (Actor3D.Available)
        {
            if (model3d == null) model3d = Actor3D.Create();
            use3d = model3d.SetModel(key);
        }
        else use3d = false;
        if (!use3d) Prepare();
        Refresh();
    }

    // x и height — доли ширины и высоты площадки; ширину по высоте подбирает AspectRatioFitter
    public void Place(float x, float height, int order)
    {
        rt.anchorMin = new Vector2(x, 0);
        rt.anchorMax = new Vector2(x, height);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.SetSiblingIndex(Mathf.Min(order, rt.parent.childCount - 1));
        if (model3d != null) model3d.Face(x);
    }

    public void Leave()
    {
        leaving = true;
        Destroy(gameObject, 0.25f);
    }

    void OnDestroy()
    {
        if (model3d != null) Destroy(model3d.gameObject);
    }

    // Нужный вариант (моргает, говорит) ещё рисуется — остаётся прежний; основной — заранее, вместе с вариантами
    void Refresh()
    {
        if (use3d)
        {
            img.texture = model3d.Texture;
            img.enabled = true;
            return;
        }
        var l = look;
        l.blink = Time.time < blinkUntil;
        l.talk = !l.blink && Time.time < talkUntil && Mathf.Repeat(Time.time * 7f, 1f) < 0.5f;
        var tex = CharacterArt.Get(key, def, mood, l);
        if (tex == null && (l.blink || l.talk)) tex = CharacterArt.Get(key, def, mood, look);
        if (tex != null) img.texture = tex;
        img.enabled = img.texture != null;
    }

    void Prepare()
    {
        var l = look;
        CharacterArt.Get(key, def, mood, l);
        l.blink = true;
        CharacterArt.Get(key, def, mood, l, false);
        l.blink = false;
        l.talk = true;
        CharacterArt.Get(key, def, mood, l, false);
    }

    void Update()
    {
        if (def == null) return;
        float t = Time.time;

        // Появление: снизу вверх и из прозрачности
        if (leaving) group.alpha = Mathf.Max(0, group.alpha - Time.deltaTime * 5f);
        else if (t >= appearAt && appearT < 1 && img.texture != null)
        {
            appearT = Mathf.Min(1, appearT + Time.deltaTime / 0.35f);
            group.alpha = appearT;
        }
        float rise = (1 - Mathf.SmoothStep(0, 1, appearT)) * 40f;

        // Дыхание и лёгкое покачивание
        float breath = Mathf.Sin(t * 2.1f + phase);
        img.rectTransform.localScale = new Vector3(1 - breath * 0.004f, 1 + breath * 0.009f, 1);
        img.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 0.7f + phase) * 0.6f);
        img.rectTransform.anchoredPosition = new Vector2(0, -rise);

        // Моргание
        if (t > nextBlink)
        {
            blinkUntil = t + 0.13f;
            nextBlink = t + 2.5f + Random.value * 3.5f;
        }
        if (use3d)
        {
            // герой — в том, что надел из рюкзака (продал или отняли — снимется само)
            if (key.StartsWith("hero_")) model3d.SetOutfit(Outfit.Parts());
            model3d.Animate(mood, t < talkUntil, look.tired, phase);
        }
        Refresh();
    }
}
