// Выход из зависимости и дно. Удалить приложение / отписаться (с ночи 5-го дня) — не концовка: в главе 2
// одноклассники подбивают сыграть, по ночам выбор остаётся, и только CLEAN_DAYS_FOR_ENDING ночей без срыва
// дают хорошую концовку. Дно: ADDICTION_BOTTOM_NIGHTS ночей подряд на пределе — ночь с маминой картой.
// Тут же — утренние события с родственниками.
using System;
using System.Linq;

public static class RecoveryLogic
{
    static GameState S => G.S;

    // Соблазны: сцена → когда уместна. Каждая — один раз, не чаще раза в TEMPTATION_EVERY_DAYS дней
    static readonly (string id, Func<bool> when)[] TEMPTATIONS =
    {
        ("tempt_denis", () => true),
        ("tempt_push", () => true),
        ("tempt_ad", () => true),
        ("tempt_senior", () => S.money >= 200),
        ("tempt_kirill", () => S.betsMade > 0),
        ("tempt_pasha", () => S.pashaSaved),
        ("tempt_max_hold", () => Rules.MaxIsFriend() && S.relapses > 0),
    };

    // Родственники: утром с шансом FAMILY_EVENT_CHANCE — одно из событий (каждое один раз)
    static readonly (string id, Func<bool> when)[] FAMILY_EVENTS =
    {
        ("family_grandma", () => true),
        ("family_uncle", () => true),
        ("family_aunt", () => true),
        ("family_dad_bonus", () => Rules.ParentsTrust() && S.day > S.punishedUntil),
        ("family_grandma_jam", () => true),
        ("family_cousin", () => S.day > B.PASHA_ARREST_DAY),
    };

    public static void Register()
    {
        // ===== УДАЛИЛ ПРИЛОЖЕНИЕ / ОТПИСАЛСЯ =====
        Func<SceneDef, bool> quit = _ =>
        {
            S.appDeleted = true;
            S.cleanDays = 0;
            return false;
        };
        Game.Routes["app_delete"] = quit;
        Game.Routes["app_unsubscribe"] = quit;

        // Попросил помощи (утром или в ночь с картой): телефон у папы, зависимость ниже, родители знают
        Func<SceneDef, bool> help = _ =>
        {
            S.phoneLockedUntil = S.day + B.PHONE_LOCK_DAYS;
            Rules.ChangeAddiction(-B.ADDICTION_HELP_RELIEF);
            S.dadTalked = true;
            S.dadTalkPending = false;
            S.bottomNights = 0;
            return false;
        };
        Game.Routes["addiction_help"] = help;
        Game.Routes["addiction_bottom_wake"] = help;

        // ===== РОДСТВЕННИКИ =====
        Game.Routes["family_grandma"] = _ => { GiftMoney(B.GRANDMA_GIFT); return false; };
        Game.Routes["family_dad_bonus"] = _ => { GiftMoney(B.DAD_BONUS_GIFT); return false; };
        Game.Routes["family_grandma_jam"] = _ => { Rules.ChangeSatiety(B.GRANDMA_PIES_SATIETY); return false; };

        // Двоюродный брат-студент: по ситуации героя
        Game.Texts["family_cousin"] = scene =>
        {
            if (S.betsMade > 0) return scene.S("textPlayed");
            if (S.income.jobs > 0) return scene.S("textWorker");
            return scene.text;
        };

        // Дядя: подарок, а если племянник работает — ещё сверху
        Game.Routes["family_uncle"] = _ =>
        {
            GiftMoney(B.UNCLE_GIFT + (S.income.jobs > 0 ? B.UNCLE_WORK_BONUS : 0));
            return false;
        };
        Game.Texts["family_uncle"] = scene => S.income.jobs > 0 ? scene.S("textWorker") : scene.text;

        // Тётя прислала посылку: вещь из одежды, которой ещё нет, или деньги
        Game.Routes["family_aunt"] = _ =>
        {
            var id = new[] { "hoodie", "tshirt", "jeans" }.FirstOrDefault(i => !Rules.HasItem(i));
            S.parcelItem = id;
            if (id != null) Rules.GiveItem(id, "gift");
            else GiftMoney(B.AUNT_PARCEL_MONEY);
            return false;
        };
        Game.Texts["family_aunt"] = scene => S.parcelItem != null
            ? Tpl.Fill(scene.text, "parcelItem", Data.Item(S.parcelItem).name.ToLower())
            : scene.S("textMoney");

        // Подарок родителей тому, кто не ставил: если он ещё и отписался — родители это заметили;
        // ответил «Я не Паша» — признаются, что тогда не поверили
        // Сорвался ради «подарка» конторы — что пришло на самом деле
        Game.Texts["tempt_relapse"] = scene =>
            Game.BaseText(scene) + (S.lastBonusDay == S.day && S.lastBonus > 0 ? "\n\n" + scene.S("bonusLine") : "");

        Game.Texts["parents_gift"] = scene => Tpl.Fill(S.appDeleted ? scene.S("textUnsubscribed") : scene.text,
            "waryLine", S.parentsWary ? scene.S("waryLine") : "");
    }

    // Ночь в главе 2 с зависимостью ≥ ADDICTION_NO_EXIT: первая — утром мама застанет, предел — ночь с картой.
    // Возвращает true, если вместо сна показана сцена дна
    public static bool CountBottomNight()
    {
        if (S.chapter != 2 || S.addiction < B.ADDICTION_NO_EXIT)
        {
            S.bottomNights = 0;
            return false;
        }
        S.bottomNights++;
        if (S.bottomNights >= B.ADDICTION_BOTTOM_NIGHTS)
        {
            S.bottomNights = 0;
            Game.Render("addiction_bottom");
            return true;
        }
        if (!S.addictionWarned)
        {
            S.addictionWarned = true;
            S.warningDue = true;
        }
        return false;
    }

    public static string TemptationForToday()
    {
        if (!S.appDeleted || S.chapter != 2 || S.day - S.temptationDay < B.TEMPTATION_EVERY_DAYS || !Rules.CanHaveRandomEvent()) return null;
        var ready = TEMPTATIONS.Where(t => !S.temptationsSeen.Contains(t.id) && t.when()).Select(t => t.id).ToList();
        if (ready.Count == 0) return null;
        var id = ready[0];
        S.temptationsSeen.Add(id);
        S.temptationDay = S.day;
        Rules.MarkRandomEvent();
        return id;
    }

    public static void RollFamilyEvent()
    {
        if (S.day < B.FAMILY_EVENT_FROM_DAY || S.day == B.PASHA_LOAN_DAY || S.day == B.PASHA_ARREST_DAY) return;
        if (!Rules.CanHaveRandomEvent() || Rules.Rand() >= B.FAMILY_EVENT_CHANCE) return;
        var ready = FAMILY_EVENTS.Where(e => !S.familyEventsSeen.Contains(e.id) && e.when()).Select(e => e.id).ToList();
        if (ready.Count == 0) return;
        var id = Rules.Pick(ready);
        S.familyEventsSeen.Add(id);
        S.familyEvent = id;
        Rules.MarkRandomEvent();
    }

    static void GiftMoney(int amount)
    {
        S.money += amount;
        S.income.gift += amount;
    }
}
