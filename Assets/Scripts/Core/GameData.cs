// Загрузка данных игры из Resources/Data: сцены (story), строки интерфейса (TEXTS), каталоги, персонажи.
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class Data
{
    public static Dictionary<string, SceneDef> story;
    public static JObject texts;
    public static Catalog C;
    public static Dictionary<string, CharacterDef> characters;

    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        story = new Dictionary<string, SceneDef>();
        var storyJson = JObject.Parse(Read("story"));
        foreach (var p in storyJson.Properties()) story[p.Name] = SceneDef.From(p.Name, (JObject)p.Value);

        texts = JObject.Parse(Read("texts"));
        // Строки, которых нет в браузерной версии (там были эмодзи и картинки)
        var extra = JObject.Parse(Read("texts_unity"));
        texts.Merge(extra, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

        C = JObject.Parse(Read("catalog")).ToObject<Catalog>();
        characters = JObject.Parse(Read("characters")).ToObject<Dictionary<string, CharacterDef>>();
        // Другие варианты внешности героя (своё для Unity-версии): hero_m_1, hero_m_2, hero_f_1…
        heroLooks = JObject.Parse(Read("heroes")).ToObject<Dictionary<string, List<CharacterDef>>>();
        foreach (var kv in heroLooks)
            for (int i = 0; i < kv.Value.Count; i++) characters["hero_" + kv.Key + "_" + (i + 1)] = kv.Value[i];
    }

    static Dictionary<string, List<CharacterDef>> heroLooks;

    // Сколько вариантов внешности у героя этого пола (первый — из characters.json)
    public static int HeroLookCount(string gender) => 1 + (heroLooks != null && heroLooks.TryGetValue(gender, out var l) ? l.Count : 0);

    // Ключ персонажа-героя: hero_m — основной вид, hero_m_1… — варианты
    public static string HeroKey(string gender, int look)
    {
        look = look < 0 || look >= HeroLookCount(gender) ? 0 : look;
        return look == 0 ? "hero_" + gender : "hero_" + gender + "_" + look;
    }

    public static string HeroKey() => HeroKey(G.S.gender, G.S.look);

    static string Read(string name)
    {
        var asset = Resources.Load<TextAsset>("Data/" + name);
        if (asset == null)
        {
            Debug.LogError("Нет файла данных: Resources/Data/" + name + ".json");
            return "{}";
        }
        return asset.text;
    }

    public static SceneDef Scene(string id) => story.TryGetValue(id, out var s) ? s : null;

    // TEXTS по пути: T("casino.betAll")
    public static string T(string path)
    {
        var token = texts.SelectToken(path);
        if (token == null) Debug.LogWarning("Нет строки TEXTS." + path);
        return token?.ToString() ?? "";
    }

    public static List<string> TList(string path) => texts.SelectToken(path)?.ToObject<List<string>>() ?? new List<string>();

    public static ItemDef Item(string id) => C.ITEMS[id];
}
