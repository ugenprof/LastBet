// Дом: что ждёт вечером (победа, ужин, мама, папа, родители про Пашу), больница, папин шкаф.
using System.Collections.Generic;

public static class HomeLogic
{
    static GameState S => G.S;

    public static void Register()
    {
        Game.Routes["home_evening"] = _ =>
        {
            // Цель игры — все вещи в рюкзаке
            if (Rules.HasAllItems())
            {
                Game.Render("victory_items");
                return true;
            }
            // Неделя без приложения и без срыва — хорошая концовка
            if (S.appDeleted && S.cleanDays >= B.CLEAN_DAYS_FOR_ENDING)
            {
                Game.Render("clean_week");
                return true;
            }

            bool parentsTalkPending = S.day == B.PASHA_ARREST_DAY && !S.parentsTalked;

            // Ужин — один раз в день. Мама видит, какой ребёнок пришёл, — до ужина.
            // В день ареста Паши вечер занят разговором о нём
            if (S.dinnerDay != S.day)
            {
                S.dinnerDay = S.day;
                bool looksHungry = S.satiety < B.SATIETY_MOM_NOTICE;
                Rules.ChangeSatiety(B.MEAL_SATIETY);
                if (looksHungry && !parentsTalkPending)
                {
                    Game.Render(S.momAskCount++ == 0 ? "mom_pale" : "mom_pale_again");
                    return true;
                }
            }

            // Признался маме про ставки — приходит папа (в день ареста Паши — на следующий вечер)
            if (S.dadTalkPending && !parentsTalkPending)
            {
                S.dadTalkPending = false;
                S.dadTalked = true;
                Game.Render(S.day >= B.PASHA_ARREST_DAY ? "dad_talk_pasha" : "dad_talk");
                return true;
            }

            if (parentsTalkPending)
            {
                S.parentsTalked = true;
                Game.Render("day5_parents");
                return true;
            }
            if (S.day > B.PASHA_ARREST_DAY)
            {
                Game.Render("home_evening_after_pasha");
                return true;
            }
            if (S.betsMade > 0)
            {
                Game.Render("home_evening_later");
                return true;
            }
            return false;
        };

        // Папа забирает купленное; если ничего нет — забирать нечего
        Game.Routes["dad_confiscate"] = _ =>
        {
            if (S.inventory.Count == 0)
            {
                Game.Render("home_evening");
                return true;
            }
            S.confiscated = S.inventory;
            S.inventory = new List<InvItem>();
            return false;
        };

        // Подарок родителей тому, кто ни разу не ставил: смартфон, деньги, подработки
        Game.Routes["parents_gift"] = _ =>
        {
            if (S.parentsGiftGiven) return false;
            S.parentsGiftGiven = true;
            if (!Rules.HasItem(B.PARENTS_GIFT_ITEM)) Rules.GiveItem(B.PARENTS_GIFT_ITEM, "gift");
            S.money += B.PARENTS_GIFT_MONEY;
            S.income.gift += B.PARENTS_GIFT_MONEY;
            S.jobsUnlocked = true;
            S.moneyChoiceDone = true;
            return false;
        };

        // После голодного обморока: капельница, и мама теперь собирает бутерброды
        Game.Routes["hospital_wake"] = _ =>
        {
            S.hospitalVisits++;
            S.satiety = B.SATIETY_HOSPITAL;
            S.momPacksLunch = true;
            return false;
        };
    }
}
