// Меню «Последняя ставка → Снимок 3D-персонажей»: все модели из Resources/Models/Characters рендерятся тем же кодом,
// что и на сцене (Actor3D), на фоне цвета интерфейса — в Docs/unity_characters.png. Проверка импорта и материалов.
// Из командной строки: Unity -batchmode -projectPath . -executeMethod ModelPreview.SnapshotBatch -logFile -
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ModelPreview
{
    const string Dir = "Assets/Resources/Models/Characters";
    const string Out = "Docs/unity_characters.png";

    [UnityEditor.MenuItem("Последняя ставка/Снимок 3D-персонажей")]
    public static void Snapshot()
    {
        var keys = Directory.GetFiles(Dir, "ch_*.fbx").Select(f => Path.GetFileNameWithoutExtension(f).Substring(3)).OrderBy(k => k).ToList();
        if (keys.Count == 0)
        {
            Debug.LogError("Нет моделей в " + Dir + " — соберите их: Tools/Blender/build_models.py");
            return;
        }
        const int cols = 10;
        int rows = (keys.Count + cols - 1) / cols;
        Texture2D sheet = null;
        int tw = 0, th = 0;
        var bg = Theme.Bg;
        for (int i = 0; i < keys.Count; i++)
        {
            var a = Actor3D.Create();
            if (!a.SetModel(keys[i]))
            {
                Debug.LogError("Не загрузилась модель " + keys[i]);
                Object.DestroyImmediate(a.gameObject);
                continue;
            }
            a.Face(i % 3 == 0 ? 0.17f : i % 3 == 1 ? 0.5f : 0.83f);
            a.Animate("normal", false, false, 0);
            var cam = a.GetComponentInChildren<Camera>();
            var rt = (RenderTexture)a.Texture;
            cam.Render();
            if (sheet == null)
            {
                tw = rt.width;
                th = rt.height;
                sheet = new Texture2D(tw * cols, th * rows, TextureFormat.RGBA32, false);
                var fill = Enumerable.Repeat(bg, sheet.width * sheet.height).ToArray();
                sheet.SetPixels(fill);
            }
            var tile = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tile.ReadPixels(new Rect(0, 0, tw, th), 0, 0);
            RenderTexture.active = prev;
            // Накладываем по альфе — так видно, что фон за персонажем прозрачный
            var px = tile.GetPixels();
            int ox = (i % cols) * tw, oy = (rows - 1 - i / cols) * th;
            var dst = sheet.GetPixels(ox, oy, tw, th);
            for (int k = 0; k < px.Length; k++) dst[k] = Color.Lerp(dst[k], new Color(px[k].r, px[k].g, px[k].b, 1), px[k].a);
            sheet.SetPixels(ox, oy, tw, th, dst);
            Object.DestroyImmediate(tile);
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(a.gameObject);
        }
        var lights = GameObject.Find("Lights3D");
        if (lights != null) Object.DestroyImmediate(lights);
        if (sheet == null) return;
        sheet.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(Out));
        File.WriteAllBytes(Out, sheet.EncodeToPNG());
        Debug.Log("Снимок 3D-персонажей: " + Path.GetFullPath(Out) + " (" + keys.Count + " моделей)");
    }

    public static void SnapshotBatch()
    {
        try { Snapshot(); }
        finally { EditorApplication.Exit(0); }
    }
}
