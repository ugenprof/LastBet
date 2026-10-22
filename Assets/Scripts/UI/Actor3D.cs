// 3D-персонаж. Модели — Resources/Models/Characters/ch_<ключ>.fbx (собирает Tools/Blender/build_models.py).
// У каждого персонажа своя камера: рендерит модель в текстуру, интерфейс показывает её как картинку
// (на сцене — StageActor вместо 2D-портрета, в мини-игре листовок — прохожий, в рюкзаке — примерка).
// Нет моделей — сцена остаётся 2D (CharacterArt).
// Кадр: Bust — по пояс (сцена), Full — в полный рост (прохожие, примерка).
// Эмоции передаются позой: happy — пружинит, sad — никнет и отворачивается, angry — подаётся к собеседнику.
// Герой собран из частей (child «…__base», «…__top_default», «…__acc_watch»…) — SetOutfit включает надетое.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Actor3D : MonoBehaviour
{
    public enum Framing { Bust, Full }

    const string ModelPath = "Models/Characters/ch_";
    // Персонажи стоят далеко от основной камеры и друг от друга: каждая камера видит только своего
    static readonly Vector3 Origin = new Vector3(5000, 0, 0);
    const float Spacing = 40f;
    const float Bust = 0.68f;   // какая доля роста попадает в кадр по пояс (как 2D-портреты)

    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    static bool? available;
    static int nextSpot;
    static GameObject lights;

    Camera cam;
    RenderTexture rt;
    Transform model;
    string key;
    Framing framing;
    float aspect;
    float height, yaw, faceYaw;
    readonly Dictionary<string, GameObject> parts = new Dictionary<string, GameObject>();

    public Texture Texture => rt;

    public static bool Available => available ?? (available = Load("hero_m") != null).Value;

    static GameObject Load(string key)
    {
        if (!prefabs.TryGetValue(key, out var p)) prefabs[key] = p = Resources.Load<GameObject>(ModelPath + key);
        return p;
    }

    // Загрузить заранее, чтобы первое появление не ждало диска
    public static void Preload(string key) => Load(key);

    // aspect — ширина/высота картинки (по умолчанию как у 2D-портрета)
    public static Actor3D Create(Framing framing = Framing.Bust, float aspect = 0)
    {
        EnsureLights();
        var go = new GameObject("Actor3D");
        go.transform.position = Origin + new Vector3(Spacing * (nextSpot++ % 64), 0, 0);
        var a = go.AddComponent<Actor3D>();
        a.framing = framing;
        a.aspect = aspect > 0 ? aspect : (float)CharacterArt.UW / CharacterArt.UH;

        int h = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.8f, 480, 1100));
        int w = Mathf.RoundToInt(h * a.aspect);
        a.rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Actor3D" };

        var camGo = new GameObject("Camera");
        camGo.transform.SetParent(go.transform, false);
        a.cam = camGo.AddComponent<Camera>();
        a.cam.clearFlags = CameraClearFlags.SolidColor;
        a.cam.backgroundColor = new Color(0, 0, 0, 0);   // прозрачный фон: за персонажем — 2D-фон сцены
        a.cam.orthographic = true;
        a.cam.nearClipPlane = 0.1f;
        a.cam.farClipPlane = 12f;
        a.cam.depth = -10;
        a.cam.targetTexture = a.rt;
        return a;
    }

    // Свет общий для всех: тёплый ключевой слева сверху, холодный заполняющий справа, фиолетовый контровой сзади
    static void EnsureLights()
    {
        if (lights != null) return;
        lights = new GameObject("Lights3D");
        AddLight(new Vector3(-0.55f, -0.6f, -0.6f), new Color(1f, 0.94f, 0.86f), 1.05f);
        AddLight(new Vector3(0.75f, -0.15f, -0.5f), new Color(0.72f, 0.78f, 1f), 0.35f);
        AddLight(new Vector3(0.1f, -0.35f, 1f), new Color(0.8f, 0.55f, 1f), 0.7f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.43f, 0.41f, 0.5f);
    }

    static void AddLight(Vector3 dir, Color color, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(lights.transform, false);
        go.transform.rotation = Quaternion.LookRotation(dir);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = color;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
    }

    // false — модели нет (тогда показывается 2D)
    public bool SetModel(string key)
    {
        if (key == this.key && model != null) return true;
        var prefab = Load(key);
        if (prefab == null) return false;
        if (model != null) Destroy(model.gameObject);
        this.key = key;
        model = Instantiate(prefab, transform).transform;
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;   // модель смотрит вдоль +Z, камера стоит перед ней

        // Части героя: имя child «ch_hero_m__top_default» → «top_default»
        parts.Clear();
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            string n = r.gameObject.name;
            int i = n.IndexOf("__");
            if (i >= 0) parts[n.Substring(i + 2)] = r.gameObject;
        }
        if (parts.Count > 0) SetOutfit(new HashSet<string>());
        Frame();
        return true;
    }

    // Надетые вещи (Outfit.Parts): своя одежда героя уступает место вещи того же слота
    public void SetOutfit(HashSet<string> worn)
    {
        if (parts.Count == 0) return;
        foreach (var kv in parts)
        {
            string p = kv.Key;
            bool on;
            if (p == "base") on = true;
            else if (p.EndsWith("_default"))
            {
                string slot = p.Substring(0, p.Length - "_default".Length);
                on = true;
                foreach (var w in worn)
                    if (w.StartsWith(slot + "_")) on = false;
            }
            else on = worn.Contains(p);
            kv.Value.SetActive(on);
        }
    }

    void Frame()
    {
        // Рост и ширина — по границам видимой модели (у взрослых больше: scale из characters.json уже в модели)
        height = 1.4f;
        float width = 0.65f;
        bool any = false;
        Bounds b = default;
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            if (!r.gameObject.activeInHierarchy) continue;
            if (!any) b = r.bounds;
            else b.Encapsulate(r.bounds);
            any = true;
        }
        if (any)
        {
            height = b.max.y - transform.position.y;
            // в полный рост персонаж может поворачиваться боком (велосипед) — берём большую из сторон
            width = framing == Framing.Full ? Mathf.Max(b.size.x, b.size.z) : b.size.x;
        }
        float imgAspect = (float)rt.width / rt.height;
        if (framing == Framing.Bust)
        {
            // по пояс, с запасом над макушкой и по бокам: персонаж покачивается и не упирается в край текстуры
            float top = height * 1.08f;
            float half = Mathf.Max(height * Bust / 2f, width * 0.6f / imgAspect);
            cam.orthographicSize = half;
            cam.transform.localPosition = new Vector3(0, top - half, 5f);
        }
        else
        {
            float half = Mathf.Max(height * 1.1f / 2f, width * 0.58f / imgAspect);
            cam.orthographicSize = half;
            cam.transform.localPosition = new Vector3(0, half - height * 0.04f, 5f);
        }
        cam.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    // x — место на площадке (0…1): крайние поворачиваются к центру, к собеседнику
    public void Face(float x) => faceYaw = (x - 0.5f) * 36f;

    // Сразу повернуть на угол (прохожий идёт боком): yaw > 0 — к левому краю картинки
    public void Turn(float degrees)
    {
        faceYaw = yaw = degrees;
        if (model != null) model.localRotation = Quaternion.Euler(0, yaw, 0);
    }

    public void Animate(string mood, bool talking, bool tired, float phase)
    {
        if (model == null) return;
        float t = Time.time + phase, dt = Time.deltaTime;
        float targetYaw = faceYaw + Mathf.Sin(t * 0.55f) * 3f;
        float y = 0, roll = Mathf.Sin(t * 0.8f) * 0.6f, pitch = 0;
        switch (mood)
        {
            case "happy":
                y = Mathf.Abs(Mathf.Sin(t * 2.6f)) * 0.008f * height;
                roll += Mathf.Sin(t * 2.6f) * 1.2f;
                break;
            case "sad":
                y = -0.012f * height;
                roll += 4f;
                pitch = 6f;
                targetYaw = -faceYaw * 0.6f;   // отворачивается от собеседника
                break;
            case "angry":
                pitch = 4f;
                targetYaw = faceYaw * 1.4f + Mathf.Sin(t * 21f) * 1.2f;
                break;
        }
        if (tired)
        {
            y -= 0.008f * height;
            roll += 2f;
        }
        if (talking)
        {
            float k = Mathf.Abs(Mathf.Sin(Time.time * 10f));
            y += k * 0.006f * height;
            pitch += k * 2f;
        }
        yaw = Mathf.Lerp(yaw, targetYaw, 1f - Mathf.Exp(-dt * 6f));
        model.localPosition = new Vector3(0, y, 0);
        model.localRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    // Витрина (примерка в рюкзаке): медленно поворачивается, чтобы было видно вещи
    public void Showcase(float phase)
    {
        if (model == null) return;
        float t = Time.time + phase;
        model.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.7f) * 28f, 0);
        model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * 2.2f)) * 0.006f * height, 0);
    }

    // Прохожий в пути: пешком — шаг вперевалку, на колёсах — плавно и с лёгким креном
    public void Walk(float t, bool wheels, bool fast)
    {
        if (model == null) return;
        float f = fast ? 11f : 7f;
        float bob = wheels ? Mathf.Sin(t * 3f) * 0.004f : Mathf.Abs(Mathf.Sin(t * f)) * 0.022f;
        float roll = wheels ? Mathf.Sin(t * 1.5f) * 2f : Mathf.Sin(t * f) * (fast ? 4f : 3f);
        model.localPosition = new Vector3(0, bob * height, 0);
        model.localRotation = Quaternion.Euler(wheels ? 0 : 3f, yaw, roll);
    }

    void OnDestroy()
    {
        if (cam != null) cam.targetTexture = null;
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }
    }
}

// Удаляет 3D-персонажа вместе с картинкой интерфейса, которая его показывает
public class Actor3DLink : MonoBehaviour
{
    public Actor3D actor;
    public bool showcase;     // витрина: медленно поворачивается (примерка в рюкзаке)

    void Update()
    {
        if (showcase && actor != null) actor.Showcase(0);
    }

    void OnDestroy()
    {
        if (actor != null) Destroy(actor.gameObject);
    }
}
