// Рулетка: 53.5% проигрыш, 46% ×1.9, 0.5% джекпот ×20 (RTP ~97%) (+ скрытый джекпот, подкрутка с RIGGING_START_DAY).
using UnityEngine;

public static class Roulette
{
    static GameState S => G.S;

    // Границы секторов в градусах (по часовой от верха) — так же нарисовано колесо (Art/GameArt.Wheel)
    public static readonly float[] LOSS = { 0f, 192.6f }, X19 = { 192.6f, 358.2f }, X20 = { 358.2f, 360f };

    static RectTransform wheel;

    public static void Register() { }

    public static void RenderScreen()
    {
        if (S.money <= 0)
        {
            Game.Render("no_money");
            return;
        }
        S.lastGame = "bet_roulette";
        UI.SetText(Tpl.Fill(Data.T("roulette.info"), "mood", MoodText()));
        DrawWheel();
        UI.ClearChoices();
        Casino.AddBetButtons(B.ROULETTE_BET_AMOUNTS, Spin);
        Casino.AddNavButtons();
    }

    static string MoodText() => Casino.MoodText();

    static void DrawWheel()
    {
        var board = UI.ShowBoard();
        var at = UI.BoardPoint(Backgrounds.CasinoRing);
        var holder = UI.Rect(board, "WheelHolder", at, at);
        holder.sizeDelta = new Vector2(620, 620);
        UI.Image(holder, "Shadow", GameArt.Glow(new Color(0.95f, 0.3f, 0.5f, 0.55f)), new Vector2(-90, -90), new Vector2(90, 90));
        wheel = UI.Image(holder, "Wheel", GameArt.Wheel(), Vector2.zero, Vector2.zero).rectTransform;
        UI.Image(holder, "Hub", GameArt.WheelHub(), new Vector2(230, 230), new Vector2(-230, -230));
        var pointer = UI.Image(holder, "Pointer", GameArt.Pointer(), Vector2.zero, Vector2.zero).rectTransform;
        pointer.anchorMin = pointer.anchorMax = new Vector2(0.5f, 1f);
        pointer.sizeDelta = new Vector2(70, 90);
        pointer.anchoredPosition = new Vector2(0, 10);
    }

    static string Roll(string forced)
    {
        if (forced == "loss") return "loss";
        if (forced == "win") return "x19";
        string result;
        if (S.betsMade == B.HIDDEN_JACKPOT_BET && !S.jackpotWon && Rules.Rand() < B.HIDDEN_JACKPOT_CHANCE) result = "x20";
        else
        {
            float f = Casino.WinFactor();
            float r = Rules.Rand();
            if (r < B.ROULETTE_JACKPOT * f) result = "x20";
            else if (r < (B.ROULETTE_JACKPOT + B.ROULETTE_WIN) * f) result = "x19";
            else result = "loss";
        }
        // Джекпот — один на игру; повторный превращается в обычный выигрыш
        if (result == "x20" && S.jackpotWon) result = "x19";
        return result;
    }

    static void Spin(int amount)
    {
        if (amount <= 0 || amount > S.money) return;

        string result = Roll(Casino.TakeBet(amount));
        UI.SetText(Tpl.Fill(Data.T("roulette.spinning"), "amount", amount));
        SpinWheelTo(result);

        Run.After(2.3f, () =>
        {
            if (result == "loss") Casino.FinishLoss(amount);
            else if (result == "x19") Casino.FinishWin(Mathf.RoundToInt(amount * 1.9f), Data.T("roulette.win"));
            else Casino.WinBigJackpot(amount, 20);
        });
    }

    // Колесо останавливается в случайной точке выпавшего сектора, не вплотную к границе
    static void SpinWheelTo(string result)
    {
        var s = result == "loss" ? LOSS : result == "x19" ? X19 : X20;
        float from = s[0], to = s[1];
        float pad = Mathf.Min(3f, (to - from) / 4f);
        float landAt = from + pad + Rules.Rand() * (to - from - 2 * pad);
        int spins = 4 + Rules.RandInt(3);
        float target = spins * 360f + (360f - landAt);
        var w = wheel;
        Run.Tween(2.2f, k =>
        {
            if (w == null) return;
            // Как cubic-bezier(0.17, 0.67, 0.12, 1): быстрый разгон и долгое торможение
            float e = 1f - Mathf.Pow(1f - k, 3.2f);
            w.localEulerAngles = new Vector3(0, 0, -target * e);
        });
    }
}
