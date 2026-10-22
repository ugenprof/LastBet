// Общие правила игры, которыми пользуются разные сцены и игры.
using System;
using System.Collections.Generic;
using System.Linq;

public static class Rules
{
    static GameState S => G.S;

    // Как Math.random(): [0, 1)
    public static float Rand()
    {
        float v = UnityEngine.Random.value;
        return v >= 1f ? 0.9999999f : v;
    }
    public static int RandInt(int n) => Math.Min(n - 1, (int)(UnityEngine.Random.value * n));
    public static T Pick<T>(IList<T> list) => list[RandInt(list.Count)];

    public static List<T> Shuffle<T>(IEnumerable<T> list)
    {
        var a = new List<T>(list);
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = RandInt(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
        return a;
    }

    public static T Last<T>(List<T> list) where T : class => list.Count > 0 ? list[list.Count - 1] : null;

    // ===== ПОКАЗАТЕЛИ =====
    public static void ChangeAddiction(int delta) => S.addiction = Math.Min(B.ADDICTION_MAX, Math.Max(0, S.addiction + delta));
    public static void ChangeSatiety(int delta) => S.satiety = Math.Min(B.SATIETY_MAX, Math.Max(0, S.satiety + delta));

    // ===== ВЕЩИ =====
    // Вещь «есть», если она в рюкзаке или её запер папа (вернётся после наказания)
    public static bool HasItem(string id) => S.inventory.Any(i => i.id == id) || S.confiscated.Any(i => i.id == id);

    // Цель игры: все вещи из магазинов в рюкзаке
    public static bool HasAllItems() => Data.C.ITEMS.Keys.All(id => S.inventory.Any(i => i.id == id));

    public static void GiveItem(string id, string source = "bought")
    {
        var def = Data.Item(id);
        S.inventory.Add(new InvItem { id = id, name = def.name, price = def.price, boughtDay = S.day, source = source });
    }

    // ===== НЕДЕЛЯ =====
    public static int WeekdayIndex() => WeekdayIndex(S.day);
    public static int WeekdayIndex(int day) => (day - 1) % 7; // 0 — понедельник
    public static int WeekNumber() => (S.day - 1) / 7;
    public static bool IsDayOff() => WeekdayIndex() == B.DAY_OFF_WEEKDAY;

    // ===== СЛУЧАЙНЫЕ СОБЫТИЯ =====
    // Родственники утром, события на работе, гараж, соблазны в школе: не больше одного за день
    public static bool CanHaveRandomEvent() => S.randomEventDay != S.day;
    public static void MarkRandomEvent() => S.randomEventDay = S.day;

    // ===== ОБЕД =====
    public static bool MomLunchToday() => S.momPacksLunch && B.MOM_LUNCH_WEEKDAYS.Contains(WeekdayIndex());

    public static LunchDish TodayDish() => Data.C.LUNCH_DISHES[Math.Min(WeekdayIndex(), Data.C.LUNCH_DISHES.Count - 1)];

    public static int LunchPrice(string kind) =>
        kind == "full" ? B.SCHOOL_LUNCH_PRICE : kind == "half" ? B.LUNCH_HALF_PRICE : B.SNACK_PRICE;

    public static int LunchSatiety(string kind) =>
        kind == "full" ? TodayDish().satiety : kind == "half" ? TodayDish().satiety - B.LUNCH_HALF_LESS : B.SNACK_SATIETY;

    // ===== ЛЮДИ =====
    // Макс — друг: признался ему, заступился за Пашу или помог Паше. Посмеялся / огрызнулся — нет
    public static bool MaxIsFriend()
    {
        if (S.classReaction == "laugh" || (S.maxTalked && !S.maxSupport)) return false;
        return S.maxSupport || S.classReaction == "defend" || S.pashaSaved;
    }

    // Родители верят, пока герой не врёт им раз за разом
    public static bool ParentsTrust() => S.lies < B.PARENTS_TRUST_LIES;

    // Попробовал пару раз и остановился — честно может так и сказать
    public static bool TriedAndStopped() => S.betsMade > 0 && S.betsMade <= B.FEW_BETS && S.addiction < B.ADDICTION_LOW;

    // Выигрыш (+) или проигрыш (−) на ставках за всю игру
    public static int CasinoNet() => S.totalWon - S.totalStaked;

    // Что можно попытаться стащить: в одиночку — только то, чего ещё нет и что ещё не пытался украсть
    public static List<string> StealableItems(List<string> ids, bool solo) =>
        solo ? ids.Where(id => !HasItem(id) && !S.theftAttempted.Contains(id)).ToList() : new List<string>(ids);

    // ===== КАЗИНО =====
    public static readonly string[] CASINO_SCENES = { "first_bet", "casino_lobby", "bet_again", "bet_roulette", "bet_match", "bet_slots", "bet_crash" };
    public static bool IsCasinoChoice(Choice c) => c.next != null && CASINO_SCENES.Contains(c.next);

    // Удалить приложение — с ночи 5-го дня, пока зависимость не зашла далеко. Это не концовка
    public static bool CanQuitApp() => !S.appDeleted && S.day >= B.PASHA_ARREST_DAY && S.addiction < B.ADDICTION_CAN_QUIT;

    // Телефон у папы — в казино не попасть
    public static bool PhoneLocked() => S.day <= S.phoneLockedUntil;

    // Сорвался: снова скачал приложение — счёт «чистых» дней с нуля
    public static void Relapse()
    {
        S.appDeleted = false;
        S.cleanDays = 0;
        S.relapses++;
        ChangeAddiction(B.RELAPSE_ADDICTION);
    }

    // Вещь из подарка, купленная вчера (реплики друзей)
    public static InvItem YesterdayPurchase() =>
        S.inventory.LastOrDefault(i => i.source == "bought" && i.boughtDay == S.day - 1);

    public static InvItem BoughtToday() =>
        S.inventory.LastOrDefault(i => i.source == "bought" && i.boughtDay == S.day);

    // Займ у гопников — для итогов
    public static string DescribeGopnikLoan()
    {
        if (S.loanDay == 0) return "";
        if (S.loanActive) return "должен " + (B.GOPNIK_LOAN_RETURN - S.loanPaid) + " ₽";
        if (S.beatenDay != 0) return "не вернул — избили";
        return "вернул";
    }

    // ===== ИГРА =====
    public static void ResetGame()
    {
        G.S = new GameState();
    }
}
