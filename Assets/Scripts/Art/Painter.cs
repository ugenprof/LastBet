// Растеризатор: рисует сглаженные фигуры в текстуру (по расстоянию до края — SDF).
// Координаты — в «единицах» холста (Scale — пикселей на единицу), начало — левый верхний угол, y вниз.
using System;
using UnityEngine;

public class Painter
{
    public readonly int W, H;
    readonly Color[] px;
    public float Scale = 1f;

    // Обрезка по высоте (в единицах): рисуется только ClipTop ≤ y ≤ ClipBottom
    public float ClipTop = float.NegativeInfinity, ClipBottom = float.PositiveInfinity;
    public float ClipLeft = float.NegativeInfinity, ClipRight = float.PositiveInfinity;

    public Painter(int w, int h, float scale = 1f)
    {
        W = w;
        H = h;
        Scale = scale;
        px = new Color[w * h];
    }

    public void NoClip()
    {
        ClipTop = ClipLeft = float.NegativeInfinity;
        ClipBottom = ClipRight = float.PositiveInfinity;
    }

    // ===== ЦВЕТ =====
    // Свой разбор "#rgb" / "#rrggbb": рисование идёт и в фоновом потоке, а ColorUtility — только для главного
    public static Color Hex(string hex, float alpha = 1f)
    {
        if (string.IsNullOrEmpty(hex)) return new Color(0.5f, 0.5f, 0.5f, alpha);
        string s = hex[0] == '#' ? hex.Substring(1) : hex;
        if (s.Length == 3) s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
        if (s.Length < 6 || !uint.TryParse(s.Substring(0, 6), System.Globalization.NumberStyles.HexNumber, null, out uint v))
            return new Color(0.5f, 0.5f, 0.5f, alpha);
        return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, alpha);
    }

    // amount < 0 — темнее, > 0 — светлее
    public static Color Shade(Color c, float amount)
    {
        if (amount < 0) return new Color(c.r * (1 + amount), c.g * (1 + amount), c.b * (1 + amount), c.a);
        return new Color(c.r + (1 - c.r) * amount, c.g + (1 - c.g) * amount, c.b + (1 - c.b) * amount, c.a);
    }

    public static Color A(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);
    public static Color Mix(Color a, Color b, float t) => Color.LerpUnclamped(a, b, t);

    // ===== ПИКСЕЛИ =====
    void Blend(int x, int y, Color c, float cov)
    {
        float sa = c.a * cov;
        if (sa <= 0.001f) return;
        int i = y * W + x;
        var d = px[i];
        float oa = sa + d.a * (1 - sa);
        if (oa <= 0) return;
        float k = d.a * (1 - sa);
        px[i] = new Color((c.r * sa + d.r * k) / oa, (c.g * sa + d.g * k) / oa, (c.b * sa + d.b * k) / oa, oa);
    }

    // Общая фигура: рамка (в единицах), расстояние до края (в единицах, < 0 — внутри), цвет (может зависеть от точки)
    public void Shape(float x0, float y0, float x1, float y1, Func<float, float, float> sdf, Func<float, float, Color> color)
    {
        float pad = 1.5f / Scale;
        y0 = Math.Max(y0 - pad, ClipTop);
        y1 = Math.Min(y1 + pad, ClipBottom);
        x0 = Math.Max(x0 - pad, ClipLeft);
        x1 = Math.Min(x1 + pad, ClipRight);
        int px0 = Math.Max(0, (int)Math.Floor(x0 * Scale)), px1 = Math.Min(W - 1, (int)Math.Ceiling(x1 * Scale));
        int py0 = Math.Max(0, (int)Math.Floor(y0 * Scale)), py1 = Math.Min(H - 1, (int)Math.Ceiling(y1 * Scale));
        for (int py = py0; py <= py1; py++)
        {
            float y = (py + 0.5f) / Scale;
            if (y < ClipTop || y > ClipBottom) continue;
            for (int pxx = px0; pxx <= px1; pxx++)
            {
                float x = (pxx + 0.5f) / Scale;
                if (x < ClipLeft || x > ClipRight) continue;
                float d = sdf(x, y) * Scale;
                float cov = Mathf.Clamp01(0.5f - d);
                if (cov <= 0) continue;
                Blend(pxx, py, color(x, y), cov);
            }
        }
    }

    public void Shape(float x0, float y0, float x1, float y1, Func<float, float, float> sdf, Color c) =>
        Shape(x0, y0, x1, y1, sdf, (x, y) => c);

    // ===== ФИГУРЫ =====
    public void Fill(Color c) => Rect(-10, -10, W / Scale + 20, H / Scale + 20, c);

    public void Rect(float x, float y, float w, float h, Color c) => RoundRect(x, y, w, h, 0, c);

    public void RoundRect(float x, float y, float w, float h, float r, Color c) =>
        Shape(x, y, x + w, y + h, RoundRectSdf(x, y, w, h, r), c);

    public static Func<float, float, float> RoundRectSdf(float x, float y, float w, float h, float r)
    {
        float cx = x + w / 2, cy = y + h / 2, hx = w / 2 - r, hy = h / 2 - r;
        return (px, py) =>
        {
            float qx = Math.Abs(px - cx) - hx, qy = Math.Abs(py - cy) - hy;
            float ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
            return (float)Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - r;
        };
    }

    public void Circle(float cx, float cy, float r, Color c) =>
        Shape(cx - r, cy - r, cx + r, cy + r, (x, y) => (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c);

    public void Ellipse(float cx, float cy, float rx, float ry, Color c) =>
        Shape(cx - rx, cy - ry, cx + rx, cy + ry, EllipseSdf(cx, cy, rx, ry), c);

    public static Func<float, float, float> EllipseSdf(float cx, float cy, float rx, float ry)
    {
        float m = Math.Min(rx, ry);
        return (x, y) =>
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            return ((float)Math.Sqrt(dx * dx + dy * dy) - 1) * m;
        };
    }

    // Кольцо (обводка круга)
    public void Ring(float cx, float cy, float r, float width, Color c) =>
        Shape(cx - r - width, cy - r - width, cx + r + width, cy + r + width,
            (x, y) => Math.Abs((float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - width / 2, c);

    // Отрезок со скруглёнными концами
    public void Line(float x1, float y1, float x2, float y2, float width, Color c)
    {
        float r = width / 2;
        Shape(Math.Min(x1, x2) - r, Math.Min(y1, y2) - r, Math.Max(x1, x2) + r, Math.Max(y1, y2) + r, (x, y) =>
        {
            float pax = x - x1, pay = y - y1, bax = x2 - x1, bay = y2 - y1;
            float len2 = bax * bax + bay * bay;
            float h = len2 > 0 ? Mathf.Clamp01((pax * bax + pay * bay) / len2) : 0;
            float dx = pax - bax * h, dy = pay - bay * h;
            return (float)Math.Sqrt(dx * dx + dy * dy) - r;
        }, c);
    }

    // Ломаная: последовательные отрезки
    public void Path(float width, Color c, params float[] xy)
    {
        for (int i = 0; i + 3 < xy.Length; i += 2) Line(xy[i], xy[i + 1], xy[i + 2], xy[i + 3], width, c);
    }

    // Многоугольник: x0, y0, x1, y1, ...
    public void Poly(Color c, params float[] xy) => Poly((x, y) => c, xy);

    public void Poly(Func<float, float, Color> color, params float[] xy)
    {
        int n = xy.Length / 2;
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            x0 = Math.Min(x0, xy[2 * i]); x1 = Math.Max(x1, xy[2 * i]);
            y0 = Math.Min(y0, xy[2 * i + 1]); y1 = Math.Max(y1, xy[2 * i + 1]);
        }
        Shape(x0, y0, x1, y1, (x, y) =>
        {
            // Расстояние до многоугольника (Inigo Quilez, sdPolygon)
            float d = (x - xy[0]) * (x - xy[0]) + (y - xy[1]) * (y - xy[1]);
            float s = 1;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float vix = xy[2 * i], viy = xy[2 * i + 1], vjx = xy[2 * j], vjy = xy[2 * j + 1];
                float ex = vjx - vix, ey = vjy - viy, wx = x - vix, wy = y - viy;
                float h = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * h, by = wy - ey * h;
                d = Math.Min(d, bx * bx + by * by);
                bool c1 = y >= viy, c2 = y < vjy, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * (float)Math.Sqrt(d);
        }, color);
    }

    // Прямоугольник с вертикальным градиентом
    public void VGrad(float x, float y, float w, float h, Color top, Color bottom) =>
        Shape(x, y, x + w, y + h, RoundRectSdf(x, y, w, h, 0), (px, py) => Color.Lerp(top, bottom, (py - y) / h));

    // Мягкое свечение: прозрачность падает от центра к краю
    public void Glow(float cx, float cy, float r, Color c)
    {
        Shape(cx - r, cy - r, cx + r, cy + r, (x, y) => -1, (x, y) =>
        {
            float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
            float k = Mathf.Clamp01(1 - d);
            return A(c, c.a * k * k);
        });
    }

    // ===== ВЕСЬ ХОЛСТ =====
    // Плёночное зерно — чтобы плоская заливка не выглядела пластиком
    public void Grain(float amount, int seed)
    {
        var rnd = new System.Random(seed);
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a <= 0) continue;
            float n = ((float)rnd.NextDouble() - 0.5f) * amount;
            var c = px[i];
            px[i] = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), c.a);
        }
    }

    // Затемнение к краям
    public void Vignette(float strength)
    {
        float cx = W / 2f, cy = H / 2f, maxD = (float)Math.Sqrt(cx * cx + cy * cy);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxD;
                float k = 1 - strength * d * d;
                var c = px[y * W + x];
                px[y * W + x] = new Color(c.r * k, c.g * k, c.b * k, c.a);
            }
    }

    // Готовые пиксели для текстуры (снизу вверх, как ждёт Unity). Можно вызывать в фоновом потоке
    public Color32[] Pixels32()
    {
        var result = new Color32[px.Length];
        for (int y = 0; y < H; y++)
        {
            int src = y * W, dst = (H - 1 - y) * W;
            for (int x = 0; x < W; x++)
            {
                var c = px[src + x];
                result[dst + x] = new Color32(B8(c.r), B8(c.g), B8(c.b), B8(c.a));
            }
        }
        return result;
    }

    static byte B8(float v) => (byte)(v <= 0 ? 0 : v >= 1 ? 255 : v * 255f + 0.5f);

    // Текстура из пикселей — только в главном потоке
    public static Texture2D MakeTexture(int w, int h, Color32[] pixels, FilterMode filter = FilterMode.Bilinear)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = filter;
        return tex;
    }

    public Texture2D ToTexture(FilterMode filter = FilterMode.Bilinear) => MakeTexture(W, H, Pixels32(), filter);

    public Sprite ToSprite(Vector4 border = default)
    {
        var tex = ToTexture();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
