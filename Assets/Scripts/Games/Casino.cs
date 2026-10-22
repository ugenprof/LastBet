// Казино: общее для всех игр — настроение, кнопки ставок, исход ставки, навигация, переходы лобби.
using System;
using System.Collections.Generic;
using System.Linq;

public static class Casino
{
    static GameState S => G.S;

    // ===== НАСТРОЕНИЕ ПО ЗАВИСИМОСТИ =====
    public static string MoodLevel()
    {
        if (S.addiction >= B.ADDICTION_MAX) return "max";
        if (S.addiction >= B.ADDICTION_NO_EXIT) return "noExit";
        if (S.addiction >= B.ADDICTION_ALL_IN) return "allIn";
        if (S.addiction >= B.ADDICTION_LOW) return "low";
        return "calm";
    }

    public static string MoodText() => Data.T("casino.mood." + MoodLevel());

    // ===== КНОПКИ =====
    // Суммы, которые можно поставить: из набора игры, «всё» — при высокой зависимости, при 100 — только «всё».
    // Ставить можно и с бонусного счёта (Rules.BetBalance). −1 — «всё»
    static List<int> BetOptions(int[] amounts)
    {
        if (S.addiction >= B.ADDICTION_MAX) return new List<int> { -1 };
        var options = amounts.Where(v => v <= Rules.BetBalance()).ToList();
        if (S.addiction > B.ADDICTION_ALL_IN) options.Add(-1);
        if (options.Count == 0) options.Add(Rules.BetBalance());
        return options;
    }

    public static void AddBetButtons(int[] amounts, Action<int> onBet)
    {
        foreach (var v in BetOptions(amounts))
        {
            string label;
            if (v == -1) label = Tpl.Apply(Data.T("casino.betAll"));
            else if (v == Rules.BetBalance() && v < B.BET_STEP) label = Tpl.Fill(Data.T("casino.betEverythingLeft"), "amount", v);
            else label = Tpl.Fill(Data.T("casino.bet"), "amount", v);
            int amount = v;
            UI.AddButton(label, () => onBet(amount == -1 ? Rules.BetBalance() : amount));
        }
    }

    // При зависимости 100: кнопка «Лечь спать» есть, но вместо сна — окно «хочу отыграться», и кнопка гаснет
    public static void AddFakeSleepButton()
    {
        ChoiceButton btn = null;
        btn = UI.AddButton(Data.Scene("compulsion_dialog").S("sleepText"), () =>
        {
            btn.Disabled = true;
            UI.OpenCompulsion();
        });
    }

    public static void AddNavButtons()
    {
        if (S.addiction >= B.ADDICTION_MAX)
        {
            AddFakeSleepButton();
            return;
        }
        if (S.betsMade == 0) return;
        UI.AddButton(Data.T("casino.otherGame"), () => Game.Render("casino_lobby"));
        if (S.addiction < B.ADDICTION_NO_EXIT) UI.AddButton(Data.T("casino.leave"), () => Game.Render("leave_casino"));
    }

    // ===== СТАВКА =====
    // Во сколько раз урезан шанс на выигрыш (1 — честно)
    public static float WinFactor()
    {
        int days = S.day - B.RIGGING_START_DAY + 1;
        if (days <= 0) return 1f;
        return Math.Max(B.RIGGING_MIN_FACTOR, 1f - B.RIGGING_STEP * days);
    }

    // Списывает ставку. Возвращает исход, навязанный правилами ("loss" / "win"), или null — решает рандом
    public static string TakeBet(int amount)
    {
        bool forcedLoss = S.addiction >= B.ADDICTION_MAX;

        int balanceBefore = Rules.BetBalance();
        S.totalStaked += Rules.SpendBet(amount); // в итогах — только свои деньги, бонусные не считаются
        S.betsMade++;
        Rules.ChangeAddiction(B.ADDICTION_PER_BET);
        S.lastBet = amount;
        S.wasAllIn = amount >= balanceBefore;
        UI.ClearChoices();
        UI.UpdateHud();

        if (forcedLoss) return "loss";
        if (S.betsMade <= B.NEWBIE_WINS) return "win";
        return null;
    }

    // Строка про бонус после ставки: отыгран и выведен на карту / проигран вместе с отыгрышем.
    // Она же повторяется в следующей сцене (результат ставки виден всего секунду)
    static string BonusResultLine(bool hadBonus)
    {
        int cash = Rules.SettleBonus();
        string line = "";
        if (cash > 0) line = "\n\n" + Tpl.Fill(Data.T("casino.bonusCashout"), "amount", cash, "cashMax", B.BONUS_CASHOUT_MAX);
        else if (hadBonus && S.bonus <= 0) line = "\n\n" + Data.T("casino.bonusLost");
        S.bonusAfterLine = line;
        return line;
    }

    static string TakeBonusAfterLine()
    {
        string line = S.bonusAfterLine ?? "";
        S.bonusAfterLine = "";
        return line;
    }

    // Проигрыш: короткая пауза с результатом, потом «после проигрыша» или «денег нет»
    public static void FinishLoss(int amount)
    {
        string bonusLine = BonusResultLine(S.bonusWager > 0 || S.bonusShare > 0);
        if (Rules.BetBalance() <= 0) S.lostEverything = true;
        string level = MoodLevel() == "low" ? "calm" : MoodLevel();
        UI.SetText(Tpl.Fill(Data.T("casino.lossResult"), "lossText", Data.T("casino.loss." + level), "amount", amount) + bonusLine);
        UI.UpdateHud();
        UI.ApplyMood();
        UI.SetBrightness(0.4f);
        UI.Shake();
        Run.After(1.4f, () =>
        {
            UI.SetBrightness(0.85f);
            Game.Render(Rules.BetBalance() <= 0 ? "no_money" : "after_loss");
        });
    }

    public static void FinishWin(int payout, string label)
    {
        S.totalWon += Rules.PayOut(payout);
        S.betsWon++;
        string bonusLine = BonusResultLine(false);

        string winText = Tpl.Fill(Data.T(S.addiction >= B.ADDICTION_ALL_IN ? "casino.winGreedy" : "casino.win"), "label", label);
        UI.SetText(Tpl.Fill(Data.T("casino.winResult"), "winText", winText, "payout", payout) + bonusLine);
        UI.UpdateHud();
        UI.ApplyMood();
        UI.SetBrightness(1.25f);
        UI.Coins();
        Run.After(1.4f, () =>
        {
            UI.SetBrightness(0.85f);
            Game.Render("after_win");
        });
    }

    // Джекпот (×20 в рулетке, три семёрки в слотах): +ADDICTION_JACKPOT, один раз за игру
    public static void WinBigJackpot(int amount, int mult)
    {
        int payout = amount * mult;
        S.totalWon += Rules.PayOut(payout);
        S.betsWon++;
        string bonusLine = BonusResultLine(false);
        S.lastPayout = payout;
        S.lastJackpotMult = mult;
        Rules.ChangeAddiction(B.ADDICTION_JACKPOT);
        S.hadJackpot = true;
        S.jackpotWon = true;
        UI.JackpotFlash(Tpl.Fill(Data.T("casino.jackpotFlash"), "mult", mult));

        UI.SetText(Tpl.Fill(Data.T("casino.jackpotResult"), "payout", payout, "mult", mult) + bonusLine);
        UI.UpdateHud();
        UI.ApplyMood();
        UI.SetBrightness(1.4f);
        Run.After(2f, () =>
        {
            UI.SetBrightness(0.85f);
            Game.Render("jackpot_win");
        });
    }

    public static void Register()
    {
        // «Первый депозит» — только в первый раз, дальше сразу в лобби. Бонус 100% (до FIRST_DEPOSIT_BONUS_MAX)
        // приходит на бонусный счёт — один раз за игру
        Game.Routes["first_bet"] = _ =>
        {
            if (S.betsMade == 0)
            {
                if (!S.firstBonusGiven)
                {
                    S.firstBonusGiven = true;
                    Rules.GrantBonus(Math.Min(S.money, B.FIRST_DEPOSIT_BONUS_MAX));
                }
                return false;
            }
            Game.Render("casino_lobby");
            return true;
        };

        // «Поставить ещё» — в ту игру, в которую играл последней
        Game.Routes["bet_again"] = _ =>
        {
            Game.Render(S.lastGame);
            return true;
        };

        // ===== ТЕКСТЫ ПОСЛЕ СТАВКИ =====
        Game.Texts["after_loss"] = scene =>
        {
            if (S.money > B.START_MONEY) return scene.S("textProfit") + TakeBonusAfterLine();
            if (S.money == B.START_MONEY) return scene.S("textEven") + TakeBonusAfterLine();
            return scene.text + TakeBonusAfterLine();
        };
        foreach (var id in new[] { "after_win", "no_money", "jackpot_win" })
            Game.Texts[id] = scene => Game.BaseText(scene) + TakeBonusAfterLine();

        Game.Texts["stop_but_drawn"] = scene =>
        {
            if (S.money > B.RICH_MONEY) return scene.S("textRich");
            if (S.wasAllIn) return scene.S("textAllIn");
            return scene.text;
        };

        // ===== ЛОББИ И ДЖЕКПОТ =====
        // С главы 2 в приложении новые игры
        Game.Builders["casino_lobby"] = (choices, _) => choices.Where(c => !c.newGame || S.chapter >= B.NEW_GAMES_CHAPTER).ToList();
        Game.Texts["casino_lobby"] = scene => S.chapter >= B.NEW_GAMES_CHAPTER ? scene.S("textNewGames") : scene.text;
        Game.Texts["jackpot_stop"] = scene => S.day >= B.PASHA_ARREST_DAY && !S.pashaSaved ? scene.S("textLater") : scene.text;
    }
}
