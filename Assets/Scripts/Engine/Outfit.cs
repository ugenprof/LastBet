// Что надето на герое из рюкзака (окно рюкзака → «Надеть»). Слот — одна вещь: футболка или худи, шорты, кроссовки,
// наушники, часы, смартфон в руке. Надетое — только из рюкзака: проданное, отнятое папой, украденное у героя — снимается само.
// 3D-модель героя содержит все части (Tools/Blender: top_tshirt, acc_watch…), Actor3D включает нужные.
using System.Collections.Generic;
using System.Linq;

public static class Outfit
{
    static GameState S => G.S;

    // вещь → (слот, часть 3D-модели героя)
    static readonly Dictionary<string, (string slot, string part)> Items = new Dictionary<string, (string, string)>
    {
        { "tshirt", ("top", "top_tshirt") }, { "hoodie", ("top", "top_hoodie") }, { "jeans", ("pants", "pants_jeans") },
        { "sneakers", ("shoes", "shoes_sneakers") }, { "headphones", ("head", "acc_headphones") },
        { "watch", ("wrist", "acc_watch") }, { "smartphone", ("hand", "acc_smartphone") },
    };

    public static bool Wearable(string id) => Items.ContainsKey(id);

    static bool Owned(string id) => S.inventory.Any(i => i.id == id);

    public static bool IsWorn(string id) => S.worn.Contains(id) && Owned(id);

    // Надеть / снять; вещь из того же слота снимается
    public static void Toggle(string id)
    {
        if (!Wearable(id) || !Owned(id)) return;
        if (S.worn.Contains(id))
        {
            S.worn.Remove(id);
            return;
        }
        string slot = Items[id].slot;
        S.worn.RemoveAll(w => Items.TryGetValue(w, out var x) && x.slot == slot);
        S.worn.Add(id);
    }

    // Части 3D-модели, которые сейчас надеты
    public static HashSet<string> Parts() =>
        new HashSet<string>(S.worn.Where(IsWorn).Select(id => Items[id].part));
}
