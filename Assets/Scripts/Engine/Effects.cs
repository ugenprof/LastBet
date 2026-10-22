// Что происходит при нажатии на выбор. Поля выбора — Choice (Core/Models.cs).
public static class Effects
{
    static GameState S => G.S;

    // Возвращает false, если действие не выполнено
    public static bool ApplyChoice(Choice choice)
    {
        if (choice.buyItem != null && S.money >= Data.Item(choice.buyItem).price && !Rules.HasItem(choice.buyItem))
        {
            S.money -= Data.Item(choice.buyItem).price;
            Rules.GiveItem(choice.buyItem);
        }

        if (choice.sellIndex >= 0 && choice.sellIndex < S.inventory.Count)
        {
            var item = S.inventory[choice.sellIndex];
            S.money += item.price / 2;
            S.income.sales += item.price / 2;
            S.inventory.RemoveAt(choice.sellIndex);
        }

        if (choice.lie == "always" || (choice.lie == "ifPlayed" && S.betsMade > 0)) S.lies++;

        // Обед: full — полный обед дня, half — полпорции, snack — сосиска в тесте
        if (choice.buyLunch != null)
        {
            S.money -= Rules.LunchPrice(choice.buyLunch);
            S.lunchBoughtDay = S.day;
            Rules.ChangeSatiety(Rules.LunchSatiety(choice.buyLunch));
        }
        if (choice.packedLunch) Rules.ChangeSatiety(B.MEAL_SATIETY);
        if (choice.sundayActivity != null) SundayLogic.ApplyActivity(choice.sundayActivity);
        if (choice.theftItem != null) S.theftItemId = choice.theftItem;

        if (choice.effect != null) ApplyEffect(choice.effect);
        if (choice.reset) Rules.ResetGame();
        return true;
    }

    // Кнопка «Заново» в HUD: окно «Да / Нет», «Да» — сюда
    public static void ConfirmRestart()
    {
        ApplyChoice(new Choice { reset = true });
        SaveGame.Delete();
        Game.Render("chapter1_title");
    }

    public static void ApplyEffect(Effect e)
    {
        if (e.money != 0) S.money += e.money;
        if (e.addiction != 0) Rules.ChangeAddiction(e.addiction);
        if (e.satiety != 0) Rules.ChangeSatiety(e.satiety);

        // Семья
        if (e.packLunch) S.momPacksLunch = true;
        if (e.dadTalk != null)                       // вечером придёт папа
        {
            S.dadTalkPending = true;
            S.dadHeardFrom = e.dadTalk == "car" ? "car" : "mom";
        }
        if (e.dadTalked) S.dadTalked = true;         // родители знают про ставки
        if (e.confessed)                             // сам рассказал — отдельного разговора с папой не будет
        {
            S.dadTalked = true;
            S.dadTalkPending = false;
        }
        if (e.punishDays != 0) S.punishedUntil = S.day + e.punishDays;

        // Работа: чаевые и премии считаются заработком
        if (e.tip != 0)
        {
            S.money += e.tip;
            S.income.jobs += e.tip;
        }

        // Воскресенье: с кем пошёл
        if (e.company != null) S.sundayCompany = e.company;

        // Дорога домой из магазина; после смены — зайти в магазин по пути
        if (e.route != null) S.walkRoute = e.route;
        if (e.detour != null) S.shopDetour = e.detour;

        // Работа: попроситься обратно / вернули с последним шансом
        if (e.rehireAsk != null) S.lastJob = e.rehireAsk;
        if (e.rehire)
        {
            S.jobRehired[S.lastJob] = true;
            S.jobFails[S.lastJob] = B.JOB_MAX_FAILS - 1;
        }

        // Приложение: сорваться и скачать заново
        if (e.relapse) Rules.Relapse();
        if (e.temptation == "refused") S.temptationsRefused++;

        // Родители забирают телефон
        if (e.phoneLock) S.phoneLockedUntil = S.day + B.PHONE_LOCK_DAYS;

        // Паша
        if (e.pashaSaved) S.pashaSaved = true;
        if (e.pashaHelp != null) S.pashaHelp = e.pashaHelp;
        if (e.pashaHelp == "loan") S.loanPending = true;
        if (e.theftStore != null)
        {
            S.pashaHelp = "theft";
            S.theftStore = e.theftStore;
        }
        if (e.jobs) S.jobsUnlocked = true;

        // Класс и Макс
        if (e.classReaction != null) S.classReaction = e.classReaction;
        if (e.maxTalk != null)
        {
            S.maxTalked = true;
            S.maxSupport = e.maxTalk == "support";
        }
    }
}
