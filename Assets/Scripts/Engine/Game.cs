// Отрисовка сцены. Особая логика сцен живёт в Scripts/Scenes и Scripts/Games и регистрируется здесь:
//
//   Routes[id](scene)    — выполняется при входе в сцену. Может поменять состояние и/или
//                          отрисовать вместо неё другую сцену — тогда возвращает true.
//   Screens[id](scene)   — свой экран целиком вместо обычного «текст + кнопки».
//   Texts[id](scene)     — текст сцены вместо scene.text (например, вариант по состоянию).
//   Builders[id](choices, scene) — кнопки, которые собираются по состоянию.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Game
{
    public static readonly Dictionary<string, Func<SceneDef, bool>> Routes = new Dictionary<string, Func<SceneDef, bool>>();
    public static readonly Dictionary<string, Action<SceneDef>> Screens = new Dictionary<string, Action<SceneDef>>();
    public static readonly Dictionary<string, Func<SceneDef, string>> Texts = new Dictionary<string, Func<SceneDef, string>>();
    public static readonly Dictionary<string, Func<List<Choice>, SceneDef, List<Choice>>> Builders =
        new Dictionary<string, Func<List<Choice>, SceneDef, List<Choice>>>();

    // Сцены-концовки: на них нет кнопки «Заново» в HUD
    static readonly HashSet<string> Endings = new HashSet<string> { "victory_items", "clean_week", "lose_school", "lose_addiction", "good_ending_epilogue" };
    public static bool IsEndingScene(string id) => Endings.Contains(id);

    static GameState S => G.S;
    static bool initialized;

    public static void Init()
    {
        if (initialized) return;
        initialized = true;
        Data.Load();
        DayLogic.Register();
        SchoolLogic.Register();
        PashaLogic.Register();
        HomeLogic.Register();
        TownLogic.Register();
        TheftLogic.Register();
        RecoveryLogic.Register();
        SundayLogic.Register();
        EndingsLogic.Register();
        Casino.Register();
        Roulette.Register();
        MatchGame.Register();
        Slots.Register();
        Crash.Register();
        Jobs.Register();
    }

    // skipRoute — продолжение сохранённой игры: состояние уже такое, каким было после входа в сцену
    public static void Render(string sceneId, bool skipRoute = false)
    {
        var scene = Data.Scene(sceneId);
        if (scene == null)
        {
            Debug.LogError("Сцена не найдена: " + sceneId);
            return;
        }
        S.scene = sceneId;
        Run.CancelAll(); // таймеры прошлой сцены (мини-игры, анимации исхода) больше не нужны

        if (!skipRoute && Routes.TryGetValue(sceneId, out var route) && route(scene)) return;

        UI.HideWidgets();
        UI.SetBackground(scene.bg);
        UI.ShowCast(SceneCast(scene));
        Music.SetZone(MusicZoneFor(scene));

        if (Screens.TryGetValue(sceneId, out var screen)) screen(scene);
        else if (scene.nameInput) IntroLogic.RenderNameInput(scene);
        else if (scene.roulette) Roulette.RenderScreen();
        else if (scene.match) MatchGame.RenderScreen();
        else if (scene.minigame != null) Jobs.RenderIntro(scene);
        else RenderStandard(sceneId, scene);

        UI.UpdateHud();
        UI.ApplyMood();

        // Сохранение — каждое утро: продолжить можно с начала дня. Игра закончилась — продолжать нечего
        if (sceneId == "new_day") SaveGame.Checkpoint();
        if (IsEndingScene(sceneId)) SaveGame.Delete();
    }

    // Обычная сцена: текст (заставка главы / заголовок + текст) и кнопки
    public static void RenderStandard(string sceneId, SceneDef scene, ChapterCard chapter = null, string headline = null)
    {
        RenderText(sceneId, scene, chapter ?? scene.chapter, headline ?? scene.headline);
        var choices = Choices.Prepare(sceneId, scene, out bool showFakeSleep);
        Choices.RenderAll(choices);
        if (showFakeSleep) Casino.AddFakeSleepButton();
    }

    static void RenderText(string sceneId, SceneDef scene, ChapterCard chapter, string headline)
    {
        if (chapter != null)
        {
            UI.SetChapter(chapter);
            return;
        }
        string text = Tpl.Apply(Texts.TryGetValue(sceneId, out var f) ? f(scene) : BaseText(scene));
        UI.SetText(text, headline != null ? Tpl.Apply(headline) : null, scene.headlineClass);
    }

    // Кто из персонажей на экране (поле cast, при спасённом Паше — castSaved, если есть)
    public static List<string> SceneCast(SceneDef scene) =>
        (S.pashaSaved && scene.castSaved != null ? scene.castSaved : scene.cast) ?? new List<string>();

    // weekdayLines — реплика про день недели перед основным текстом
    public static string BaseText(SceneDef scene)
    {
        var lines = scene.O("weekdayLines");
        string line = lines?.Value<string>(Rules.WeekdayIndex().ToString());
        string text = PickBaseText(scene);
        return line != null ? line + "\n\n" + text : text;
    }

    // Если Пашу не арестовали — textSaved; повторяющиеся сцены — textVariants: каждый день другой вариант
    static string PickBaseText(SceneDef scene)
    {
        if (S.betsMade == 0 && scene.Has("textNeverPlayed")) return scene.S("textNeverPlayed");
        if (S.pashaSaved && scene.textSavedVariants != null) return PickByDay(scene.textSavedVariants);
        if (S.pashaSaved && scene.Has("textSaved")) return scene.S("textSaved");
        if (scene.textVariants != null) return PickByDay(scene.textVariants);
        return scene.text ?? "";
    }

    // Вариант по дню: одна и та же сцена каждый день звучит по-разному
    public static string PickByDay(List<string> list, int salt = 0) => list[(S.day + salt) % list.Count];

    // Зона музыки: своя (scene.music) или по фону. "keep" — не менять
    static string MusicZoneFor(SceneDef scene)
    {
        if (scene.music == "keep") return null;
        if (scene.music != null) return scene.music;
        if (scene.bg != null && Data.C.MUSIC_BY_BG.TryGetValue(scene.bg, out var zone)) return zone;
        return null;
    }
}
