// Настройка проекта при первом открытии в Unity: сцена Main (игра строится кодом, сцена пустая),
// список сцен сборки, название и окно игры. Меню «Последняя ставка» — сборка под Windows.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    static ProjectSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.09f);
            cam.orthographic = true;
            cam.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Последняя ставка: создана сцена " + ScenePath);
        }

        if (EditorBuildSettings.scenes.Length == 0 || EditorBuildSettings.scenes[0].path != ScenePath)
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        if (PlayerSettings.productName != "Последняя ставка")
        {
            PlayerSettings.productName = "Последняя ставка";
            PlayerSettings.companyName = "LastBet";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        // Открыть сцену игры, если в редакторе пустая несохранённая сцена без правок
        var active = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path) && !active.isDirty) EditorSceneManager.OpenScene(ScenePath);
    }

    [UnityEditor.MenuItem("Последняя ставка/Собрать для Windows (.exe)")]
    static void BuildWindows()
    {
        Setup();
        string dir = EditorUtility.SaveFolderPanel("Куда собрать игру", Path.GetFullPath("Build"), "");
        if (string.IsNullOrEmpty(dir)) return;
        if (Build(dir)) EditorUtility.RevealInFinder(Path.Combine(dir, "LastBet.exe"));
    }

    // Из командной строки: Unity.exe -batchmode -quit -projectPath . -executeMethod ProjectSetup.BuildWindowsBatch
    // Игра собирается в папку Build рядом с проектом.
    public static void BuildWindowsBatch()
    {
        Setup();
        EditorApplication.Exit(Build(Path.GetFullPath("Build")) ? 0 : 1);
    }

    static bool Build(string dir)
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(dir, "LastBet.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Готово: " + options.locationPathName);
            return true;
        }
        Debug.LogError("Сборка не удалась: " + report.summary.result);
        return false;
    }

    [UnityEditor.MenuItem("Последняя ставка/Удалить сохранение")]
    static void DeleteSave()
    {
        SaveGame.Delete();
        Debug.Log("Сохранение удалено");
    }
}
