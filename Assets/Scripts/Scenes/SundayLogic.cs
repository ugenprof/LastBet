// Воскресенье — выходной: ни школы, ни подработок. С кем (друзья, если есть) → куда (парк, торговый центр,
// двор, дома) → занятия за деньги (SUNDAY_ACTIVITIES): еда поднимает питание, аттракционы, кино и футбол
// отвлекают от ставок, автомат-хваталка — тот же азарт. В каждом месте может случиться событие (одно в день).
using System;
using System.Collections.Generic;
using System.Linq;

public static class SundayLogic
{
    static GameState S => G.S;

    static readonly Dictionary<string, (string id, Func<bool> when)[]> SUNDAY_EVENTS = new Dictionary<string, (string, Func<bool>)[]>
    {
        { "park", new (string, Func<bool>)[] { ("sunday_event_lost_kid", () => true), ("sunday_event_lottery", () => S.money >= B.SUNDAY_LOTTERY_COST) } },
        { "mall", new (string, Func<bool>)[] { ("sunday_event_arcade", () => S.money >= B.SUNDAY_ARCADE_COST), ("sunday_event_promo", () => S.betsMade > 0) } },
        { "yard", new (string, Func<bool>)[] { ("sunday_event_window", () => S.money >= B.SUNDAY_WINDOW_COST), ("sunday_event_coach", () => true) } },
        { "home", new (string, Func<bool>)[] { ("sunday_event_dad_garage", () => Rules.ParentsTrust()), ("sunday_event_mom_cooking", () => true) } },
    };

    static List<string> Friends()
    {
        var friends = new List<string>();
        if (Rules.MaxIsFriend()) friends.Add("max");
        if (S.pashaSaved && S.day > B.PASHA_ARREST_DAY) friends.Add("pasha");
        return friends;
    }

    public static void Register()
    {
        // Новое воскресенье: обед дома, список занятий с нуля. Друзей нет — сразу выбор места
        Game.Routes["sunday_plans"] = _ =>
        {
            if (S.lunchDay != S.day)
            {
                S.lunchDay = S.day;
                Rules.ChangeSatiety(B.SUNDAY_LUNCH_SATIETY);
                S.sundayDone = new List<string>();
                S.sundayLast = null;
                S.sundayPlace = null;
                S.sundayCompany = "alone";
            }
            if (Friends().Count == 0)
            {
                Game.Render("sunday_where");
                return true;
            }
            return false;
        };

        Game.Builders["sunday_plans"] = (choices, _) =>
            choices.Where(c => c.effect?.company == "alone" || Friends().Contains(c.effect?.company)).ToList();

        Game.Texts["sunday_where"] = scene => Tpl.Fill(Game.BaseText(scene), "companyLine", scene.Sub("company", S.sundayCompany));

        foreach (var place in Data.C.SUNDAY_ACTIVITIES.Keys)
        {
            string p = place;
            string sceneId = "sunday_" + p;

            // Первый вход: «день с другом» (один раз) и, может быть, событие
            Game.Routes[sceneId] = _ =>
            {
                if (S.sundayPlace == p) return false;
                S.sundayPlace = p;
                S.sundayLast = null;
                if (S.sundayCompany != "alone" && S.sundayFriendDay != S.day && p != "home")
                {
                    S.sundayFriendDay = S.day;
                    Rules.ChangeAddiction(-B.SUNDAY_FRIEND_RELIEF);
                }
                var ev = EventFor(p);
                if (ev != null)
                {
                    Game.Render(ev);
                    return true;
                }
                return false;
            };

            // Текст: что было в последнем занятии — или как пришёл (с кем)
            Game.Texts[sceneId] = scene =>
            {
                if (S.sundayLast != null)
                {
                    string result = scene.Sub("activities", S.sundayLast, "result");
                    if (S.sundayDiscounted) result += "\n\n" + Data.T("sunday.ashotDiscount");
                    return result + "\n\n" + scene.S("more");
                }
                return Tpl.Fill(Game.BaseText(scene), "companyLine", scene.Has("company") ? scene.Sub("company", S.sundayCompany) : "");
            };

            // Занятия, которых сегодня ещё не было; на что не хватает денег — с пометкой
            Game.Builders[sceneId] = (choices, scene) =>
            {
                var activities = Data.C.SUNDAY_ACTIVITIES[p]
                    .Where(kv => !S.sundayDone.Contains(p + ":" + kv.Key))
                    .Where(kv => kv.Key != "boardgame" || Rules.ParentsTrust())
                    .Select(kv =>
                    {
                        var a = kv.Value;
                        int cost = Price(p, kv.Key, a);
                        string price = cost < a.price ? Tpl.Fill(Data.T("sunday.ashotPrice"), "price", cost, "old", a.price)
                            : a.price > 0 ? Tpl.Fill(Data.T("sunday.price"), "price", a.price) : Data.T("sunday.free");
                        var choice = new Choice { text = scene.Sub("activities", kv.Key, "label") + " " + price, next = sceneId, sundayActivity = p + ":" + kv.Key };
                        return S.money < cost ? Choices.WithNote(choice, Data.T("shop.tooExpensive")) : choice;
                    }).ToList();
                activities.AddRange(choices);
                return activities;
            };
        }
    }

    static string EventFor(string place)
    {
        if (!Rules.CanHaveRandomEvent() || Rules.Rand() >= B.SUNDAY_EVENT_CHANCE) return null;
        var ready = SUNDAY_EVENTS[place].Where(e => !S.sundayEventsSeen.Contains(e.id) && e.when()).Select(e => e.id).ToList();
        if (ready.Count == 0) return null;
        var id = Rules.Pick(ready);
        S.sundayEventsSeen.Add(id);
        Rules.MarkRandomEvent();
        return id;
    }

    // Ашот помнит своего кассира: повышение в шаурмичной и сыграл в футбол во дворе — шаурма со скидкой
    public static bool AshotDiscount(string place, string activity) =>
        place == "yard" && activity == "shawarma" && Jobs.Mastered("cafe") && S.sundayDone.Contains("yard:football");

    static int Price(string place, string activity, SundayActivity a) =>
        AshotDiscount(place, activity) ? a.price * (100 - BU.ASHOT_FRIEND_DISCOUNT) / 100 : a.price;

    public static void ApplyActivity(string key)
    {
        var parts = key.Split(':');
        var a = Data.C.SUNDAY_ACTIVITIES[parts[0]][parts[1]];
        int cost = Price(parts[0], parts[1], a);
        if (S.money < cost) return;
        S.money -= cost;
        S.sundayDiscounted = cost < a.price;
        if (a.satiety != 0) Rules.ChangeSatiety(a.satiety);
        if (a.addiction != 0) Rules.ChangeAddiction(a.addiction);
        S.sundayDone.Add(key);
        S.sundayLast = parts[1];
    }
}
