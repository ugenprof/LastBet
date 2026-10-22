// Слоты (глава 2): три барабана, символы с весами (SLOT_SYMBOLS). Три одинаковых — выплата символа,
// две вишни — ×SLOTS_TWO_CHERRIES. RTP ≈ 91%. Три семёрки — джекпот (один раз за игру).
// Проигрыш часто показывается как «почти выиграл» (две семёрки) — именно так автоматы и цепляют.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class Slots
{
    static GameState S => G.S;
    static List<SlotSymbol> SYM => Data.C.SLOT_SYMBOLS;

    static Image[] reels;
    static int run;

    public static void Register()
    {
        Game.Screens["bet_slots"] = _ => RenderScreen();
    }

    static void RenderScreen()
    {
        if (Rules.BetBalance() <= 0)
        {
            Game.Render("no_money");
            return;
        }
        S.lastGame = "bet_slots";
        UI.SetText(Tpl.Fill(Data.T("slots.info"), "mood", Casino.MoodText(), "paytable", Paytable()));
        DrawMachine();
        UI.ClearChoices();
        Casino.AddBetButtons(B.SLOTS_BET_AMOUNTS, Spin);
        Casino.AddNavButtons();
    }

    // Таблица выплат словами: эмодзи шрифт не рисует
    static string Paytable()
    {
        var names = Data.TList("slotsUnity.names");
        var rows = Enumerable.Range(0, SYM.Count).Reverse().Select(i => names[i] + " ×3 → ×" + SYM[i].pays);
        return string.Join("\n", rows) + "\n" + Tpl.Fill(Data.T("slotsUnity.twoCherries"), "mult", B.SLOTS_TWO_CHERRIES);
    }

    static void DrawMachine()
    {
        var board = UI.ShowBoard();
        var body = UI.Panel(board, "Machine", new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.8f), Theme.SlotBody);
        UI.Label(body, Data.T("slotsUnity.title"), 34, Theme.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0.82f), new Vector2(1, 0.98f)).fontStyle = FontStyle.Bold;
        reels = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.06f + i * 0.3f;
            var frame = UI.Panel(body, "Reel" + i, new Vector2(x0, 0.12f), new Vector2(x0 + 0.28f, 0.78f), Theme.ReelFace);
            reels[i] = UI.Image(frame, "Symbol", GameArt.Slot(Pick()), new Vector2(18, 18), new Vector2(-18, -18));
            reels[i].preserveAspect = true;
        }
    }

    static SlotSymbol Pick()
    {
        int total = SYM.Sum(s => s.weight);
        float r = Rules.Rand() * total;
        foreach (var s in SYM)
        {
            r -= s.weight;
            if (r < 0) return s;
        }
        return SYM[0];
    }

    // Выплата за комбинацию: три одинаковых — pays символа, две вишни — ×SLOTS_TWO_CHERRIES, иначе 0
    static int Payout(List<SlotSymbol> r)
    {
        if (r[0] == r[1] && r[1] == r[2]) return r[0].pays;
        return r.Count(s => s == SYM[0]) == 2 ? B.SLOTS_TWO_CHERRIES : 0;
    }

    static bool IsJackpot(List<SlotSymbol> r) => r.All(s => s.jackpot);

    static List<SlotSymbol> LosingReels()
    {
        var seven = SYM.First(s => s.jackpot);
        if (Rules.Rand() < B.SLOTS_NEAR_MISS)
        {
            var others = SYM.Where(s => s != seven && s != SYM[0]).ToList();
            return Rules.Shuffle(new[] { seven, seven, Rules.Pick(others) });
        }
        List<SlotSymbol> reels;
        do reels = new List<SlotSymbol> { Pick(), Pick(), Pick() };
        while (Payout(reels) > 0);
        return reels;
    }

    static List<SlotSymbol> Roll(string forced)
    {
        if (forced == "loss") return LosingReels();
        if (forced == "win") return Rules.Shuffle(new[] { SYM[0], SYM[0], SYM[1] }); // «новичкам везёт» — две вишни
        var r = new List<SlotSymbol> { Pick(), Pick(), Pick() };
        if (Payout(r) == 0) return r;
        // Подкрутка с RIGGING_START_DAY; джекпот — один на игру
        if (Rules.Rand() > Casino.WinFactor()) return LosingReels();
        if (IsJackpot(r) && S.jackpotWon) return LosingReels();
        return r;
    }

    static void Spin(int amount)
    {
        if (amount <= 0 || amount > Rules.BetBalance()) return;
        int myRun = ++run;
        var result = Roll(Casino.TakeBet(amount));
        var imgs = reels;
        UI.SetText(Tpl.Fill(Data.T("slots.spinning"), "amount", amount));

        // Пока барабан крутится — символы мелькают; останавливаются по очереди
        var stopped = new bool[3];
        Run.Every(0.07f, () =>
        {
            if (myRun != run) return false;
            for (int i = 0; i < 3; i++)
                if (!stopped[i] && imgs[i] != null) imgs[i].sprite = GameArt.Slot(SYM[Rules.RandInt(SYM.Count)], true);
            return !stopped.All(x => x);
        });
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            Run.After(0.6f + k * 0.45f, () =>
            {
                if (myRun != run || imgs[k] == null) return;
                stopped[k] = true;
                imgs[k].sprite = GameArt.Slot(result[k]);
                UI.Bump(imgs[k].rectTransform);
            });
        }

        Run.After(0.6f + 3 * 0.45f + 0.3f, () =>
        {
            if (myRun != run) return;
            int mult = Payout(result);
            var names = Data.TList("slotsUnity.names");
            string combo = string.Join(" ", result.Select(s => names[SYM.IndexOf(s)]));
            if (mult == 0) Casino.FinishLoss(amount);
            else if (IsJackpot(result)) Casino.WinBigJackpot(amount, mult);
            else Casino.FinishWin(amount * mult, Tpl.Fill(Data.T("slots.win"), "combo", combo, "mult", mult));
        });
    }
}
