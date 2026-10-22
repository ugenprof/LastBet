// Модели данных: сцена, выбор, эффект, вещи и каталоги. Сами данные — Assets/Resources/Data/*.json
// (выгружены из браузерной версии скриптом Tools/export_story.js).
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Заставка главы
public class ChapterCard
{
    public string number;
    public string title;
    public string epigraph;
}

// Изменения состояния при выборе — см. Effects.ApplyEffect
public class Effect
{
    public int money, addiction, satiety, punishDays, tip;
    public bool packLunch, dadTalked, confessed, rehire, relapse, phoneLock, pashaSaved, jobs;
    public string dadTalk;       // "mom" | "car" — вечером придёт папа
    public string company;       // воскресенье: с кем пошёл
    public string route;         // дорога домой из магазина
    public string detour;        // после смены — зайти в магазин
    public string rehireAsk;     // попроситься обратно на работу
    public string temptation;    // "refused"
    public string pashaHelp;     // "loan" | "theft" | "job"
    public string theftStore;    // "tech" | "clothes"
    public string classReaction; // "laugh" | "silent" | "defend"
    public string maxTalk;       // "support" | "cold"

    public Effect Clone() => (Effect)MemberwiseClone();
}

// Кнопка выбора. Поля — как в браузерной версии (js/engine/effects.js)
public class Choice
{
    public string text, textRude, rouletteText, next;
    public List<string> textVariants;
    public Effect effect;
    public bool reset, quit, ifPlayed, ifNeverPlayed, ifFewBets, ifManyBets, packedLunch, newGame, disabled;
    public string lie;            // "always" | "ifPlayed"
    public string buyLunch;       // "full" | "half" | "snack"
    public string buyItem, theftItem, sundayActivity;
    public int sellIndex = -1;

    // Не из сюжета: иконка вещи на кнопке (магазин, кража)
    [JsonIgnore] public string iconItem;
    // Не из сюжета: своя реакция на нажатие вместо перехода (кнопки ставок, мини-игр)
    [JsonIgnore] public System.Action onClick;

    public Choice Clone()
    {
        var c = (Choice)MemberwiseClone();
        if (effect != null) c.effect = effect.Clone();
        return c;
    }
}

// Сцена: общие поля разобраны, остальные (варианты текста, словари) — через S / L / O
public class SceneDef
{
    public string id;
    public JObject raw;
    public string bg, music, text, next, headline, headlineClass, minigame;
    public bool nameInput, roulette, match;
    public ChapterCard chapter;
    public List<string> cast, castSaved, textVariants, textSavedVariants;
    public List<Choice> choices = new List<Choice>();

    public static SceneDef From(string id, JObject o)
    {
        var s = new SceneDef { id = id, raw = o };
        s.bg = o.Value<string>("bg");
        s.music = o.Value<string>("music");
        s.text = o.Value<string>("text");
        s.next = o.Value<string>("next");
        s.headline = o.Value<string>("headline");
        s.headlineClass = o.Value<string>("headlineClass");
        s.minigame = o.Value<string>("minigame");
        s.nameInput = o.Value<bool?>("nameInput") ?? false;
        s.roulette = o.Value<bool?>("roulette") ?? false;
        s.match = o.Value<bool?>("match") ?? false;
        s.chapter = o["chapter"]?.ToObject<ChapterCard>();
        s.cast = s.L("cast");
        s.castSaved = s.L("castSaved");
        s.textVariants = s.L("textVariants");
        s.textSavedVariants = s.L("textSavedVariants");
        if (o["choices"] is JArray arr)
            foreach (var c in arr) s.choices.Add(c.ToObject<Choice>());
        return s;
    }

    public bool Has(string key) => raw[key] != null;
    public string S(string key) => raw.Value<string>(key);
    public List<string> L(string key) => raw[key] is JArray a ? a.ToObject<List<string>>() : null;
    public JObject O(string key) => raw[key] as JObject;
    public ChapterCard Card(string key) => raw[key]?.ToObject<ChapterCard>();
    // Строка из словаря-поля: scene.dishes[id], scene.company[who]
    public string Sub(string key, string sub) => (raw[key] as JObject)?.Value<string>(sub);
    // Вложенное: scene.activities[id].label
    public string Sub(string key, string sub, string field) => ((raw[key] as JObject)?[sub] as JObject)?.Value<string>(field);
}

public class ItemDef
{
    public string name, img;
    public int price;
}

// Вещь в рюкзаке
public class InvItem
{
    public string id, name, source; // source: bought | stolen | gift
    public int price, boughtDay;
}

public class LunchDish { public string id; public int satiety; }
public class FlyerPasser { public string emoji, who, desc; public bool ok; }
public class Product { public string emoji, name; }
public class District { public string emoji, name; public List<string> streets; }
public class MenuItem { public string name; public int price; }
public class SlotSymbol { public string emoji; public int weight, pays; public bool jackpot; }
public class SundayActivity { public int price, addiction, satiety; }
public class MusicTrack { public string file; public float volume; }

public class Catalog
{
    public Dictionary<string, ItemDef> ITEMS;
    public List<string> CLOTHES_ITEMS, TECH_ITEMS, MATCH_TEAMS;
    public List<LunchDish> LUNCH_DISHES;
    public List<FlyerPasser> FLYER_PASSERS;
    public List<Product> GROCERY_PRODUCTS;
    public List<District> COURIER_DISTRICTS;
    public List<MenuItem> CAFE_MENU;
    public List<int> CAFE_BILLS;
    public List<SlotSymbol> SLOT_SYMBOLS;
    // Порядок мест и занятий важен (кнопки) — Newtonsoft сохраняет порядок ключей
    public Dictionary<string, Dictionary<string, SundayActivity>> SUNDAY_ACTIVITIES;
    public Dictionary<string, int> JOB_RAISE;
    public Dictionary<string, string> MUSIC_BY_BG;
    public Dictionary<string, MusicTrack> MUSIC_TRACKS;
}

// Внешность персонажа (js/config/characters.js)
public class CharacterDef
{
    public string name, skin, hair, hairStyle, hat, hatColor, top, topStyle, accent, tie, scarf;
    public bool glasses, mustache, beard, stubble, wrinkles, earrings, noTie, stethoscope;
    public float scale = 1f;
    public string model;            // 3D-модель (прохожие: passers.json)
    public List<string> props;      // реквизит 3D-модели — только для генератора моделей
    public CharacterDef Clone() => (CharacterDef)MemberwiseClone();
}
