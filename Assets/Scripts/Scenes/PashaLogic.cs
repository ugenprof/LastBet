// День, когда Паша просит в долг: поле для суммы и ответы.
public static class PashaLogic
{
    static GameState S => G.S;

    public static void Register()
    {
        Game.Screens["day4_pasha_beggar"] = scene =>
        {
            Rules.ChangeAddiction(-B.PASHA_LOAN_ADDICTION_RELIEF);
            UI.SetText(Tpl.Apply(scene.text));
            UI.ClearChoices();

            bool canLend = S.money >= B.PASHA_MIN_LOAN;
            var input = UI.ShowNumberInput(Tpl.Apply(Data.T(canLend ? "loan.placeholder" : "loan.placeholderPoor")), canLend);

            UI.AddButton(Data.T("loan.submit"), () =>
            {
                int.TryParse(input.text, out int amount);
                if (amount < B.PASHA_MIN_LOAN)
                {
                    UI.Alert(Tpl.Apply(Data.T("loan.tooLittle")));
                    return;
                }
                if (amount > S.money)
                {
                    UI.Alert(Data.T("loan.notEnough"));
                    return;
                }
                S.money -= amount;
                S.pashaDebt = amount;
                S.pashaLent = true;
                Game.Render("day4_after_loan");
            }, !canLend);

            // Глава 2 (всё проиграно): можно признаться Паше, что сам на мели
            if (S.chapter == 2) UI.AddButton(Tpl.Apply(Data.T("loan.lost")), () => Game.Render("day4_both_broke"));
            // Не деньги, а помощь: уговорить Пашу рассказать взрослым
            UI.AddButton(Data.T("loan.help"), () => Game.Render("day4_help"));
            UI.AddButton(Data.T("loan.skip"), () => Game.Render("day4_refused"), false, null, true);
        };

        // «Я сам всё проиграл», а деньги на самом деле есть — Паша чует враньё
        Game.Texts["day4_both_broke"] = scene => S.money >= B.PASHA_MIN_LOAN ? scene.S("textHasMoney") : scene.text;
    }
}
