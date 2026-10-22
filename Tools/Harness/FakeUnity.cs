// Minimal functional fake of the Unity API surface used by the game — for compile checks and headless simulation.
#pragma warning disable CS0067, CS0414, CS0649, CS0169
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public enum KeyCode { None, Return, KeypadEnter, Escape, F11, Alpha1 = 49, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, Keypad1 = 257, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9 }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum TextureFormat { RGBA32 = 4 }
    public enum SpriteMeshType { FullRect, Tight }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }
    public enum ColorSpace { Gamma, Linear }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return LerpUnclamped(a, b, t); }
        public static Color LerpUnclamped(Color a, Color b, float t) => new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string s, out Color c)
        {
            c = Color.white;
            if (s == null || !s.StartsWith("#")) return false;
            s = s.Substring(1);
            if (s.Length == 3 || s.Length == 4) s = string.Concat(s.Select(ch => new string(ch, 2)));
            if (s.Length != 6 && s.Length != 8) return false;
            uint v = Convert.ToUInt32(s, 16);
            if (s.Length == 6) c = new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1);
            else c = new Color(((v >> 24) & 255) / 255f, ((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
            return true;
        }
        public static string ToHtmlStringRGBA(Color c) => $"{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}{(int)(c.a * 255):X2}";
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static Vector2 Scale(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);
        public static float Distance(Vector2 a, Vector2 b) => (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 operator *(Vector3 a, float k) => new Vector3(a.x * k, a.y * k, a.z * k);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity => new Quaternion { w = 1 };
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion LookRotation(Vector3 forward) => identity;
    }

    public struct Bounds
    {
        public Vector3 center, size;
        public Vector3 max => new Vector3(center.x + size.x / 2, center.y + size.y / 2, center.z + size.z / 2);
        public void Encapsulate(Bounds b) { }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public Vector2 size => new Vector2(width, height);
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Abs(float a) => Math.Abs(a);
        public static int Abs(int a) => Math.Abs(a);
        public static float Sin(float a) => (float)Math.Sin(a);
        public static float Cos(float a) => (float)Math.Cos(a);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Log(float a) => (float)Math.Log(a);
        public static float Exp(float a) => (float)Math.Exp(a);
        public static float Round(float a) => (float)Math.Round(a, MidpointRounding.ToEven);
        public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.ToEven);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = t * t * (3 - 2 * t); return a + (b - a) * t; }
        public static float Repeat(float t, float len) => t - (float)Math.Floor(t / len) * len;
    }

    public static class Random
    {
        public static System.Random Rng = new System.Random(1);
        public static float value => (float)Rng.NextDouble();
        public static float Range(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
        public static int Range(int a, int b) => Rng.Next(a, b);
    }

    public static class Time
    {
        public static float time, deltaTime = 1 / 60f;
    }

    public static class Debug
    {
        public static List<string> Errors = new List<string>();
        public static void Log(object o) { }
        public static void LogWarning(object o) { Errors.Add("WARN " + o); }
        public static void LogError(object o) { Errors.Add("ERROR " + o); }
        public static void LogException(Exception e) { Errors.Add("EXC " + e); }
    }

    public static class Application
    {
        public static string persistentDataPath = System.IO.Path.GetTempPath();
        public static int targetFrameRate;
    }

    public static class PlayerPrefs
    {
        static Dictionary<string, int> d = new Dictionary<string, int>();
        public static int GetInt(string k, int def) => d.TryGetValue(k, out var v) ? v : def;
        public static void SetInt(string k, int v) => d[k] = v;
    }

    public static class Screen { public static bool fullScreen; public static int width = 1920, height = 1080; }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode k) => false;
    }

    public class Object
    {
        public string name;
        public static void Destroy(Object o, float t = 0)
        {
            if (o is GameObject go) go.DestroyNow();
            else if (o is Component c) c.gameObject.RemoveComponent(c);
        }
        public static void DontDestroyOnLoad(Object o) { }
        public static T FindAnyObjectByType<T>() where T : Object => null;
        public static GameObject Instantiate(GameObject original, Transform parent)
        {
            var go = new GameObject(original.name);
            go.transform.SetParent(parent, false);
            return go;
        }
        public static implicit operator bool(Object o) => o != null && !o.destroyed;
        internal bool destroyed;
    }

    public class GameObject : Object
    {
        public static List<GameObject> All = new List<GameObject>();
        internal List<Component> components = new List<Component>();
        public Transform transform;
        bool active = true;
        public string tag;

        public GameObject(string name = "GameObject", params Type[] types)
        {
            this.name = name;
            All.Add(this);
            // В Unity UI-компоненты сами превращают Transform в RectTransform — здесь он такой сразу
            transform = new RectTransform();
            transform.go = this;
            components.Add(transform);
            foreach (var t in types) if (t != typeof(RectTransform) && t != typeof(Transform)) AddComponent(t);
        }

        public T AddComponent<T>() where T : Component => (T)AddComponent(typeof(T));
        public Component AddComponent(Type t)
        {
            var c = (Component)Activator.CreateInstance(t);
            c.go = this;
            components.Add(c);
            c.OnAdded();
            return c;
        }
        public T GetComponent<T>() where T : class => components.FirstOrDefault(c => c is T) as T;
        public List<Component> GetComponents() => components.ToList();
        public bool activeSelf => active;
        public void SetActive(bool v) => active = v;
        public bool activeInHierarchy => active && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);
        internal void RemoveComponent(Component c) => components.Remove(c);
        internal void DestroyNow()
        {
            if (destroyed) return;
            destroyed = true;
            foreach (var ch in transform.children.ToList()) ch.gameObject.DestroyNow();
            transform.SetParent(null);
            All.Remove(this);
            foreach (var c in components) c.destroyed = true;
        }
    }

    public class Component : Object
    {
        internal GameObject go;
        public GameObject gameObject => go;
        public Transform transform => go.transform;
        public string tag { get => go.tag; set => go.tag = value; }
        public T GetComponent<T>() where T : class => go.GetComponent<T>();
        public T[] GetComponentsInChildren<T>() where T : class
        {
            var list = new List<T>();
            void Walk(Transform t)
            {
                list.AddRange(t.gameObject.GetComponents().OfType<T>());
                foreach (var ch in t.children) Walk(ch);
            }
            Walk(go.transform);
            return list.ToArray();
        }
        internal virtual void OnAdded() { }
    }

    public class Transform : Component, IEnumerable
    {
        internal List<Transform> children = new List<Transform>();
        Transform _parent;
        public Vector3 localScale = Vector3.one, localEulerAngles, localPosition, position;
        public Quaternion localRotation = Quaternion.identity, rotation = Quaternion.identity;
        public Transform parent => _parent;
        public int childCount => children.Count;
        public void SetParent(Transform p, bool worldPositionStays = true)
        {
            _parent?.children.Remove(this);
            _parent = p;
            p?.children.Add(this);
        }
        public void SetAsFirstSibling() { if (_parent != null) { _parent.children.Remove(this); _parent.children.Insert(0, this); } }
        public void SetSiblingIndex(int i) { if (_parent != null) { _parent.children.Remove(this); _parent.children.Insert(Math.Min(i, _parent.children.Count), this); } }
        public IEnumerator GetEnumerator() => children.ToList().GetEnumerator();
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax = Vector2.one, pivot = new Vector2(0.5f, 0.5f), sizeDelta, anchoredPosition;
        public Vector2 offsetMin
        {
            get => anchoredPosition - Vector2.Scale(sizeDelta, pivot);
            set { var d = value - offsetMin; sizeDelta = sizeDelta - d; anchoredPosition = anchoredPosition + Vector2.Scale(d, Vector2.one - pivot); }
        }
        public Vector2 offsetMax
        {
            get => anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            set { var d = value - offsetMax; sizeDelta = sizeDelta + d; anchoredPosition = anchoredPosition + Vector2.Scale(d, pivot); }
        }
        public Rect rect
        {
            get
            {
                var ps = parent is RectTransform p ? p.rect.size : new Vector2(1920, 1080);
                return new Rect(0, 0, ps.x * (anchorMax.x - anchorMin.x) + sizeDelta.x, ps.y * (anchorMax.y - anchorMin.y) + sizeDelta.y);
            }
        }
    }

    public class Behaviour : Component { public bool enabled = true; }

    public class Coroutine { internal IEnumerator e; internal float wakeAt; internal bool dead; }
    public class WaitForSeconds { public float s; public WaitForSeconds(float s) { this.s = s; } }

    public class MonoBehaviour : Behaviour
    {
        public static List<Coroutine> Running = new List<Coroutine>();
        public Coroutine StartCoroutine(IEnumerator e)
        {
            var c = new Coroutine { e = e, wakeAt = Time.time };
            Running.Add(c);
            Step(c); // как в Unity: до первого yield — сразу
            return c;
        }
        public void StopCoroutine(Coroutine c) { if (c != null) c.dead = true; }

        internal static void Step(Coroutine c)
        {
            if (c.dead) return;
            try
            {
                if (!c.e.MoveNext()) { c.dead = true; return; }
                if (c.e.Current is WaitForSeconds w) c.wakeAt = Time.time + w.s;
                else c.wakeAt = Time.time + 0.0001f;
            }
            catch (Exception ex) { c.dead = true; Debug.LogException(ex); }
        }

        public static void Tick()
        {
            foreach (var c in Running.ToList()) if (!c.dead && c.wakeAt <= Time.time) Step(c);
            Running.RemoveAll(c => c.dead);
        }
    }

    public class TextAsset : Object { public string text; }

    public static class Resources
    {
        public static string Root;
        public static T Load<T>(string path) where T : Object
        {
            if (typeof(T) == typeof(TextAsset))
            {
                var f = System.IO.Path.Combine(Root, path + ".json");
                return System.IO.File.Exists(f) ? new TextAsset { text = System.IO.File.ReadAllText(f) } as T : null;
            }
            if (typeof(T) == typeof(AudioClip)) return new AudioClip { name = System.IO.Path.GetFileName(path) } as T;
            return null;
        }
        public static T GetBuiltinResource<T>(string path) where T : Object => Activator.CreateInstance<T>();
    }

    public class Texture : Object { public FilterMode filterMode; public TextureWrapMode wrapMode; }
    public class Texture2D : Texture
    {
        public int width, height;
        public Color[] pixels;
        public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; }
        public void SetPixels(Color[] c) => pixels = c;
        public void SetPixels32(Color32[] c) => pixels = c.Select(x => new Color(x.r / 255f, x.g / 255f, x.b / 255f, x.a / 255f)).ToArray();
        public void Apply(bool a, bool b) { }
    }

    public class Sprite : Object
    {
        public Texture2D texture;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType mesh, Vector4 border) => new Sprite { texture = t };
    }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string[] names, int size) => new Font();
    }

    public class Camera : Behaviour
    {
        public static Camera main;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public bool orthographic;
        public float orthographicSize = 5, nearClipPlane = 0.3f, farClipPlane = 1000, depth;
        public RenderTexture targetTexture;
    }

    public enum RenderTextureFormat { ARGB32 }
    public class RenderTexture : Texture
    {
        public int width, height, antiAliasing = 1;
        public RenderTexture(int w, int h, int depth, RenderTextureFormat f) { width = w; height = h; }
        public void Release() { }
    }

    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour
    {
        public LightType type;
        public Color color;
        public float intensity;
        public LightShadows shadows;
    }

    public class Renderer : Component { public Bounds bounds; }
    public class AudioListener : Behaviour { }

    public static class RenderSettings
    {
        public static UnityEngine.Rendering.AmbientMode ambientMode;
        public static Color ambientLight;
    }

    public class AudioClip : Object
    {
        public static AudioClip Create(string n, int len, int ch, int rate, bool stream) => new AudioClip { name = n };
        public bool SetData(float[] d, int off) => true;
    }

    public class AudioSource : Behaviour
    {
        public bool loop, playOnAwake, isPlaying;
        public float volume;
        public AudioClip clip;
        public void Play() => isPlaying = true;
        public void Stop() => isPlaying = false;
        public void PlayOneShot(AudioClip c, float v) { }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode;
        public static void ForceUpdateCanvases() { }
    }

    public class CanvasGroup : Behaviour { public float alpha = 1; public bool blocksRaycasts = true; }
}

namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox, Trilight, Flat = 3, Custom }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T>(T a);
    public class UnityEvent
    {
        List<UnityAction> l = new List<UnityAction>();
        public void AddListener(UnityAction a) => l.Add(a);
        public void Invoke() { foreach (var a in l.ToList()) a(); }
    }
    public class UnityEvent<T>
    {
        List<UnityAction<T>> l = new List<UnityAction<T>>();
        public void AddListener(UnityAction<T> a) => l.Add(a);
        public void Invoke(T v) { foreach (var a in l.ToList()) a(v); }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour
    {
        public static EventSystem current;
        public GameObject currentSelectedGameObject;
        internal override void OnAdded() => current = this;
    }
    public class StandaloneInputModule : Behaviour { }
    public class PointerEventData { }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData e); }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;

    public class RectOffset
    {
        public int left, right, top, bottom;
        public RectOffset() { }
        public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
    }

    public class Graphic : Behaviour
    {
        public Color color = Color.white;
        public bool raycastTarget = true;
        public RectTransform rectTransform => (RectTransform)transform;
    }
    public class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Sprite sprite;
        public Type type;
        public bool preserveAspect;
    }

    public class RawImage : MaskableGraphic { public Texture texture; }

    public class Text : MaskableGraphic
    {
        public bool alignByGeometry;
        public string text = "";
        public Font font;
        public int fontSize;
        public TextAnchor alignment;
        public bool supportRichText;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public float lineSpacing;
        public FontStyle fontStyle;
        public float preferredWidth => (text ?? "").Length * fontSize * 0.5f;
    }

    public class Shadow : Behaviour { public Color effectColor; public Vector2 effectDistance; }

    public struct ColorBlock
    {
        public Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor;
        public float colorMultiplier, fadeDuration;
    }

    public struct Navigation
    {
        public enum Mode { None, Horizontal, Vertical, Automatic, Explicit }
        public Mode mode;
    }

    public class Selectable : Behaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public bool interactable = true;
        public ColorBlock colors;
        public Graphic targetGraphic;
        public Transition transition = Transition.ColorTint;
        public Navigation navigation;
        public void Select() { if (EventSystems.EventSystem.current != null) EventSystems.EventSystem.current.currentSelectedGameObject = gameObject; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick = new ButtonClickedEvent();
    }

    public class InputField : Selectable
    {
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin, Custom }
        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public class OnChangeEvent : UnityEvent<string> { }
        public class SubmitEvent : UnityEvent<string> { }
        string _text = "";
        public string text { get => _text; set { _text = value ?? ""; onValueChanged.Invoke(_text); } }
        public Text textComponent;
        public Graphic placeholder;
        public int characterLimit;
        public ContentType contentType;
        public LineType lineType;
        public Color caretColor, selectionColor;
        public OnChangeEvent onValueChanged = new OnChangeEvent();
        public SubmitEvent onEndEdit = new SubmitEvent();
        public bool isFocused;
        public void ActivateInputField() { }
    }

    public class Scrollbar : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public Direction direction;
        public RectTransform handleRect;
    }

    public class ScrollRect : Behaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
        public bool horizontal, vertical = true;
        public MovementType movementType;
        public float scrollSensitivity, verticalNormalizedPosition;
        public RectTransform viewport, content;
        public Scrollbar verticalScrollbar;
        public ScrollbarVisibility verticalScrollbarVisibility;
    }

    public class RectMask2D : Behaviour { }
    public class GraphicRaycaster : Behaviour { }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode;
        public Vector2 referenceResolution;
        public ScreenMatchMode screenMatchMode;
        public float matchWidthOrHeight;
    }

    public class LayoutGroup : Behaviour
    {
        public RectOffset padding = new RectOffset();
        public TextAnchor childAlignment;
    }
    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing;
        public bool childControlWidth, childControlHeight, childForceExpandWidth, childForceExpandHeight;
    }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Vector2 cellSize, spacing;
        public Constraint constraint;
        public int constraintCount;
    }
    public class LayoutElement : Behaviour { public float minHeight = -1, preferredWidth = -1, preferredHeight = -1, flexibleWidth = -1; }
    public class ContentSizeFitter : Behaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode verticalFit;
    }
    public class AspectRatioFitter : Behaviour
    {
        public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent }
        public AspectMode aspectMode;
        public float aspectRatio = 1;
    }
    public static class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(RectTransform r) { } }
}
