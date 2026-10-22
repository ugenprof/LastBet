// Проверка без Unity: заглушки Unity (FakeUnity.cs) + симуляция случайных прохождений и выгрузка арта в PNG.
// Запуск: dotnet run -- sim 500 1   (партий, зерно)   |   dotnet run -- art <папка>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

static class Program
{
    static readonly BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    static List<ChoiceButton> Buttons() => (List<ChoiceButton>)typeof(UI).GetField("buttons", SF).GetValue(null);
    static Text Body() => (Text)typeof(UI).GetField("bodyText", SF).GetValue(null);
    static Text Head() => (Text)typeof(UI).GetField("headlineText", SF).GetValue(null);

    static void Frame(float dt)
    {
        UnityEngine.Time.deltaTime = dt;
        UnityEngine.Time.time += dt;
        MonoBehaviour.Tick();
        foreach (var go in GameObject.All.ToList())
        {
            if (!go.activeInHierarchy) continue;
            foreach (var c in go.GetComponents())
            {
                if (!(c is MonoBehaviour)) continue;
                foreach (var name in new[] { "Update", "LateUpdate" })
                {
                    var m = c.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (m != null) m.Invoke(c, null);
                }
            }
        }
    }

    static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "sim";
        // Tools/Harness/bin/Debug/net8.0 → Assets/Resources
        Resources.Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "Resources"));
        if (mode == "art") return Art(args.Length > 1 ? args[1] : ".");

        int games = args.Length > 1 ? int.Parse(args[1]) : 200;
        int seed = args.Length > 2 ? int.Parse(args[2]) : 1;
        UnityEngine.Random.Rng = new System.Random(seed);
        var rnd = new System.Random(seed + 7);

        Game.Init();
        Music.Init();
        UI.Build();
        SaveGame.Delete();

        var visited = new Dictionary<string, int>();
        var endings = new Dictionary<string, int>();
        var problems = new Dictionary<string, int>();
        void Problem(string p) { problems[p] = problems.TryGetValue(p, out var n) ? n + 1 : 1; }
        var unresolved = new Regex(@"\{[^{}\s]*\}");

        for (int g = 0; g < games; g++)
        {
            G.S = new GameState();
            Game.Render("intro");
            string last = null;
            int steps = 0, idle = 0;
            double casinoBias = rnd.NextDouble(); // склонность к казино у этого «игрока»
            if (rnd.NextDouble() < 0.4) casinoBias *= 0.15;
            bool smart = rnd.NextDouble() < 0.7;
            grocerySeen.Clear();
            while (steps < 4000)
            {
                Frame(0.05f);
                var s = G.S.scene;
                if (s != last)
                {
                    visited[s] = visited.TryGetValue(s, out var v) ? v + 1 : 1;
                    last = s;
                }
                var txt = Body().text + " " + Head().text;
                foreach (Match m in unresolved.Matches(txt)) Problem("unresolved " + m.Value + " in " + s);

                if (Game.IsEndingScene(s) && Buttons().Count > 0)
                {
                    endings[s] = endings.TryGetValue(s, out var e) ? e + 1 : 1;
                    break;
                }
                if (UI.ModalOpen)
                {
                    UI.CloseModal();
                    continue;
                }
                if (s == "name_input")
                {
                    G.S.gender = rnd.Next(2) == 0 ? "m" : "f";
                    G.S.playerName = "Тест";
                    Game.Render(Data.Scene("name_input").next);
                    continue;
                }
                var input = GameObject.All.Where(o => o.activeInHierarchy).Select(o => o.GetComponent<InputField>())
                    .FirstOrDefault(f => f != null && f.interactable && f.contentType == InputField.ContentType.IntegerNumber);
                if (input != null && input.text == "")
                    input.text = (rnd.Next(2) == 0 ? B.PASHA_MIN_LOAN : rnd.Next(100, Math.Max(101, G.S.money + 200))).ToString();

                var btns = Buttons().Where(b => !b.Disabled).ToList();
                var board = (RectTransform)typeof(UI).GetField("boardArea", SF).GetValue(null);
                // «Умный» игрок проходит мини-игры подработок правильно (в 90% случаев)
                if (s.StartsWith("job_") && board.gameObject.activeSelf && smart)
                {
                    int r = Smart(s, board, btns, rnd);
                    if (r != 0) { if (r == 1) steps++; continue; }
                }
                // Кнопки на игровом поле: карточки команд, товары лавки
                var boardBtns = Desc(board).Select(t => t.gameObject.GetComponent<Button>())
                    .Where(b => b != null && b.interactable && b.gameObject.activeInHierarchy).ToList();
                if (boardBtns.Count > 0 && (btns.Count == 0 || rnd.NextDouble() < 0.5))
                {
                    if (rnd.NextDouble() < 0.5) continue;
                    try { boardBtns[rnd.Next(boardBtns.Count)].onClick.Invoke(); }
                    catch (Exception ex) { Problem("board exception at " + s + ": " + ex.Message + " " + (ex.StackTrace ?? "").Split('\n').FirstOrDefault(l => l.Contains("LastBet"))?.Trim()); break; }
                    continue;
                }
                if (btns.Count == 0)
                {
                    bool timers = MonoBehaviour.Running.Any(c => !c.dead);
                    if (!timers && ++idle > 40) { Problem("stuck at " + s); break; }
                    continue;
                }
                idle = 0;
                if (rnd.NextDouble() < 0.5) continue; // в мини-играх даём поработать таймерам
                ChoiceButton pick;
                var casino = btns.Where(b => Label(b).Contains("Постав") || Label(b).Contains("Крутить") || (Label(b).Contains("казино") && !Label(b).Contains("Выйти"))).ToList();
                if (casino.Count > 0 && rnd.NextDouble() < casinoBias) pick = casino[rnd.Next(casino.Count)];
                else pick = btns[rnd.Next(btns.Count)];
                try { pick.Click(); }
                catch (Exception ex)
                {
                    var where = (ex.StackTrace ?? "").Split('\n').FirstOrDefault(l => l.Contains("LastBet"));
                    Problem("exception at " + s + ": " + ex.GetType().Name + " " + ex.Message + " " + where?.Trim());
                    break;
                }
                steps++;
            }
            if (steps >= 4000) Problem("too long, ended at " + G.S.scene + " day " + G.S.day);
        }

        foreach (var e in UnityEngine.Debug.Errors.GroupBy(x => x.Split('\n')[0] + " " + x.Split('\n').Skip(1).FirstOrDefault(l => l.Contains("LastBet"))?.Trim()).OrderByDescending(x => x.Count()).Take(30))
            Console.WriteLine("LOG x" + e.Count() + ": " + e.Key);
        Console.WriteLine("== problems");
        foreach (var p in problems.OrderByDescending(p => p.Value).Take(40)) Console.WriteLine("x" + p.Value + ": " + p.Key);
        Console.WriteLine("== endings");
        foreach (var e in endings) Console.WriteLine(e.Key + ": " + e.Value);
        var never = Data.story.Keys.Where(k => !visited.ContainsKey(k)).ToList();
        Console.WriteLine("== visited " + visited.Count + "/" + Data.story.Count + "; never: " + string.Join(" ", never));
        return 0;
    }

    static readonly List<Sprite> grocerySeen = new List<Sprite>();

    // 1 — нажал, 2 — ждёт (ничего не трогать), 0 — не знает, что делать
    static int Smart(string s, RectTransform board, List<ChoiceButton> btns, System.Random rnd)
    {
        var texts = Desc(board).Select(t => t.gameObject.GetComponent<Text>()).Where(t => t != null && t.gameObject.activeInHierarchy).Select(t => t.text).ToList();
        bool right = rnd.NextDouble() < 0.9;
        if (s == "job_flyers")
        {
            var give = btns.FirstOrDefault(b => Label(b).Contains("листовку"));
            if (give == null) return 0;
            var p = Data.C.FLYER_PASSERS.FirstOrDefault(x => texts.Contains(Txt.Clean(x.desc)));
            if (p == null) return 2;
            if (p.ok == right) { give.Click(); return 1; }
            return 2;
        }
        if (s == "job_courier")
        {
            var label = texts.FirstOrDefault(t => t.StartsWith("ул. "));
            if (label == null || btns.Count == 0) return 2;
            var street = label.Substring(4).Split(',')[0];
            var d = Data.C.COURIER_DISTRICTS.First(x => x.streets.Contains(street));
            var b = btns.FirstOrDefault(x => Label(x).Contains(d.name) == right);
            if (b == null || rnd.NextDouble() < 0.6) return 2;
            b.Click();
            return 1;
        }
        if (s == "job_cafe")
        {
            var rec = texts.FirstOrDefault(t => t.Contains("Даёт:"));
            if (rec == null || btns.Count == 0) return 2;
            int total = Regex.Matches(rec, @"— (\d+) ₽").Select(m => int.Parse(m.Groups[1].Value)).Sum();
            int bill = int.Parse(Regex.Match(rec, @"Даёт: (\d+)").Groups[1].Value);
            var b = btns.FirstOrDefault(x => Label(x).Contains("Сдача " + (bill - total) + " ") == right);
            if (b == null || rnd.NextDouble() < 0.6) return 2;
            b.Click();
            return 1;
        }
        if (s == "job_grocery")
        {
            var order = Desc(board).FirstOrDefault(t => t.gameObject.name == "Order");
            var grid = Desc(board).FirstOrDefault(t => t.gameObject.name == "Grid");
            if (order == null || grid == null) return 0;
            var gridBtns = Desc(grid).Select(t => t.gameObject.GetComponent<Button>()).Where(b => b != null).ToList();
            bool picking = gridBtns.Any(b => b.interactable);
            if (!picking)
            {
                var shown = Desc(order).Where(t => t.gameObject.name == "P").Select(t => t.gameObject.GetComponent<Image>().sprite).ToList();
                if (shown.Count > 0 && Desc(order).All(t => t.gameObject.GetComponent<Text>() == null)) { grocerySeen.Clear(); grocerySeen.AddRange(shown); }
                return 2;
            }
            if (rnd.NextDouble() < 0.6) return 2;
            var want = gridBtns.Where(b => b.interactable && grocerySeen.Contains(Desc(b.transform).First(t => t.gameObject.name == "Icon").gameObject.GetComponent<Image>().sprite) == right).ToList();
            if (want.Count == 0) return 2;
            want[rnd.Next(want.Count)].onClick.Invoke();
            return 1;
        }
        return 0;
    }

    static IEnumerable<Transform> Desc(Transform t)
    {
        foreach (Transform c in t)
        {
            yield return c;
            foreach (var d in Desc(c)) yield return d;
        }
    }

    static string Label(ChoiceButton b) => ((Text)typeof(ChoiceButton).GetField("label", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(b)).text;

    // ===== Арт: сохранить картинки в PNG =====
    static int Art(string outDir)
    {
        Game.Init();
        Directory.CreateDirectory(outDir);
        foreach (var name in Backgrounds.All) Png(Backgrounds.GetNow(name), Path.Combine(outDir, "bg_" + name + ".png"));
        foreach (var kv in Data.characters)
            foreach (var mood in new[] { "normal", "happy", "sad", "angry" })
            {
                if (mood != "normal" && kv.Key != "hero_m" && kv.Key != "hero_f" && kv.Key != "pasha") continue;
                Png(CharacterArt.Draw(kv.Value, mood, default), Path.Combine(outDir, "ch_" + kv.Key + "_" + mood + ".png"));
            }
        Png(CharacterArt.Draw(Data.characters["hero_m"], "normal", new CharacterArt.Look { tired = true, pale = true, blink = true }), Path.Combine(outDir, "ch_hero_m_tired_blink.png"));
        Png(CharacterArt.Draw(Data.characters["pasha"], "happy", new CharacterArt.Look { talk = true }), Path.Combine(outDir, "ch_pasha_talk.png"));
        Png(GameArt.Wheel().texture, Path.Combine(outDir, "g_wheel.png"));
        foreach (var s in Data.C.SLOT_SYMBOLS) Png(GameArt.Slot(s).texture, Path.Combine(outDir, "g_slot" + Data.C.SLOT_SYMBOLS.IndexOf(s) + ".png"));
        foreach (var id in Data.C.ITEMS.Keys) Png(GameArt.Item(id).texture, Path.Combine(outDir, "g_item_" + id + ".png"));
        foreach (var p in Data.C.GROCERY_PRODUCTS) Png(GameArt.Product(p).texture, Path.Combine(outDir, "g_prod" + Data.C.GROCERY_PRODUCTS.IndexOf(p) + ".png"));
        foreach (var n in new[] { "day", "money", "addiction", "satiety", "bag", "soundOn", "soundOff", "restart" }) Png(GameArt.Icon(n).texture, Path.Combine(outDir, "g_icon_" + n + ".png"));
        Png(GameArt.Rocket().texture, Path.Combine(outDir, "g_rocket.png"));
        Png(GameArt.Boom().texture, Path.Combine(outDir, "g_boom.png"));
        Png(GameArt.Parcel().texture, Path.Combine(outDir, "g_parcel.png"));
        Png(GameArt.CrashSky().texture, Path.Combine(outDir, "g_sky.png"));
        foreach (var e in UnityEngine.Debug.Errors) Console.WriteLine(e);
        Console.WriteLine("done");
        return 0;
    }

    // PNG без библиотек: RGBA, zlib
    static void Png(Texture2D t, string path)
    {
        int w = t.width, h = t.height;
        var raw = new byte[(w * 4 + 1) * h];
        for (int y = 0; y < h; y++)
        {
            int row = y * (w * 4 + 1);
            int srcY = h - 1 - y; // текстура хранится снизу вверх
            for (int x = 0; x < w; x++)
            {
                var c = t.pixels[srcY * w + x];
                int i = row + 1 + x * 4;
                raw[i] = (byte)(Mathf.Clamp01(c.r) * 255); raw[i + 1] = (byte)(Mathf.Clamp01(c.g) * 255);
                raw[i + 2] = (byte)(Mathf.Clamp01(c.b) * 255); raw[i + 3] = (byte)(Mathf.Clamp01(c.a) * 255);
            }
        }
        using var fs = File.Create(path);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, w); BE(ihdr, 4, h); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(fs, "IHDR", ihdr);
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.Fastest, true)) z.Write(raw);
            Chunk(fs, "IDAT", ms.ToArray());
        }
        Chunk(fs, "IEND", new byte[0]);
    }

    static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        var td = System.Text.Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(td);
        var crc = new byte[4]; BE(crc, 0, (int)Crc(td)); s.Write(crc);
    }

    static uint Crc(byte[] d)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in d)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }
        return c ^ 0xFFFFFFFF;
    }
}
