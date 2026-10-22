// Ставки на матчи: киберспорт, матч до двух побед. Коэффициенты честные минус маржа конторы.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MatchGame
{
    static GameState S => G.S;

    static Text scoreText, logText;
    static BoardButton[] teamButtons;

    public static void Register() { }

    static MatchState Create()
    {
        var teams = Rules.Shuffle(Data.C.MATCH_TEAMS);
        float chanceA = 0.3f + Rules.Rand() * 0.4f;
        float Odds(float p) => Mathf.Max(1.05f, Mathf.Round(B.MATCH_MARGIN / p * 100f) / 100f);
        var m = new MatchState();
        m.teams[0] = teams[0];
        m.teams[1] = teams[1];
        m.chances[0] = chanceA;
        m.chances[1] = 1 - chanceA;
        m.odds[0] = Odds(chanceA);
        m.odds[1] = Odds(1 - chanceA);
        return m;
    }

    public static void RenderScreen()
    {
        if (S.money <= 0)
        {
            Game.Render("no_money");
            return;
        }
        S.lastGame = "bet_match";
        if (S.currentMatch == null) S.currentMatch = Create();
        var match = S.currentMatch;

        DrawCard(match);
        scoreText.text = "VS";
        logText.text = Data.T("match.league");

        string intro = Tpl.Fill(Data.T("match.intro"), "mood", Casino.MoodText());
        UI.ClearChoices();
        if (match.pick < 0) UI.SetText(intro + Data.T("match.pickTeam"));
        else
        {
            UI.SetText(intro + Tpl.Fill(Data.T("match.pickAmount"), "team", match.teams[match.pick], "odds", Txt.F2(match.odds[match.pick])));
            Casino.AddBetButtons(B.MATCH_BET_AMOUNTS, Play);
        }
        Casino.AddNavButtons();
    }

    static void DrawCard(MatchState match)
    {
        var board = UI.ShowBoard();
        var card = UI.Panel(board, "MatchCard", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.82f), Theme.CardDark);
        UI.Label(card, Data.T("match.liveTitle"), 26, Theme.Accent, TextAnchor.UpperCenter, new Vector2(0, 0.86f), new Vector2(1, 0.97f));
        teamButtons = new BoardButton[2];
        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            var min = new Vector2(i == 0 ? 0.04f : 0.62f, 0.3f);
            var max = new Vector2(i == 0 ? 0.38f : 0.96f, 0.82f);
            var btn = UI.TeamButton(card, match.teams[i], Txt.F2(match.odds[i]), i, min, max, () =>
            {
                match.pick = idx;
                RenderScreen();
            });
            btn.SetPicked(match.pick == i);
            teamButtons[i] = btn;
        }
        scoreText = UI.Label(card, "VS", 64, Theme.Text, TextAnchor.MiddleCenter, new Vector2(0.38f, 0.4f), new Vector2(0.62f, 0.75f));
        scoreText.fontStyle = FontStyle.Bold;
        logText = UI.Label(card, "", 24, Theme.TextDim, TextAnchor.UpperCenter, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.28f));
    }

    static void Play(int amount)
    {
        if (amount <= 0 || amount > S.money) return;

        var match = S.currentMatch;
        int pick = match.pick;
        string forced = Casino.TakeBet(amount);
        S.currentMatch = null; // в следующий раз — новый матч

        int winner;
        if (forced == "loss") winner = 1 - pick;
        else if (forced == "win") winner = pick;
        else winner = Rules.Rand() < match.chances[pick] * Casino.WinFactor() ? pick : 1 - pick;

        // Победитель берёт две карты, проигравший — ноль или одну (но не последнюю)
        var maps = new List<int> { winner, winner };
        if (Rules.Rand() < 0.5f) maps.Insert(Rules.RandInt(2), 1 - winner);

        UI.SetText(Tpl.Fill(Data.T("match.started"), "amount", amount, "team", match.teams[pick]));
        foreach (var b in teamButtons) b.Interactable = false;

        var score = new int[2];
        scoreText.text = "0 : 0";
        logText.text = "";
        for (int i = 0; i < maps.Count; i++)
        {
            int mapWinner = maps[i], n = i + 1;
            Run.After(0.9f * n, () =>
            {
                score[mapWinner]++;
                scoreText.text = score[0] + " : " + score[1];
                logText.text += Txt.Clean(Tpl.Fill(Data.T("match.map"), "n", n, "team", match.teams[mapWinner]));
                teamButtons[mapWinner].Pulse();
            });
        }

        Run.After(0.9f * maps.Count + 0.6f, () =>
        {
            teamButtons[0].SetResult(winner == 0);
            teamButtons[1].SetResult(winner == 1);
            if (winner == pick) Casino.FinishWin(Mathf.RoundToInt(amount * match.odds[pick]), Tpl.Fill(Data.T("match.win"), "odds", Txt.F2(match.odds[pick])));
            else Casino.FinishLoss(amount);
        });
    }
}
