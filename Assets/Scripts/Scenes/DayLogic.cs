// Смена дней: ночь, утро, куда герой попадает в школе.
public static class DayLogic
{
    static GameState S => G.S;

    public static void Register()
    {
        // ===== НОЧЬ =====
        // Следующий день, голод за ночь. Конец главы 1: прошла её последняя ночь
        // или в начале главы проиграны все деньги
        Game.Routes["sleep"] = _ =>
        {
            // Дно зависимости: столько ночей подряд на пределе — ночь с маминой картой
            if (RecoveryLogic.CountBottomNight()) return true;
            if (S.appDeleted) S.cleanDays++;

            S.day += 1;
            Rules.ChangeSatiety(-B.SATIETY_NIGHT_LOSS);

            bool lostEarly = S.lostEverything && S.day == B.CHAPTER2_EARLY_DAY;
            if (S.chapter == 1 && (S.day > B.CHAPTER1_LAST_DAY || lostEarly))
            {
                StartChapter2(lostEarly);
                Game.Render("chapter2_title");
            }
            else Game.Render("new_day");
            return true;
        };

        // ===== УТРО =====
        // Карманные (если папа не наказал), завтрак, вещи из папиного шкафа после наказания
        Game.Routes["new_day"] = _ =>
        {
            bool punished = S.day <= S.punishedUntil;
            if (!punished)
            {
                S.money += B.DAILY_POCKET_MONEY;
                S.income.pocket += B.DAILY_POCKET_MONEY;
            }
            else if (!Rules.IsDayOff())
            {
                // Наказан: карманных нет, но на обед родители всё равно дают
                S.money += B.SCHOOL_LUNCH_PRICE;
                S.income.pocket += B.SCHOOL_LUNCH_PRICE;
            }
            Rules.ChangeSatiety(B.MEAL_SATIETY);
            RecoveryLogic.RollFamilyEvent();

            bool returned = !punished && S.confiscated.Count > 0;
            if (returned)
            {
                S.inventory.AddRange(S.confiscated);
                S.confiscated.Clear();
            }
            S.morningPunished = punished;
            S.morningReturned = returned;
            return false;
        };

        Game.Texts["new_day"] = scene =>
        {
            string pocket = PocketLine(scene);
            return Tpl.Fill(Rules.IsDayOff() ? Game.PickByDay(scene.L("textSunday")) : Game.BaseText(scene),
                "pocketLine", pocket,
                "pocketLineCap", Tpl.Capitalize(pocket),
                "packedLine", Rules.MomLunchToday() ? scene.S("packedLine") : "",
                "returnedLine", S.morningReturned ? scene.S("returnedLine") : "");
        };

        // Заставка главы 2: «Отыграться» или «Заработаю сам»
        Game.Screens["chapter2_title"] = scene =>
        {
            var chapter = scene.chapter;
            if (S.chapter2Kind == "earn")
            {
                if (S.betsMade > 0) chapter = scene.Card(S.appDeleted ? "chapterDeleted" : "chapterEarn");
                else chapter = scene.Card(S.appDeleted ? "chapterUnsubscribed" : "chapterNeverPlayed");
            }
            Game.RenderStandard("chapter2_title", scene, chapter);
        };

        // ===== В ШКОЛУ =====
        Game.Routes["go_school"] = _ =>
        {
            // Воскресенье — выходной: планы на день
            Game.Render(MorningSceneForToday() ?? (Rules.IsDayOff() ? "sunday_plans" : SchoolSceneForToday()));
            return true;
        };
    }

    // Глава 2: «Заработаю сам» — удалил приложение, не ставил или зависимость низкая; «Отыграться» — иначе.
    // Подработки открываются в любом случае
    static void StartChapter2(bool lostEarly)
    {
        bool earn = !lostEarly && (S.appDeleted || S.betsMade == 0 || S.addiction < B.CHAPTER2_EARN_MAX_ADDICTION);
        S.chapter = 2;
        S.chapter2Day = S.day;
        S.chapter2Kind = earn ? "earn" : "recoup";
        S.jobsUnlocked = true;
        if (earn) S.moneyChoiceDone = true;
    }

    static string PocketLine(SceneDef scene)
    {
        if (!S.morningPunished) return scene.S("pocketLine");
        return Rules.IsDayOff() ? scene.S("punishedLineSunday") : scene.S("punishedLine");
    }

    // Утро перед уроками — по очереди, одно за раз
    static string MorningSceneForToday()
    {
        if (S.chapter == 2 && S.betsMade == 0 && !S.parentsGiftGiven) return "parents_gift";
        if (S.warningDue)
        {
            S.warningDue = false;
            return "addiction_warning";
        }
        if (S.familyEvent != null)
        {
            var ev = S.familyEvent;
            S.familyEvent = null;
            return ev;
        }
        return null;
    }

    static string SchoolSceneForToday()
    {
        // С дня ареста Паши хвастаться джекпотом некому
        if (S.day >= B.PASHA_ARREST_DAY) S.hadJackpot = false;

        if (S.day == B.PASHA_LOAN_DAY) return "day4_pasha_beggar";

        if (S.day == B.PASHA_ARREST_DAY)
        {
            if (S.pashaSaved) return "day5_class_saved";
            if (S.theftOutcome == "caught") return "day5_class_caught";
            if (S.theftOutcome == "escaped") return "day5_class_theft";
            return "day5_class";
        }

        if (S.day > B.PASHA_ARREST_DAY)
        {
            if (S.classReaction == null) return "class_gossip"; // первый школьный день после ареста
            if (S.theftSchoolTalkDue)
            {
                S.theftSchoolTalkDue = false;
                return "theft_school_talk";
            }
            var temptation = RecoveryLogic.TemptationForToday();
            if (temptation != null) return temptation;
            if (S.dadTalked && !S.maxTalked) return S.classReaction == "laugh" ? "max_knows_laughed" : "max_knows";
            if (S.maxTalked) return S.maxSupport ? "day_max_friend" : "day_max_cold";
            if (S.classReaction == "defend") return "day_after_defend";
            return "day_after_pasha";
        }

        if (S.hadJackpot)
        {
            S.hadJackpot = false;
            return "day2_jackpot";
        }
        return "day_later_hallway";
    }
}
