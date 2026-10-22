// Школа: большая перемена, голодный обморок, что делать после уроков; мысли на день ареста Паши.
using System;
using System.Collections.Generic;
using System.Linq;

public static class SchoolLogic
{
    static GameState S => G.S;

    // Реплика друзей (story.friend_lines) → когда она уместна. Первая подходящая, каждая — один раз
    static readonly (string key, Func<bool> when)[] FRIEND_LINES =
    {
        ("beaten", () => S.beatenDay != 0 && S.day - S.beatenDay <= 1),
        ("caught", () => S.theftCaughtCount > 0),
        ("walletReturned", () => S.marks.Contains("wallet_return")),
        ("walletKept", () => S.marks.Contains("wallet_keep")),
        ("jobsFriend", () => S.jobDay == S.day - 1 && Rules.MaxIsFriend()),
        ("jobs", () => S.jobDay == S.day - 1),
        ("boughtCasino", () => Rules.YesterdayPurchase() != null && Rules.CasinoNet() > 0),
        ("bought", () => Rules.YesterdayPurchase() != null && Rules.MaxIsFriend()),
    };

    public static void Register()
    {
        // ===== ПОСЛЕ УРОКОВ =====
        Game.Routes["after_school"] = _ =>
        {
            // Большая перемена — один раз в день: школа отнимает силы, можно пообедать
            if (S.lunchDay != S.day)
            {
                S.lunchDay = S.day;
                Rules.ChangeSatiety(-B.SATIETY_SCHOOL_LOSS);
                Game.Render(S.satiety <= B.SATIETY_FAINT ? "school_lunch_weak" : "school_lunch");
                return true;
            }
            if (S.satiety <= B.SATIETY_FAINT)
            {
                Game.Render("faint_school");
                return true;
            }
            // Уговорил Пашу рассказать родителям — после уроков идут к нему домой
            if (S.pashaSaved && !S.pashaVisited && S.day == B.PASHA_LOAN_DAY)
            {
                S.pashaVisited = true;
                Game.Render("day4_pasha_home");
                return true;
            }
            // Договорились с Пашей украсть — после уроков идут в магазин
            if (S.theftStore != null && S.theftOutcome == null && S.day == B.PASHA_LOAN_DAY)
            {
                Game.Render(S.theftStore == "tech" ? "theft_in_tech" : "theft_in_clothes");
                return true;
            }
            // Первый день главы 2, Паша уже сидит: где брать деньги — кража или подработка
            if (S.chapter == 2 && S.day == S.chapter2Day && S.day > B.PASHA_ARREST_DAY && !S.moneyChoiceDone)
            {
                S.moneyChoiceDone = true;
                Game.Render("ch2_money_choice");
                return true;
            }
            return false;
        };

        Game.Builders["after_school"] = (_, __) =>
        {
            var choices = new List<Choice> { new Choice { text = Data.T("afterSchool.home"), next = "go_home" } };
            choices.InsertRange(0, new[]
            {
                new Choice { text = Data.T("afterSchool.techShop"), next = "shop_electronics" },
                new Choice { text = Data.T("afterSchool.clothesShop"), next = "shop_clothes" }
            });
            if (S.inventory.Count > 0) choices.Insert(2, new Choice { text = Data.T("afterSchool.sell"), next = "sell_items" });

            // Подработки: одна смена в день, после трёх провалов — увольнение
            if (S.jobsUnlocked && S.jobDay != S.day)
            {
                var jobs = new List<Choice>();
                var open = new (string type, bool open)[]
                {
                    ("flyers", true), ("grocery", true), ("courier", S.jobsOpen["courier"]), ("cafe", S.jobsOpen["cafe"])
                };
                foreach (var (type, isOpen) in open)
                {
                    if (!isOpen) continue;
                    if (S.jobFails[type] < B.JOB_MAX_FAILS)
                        jobs.Add(new Choice { text = Tpl.Fill(Data.T("afterSchool." + type + "Job"), "pay", Jobs.JobPay(type)), next = "job_" + type });
                    else if (Jobs.CanAskRehire(type))
                        jobs.Add(new Choice { text = Data.T("afterSchool." + type + "Rehire"), next = "job_apology", effect = new Effect { rehireAsk = type } });
                }
                choices.InsertRange(0, jobs);
            }

            // Кража в одиночку (Паша уже сидит): раз в день, если в магазине осталось что стащить
            if (S.chapter == 2 && S.day > B.PASHA_ARREST_DAY && S.theftDay != S.day)
            {
                var thefts = new List<Choice>();
                if (Rules.StealableItems(Data.C.TECH_ITEMS, true).Count > 0)
                    thefts.Add(new Choice { text = Data.T("afterSchool.stealTech"), next = "theft_solo_tech", effect = new Effect { theftStore = "tech" } });
                if (Rules.StealableItems(Data.C.CLOTHES_ITEMS, true).Count > 0)
                    thefts.Add(new Choice { text = Data.T("afterSchool.stealClothes"), next = "theft_solo_clothes", effect = new Effect { theftStore = "clothes" } });
                choices.InsertRange(choices.Count - 1, thefts);
            }
            return choices;
        };

        foreach (var id in new[] { "day_after_pasha", "day_after_defend", "day_max_friend", "day_max_cold" })
            Game.Texts[id] = scene => Game.BaseText(scene) + FriendLine();

        // ===== ДЕНЬ АРЕСТА ПАШИ: МЫСЛИ ГЕРОЯ =====
        Game.Routes["day5_thoughts"] = _ =>
        {
            if (S.pashaSaved) Game.Render("day5_thoughts_saved");
            else if (S.theftOutcome == "caught") Game.Render("day5_thoughts_caught");
            else if (S.theftOutcome == "escaped") Game.Render("day5_thoughts_theft");
            else if (S.pashaHelp == "loan") Game.Render("day5_thoughts_loan");
            else if (S.pashaHelp == "job") Game.Render("day5_thoughts_job");
            else Game.Render(S.pashaLent ? "day5_thoughts_lent" : "day5_thoughts_refused");
            return true;
        };
    }

    static string FriendLine()
    {
        var found = FRIEND_LINES.FirstOrDefault(f => !S.friendLinesSeen.Contains(f.key) && f.when());
        if (found.key == null) return "";
        var key = found.key;
        // «Работу» заметили — второй раз про неё не говорят, кто бы ни сказал
        S.friendLinesSeen.Add(key);
        if (key.StartsWith("jobs")) S.friendLinesSeen.AddRange(new[] { "jobs", "jobsFriend" });
        if (key.StartsWith("bought")) S.friendLinesSeen.AddRange(new[] { "bought", "boughtCasino" });
        var item = Rules.YesterdayPurchase();
        return "\n\n" + Tpl.Fill(Data.Scene("friend_lines").S(key), "boughtItem", item != null ? item.name.ToLower() : "");
    }
}
