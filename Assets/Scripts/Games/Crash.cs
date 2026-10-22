// Ракета (краш, глава 2): множитель растёт, надо забрать до взрыва.
// Точка взрыва: P(долетит до ×m) = CRASH_RTP / m — при любой тактике в среднем возвращается 96% ставки,
// в 4% случаев ракета взрывается сразу. С RIGGING_START_DAY полёт незаметно укорачивается.
using UnityEngine;
using UnityEngine.UI;

public static class Crash
{
    static GameState S => G.S;

    static int run;
    static RectTransform rocket, area;
    static Image rocketImg;
    static Text multText;
    static TrailLine trail;

    public static void Register()
    {
        Game.Screens["bet_crash"] = _ => RenderScreen();
    }

    static void RenderScreen()
    {
        if (S.money <= 0)
        {
            Game.Render("no_money");
            return;
        }
        S.lastGame = "bet_crash";
        UI.SetText(Tpl.Fill(Data.T("crash.info"), "mood", Casino.MoodText()));
        DrawScreen();
        Draw(1f, false);
        UI.ClearChoices();
        Casino.AddBetButtons(B.CRASH_BET_AMOUNTS, Launch);
        Casino.AddNavButtons();
    }

    static void DrawScreen()
    {
        var board = UI.ShowBoard();
        area = UI.Panel(board, "Sky", new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), Color.white);
        area.GetComponent<Image>().sprite = GameArt.CrashSky();
        area.GetComponent<Image>().type = Image.Type.Simple;
        trail = TrailLine.Create(area);
        rocketImg = UI.Image(area, "Rocket", GameArt.Rocket(), Vector2.zero, Vector2.zero);
        rocket = rocketImg.rectTransform;
        rocket.anchorMin = rocket.anchorMax = Vector2.zero;
        rocket.sizeDelta = new Vector2(120, 120);
        multText = UI.Label(area, "", 72, Theme.Text, TextAnchor.UpperLeft, new Vector2(0.05f, 0.7f), new Vector2(0.6f, 0.96f));
        multText.fontStyle = FontStyle.Bold;
    }

    // Чем выше множитель, тем выше и правее ракета (до края экрана)
    static void Draw(float mult, bool boom)
    {
        if (rocket == null) return;
        float progress = Mathf.Min(1f, Mathf.Log(mult) / Mathf.Log(10f));
        var size = area.rect.size;
        var pos = new Vector2(size.x * (0.12f + progress * 0.7f), size.y * (0.1f + progress * 0.6f));
        rocket.anchoredPosition = pos;
        rocket.localEulerAngles = new Vector3(0, 0, boom ? 0 : -45 + 20 * (1 - progress));
        rocketImg.sprite = boom ? GameArt.Boom() : GameArt.Rocket();
        rocket.sizeDelta = boom ? new Vector2(200, 200) : new Vector2(120, 120);
        multText.text = "×" + Txt.F2(mult);
        multText.color = boom ? Theme.Bad : mult >= 2f ? Theme.Good : Theme.Text;
        if (!boom) trail.Add(pos);
    }

    // Где взорвётся ракета
    static float Point(string forced)
    {
        if (forced == "loss") return 1f;
        if (forced == "win") return 2f + Rules.Rand() * 3f; // «новичкам везёт»
        float point = B.CRASH_RTP / (1f - Rules.Rand());
        if (point < 1f) return 1f;
        return Mathf.Min(B.CRASH_MAX, 1f + (point - 1f) * Casino.WinFactor());
    }

    static void Launch(int amount)
    {
        if (amount <= 0 || amount > S.money) return;
        int myRun = ++run;
        float point = Point(Casino.TakeBet(amount));
        float mult = 1f;
        bool cashed = false, flying = false;
        DrawScreen();
        Draw(mult, false);
        UI.SetText(Tpl.Fill(Data.T("crash.flying"), "amount", amount));

        ChoiceButton cashout = null;
        cashout = UI.AddButton(Tpl.Fill(Data.T("crash.cashout"), "mult", "1.00", "payout", amount), () =>
        {
            if (myRun != run || cashed || !flying) return;
            cashed = true;
            run++;
            cashout.Disabled = true;
            Casino.FinishWin(Mathf.RoundToInt(amount * mult), Tpl.Fill(Data.T("crash.win"), "mult", Txt.F2(mult)));
        });

        void Tick()
        {
            if (myRun != run || cashed) return;
            float next = mult * B.CRASH_GROWTH;
            if (next >= point)
            {
                run++;
                cashout.Disabled = true;
                Draw(point, true);
                UI.Shake();
                UI.SetText(Tpl.Fill(Data.T("crash.boom"), "mult", Txt.F2(point)));
                Casino.FinishLoss(amount);
                return;
            }
            mult = next;
            Draw(mult, false);
            cashout.SetLabel(Tpl.Fill(Data.T("crash.cashout"), "mult", Txt.F2(mult), "payout", Mathf.RoundToInt(amount * mult)));
            Run.After(B.CRASH_TICK_MS / 1000f, Tick);
        }

        // Обратный отсчёт: успеть навести на «Забрать». Потом полёт (взрыв на старте — сразу)
        cashout.Disabled = true;
        const int steps = 3;
        for (int i = 0; i < steps; i++)
        {
            int n = steps - i;
            Run.After(B.CRASH_COUNTDOWN_MS / 1000f / steps * i, () =>
            {
                if (myRun == run) UI.SetText(Tpl.Fill(Data.T("crashCountdown"), "n", n));
            });
        }
        Run.After(B.CRASH_COUNTDOWN_MS / 1000f, () =>
        {
            if (myRun != run) return;
            flying = true;
            cashout.Disabled = false;
            UI.SetText(Tpl.Fill(Data.T("crash.flying"), "amount", amount));
            Run.After(point <= 1f ? 0.3f : B.CRASH_TICK_MS / 1000f, Tick);
        });
    }
}
