// Кнопки выбора: сборка, фильтры по правилам игры, отрисовка.
using System.Collections.Generic;
using System.Linq;

public static class Choices
{
    static GameState S => G.S;

    // Сцена → итоговые кнопки
    public static List<Choice> Prepare(string sceneId, SceneDef scene, out bool showFakeSleep)
    {
        var choices = scene.choices.Select(c => c.Clone()).ToList();
        if (Game.Builders.TryGetValue(sceneId, out var build)) choices = build(choices, scene);
        return Filter(sceneId, choices, out showFakeSleep);
    }

    static List<Choice> Filter(string sceneId, List<Choice> choices, out bool showFakeSleep)
    {
        // Без денег в казино не попасть (при зависимости 100 хватит и копейки)
        int minToPlay = S.addiction >= B.ADDICTION_MAX ? 1 : B.BET_STEP;
        if (Rules.BetBalance() < minToPlay) choices = choices.Where(c => c.next != "first_bet").ToList();

        if (!Rules.CanQuitApp()) choices = choices.Where(c => !c.quit).ToList();

        // Телефон у папы — казино нет. Приложение удалено — вместо «открыть» только «скачать заново» (срыв)
        if (Rules.PhoneLocked()) choices = choices.Where(c => !Rules.IsCasinoChoice(c)).ToList();
        else if (S.appDeleted)
        {
            choices = choices.Select(c =>
            {
                if (c.next != "first_bet") return c;
                var r = c.Clone();
                r.text = Data.T(S.betsMade > 0 ? "relapse.reinstall" : "relapse.unblock");
                r.textRude = null;
                r.textVariants = null;
                r.effect = r.effect ?? new Effect();
                r.effect.relapse = true;
                return r;
            }).ToList();
        }

        // Варианты, зависящие от того, ставил ли игрок хоть раз
        choices = choices.Where(c =>
        {
            if (c.ifFewBets) return Rules.TriedAndStopped();
            if (c.ifManyBets) return S.betsMade > 0 && !Rules.TriedAndStopped();
            if (c.ifPlayed) return S.betsMade > 0;
            if (c.ifNeverPlayed) return S.betsMade == 0;
            return true;
        }).ToList();

        // Из лобби не уйти до первой ставки; после ставки — при высокой зависимости;
        // при зависимости 100 выбора «не играть» нет нигде, где казино вообще доступно
        bool lockedInLobby = sceneId == "casino_lobby" && S.betsMade == 0;
        bool lockedAfterBet = (sceneId == "casino_lobby" || sceneId == "after_win" || sceneId == "after_loss") && S.addiction >= B.ADDICTION_NO_EXIT;
        bool lockedByMax = S.addiction >= B.ADDICTION_MAX && choices.Any(Rules.IsCasinoChoice);
        bool hadWayOut = choices.Any(c => !Rules.IsCasinoChoice(c));
        if (lockedInLobby || lockedAfterBet || lockedByMax) choices = choices.Where(Rules.IsCasinoChoice).ToList();

        // Мамины бутерброды — только если она их собирает; обед в столовой — только если хватает денег
        choices = choices.Where(c => !c.packedLunch || Rules.MomLunchToday()).ToList();
        choices = choices.Select(c =>
            c.buyLunch != null && S.money < Rules.LunchPrice(c.buyLunch) ? WithNote(c, Data.T("shop.tooExpensive")) : c).ToList();

        // При зависимости 100 кнопка «Лечь спать» остаётся, но вместо сна — окно «хочу отыграться»
        showFakeSleep = lockedByMax && hadWayOut;
        return choices;
    }

    // Неактивная кнопка с пометкой: «(не хватает)», «(уже есть)»
    public static Choice WithNote(Choice choice, string note)
    {
        var c = choice.Clone();
        c.disabled = true;
        c.text = Tpl.Fill(Data.T("note"), "label", choice.text, "note", note);
        return c;
    }

    public static string Label(Choice choice)
    {
        if ((S.lastGame == "bet_roulette" || S.lastGame == "bet_slots") && choice.rouletteText != null) return choice.rouletteText; // «Крутить ещё»
        if (S.addiction >= B.ADDICTION_RUDE && choice.textRude != null) return choice.textRude; // зависимость делает героя резким
        if (choice.textVariants != null) return Game.PickByDay(choice.textVariants);
        return choice.text;
    }

    public static void RenderAll(List<Choice> choices)
    {
        UI.ClearChoices();
        foreach (var choice in choices)
        {
            var c = choice;
            UI.AddButton(Tpl.Apply(Label(c)), () =>
            {
                if (c.disabled) return;
                if (c.onClick != null) { c.onClick(); return; }
                if (!Effects.ApplyChoice(c)) return;
                Game.Render(c.next);
            }, c.disabled, c.iconItem);
        }
    }
}
