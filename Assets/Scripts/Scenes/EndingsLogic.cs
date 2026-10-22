// Финалы: как герой пришёл к концу и чего это стоило. Победа описывает путь (откуда деньги на вещи),
// все финалы заканчиваются короткими итогами (story.ending_summary).
using System;
using System.Collections.Generic;
using System.Linq;

public static class EndingsLogic
{
    static GameState S => G.S;

    static readonly string[] SourceOrder = { "jobs", "casino", "theft", "savings" };
    static readonly Dictionary<string, string> MIX_NAMES = new Dictionary<string, string>
    {
        { "jobs", "смены на подработке" }, { "casino", "выигрыши" }, { "theft", "кражи" }, { "savings", "карманные" }
    };

    public static void Register()
    {
        Game.Texts["victory_items"] = scene =>
        {
            string path = EndingPath();
            var v = EndingVars();
            var costs = new List<string>();
            if ((path == "jobs" || path == "savings") && S.betsMade > 0) costs.Add(scene.Sub("costs", "fewBets"));
            if (S.inventory.Any(i => i.source == "gift")) costs.Add(scene.Sub("costs", "gift"));
            if (S.lies > 0) costs.Add(scene.Sub("costs", "lies"));
            if (S.hospitalVisits > 0) costs.Add(scene.Sub("costs", "hospital"));
            if (S.beatenDay != 0) costs.Add(scene.Sub("costs", "beaten"));
            if (S.pashaSaved) costs.Add(scene.Sub("costs", "pashaSaved"));
            else if (S.pashaLent) costs.Add(scene.Sub("costs", "pashaLent"));
            var parts = new List<string> { scene.text, scene.Sub("paths", path) };
            parts.AddRange(costs);
            parts.Add(scene.Sub("closings", path));
            return Tpl.Fill(string.Join("\n\n", parts), v) + "\n\n" + EndingSummary(true);
        };

        Func<SceneDef, string> withSummary = scene => Game.BaseText(scene) + "\n\n" + EndingSummary(false);
        Game.Texts["good_ending_epilogue"] = withSummary;
        Game.Texts["lose_school"] = withSummary;
        Game.Texts["lose_addiction"] = withSummary;
    }

    // ===== ПУТЬ К ВЕЩАМ =====
    // Доли источников: работа, выигрыши, кражи (стоимость украденного), карманные и подарки
    static int Value(string source) => S.inventory.Where(i => i.source == source).Sum(i => i.price);

    static Dictionary<string, float> EndingSources()
    {
        var sources = new Dictionary<string, float>
        {
            { "jobs", S.income.jobs },
            { "casino", Math.Max(0, Rules.CasinoNet()) },
            { "theft", Value("stolen") },
            { "savings", S.income.pocket + S.income.gift + S.income.sales + Value("gift") }
        };
        float total = sources.Values.Sum();
        if (total == 0) total = 1;
        return SourceOrder.ToDictionary(k => k, k => sources[k] / total);
    }

    // Заметный источник — от ENDING_PATH_SHARE; два заметных из «работа / ставки / кражи» — смешанный путь
    static string EndingPath()
    {
        var shares = EndingSources();
        var notable = new[] { "jobs", "casino", "theft" }.Where(k => shares[k] >= B.ENDING_PATH_SHARE).ToList();
        if (notable.Count >= 2) return "mixed";
        if (notable.Count == 1) return notable[0];
        return shares["jobs"] > shares["savings"] ? "jobs" : "savings";
    }

    // Спокойный путь: без краж, долгов, больниц и с низкой зависимостью. Возвращает список нарушений
    static List<string> CalmMisses()
    {
        var misses = new List<string>();
        if (S.theftsDone > 0 || S.theftCaughtCount > 0) misses.Add("кражи");
        if (S.loanDay != 0) misses.Add("долг гопникам");
        if (S.hospitalVisits > 0) misses.Add("больница");
        if (S.addiction >= B.ADDICTION_LOW) misses.Add("зависимость " + S.addiction);
        return misses;
    }

    static string BetsWord(int n)
    {
        int mod10 = n % 10, mod100 = n % 100;
        if (mod10 == 1 && mod100 != 11) return "ставка";
        if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return "ставки";
        return "ставок";
    }

    static string Signed(int n) => n > 0 ? "+" + n : n.ToString();

    // Значения для текстов финала
    static Dictionary<string, object> EndingVars()
    {
        var shares = EndingSources();
        int net = Rules.CasinoNet();
        return new Dictionary<string, object>
        {
            { "jobShifts", S.jobShifts },
            { "jobEarned", S.income.jobs },
            { "betsMade", S.betsMade },
            { "betsWord", BetsWord(S.betsMade) },
            { "casinoNet", net },
            { "casinoNetSigned", Signed(net) },
            { "stolenList", string.Join(", ", S.inventory.Where(i => i.source == "stolen").Select(i => i.name)) },
            { "mixList", string.Join(", ", MIX_NAMES.Keys.Where(k => shares[k] >= B.ENDING_PATH_SHARE / 2).Select(k => MIX_NAMES[k])) },
            { "lies", S.lies },
            { "theftsDone", S.theftsDone },
            { "caughtCount", S.theftCaughtCount },
            { "hospitalVisits", S.hospitalVisits },
            { "itemsCount", S.inventory.Count },
            { "fastDay", B.VICTORY_FAST_DAY },
            { "calmAddiction", B.ADDICTION_LOW },
            { "calmMiss", string.Join(", ", CalmMisses()) }
        };
    }

    // ===== ИТОГИ =====
    static string ItemsHow()
    {
        int Count(string source) => S.inventory.Count(i => i.source == source);
        var parts = new[] { ("куплено", Count("bought")), ("украдено", Count("stolen")), ("подарок", Count("gift")) }
            .Where(p => p.Item2 > 0).Select(p => p.Item1 + " " + p.Item2).ToList();
        return parts.Count > 0 ? " — " + string.Join(", ", parts) : "";
    }

    static string DebtLine()
    {
        var parts = new List<string>();
        if (S.loanDay != 0) parts.Add("гопникам — " + Rules.DescribeGopnikLoan());
        if (S.pashaLent) parts.Add("Паше дал{|а} " + S.pashaDebt + " ₽");
        return string.Join("; ", parts);
    }

    static string ParentsLine()
    {
        if (S.dadTalked) return "знают про ставки";
        if (S.betsMade > 0) return "не знают про ставки";
        return "доверяют";
    }

    static string FriendsLine()
    {
        var parts = new List<string>
        {
            Rules.MaxIsFriend() ? "Макс — друг" : S.classReaction == "laugh" || S.maxTalked ? "Макс отдалился" : "Макс — одноклассник"
        };
        if (S.pashaSaved) parts.Add("Паша дома — ты {привёл|привела} его к родителям");
        else if (S.day > B.PASHA_ARREST_DAY) parts.Add("Паша под судом");
        return string.Join(" · ", parts);
    }

    static string EndingSummary(bool withBadges)
    {
        var t = Data.Scene("ending_summary");
        var v = EndingVars();
        var items = new Dictionary<string, object>(v) { ["itemsHow"] = ItemsHow() };
        var lines = new List<string> { t.S("day"), Tpl.Fill(t.S("items"), items) };
        if (S.jobShifts > 0) lines.Add(t.S("jobs"));
        lines.Add(S.betsMade > 0 ? t.S("bets") : t.S("noBets"));
        if (S.theftsDone > 0 || S.theftCaughtCount > 0) lines.Add(t.S("thefts"));
        string debts = DebtLine();
        if (debts != "") lines.Add(Tpl.Fill(t.S("debts"), "debtLine", debts));
        lines.Add(t.S("health"));
        lines.Add(Tpl.Fill(t.S("family"), "parentsLine", ParentsLine()));
        lines.Add(Tpl.Fill(t.S("friends"), "friendsLine", FriendsLine()));
        lines.Add(t.S("addiction"));
        if (withBadges)
        {
            lines.Add("");
            lines.Add(S.day <= B.VICTORY_FAST_DAY ? t.S("fast") : t.S("fastMiss"));
            lines.Add(CalmMisses().Count > 0 ? t.S("calmMiss") : t.S("calm"));
        }
        return t.S("title") + "\n" + string.Join("\n", lines.Select(line => Tpl.Fill(line, v)));
    }
}
