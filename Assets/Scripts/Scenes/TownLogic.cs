// Город: дорога домой (долг у гопников, случайные гопники), магазины, покупка, продажа.
using System;
using System.Collections.Generic;
using System.Linq;

public static class TownLogic
{
    static GameState S => G.S;

    public static void Register()
    {
        // ===== ДОРОГА ДОМОЙ =====
        Game.Routes["go_home"] = _ =>
        {
            // После смены решил зайти в магазин по пути
            if (S.shopDetour != null)
            {
                var shop = S.shopDetour;
                S.shopDetour = null;
                Game.Render(shop);
                return true;
            }
            if (!MeetCreditors()) Game.Render("home_evening");
            return true;
        };

        Game.Routes["walk_home"] = _ => MeetCreditors();

        // Папа и Макс — по разу в неделю каждый
        Game.Builders["walk_home"] = (choices, _) => choices
            .Where(c => c.effect?.route != "max" || Rules.MaxIsFriend())
            .Select(c =>
            {
                bool used = (c.effect?.route == "dad" && S.dadCallWeek == Rules.WeekNumber())
                         || (c.effect?.route == "max" && S.maxWalkWeek == Rules.WeekNumber());
                return used ? Choices.WithNote(c, Data.T("walk.usedThisWeek")) : c;
            }).ToList();

        Game.Routes["walk_route"] = _ =>
        {
            var route = S.walkRoute;
            if (route == "dad")
            {
                S.dadCallWeek = Rules.WeekNumber();
                Game.Render(!Rules.ParentsTrust() ? "walk_dad_refused" : DadAsksAboutMoney() ? "walk_dad_question" : "walk_dad");
                return true;
            }
            if (route == "max")
            {
                S.maxWalkWeek = Rules.WeekNumber();
                Game.Render("walk_max");
                return true;
            }
            if (route == "long") Rules.ChangeSatiety(-B.WALK_LONG_SATIETY);
            float chance = route == "long" ? B.WALK_LONG_GOPNIK_CHANCE : B.WALK_SHORT_GOPNIK_CHANCE;
            if (Rules.Rand() < chance) MeetGopniks();
            // Через гаражи без приключений: дядя Витя из гаража просит помочь — и платит
            else if (route == "short" && Rules.CanHaveRandomEvent() && Rules.Rand() < B.WALK_SHORT_REWARD_CHANCE)
            {
                S.garageVisits++;
                Rules.MarkRandomEvent();
                Game.Render("garage_uncle");
            }
            else Game.Render("home_evening");
            return true;
        };

        // Реплика Макса по дороге — о том, что у героя происходит
        Game.Texts["walk_max"] = scene =>
        {
            var boughtToday = Rules.BoughtToday();
            string line = scene.S("lineDefault");
            if (S.income.jobs > 0) line = scene.S("lineJobs");
            else if (boughtToday != null && Rules.CasinoNet() > 0) line = scene.S("lineCasino");
            // Реплика заполняется отдельно: в ней самой есть {boughtItem} (в браузерной версии он оставался как есть)
            line = Tpl.Fill(line, "boughtItem", boughtToday != null ? boughtToday.name.ToLower() : "");
            return Tpl.Fill(scene.text, "maxLine", line);
        };

        // ===== ГОПНИКИ В ПОДВОРОТНЕ =====
        Game.Texts["gopniki_meet"] = scene =>
        {
            string way = Game.PickByDay(scene.L(S.walkRoute == "long" ? "wayLong" : "wayShort"));
            string demand = S.gopnikTarget != null ? scene.S("demandItem") : scene.S("demandMoney");
            return way + "\n\n" + demand;
        };

        // Отдал сам: вещь — или деньги (меньше, если сегодня обедал в столовой), или пустые карманы
        Game.Routes["gopniki_give"] = _ =>
        {
            if (S.gopnikTarget != null)
            {
                Rob(S.gopnikTarget, 0);
                Game.Render("gopniki_item");
                return true;
            }
            int wanted = S.lunchBoughtDay == S.day ? B.GOPNIK_TAKE_MONEY_AFTER_LUNCH : B.GOPNIK_TAKE_MONEY;
            Rob(null, Math.Min(wanted, S.money));
            Game.Render(S.robbery.money > 0 ? "gopniki_money" : "gopniki_empty");
            return true;
        };

        // Побежал: ушёл — всё при себе; догнали — забирают и покупку, и все деньги
        Game.Routes["gopniki_run"] = _ =>
        {
            if (Rules.Rand() < B.GOPNIK_RUN_CHANCE)
            {
                Game.Render("gopniki_escaped");
                return true;
            }
            Rob(S.gopnikTarget, S.money);
            Game.Render(Rules.Rand() < B.GOPNIK_FIGHT_CHANCE ? "gopniki_fight" : "gopniki_run_caught");
            return true;
        };

        Game.Texts["gopniki_fight"] = scene =>
        {
            string lost = LostLine();
            return Tpl.Fill(scene.text, "stolenLine", lost != "" ? lost : "даже мелочь из карманов");
        };

        Game.Texts["gopniki_run_caught"] = scene =>
        {
            string lost = LostLine();
            return Tpl.Fill(scene.text, "lostLine", lost != "" ? Tpl.Fill(scene.S("lostLine"), "lost", lost) : scene.S("lostNothing"));
        };

        // Избили за долг или в драке: больница, друзья потом заметят синяки
        Func<SceneDef, bool> beaten = _ =>
        {
            S.hospitalVisits++;
            S.beatenDay = S.day;
            return false;
        };
        Game.Routes["hospital_beaten"] = beaten;
        Game.Routes["hospital_fight"] = beaten;

        // ===== МАГАЗИНЫ =====
        Game.Builders["shop_electronics"] = (_, __) => ShopChoices(Data.C.TECH_ITEMS);
        Game.Builders["shop_clothes"] = (_, __) => ShopChoices(Data.C.CLOTHES_ITEMS);

        // ===== ПОКУПКА =====
        Game.Routes["buy_item"] = _ =>
        {
            if (S.inventory.Count > 0) return false;
            Game.Render("after_school");
            return true;
        };

        Game.Screens["buy_item"] = scene =>
        {
            var item = Rules.Last(S.inventory);
            UI.SetBackground(Data.Scene(ShopSceneFor(item.id)).bg);
            Game.RenderStandard("buy_item", scene, null, Tpl.Fill(scene.headline, "boughtItem", item.name.ToLower()));
            UI.ShowItemBanner(item.id);
        };

        Game.Routes["continue_shopping"] = _ =>
        {
            var last = Rules.Last(S.inventory);
            Game.Render(last != null ? ShopSceneFor(last.id) : "after_school");
            return true;
        };

        // ===== ПРОДАЖА СТАРШЕКЛАССНИКУ (за полцены) =====
        Game.Builders["sell_items"] = (_, __) =>
        {
            var list = S.inventory.Select((item, index) => new Choice
            {
                text = Tpl.Fill(Data.T("sell.item"), "itemName", item.name, "itemPrice", item.price / 2),
                next = "after_school",
                sellIndex = index,
                iconItem = item.id
            }).ToList();
            list.Add(new Choice { text = Data.T("sell.keep"), next = "after_school" });
            return list;
        };
    }

    // Папа видит сегодняшнюю покупку и спрашивает, откуда деньги, — если они с выигрышей, а родители не знают
    static bool DadAsksAboutMoney() =>
        !S.dadTalked && Rules.CasinoNet() > 0 && S.inventory.Any(i => i.boughtDay == S.day && i.source == "bought");

    // Займ у гопников: в день договорённости дают деньги, потом каждый день забирают всё из карманов.
    // В последний день не набралось — бьют. Возвращает true, если встреча была
    static bool MeetCreditors()
    {
        if (S.loanPending)
        {
            S.loanPending = false;
            S.loanActive = true;
            S.loanDay = S.day;
            S.loanPaid = 0;
            S.money += B.GOPNIK_LOAN_AMOUNT;
            S.income.loan += B.GOPNIK_LOAN_AMOUNT;
            Game.Render("gopnik_loan_meet");
            return true;
        }
        if (!S.loanActive || S.day <= S.loanDay || S.loanLastCollectDay == S.day) return false;

        S.loanLastCollectDay = S.day;
        S.loanTaken = Math.Min(S.money, B.GOPNIK_LOAN_RETURN - S.loanPaid);
        S.money -= S.loanTaken;
        S.loanPaid += S.loanTaken;
        if (S.loanPaid >= B.GOPNIK_LOAN_RETURN)
        {
            S.loanActive = false;
            Game.Render("gopnik_debt_paid");
        }
        else if (S.day >= S.loanDay + B.GOPNIK_LOAN_DAYS)
        {
            S.loanActive = false; // «проучили» — долг списан кулаками
            Game.Render("gopnik_beating");
        }
        else Game.Render(S.loanTaken > 0 ? "gopnik_collect" : "gopnik_collect_empty");
        return true;
    }

    // Хотят последнюю купленную сегодня электронику, иначе — деньги
    static void MeetGopniks()
    {
        S.gopnikTarget = S.inventory.LastOrDefault(i => Data.C.TECH_ITEMS.Contains(i.id) && i.boughtDay == S.day);
        Game.Render("gopniki_meet");
    }

    static void Rob(InvItem item, int money)
    {
        if (item != null) S.inventory.Remove(item);
        S.money -= money;
        S.robbery = new Robbery { item = item?.name, money = money };
        if (item != null || money > 0) S.robbedCount++;
    }

    static string LostLine()
    {
        var parts = new List<string>();
        if (S.robbery.item != null) parts.Add(S.robbery.item);
        if (S.robbery.money > 0) parts.Add(S.robbery.money + " ₽");
        return string.Join(" и ", parts);
    }

    static List<Choice> ShopChoices(List<string> ids)
    {
        var choices = ids.Select(id =>
        {
            var item = Data.Item(id);
            var choice = new Choice
            {
                text = Tpl.Fill(Data.T("shop.item"), "itemName", item.name, "itemPrice", item.price),
                next = "buy_item",
                buyItem = id,
                iconItem = id
            };
            if (Rules.HasItem(id)) return Choices.WithNote(choice, Data.T("shop.owned"));
            if (S.money < item.price) return Choices.WithNote(choice, Data.T("shop.tooExpensive"));
            return choice;
        }).ToList();
        choices.Add(new Choice { text = Data.T("shop.leave"), next = "walk_home" });
        return choices;
    }

    public static string ShopSceneFor(string itemId) => Data.C.TECH_ITEMS.Contains(itemId) ? "shop_electronics" : "shop_clothes";
}
