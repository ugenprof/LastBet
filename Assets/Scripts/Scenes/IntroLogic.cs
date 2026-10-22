// Выбор героя и имя. Пол героя задаёт только выбранная модель (мальчик / девочка) — по имени не угадываем.
// Сначала герой, потом имя: одно слово, не длиннее NAME_MAX_LENGTH.
using System.Collections.Generic;

public static class IntroLogic
{
    static GameState S => G.S;
    static bool genderPicked;

    public static void RenderNameInput(SceneDef scene)
    {
        UI.SetText(Tpl.Apply(scene.text));
        UI.ClearChoices();
        genderPicked = false;

        var w = UI.ShowNameInput(Data.T("nameInput.boy"), Data.T("nameInput.girl"), Data.T("nameInput.submit"));
        void Show()
        {
            w.SetSelected(genderPicked ? S.gender : null);
            int count = Data.HeroLookCount(S.gender);
            w.SetLook(Tpl.Fill(Data.T("nameInput.look"), "n", S.look + 1, "total", count), genderPicked && count > 1);
            w.SetEnabled(genderPicked, genderPicked
                ? Tpl.Fill(Data.T("nameInput.placeholder"), "max", B.NAME_MAX_LENGTH)
                : Data.T("nameInput.pickFirst"));
            UI.ShowCast(genderPicked ? new List<string> { "hero:happy" } : new List<string>());
        }
        void Pick(string gender)
        {
            genderPicked = true;
            if (S.gender != gender) S.look = 0;
            S.gender = gender;
            Show();
            w.Focus();
        }
        void Submit()
        {
            string name = Txt.CleanName(w.Value);
            if (!genderPicked || name == "") return;
            S.playerName = name;
            Game.Render(scene.next);
        }

        // Внешность героя — по кругу
        void Look(int step)
        {
            if (!genderPicked) return;
            int count = Data.HeroLookCount(S.gender);
            S.look = ((S.look + step) % count + count) % count;
            Show();
        }

        w.onPrevLook = () => Look(-1);
        w.onNextLook = () => Look(1);
        w.onBoy = () => Pick("m");
        w.onGirl = () => Pick("f");
        w.onSubmit = Submit;
        // Только одно слово: пробелы не вводятся
        w.onChange = value =>
        {
            string clean = Txt.CleanName(value);
            if (clean != value) w.Value = clean;
        };
        Show();
    }
}
