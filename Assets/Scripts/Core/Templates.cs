// Шаблоны в текстах. Общие ({name}, {money}, ...) берутся из состояния и настроек,
// {мужской|женский} — слово по полу героя: «Ты {бледный|бледная}».
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Tpl
{
    static GameState S => G.S;

    static string Boss(string flyers, string grocery, string courier, string cafe) =>
        S.lastJob == "grocery" ? grocery : S.lastJob == "courier" ? courier : S.lastJob == "cafe" ? cafe : flyers;

    static readonly Dictionary<string, Func<object>> T = new Dictionary<string, Func<object>>
    {
        { "name", () => string.IsNullOrEmpty(S.playerName) ? "друг" : S.playerName },
        { "money", () => S.money },
        { "day", () => S.day },
        { "addiction", () => S.addiction },
        { "satiety", () => S.satiety },
        { "itemsTotal", () => Data.C.ITEMS.Count },

        // Питание и карманные
        { "lunchPrice", () => B.SCHOOL_LUNCH_PRICE },
        { "halfPrice", () => B.LUNCH_HALF_PRICE },
        { "snackPrice", () => B.SNACK_PRICE },
        { "lunchSatiety", () => Rules.LunchSatiety("full") },
        { "halfSatiety", () => Rules.LunchSatiety("half") },
        { "snackSatiety", () => B.SNACK_SATIETY },
        { "mealSatiety", () => B.MEAL_SATIETY },
        { "dishName", () => Data.Scene("school_lunch").Sub("dishes", Rules.TodayDish().id) },
        { "dishLine", () => Data.Scene("school_lunch").Sub("dishTexts", Rules.TodayDish().id) },
        { "weekday", () => Data.TList("weekdays")[Rules.WeekdayIndex()] },
        { "friendName", () => S.sundayCompany == "max" ? "Макс" : S.sundayCompany == "pasha" ? "Паша" : "кто-то из ребят" },
        { "lotteryCost", () => B.SUNDAY_LOTTERY_COST },
        { "arcadeCost", () => B.SUNDAY_ARCADE_COST },
        { "windowCost", () => B.SUNDAY_WINDOW_COST },
        { "weekdayLower", () => Data.TList("weekdays")[Rules.WeekdayIndex()].ToLower() },
        { "pocketMoney", () => B.DAILY_POCKET_MONEY },
        { "punishDaysLeft", () => S.punishedUntil - S.day + 1 },
        { "giftMoney", () => B.PARENTS_GIFT_MONEY },

        // Казино
        { "lastBet", () => S.lastBet },
        { "lossDiff", () => Math.Abs(S.money - B.START_MONEY) },
        { "jackpotPayout", () => S.lastPayout },
        { "jackpotMult", () => S.lastJackpotMult },
        { "allBalance", () => Rules.BetBalance() },
        { "bonusLine", () => S.bonus > 0 ? Fill(Data.T("casino.bonusLine"), "bonus", S.bonus, "wager", S.bonusWager, "cashMax", B.BONUS_CASHOUT_MAX) : "" },
        { "firstBonus", () => S.bonus },
        { "lastBonus", () => S.lastBonus },
        { "bonusWagerX", () => B.BONUS_WAGER },
        { "bonusCashMax", () => B.BONUS_CASHOUT_MAX },
        { "bonusDays", () => B.BONUS_DAYS },
        { "conscience", () => S.conscience },

        // Паша, гопники, папа
        { "pashaDebt", () => S.pashaDebt },
        { "pashaMinLoan", () => B.PASHA_MIN_LOAN },
        { "loanAmount", () => B.GOPNIK_LOAN_AMOUNT },
        { "loanReturn", () => B.GOPNIK_LOAN_RETURN },
        { "loanTaken", () => S.loanTaken },
        { "loanPaid", () => S.loanPaid },
        { "loanLeft", () => B.GOPNIK_LOAN_RETURN - S.loanPaid },
        { "stolenItem", () => S.robbery != null ? S.robbery.item ?? "" : "" },
        { "stolenMoney", () => S.robbery != null ? S.robbery.money : 0 },
        { "confiscated", () => string.Join(", ", S.confiscated.Select(i => i.name.ToLower())) },
        { "dadKnows", () => S.dadHeardFrom == "car"
            ? "— Я всю дорогу думал. Про машину, про пакет. Маме я рассказал."
            : "— Мама мне всё рассказала." },
        { "walkLongSatiety", () => B.WALK_LONG_SATIETY },
        { "garageTip", () => B.GARAGE_TIP },
        { "phoneLockDays", () => B.PHONE_LOCK_DAYS },
        { "grandmaGift", () => B.GRANDMA_GIFT },
        { "uncleGift", () => B.UNCLE_GIFT },
        { "uncleTotal", () => B.UNCLE_GIFT + B.UNCLE_WORK_BONUS },
        { "auntMoney", () => B.AUNT_PARCEL_MONEY },
        { "dadBonus", () => B.DAD_BONUS_GIFT },
        { "cleanDays", () => S.cleanDays },
        { "jobBossGen", () => Boss("бригадира", "тёти Заремы", "начальника склада", "Ашота") },
        { "triedTimes", () => S.betsMade == 1 ? "один раз" : "пару раз" },
        { "jobPayAdvanced", () => B.JOB_PAY_ADVANCED },
        { "shiftPay", () => S.lastShiftPay },
        { "jobTip", () => B.JOB_TIP },
        { "walletMoney", () => B.WALLET_MONEY },
        { "teacherBuys", () => S.lastJob == "grocery" ? "пакет яблок" : "листовку" },
        { "jobBonus", () => B.JOB_BONUS },
        { "masteryWins", () => B.JOB_MASTERY_WINS },

        // Кражи
        { "commissionPaid", () => S.commissionPaid },
        { "commissionFine", () => B.THEFT_COMMISSION_FINE },
        { "theftStore", () => S.theftStore == "tech" ? "электроники" : "одежды" },
        { "theftItem", () => S.theftItemId != null ? Data.Item(S.theftItemId).name.ToLower() : "" },

        // Подработки
        { "jobPay", () => B.JOB_PAY },
        { "jobFailsLeft", () => S.lastJob != null ? B.JOB_MAX_FAILS - S.jobFails[S.lastJob] : B.JOB_MAX_FAILS },
        { "flyersGoal", () => B.FLYERS_GOAL },
        { "flyersRefusals", () => B.FLYERS_MAX_REFUSALS },
        { "flyersPassers", () => B.FLYERS_PASSERS },
        { "flyersPassSec", () => Txt.Sec(B.FLYERS_PASS_MS) },
        { "flyersYes", () => string.Join("\n", Data.C.FLYER_PASSERS.Where(p => p.ok).Select(p => "• " + p.who)) },
        { "flyersNo", () => string.Join("\n", Data.C.FLYER_PASSERS.Where(p => !p.ok).Select(p => "• " + p.who)) },
        { "groceryGoal", () => B.GROCERY_GOAL },
        { "groceryRoundsCount", () => B.GROCERY_ROUNDS.Length },
        { "groceryRounds", () => string.Join(", ", B.GROCERY_ROUNDS.Take(B.GROCERY_ROUNDS.Length - 1)) + " и " + B.GROCERY_ROUNDS[B.GROCERY_ROUNDS.Length - 1] },
        { "groceryShowSec", () => Txt.Sec(B.GROCERY_SHOW_MS) },
        { "jobPlace", () => Boss("в жёлтой жилетке у метро", "в фартуке за прилавком", "в синей куртке у пункта выдачи", "за кассой шаурмичной") },
        { "jobBoss", () => Boss("Бригадир", "Тётя Зарема", "Начальник склада", "Ашот") },
        { "courierParcels", () => B.COURIER_PARCELS },
        { "courierErrors", () => B.COURIER_MAX_ERRORS },
        { "courierSec", () => Txt.Sec(B.COURIER_PARCEL_MS) },
        { "courierLegend", () => string.Join("\n", Data.C.COURIER_DISTRICTS.Select(d => d.emoji + " " + d.name + ": " + string.Join(", ", d.streets))) },
        { "cafeCustomers", () => B.CAFE_CUSTOMERS },
        { "cafeErrors", () => B.CAFE_MAX_ERRORS },
        { "cafeSec", () => Txt.Sec(B.CAFE_ANSWER_MS) },

        // Местоимения
        { "he", () => S.gender == "f" ? "она" : "он" },
        { "his", () => S.gender == "f" ? "её" : "его" },
        { "him", () => S.gender == "f" ? "её" : "его" },
    };

    static readonly Regex KeyRe = new Regex(@"\{(\w+)\}");
    static readonly Regex GenderRe = new Regex(@"\{([^{}|]*)\|([^{}|]*)\}");

    public static string Apply(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        text = KeyRe.Replace(text, m => T.TryGetValue(m.Groups[1].Value, out var f) ? Convert.ToString(f()) : m.Value);
        return GenderRe.Replace(text, m => S.gender == "f" ? m.Groups[2].Value : m.Groups[1].Value);
    }

    // Подставляет свои значения ({amount}, {team}, ...), а затем общие шаблоны
    public static string Fill(string text, Dictionary<string, object> vars)
    {
        if (text == null) return "";
        if (vars != null)
            text = KeyRe.Replace(text, m => vars.TryGetValue(m.Groups[1].Value, out var v) ? Convert.ToString(v) : m.Value);
        return Apply(text);
    }

    // Короткая запись: Fill(text, "amount", 100, "team", "...")
    public static string Fill(string text, params object[] kv)
    {
        var vars = new Dictionary<string, object>();
        for (int i = 0; i + 1 < kv.Length; i += 2) vars[(string)kv[i]] = kv[i + 1];
        return Fill(text, vars);
    }

    public static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text.Substring(1);
}
