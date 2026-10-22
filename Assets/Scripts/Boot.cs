// Точка входа: запускается сама после загрузки сцены — сцена может быть пустой.
// Строит интерфейс, загружает данные, предлагает продолжить сохранённую игру или начинает со вступления.
using System.Collections;
using System.Globalization;
using System.Threading;
using UnityEngine;

public class Boot : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Start0()
    {
        if (FindAnyObjectByType<Boot>() != null) return;
        new GameObject("Boot").AddComponent<Boot>();
    }

    void Start()
    {
        // Числа в строках — с точкой (коэффициенты ×1.85), как в браузерной версии
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        Application.targetFrameRate = 60;

        if (Camera.main == null)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Bg;
            cam.orthographic = true;
        }

        // Без слушателя Unity молчит; камера из сцены создана кодом и своего слушателя не имеет
        if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();

        Game.Init();
        Music.Init();
        UI.Build();

        var saved = SaveGame.Peek();
        if (saved != null && !string.IsNullOrEmpty(saved.playerName))
        {
            UI.SetBackground("night");
            UI.UpdateHud();
            UI.OpenContinue(saved, () => SaveGame.Continue(saved), () =>
            {
                SaveGame.Delete();
                Game.Render(G.S.scene);
            });
        }
        else Game.Render(G.S.scene);

        StartCoroutine(Prewarm());
    }

    // Фоны и персонажи готовятся заранее (фоны — в фоновом потоке) — чтобы смена сцены не ждала
    IEnumerator Prewarm()
    {
        yield return null;
        foreach (var name in Backgrounds.All) Backgrounds.Get(name);
        if (Actor3D.Available)
        {
            foreach (var key in Data.characters.Keys)
            {
                Actor3D.Preload(key);
                yield return null;   // по модели за кадр — без рывка на старте
            }
            yield break;
        }
        foreach (var kv in Data.characters)
            if (!kv.Key.StartsWith("hero_")) CharacterArt.Get(kv.Key, kv.Value, "normal", default, false);
    }
}
