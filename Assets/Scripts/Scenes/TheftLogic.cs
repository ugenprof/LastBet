// Кражи. С Пашей (день, когда он просит в долг): руки могут не послушаться (THEFT_FREEZE_WITH_PASHA) —
// иначе как в одиночку: шанс попасться зависит от цены. В одиночку (после ареста Паши) поймали:
// 1-й раз — участок и учёт, 2-й — комиссия и штраф, 3-й (THEFT_MAX_CAUGHT) — арест.
using System;
using System.Collections.Generic;
using System.Linq;

public static class TheftLogic
{
    static GameState S => G.S;

    public static void Register()
    {
        // ===== ВЫБОР ВЕЩИ =====
        Game.Builders["theft_in_tech"] = (_, __) => TheftChoices(Data.C.TECH_ITEMS, false);
        Game.Builders["theft_in_clothes"] = (_, __) => TheftChoices(Data.C.CLOTHES_ITEMS, false);
        Game.Builders["theft_solo_tech"] = (_, __) => TheftChoices(Data.C.TECH_ITEMS, true);
        Game.Builders["theft_solo_clothes"] = (_, __) => TheftChoices(Data.C.CLOTHES_ITEMS, true);

        // В одиночку — одна попытка в день
        Func<SceneDef, bool> solo = _ =>
        {
            S.theftDay = S.day;
            return false;
        };
        Game.Routes["theft_solo_tech"] = solo;
        Game.Routes["theft_solo_clothes"] = solo;

        Game.Routes["theft_bail"] = _ =>
        {
            S.theftOutcome = "escaped";
            return false;
        };

        // ===== ИСХОД =====
        Game.Routes["theft_attempt"] = _ =>
        {
            bool withPasha = S.day <= B.PASHA_ARREST_DAY;
            if (withPasha && Rules.Rand() < B.THEFT_FREEZE_WITH_PASHA)
            {
                S.theftOutcome = "escaped"; // ничего не вынес, но и не попался; Пашу ловят
                Game.Render("theft_fail_early");
                return true;
            }

            var item = Data.Item(S.theftItemId);
            if (!withPasha) S.theftAttempted.Add(S.theftItemId); // эту вещь больше не пытаться
            bool caught = Rules.Rand() < CatchChance(item);

            if (caught && withPasha)
            {
                S.theftOutcome = "caught";
                S.theftCaughtCount++;     // на учёт ставят уже сейчас — это первая ступень
                S.parentsTalked = true;   // родители и так всё знают — разговора «про Пашу» не будет
                Game.Render("theft_caught");
            }
            else if (caught)
            {
                S.theftCaughtCount++;
                if (S.theftCaughtCount >= B.THEFT_MAX_CAUGHT) Game.Render("theft_arrest_final");
                else Game.Render(S.theftCaughtCount == 1 ? "theft_solo_caught" : "theft_commission");
            }
            else
            {
                if (withPasha) S.theftOutcome = "escaped";
                Rules.GiveItem(S.theftItemId, "stolen");
                S.theftsDone++;
                Game.Render(withPasha ? "theft_success" : "theft_solo_success");
            }
            return true;
        };

        // ===== ПОСЛЕДСТВИЯ =====
        // Первый раз на учёте: родители забирают телефон, утром — разговор с классной
        Game.Routes["theft_parents_scold"] = _ =>
        {
            S.phoneLockedUntil = S.day + B.PHONE_LOCK_DAYS;
            S.theftSchoolTalkDue = true;
            return false;
        };

        // Второй раз: комиссия, штраф родителям — папа забирает у героя сколько есть, наказание
        Game.Routes["theft_commission_home"] = _ =>
        {
            S.commissionPaid = Math.Min(S.money, B.THEFT_COMMISSION_FINE);
            S.money -= S.commissionPaid;
            S.punishedUntil = S.day + B.THEFT_COMMISSION_PUNISH_DAYS;
            S.phoneLockedUntil = S.day + B.PHONE_LOCK_DAYS;
            S.dadTalked = true;
            return false;
        };
    }

    static List<Choice> TheftChoices(List<string> ids, bool solo)
    {
        var choices = Rules.StealableItems(ids, solo).Select(id => new Choice
        {
            text = Tpl.Fill(Data.T("theft.item"), "itemName", Data.Item(id).name, "itemPrice", Data.Item(id).price),
            next = "theft_attempt",
            theftItem = id,
            iconItem = id
        }).ToList();
        choices.Add(new Choice { text = Data.T("theft.bail"), next = solo ? "theft_solo_bail" : "theft_bail" });
        return choices;
    }

    static float CatchChance(ItemDef item) => Math.Min(B.THEFT_CATCH_MAX, B.THEFT_CATCH_BASE + item.price * B.THEFT_CATCH_PER_RUB);
}
