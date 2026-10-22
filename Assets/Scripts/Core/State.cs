// Всё состояние игры — единственный источник правды. Логика меняет только эти поля.
// Новая игра — new GameState() (как INITIAL_STATE в браузерной версии).
using System.Collections.Generic;

public class Income
{
    public int pocket = B.START_MONEY, gift, jobs, sales, loan;
}

public class Robbery
{
    public string item; // название вещи или null
    public int money;
}

public class MatchState
{
    public string[] teams = new string[2];
    public float[] chances = new float[2];
    public float[] odds = new float[2];
    public int pick = -1;
}

public class GameState
{
    // ===== ИГРОК И ВРЕМЯ =====
    public string playerName = "";
    public string gender = "m";
    public int look;                 // вариант внешности героя (Data.HeroKey)
    public string scene = "intro";
    public int chapter = 1;
    public int chapter2Day;          // день, когда началась глава 2
    public string chapter2Kind;      // "earn" («Заработаю сам») | "recoup» («Отыграться»)
    public int day = 1;

    // ===== РЕСУРСЫ =====
    public int money = B.START_MONEY;
    public int addiction;
    public int satiety = B.SATIETY_MAX;
    public List<InvItem> inventory = new List<InvItem>();
    public List<string> worn = new List<string>();   // что надето из рюкзака (Outfit)
    public float mood;               // 0..1 — затемнение интерфейса
    public int conscience = B.CONSCIENCE_START; // совесть: честные поступки — выше, враньё и кражи — ниже
    public bool selfConfessed;       // сам рассказал родителям про ставки
    public Income income = new Income();

    // ===== ПИТАНИЕ ПО ДНЯМ =====
    public int lunchDay, lunchBoughtDay, dinnerDay;

    // ===== СЕМЬЯ =====
    public bool parentsTalked;
    public int momAskCount;
    public bool momPacksLunch;
    public bool dadTalkPending;
    public bool dadTalked;
    public string dadHeardFrom = "mom"; // "mom" | "car"
    public bool parentsGiftGiven;
    public bool parentsWary;    // на 5-й день ответил «Я не Паша» — родители проверят выписку
    public int waryDay, waryBets; // когда был разговор и сколько ставок было к тому моменту
    public bool parentsChecked; // выписку уже проверяли
    public int punishedUntil;
    public List<InvItem> confiscated = new List<InvItem>();
    public int lies;
    public int hospitalVisits;
    public int randomEventDay;
    public int dadCallWeek = -1;
    public int maxWalkWeek = -1;

    // ===== УТРО (в браузерной версии — переменная morning в js/scenes/day.js) =====
    public bool morningPunished, morningReturned;

    // ===== ВОСКРЕСЕНЬЕ =====
    public string sundayCompany = "alone";
    public string sundayPlace;
    public List<string> sundayDone = new List<string>();
    public string sundayLast;
    public bool sundayDiscounted;    // последняя покупка — со скидкой Ашота (SundayLogic.AshotDiscount)
    public int sundayFriendDay;
    public List<string> sundayEventsSeen = new List<string>();
    public int phoneLockedUntil;

    // ===== ВЫХОД ИЗ ЗАВИСИМОСТИ =====
    public bool appDeleted;
    public int cleanDays, relapses, temptationDay;
    public List<string> temptationsSeen = new List<string>();
    public int temptationsRefused;
    public int bottomNights;
    public bool addictionWarned, warningDue;

    // ===== РОДСТВЕННИКИ =====
    public string familyEvent;
    public List<string> familyEventsSeen = new List<string>();
    public string parcelItem;

    // ===== ПАША, КЛАСС, МАКС =====
    public int pashaDebt;
    public bool pashaLent;
    public string pashaHelp;
    public bool pashaSaved, pashaVisited;
    public List<string> friendLinesSeen = new List<string>();
    public string classReaction;
    public bool maxTalked, maxSupport;
    public int maxQuarrelDay;   // день ссоры с Максом (огрызнулся или посмеялся над Пашей)
    public bool maxMakeupSeen;  // Макс уже давал шанс помириться
    public bool maxForgiven;    // помирились — Макс снова друг
    public List<string> marks = new List<string>(); // поступки, которые аукнутся: "granny", "wallet_return", "wallet_keep"

    // ===== КАЗИНО =====
    public int betsMade, betsWon, totalStaked, totalWon;
    public int lastBet = 100;
    public bool wasAllIn;
    public string lastGame = "bet_roulette";
    public MatchState currentMatch;
    public int lastPayout;
    public int lastJackpotMult = 20;
    public bool hadJackpot, jackpotWon, lostEverything;
    // Бонусы конторы: только для ставок, вывод — после отыгрыша (B.BONUS_*)
    public int bonus;                // бонусный счёт
    public int bonusWager;           // сколько ещё поставить, чтобы вывести
    public int bonusUntil;           // последний день, когда бонус жив
    public float bonusShare;         // доля бонусных денег в последней ставке
    public int bonusesTaken, bonusCashedOut;
    public string bonusNote;         // "burned" — сгорел ночью (сказать утром)
    public int bonusNoteAmount;
    public bool firstBonusGiven;
    public string bonusAfterLine = ""; // строка про бонус для сцены после ставки
    public int lastBonus, lastBonusDay;

    // ===== ГОПНИКИ И ДОЛГ =====
    public string walkRoute;
    public InvItem gopnikTarget;
    public Robbery robbery;
    public int robbedCount, beatenDay;
    public bool loanPending, loanActive;
    public int loanDay, loanPaid, loanTaken, loanLastCollectDay;

    // ===== КРАЖИ =====
    public string theftStore, theftItemId;
    public List<string> theftAttempted = new List<string>();
    public int theftCaughtCount, theftDay, theftsDone;
    public bool theftSchoolTalkDue;
    public int commissionPaid;
    public string theftOutcome;      // "escaped" | "caught"

    // ===== ГЛАВА 2: ДЕНЬГИ И ПОДРАБОТКИ =====
    public bool moneyChoiceDone, jobsUnlocked;
    public int jobDay;
    public Dictionary<string, int> jobFails = Jobs4(0);
    public Dictionary<string, int> jobWins = Jobs4(0);
    public Dictionary<string, bool> jobsOpen = new Dictionary<string, bool> { { "courier", false }, { "cafe", false } };
    public int jobShifts;
    public string lastJob;
    public int lastShiftPay;
    public Dictionary<string, int> jobFiredDay = Jobs4(0);
    public Dictionary<string, bool> jobRehired = new Dictionary<string, bool> { { "flyers", false }, { "grocery", false }, { "courier", false }, { "cafe", false } };
    public string shopDetour;
    public int garageVisits;
    public List<string> jobEventsSeen = new List<string>();

    static Dictionary<string, int> Jobs4(int v) =>
        new Dictionary<string, int> { { "flyers", v }, { "grocery", v }, { "courier", v }, { "cafe", v } };
}

// Текущее состояние — одно на игру
public static class G
{
    public static GameState S = new GameState();
}
