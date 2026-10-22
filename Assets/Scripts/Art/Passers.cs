// Внешность прохожих в мини-игре «листовки» (Resources/Data/passers.json, ключ — эмодзи из FLYER_PASSERS).
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class Passers
{
    static Dictionary<string, CharacterDef> defs;

    public static CharacterDef Def(string emoji)
    {
        if (defs == null)
        {
            var asset = Resources.Load<TextAsset>("Data/passers");
            defs = asset != null
                ? JsonConvert.DeserializeObject<Dictionary<string, CharacterDef>>(asset.text)
                : new Dictionary<string, CharacterDef>();
        }
        if (defs.TryGetValue(emoji, out var d)) return d;
        return new CharacterDef { name = "", skin = "#efc29b", hair = "#3f2a1c", hairStyle = "short", top = "#5a6b7d", topStyle = "hoodie" };
    }
}
