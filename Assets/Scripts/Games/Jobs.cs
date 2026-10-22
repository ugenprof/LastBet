// Подработки: правила + «Начать смену», мини-игры, итог смены (деньги, зависимость, увольнение),
// освоенная работа (смена без мини-игры), повышение (открывает работу дороже), события после смены.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class Jobs
{
    static GameState S => G.S;

    static int run;                 // номер текущей смены — таймеры прошлых смен её не трогают
    static bool lastShiftSuccess;

    static readonly Dictionary<string, Action> MINIGAMES = new Dictionary<string, Action>
    {
        { "flyers", StartFlyers }, { "grocery", StartGrocery }, { "courier", StartCourier }, { "cafe", StartCafe }
    };
    static readonly Dictionary<string, int> JOB_PAYS = new Dictionary<string, int>
    {
        { "flyers", B.JOB_PAY }, { "grocery", B.JOB_PAY }, { "courier", B.JOB_PAY_ADVANCED }, { "cafe", B.JOB_PAY_ADVANCED }
    };
    // Повышение на одной работе открывает другую
    static readonly Dictionary<string, string> JOB_PROMOTION_OPENS = new Dictionary<string, string> { { "flyers", "courier" }, { "grocery", "cafe" } };

    // Начальник на каждой работе — кто подставляется вместо {boss} в cast сцены
    public static readonly Dictionary<string, string> JOB_BOSS_CHARACTERS = new Dictionary<string, string>
    {
        { "flyers", "brigadier" }, { "grocery", "zarema" }, { "courier", "warehouse" }, { "cafe", "ashot" }
    };

    public static bool Mastered(string type) => S.jobWins[type] >= B.JOB_MASTERY_WINS;

    // Оплата смены: после повышения (работа освоена) — с прибавкой
    public static int JobPay(string type) => JOB_PAYS[type] + (Mastered(type) ? Data.C.JOB_RAISE[type] : 0);

    // Уволили — через JOB_REHIRE_DAYS дней можно один раз попроситься обратно
    public static bool CanAskRehire(string type) =>
        !S.jobRehired[type] && S.jobFiredDay[type] > 0 && S.day - S.jobFiredDay[type] >= B.JOB_REHIRE_DAYS;

    static void AddFail(string type, int count = 1)
    {
        S.jobFails[type] = Math.Min(B.JOB_MAX_FAILS, S.jobFails[type] + count);
        if (S.jobFails[type] >= B.JOB_MAX_FAILS && S.jobFiredDay[type] == 0) S.jobFiredDay[type] = S.day;
    }

    // Экран перед сменой: правила из story и кнопка. Освоил работу — смена без мини-игры
    public static void RenderIntro(SceneDef scene)
    {
        string type = scene.minigame;
        UI.ClearChoices();
        // Пришёл на смену голодным — обморок прямо на работе, больница
        if (S.satiety <= B.WORK_FAINT_SATIETY)
        {
            S.jobDay = S.day;
            S.lastJob = type;
            Game.Render("faint_work");
            return;
        }
        if (!Mastered(type))
        {
            UI.SetText(Tpl.Apply(scene.text));
            UI.AddButton(Data.T("jobs.start"), () => MINIGAMES[type]());
            return;
        }
        UI.SetText(Tpl.Apply(scene.S("textMastered")));
        UI.AddButton(Tpl.Fill(Data.T("jobs.auto"), "pay", JobPay(type)), () =>
        {
            S.jobDay = S.day;
            Finish(type, true, true);
        });
    }

    static int Begin(string type)
    {
        S.jobDay = S.day;
        UI.ShowCast(new List<string>());
        // Во время смены вместо длинных правил — короткая памятка
        UI.SetText(Tpl.Apply(Data.Scene("job_" + type).S("playText")));
        UI.ClearChoices();
        return ++run;
    }

    // auto — освоенная смена без мини-игры
    static void Finish(string type, bool success, bool auto = false)
    {
        run++;
        S.lastJob = type;
        S.jobShifts++;
        Rules.ChangeConscience(B.CONSCIENCE_JOB); // заработал сам
        lastShiftSuccess = success;
        Rules.ChangeAddiction(-B.JOB_ADDICTION_RELIEF);

        if (!success)
        {
            S.lastShiftPay = 0;
            AddFail(type);
            Game.Render(S.jobFails[type] >= B.JOB_MAX_FAILS ? "job_" + type + "_fired" : "job_" + type + "_fail");
            return;
        }

        int pay = JobPay(type);
        S.money += pay;
        S.income.jobs += pay;
        S.lastShiftPay = pay;
        S.jobWins[type]++;
        if (auto) Game.Render("job_" + type + "_auto");
        else if (S.jobWins[type] == B.JOB_MASTERY_WINS)
        {
            if (JOB_PROMOTION_OPENS.TryGetValue(type, out var opens)) S.jobsOpen[opens] = true;
            Game.Render("job_" + type + "_promo");
        }
        else Game.Render("job_" + type + "_win");
    }

    // ===== СОБЫТИЯ ПОСЛЕ СМЕНЫ =====
    // Сцена события → когда оно может случиться. Каждое — один раз за игру
    static readonly (string id, Func<string, bool, bool> when)[] JOB_EVENTS =
    {
        ("job_event_granny", (t, ok) => t == "grocery"),
        ("job_event_banner", (t, ok) => t == "flyers"),
        ("job_event_parcel", (t, ok) => t == "courier" && ParcelLoot() != null),
        ("job_event_bookmaker", (t, ok) => t == "courier"),
        ("job_event_change", (t, ok) => t == "cafe"),
        ("job_event_taxi", (t, ok) => t == "cafe"),
        ("job_event_classmates", (t, ok) => true),
        ("job_event_max", (t, ok) => Rules.MaxIsFriend()),
        ("job_event_bonus", (t, ok) => ok),
        ("job_event_pasha", (t, ok) => S.pashaSaved && S.day > B.PASHA_ARREST_DAY),
        ("job_event_rain", (t, ok) => t == "flyers"),
        ("job_event_teacher", (t, ok) => t == "flyers" || t == "grocery"),
        ("job_event_watermelon", (t, ok) => t == "grocery"),
        ("job_event_overtime", (t, ok) => t == "courier" && ok),
        ("job_event_ashot_son", (t, ok) => t == "cafe"),
        ("job_event_wallet", (t, ok) => true),
    };

    // Порванная посылка: внутри вещь, которой у героя ещё нет (наушники или часы)
    static string ParcelLoot() => new[] { "headphones", "watch" }.FirstOrDefault(id => !Rules.HasItem(id));

    public static void Register()
    {
        // ===== ПОСЛЕ СМЕНЫ: ДОМОЙ ИЛИ В МАГАЗИН ПО ПУТИ =====
        foreach (var type in JOB_PAYS.Keys)
            foreach (var result in new[] { "win", "fail", "fired", "promo", "auto" })
                Game.Builders["job_" + type + "_" + result] = (choices, _) =>
                {
                    choices.Add(new Choice { text = Data.T("jobs.detourTech"), next = "job_done", effect = new Effect { detour = "shop_electronics" } });
                    choices.Add(new Choice { text = Data.T("jobs.detourClothes"), next = "job_done", effect = new Effect { detour = "shop_clothes" } });
                    return choices;
                };

        // ===== ИЗВИНИТЬСЯ И ВЕРНУТЬСЯ НА РАБОТУ =====
        Game.Texts["job_apology"] = scene => scene.Sub("texts", S.lastJob ?? "") ?? scene.text;
        // Фон — той работы, куда пришёл проситься
        Action<SceneDef> apology = scene =>
        {
            UI.SetBackground((Data.Scene("job_" + S.lastJob) ?? Data.Scene("job_flyers")).bg);
            Game.RenderStandard(S.scene, scene);
        };
        Game.Screens["job_apology"] = apology;
        Game.Screens["job_apology_ok"] = apology;

        Game.Routes["job_done"] = _ =>
        {
            var ready = JOB_EVENTS.Where(e => !S.jobEventsSeen.Contains(e.id) && e.when(S.lastJob, lastShiftSuccess)).Select(e => e.id).ToList();
            if (ready.Count > 0 && Rules.CanHaveRandomEvent() && Rules.Rand() < B.JOB_EVENT_CHANCE)
            {
                var ev = Rules.Pick(ready);
                S.jobEventsSeen.Add(ev);
                Rules.MarkRandomEvent();
                Game.Render(ev);
            }
            else Game.Render("go_home");
            return true;
        };

        Game.Texts["job_event_banner"] = scene => S.betsMade == 0 ? scene.S("textNeverPlayed") : scene.text;
        Game.Texts["job_event_parcel"] = scene => Tpl.Fill(scene.text, "lootItem", Data.Item(ParcelLoot()).name.ToLower());

        // Взял себе — могут заметить: тогда с сортировки выгоняют
        Game.Routes["job_event_parcel_take"] = _ =>
        {
            if (Rules.Rand() < B.COURIER_EVENT_TAKE_NOTICED)
            {
                AddFail("courier", B.JOB_MAX_FAILS);
                Game.Render("job_event_parcel_caught");
            }
            else
            {
                Rules.GiveItem(ParcelLoot(), "stolen");
                S.theftsDone++;
                Game.Render("job_event_parcel_kept");
            }
            return true;
        };

        // Оставил себе лишнюю тысячу — хозяин может заметить недостачу в кассе
        Game.Routes["job_event_change_keep"] = _ =>
        {
            if (Rules.Rand() < B.CAFE_EVENT_KEEP_NOTICED)
            {
                AddFail("cafe");
                Game.Render("job_event_change_caught");
            }
            else
            {
                S.money += 1000;
                Game.Render("job_event_change_kept");
            }
            return true;
        };
    }

    // ===== ЛИСТОВКИ: прохожие идут по одному, листовку берут только те, кто не спешит =====
    static void StartFlyers()
    {
        int myRun = Begin("flyers");
        var receptive = Data.C.FLYER_PASSERS.Where(p => p.ok).ToList();
        var hurried = Data.C.FLYER_PASSERS.Where(p => !p.ok).ToList();
        var people = Rules.Shuffle(
            Enumerable.Range(0, B.FLYERS_RECEPTIVE).Select(_ => Rules.Pick(receptive))
            .Concat(Enumerable.Range(0, B.FLYERS_PASSERS - B.FLYERS_RECEPTIVE).Select(_ => Rules.Pick(hurried))));
        int given = 0, refusals = 0;
        FlyerPasser current = null;
        bool acted = false;

        MiniView.PreparePassers();
        var view = MiniView.Create();
        void ShowStats() => view.Stats(Tpl.Fill(Data.T("jobs.flyers.stats"),
            "given", given, "goal", B.FLYERS_GOAL, "refusals", refusals, "maxRefusals", B.FLYERS_MAX_REFUSALS, "left", people.Count));

        UI.AddButton(Data.T("jobs.flyers.give"), () =>
        {
            if (myRun != run || current == null || acted) return;
            acted = true;
            if (current.ok) given++;
            else refusals++;
            view.Feedback(Data.T(current.ok ? "jobs.flyers.taken" : "jobs.flyers.refused"), current.ok);
            view.PasserReact(current.ok);
            ShowStats();
            if (given >= B.FLYERS_GOAL) Finish("flyers", true);
            else if (refusals >= B.FLYERS_MAX_REFUSALS) Finish("flyers", false);
        });

        void NextPasser()
        {
            if (myRun != run) return;
            if (people.Count == 0)
            {
                Finish("flyers", false);
                return;
            }
            current = people[0];
            people.RemoveAt(0);
            acted = false;
            view.Passer(current, B.FLYERS_PASS_MS / 1000f);
            view.Caption(current.desc);
            view.Feedback("", true);
            ShowStats();
            view.Timer(B.FLYERS_PASS_MS / 1000f);
            Run.After(B.FLYERS_PASS_MS / 1000f, NextPasser);
        }
        NextPasser();
    }

    // ===== ОВОЩНАЯ ЛАВКА: запомнить заказ, пока он виден, и собрать корзину без ошибок =====
    static void StartGrocery()
    {
        int myRun = Begin("grocery");
        var rounds = B.GROCERY_ROUNDS;
        int round = 0, wins = 0, losses = 0;
        int roundsToLose = rounds.Length - B.GROCERY_GOAL + 1;
        var order = new List<Product>();
        var picked = new List<Product>();
        string phase = "show";

        var view = MiniView.Create();
        void ShowStats() => view.Stats(Tpl.Fill(Data.T("jobs.grocery.stats"),
            "round", Math.Min(round + 1, rounds.Length), "rounds", rounds.Length, "wins", wins, "goal", B.GROCERY_GOAL));

        GroceryGrid grid = null;
        grid = view.Grocery(Data.C.GROCERY_PRODUCTS, p => PickProduct(p));

        void StartRound()
        {
            if (myRun != run) return;
            int size = rounds[round];
            order = Rules.Shuffle(Data.C.GROCERY_PRODUCTS).Take(size).ToList();
            picked = new List<Product>();
            phase = "show";
            view.Order(order, size, false);
            view.Caption(Data.T("jobs.grocery.memorize"));
            view.Feedback("", true);
            grid.SetEnabled(false);
            ShowStats();
            view.Timer(B.GROCERY_SHOW_MS / 1000f);
            Run.After(B.GROCERY_SHOW_MS / 1000f, () =>
            {
                if (myRun != run) return;
                phase = "pick";
                view.Order(picked, size, true);
                view.Caption(Tpl.Fill(Data.T("jobs.grocery.pick"), "size", size));
                grid.SetEnabled(true);
            });
        }

        void EndRound(bool ok)
        {
            phase = "done";
            if (ok) wins++;
            else losses++;
            string names = string.Join(", ", order.Select(p => p.name));
            view.Feedback(ok ? Data.T("jobs.grocery.right") : Tpl.Fill(Data.T("jobs.grocery.wrong"), "order", names), ok);
            if (!ok) view.Order(order, order.Count, false);
            grid.SetEnabled(false);
            round++;
            ShowStats();
            Run.After(1.2f, () =>
            {
                if (myRun != run) return;
                if (wins >= B.GROCERY_GOAL) Finish("grocery", true);
                else if (losses >= roundsToLose) Finish("grocery", false);
                else StartRound();
            });
        }

        void PickProduct(Product p)
        {
            if (myRun != run || phase != "pick" || picked.Contains(p)) return;
            grid.MarkPicked(p);
            if (!order.Contains(p))
            {
                EndRound(false);
                return;
            }
            picked.Add(p);
            view.Order(picked, order.Count, true);
            if (picked.Count == order.Count) EndRound(true);
        }

        StartRound();
    }

    // ===== СОРТИРОВКА ПОСЫЛОК: улица на наклейке → район =====
    class Parcel { public District district; public string street; public int house; }

    static void StartCourier()
    {
        int myRun = Begin("courier");
        var parcels = Enumerable.Range(0, B.COURIER_PARCELS).Select(_ =>
        {
            var d = Rules.Pick(Data.C.COURIER_DISTRICTS);
            return new Parcel { district = d, street = Rules.Pick(d.streets), house = 1 + Rules.RandInt(60) };
        }).ToList();
        int index = -1, errors = 0;
        bool acted = true; // перед первой посылкой «опоздания» нет

        var view = MiniView.Create();
        void ShowStats() => view.Stats(Tpl.Fill(Data.T("jobs.courier.stats"),
            "n", Math.Min(index + 1, parcels.Count), "total", parcels.Count, "errors", errors, "max", B.COURIER_MAX_ERRORS));
        void Mark(bool ok, string text)
        {
            acted = true;
            if (!ok) errors++;
            view.Feedback(text, ok);
            ShowStats();
            if (errors >= B.COURIER_MAX_ERRORS) Finish("courier", false);
        }

        // Посылка уезжает по таймеру или через полсекунды после ответа — что раньше (from — номер посылки)
        void Advance(int from)
        {
            if (myRun != run || index != from) return;
            if (!acted)
            {
                Mark(false, Data.T("jobs.courier.late"));
                if (myRun != run) return;
            }
            index++;
            if (index >= parcels.Count)
            {
                Finish("courier", true);
                return;
            }
            var parcel = parcels[index];
            int current = index;
            acted = false;
            view.Parcel(Tpl.Fill(Data.T("jobs.courier.label"), "street", parcel.street, "house", parcel.house));
            view.Caption(Data.T("courierUnity.hint"));
            ShowStats();
            view.Timer(B.COURIER_PARCEL_MS / 1000f);
            Run.After(B.COURIER_PARCEL_MS / 1000f, () => Advance(current));
        }

        foreach (var d in Data.C.COURIER_DISTRICTS)
        {
            var district = d;
            UI.AddButton(d.emoji + " " + d.name, () =>
            {
                if (myRun != run || acted || index < 0) return;
                var parcel = parcels[index];
                if (district == parcel.district) Mark(true, Data.T("jobs.courier.right"));
                else Mark(false, Tpl.Fill(Data.T("jobs.courier.wrong"), "district", parcel.district.emoji + " " + parcel.district.name));
                int current = index;
                if (myRun == run) Run.After(0.5f, () => Advance(current));
            });
        }

        Advance(-1);
    }

    // ===== КАССА В ШАУРМИЧНОЙ: посчитать сдачу =====
    class CafeOrder { public List<MenuItem> items; public int total, bill, change; public List<int> options; }

    static CafeOrder MakeCafeOrder()
    {
        int count = 1 + Rules.RandInt(3);
        var items = Rules.Shuffle(Data.C.CAFE_MENU).Take(count).ToList();
        int total = items.Sum(i => i.price);
        var fits = Data.C.CAFE_BILLS.Where(b => b >= total).ToList();
        int bill = Rules.Pick(fits);
        int change = bill - total;
        var wrong = Rules.Shuffle(new[] { 10, 50, 100, -10, -50, -100 }).Select(d => change + d).Where(v => v >= 0).ToList();
        return new CafeOrder { items = items, total = total, bill = bill, change = change, options = Rules.Shuffle(new[] { change, wrong[0], wrong[1] }) };
    }

    static void StartCafe()
    {
        int myRun = Begin("cafe");
        int index = -1, errors = 0;
        bool acted = false;
        CafeOrder order = null;

        var view = MiniView.Create();
        void ShowStats() => view.Stats(Tpl.Fill(Data.T("jobs.cafe.stats"),
            "n", Math.Min(index + 1, B.CAFE_CUSTOMERS), "total", B.CAFE_CUSTOMERS, "errors", errors, "max", B.CAFE_MAX_ERRORS));

        void Next()
        {
            if (myRun != run) return;
            index++;
            if (index >= B.CAFE_CUSTOMERS)
            {
                Finish("cafe", true);
                return;
            }
            order = MakeCafeOrder();
            int customer = index;
            acted = false;
            view.Receipt(Tpl.Fill(Data.T("jobs.cafe.order"), "items", string.Join("\n", order.items.Select(i => i.name + " — " + i.price + " ₽")), "bill", order.bill), index);
            view.Feedback("", true);
            ShowStats();
            UI.ClearChoices();
            foreach (var v in order.options)
            {
                int value = v;
                UI.AddButton(Tpl.Fill(Data.T("jobs.cafe.option"), "change", v), () => Answer(value));
            }
            view.Timer(B.CAFE_ANSWER_MS / 1000f);
            Run.After(B.CAFE_ANSWER_MS / 1000f, () =>
            {
                if (myRun != run || index != customer || acted) return;
                Answer(int.MinValue); // не успел — ошибка
            });
        }

        void Answer(int value)
        {
            if (myRun != run || acted || order == null) return;
            acted = true;
            bool ok = value == order.change;
            if (!ok) errors++;
            view.Feedback(ok ? Data.T("jobs.cafe.right") : Tpl.Fill(Data.T("jobs.cafe.wrong"), "change", order.change), ok);
            ShowStats();
            if (errors >= B.CAFE_MAX_ERRORS)
            {
                Finish("cafe", false);
                return;
            }
            Run.After(0.9f, Next);
        }

        Next();
    }
}
