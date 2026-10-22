// Игровая графика кодом: колесо рулетки, символы слотов, ракета, иконки вещей, товары лавки, значки HUD.
// Всё рисуется один раз и кэшируется.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static Color H(string hex, float a = 1f) => Painter.Hex(hex, a);
    static Color Sh(Color c, float k) => Painter.Shade(c, k);

    static Sprite Cached(string key, int size, Action<Painter> draw, float scale = 1f)
    {
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        var p = new Painter(size, size, scale);
        draw(p);
        s = p.ToSprite();
        cache[key] = s;
        return s;
    }

    // ===== ОБЩЕЕ =====
    public static Sprite Glow(Color c) => Cached("glow" + ColorUtility.ToHtmlStringRGBA(c), 256, p => p.Glow(128, 128, 128, c));

    // Скруглённый прямоугольник для 9-slice (панели, кнопки)
    public static Sprite Rounded(int radius)
    {
        string key = "round" + radius;
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        int size = radius * 2 + 4;
        var p = new Painter(size, size);
        p.RoundRect(0, 0, size, size, radius, Color.white);
        var tex = p.ToTexture();
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
            new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
        cache[key] = s;
        return s;
    }

    public static Sprite Dot() => Cached("dot", 32, p => p.Circle(16, 16, 15, Color.white));

    // ===== РУЛЕТКА =====
    // Сектора как в Roulette.LOSS / X19 / X20: угол по часовой от верха
    public static Sprite Wheel() => Cached("wheel", 620, p =>
    {
        float c = 310, r = 300;
        var loss = H("#b3243f"); var lossAlt = H("#8e1830");
        var win = H("#1f9a62"); var winAlt = H("#167a4c");
        var gold = H("#ffd23f");
        p.Circle(c, c, r + 8, H("#2a1a12"));
        p.Shape(c - r, c - r, c + r, c + r, (x, y) => (float)Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) - r, (x, y) =>
        {
            float ang = Mathf.Atan2(x - c, -(y - c)) * Mathf.Rad2Deg; // 0 — верх, по часовой (y вниз)
            if (ang < 0) ang += 360;
            if (ang >= Roulette.X20[0]) return gold;
            int stripe = (int)(ang / 12f);
            if (ang < Roulette.LOSS[1]) return stripe % 2 == 0 ? loss : lossAlt;
            return stripe % 2 == 0 ? win : winAlt;
        });
        // Деления и обод
        for (int i = 0; i < 30; i++)
        {
            float a = i * 12 * Mathf.Deg2Rad;
            p.Line(c + Mathf.Sin(a) * (r - 40), c - Mathf.Cos(a) * (r - 40), c + Mathf.Sin(a) * r, c - Mathf.Cos(a) * r, 2, H("#000000", 0.25f));
        }
        p.Ring(c, c, r, 10, H("#e8c27a"));
        p.Ring(c, c, r - 70, 4, H("#000000", 0.25f));
        for (int i = 0; i < 24; i++)
        {
            float a = i * 15 * Mathf.Deg2Rad;
            p.Circle(c + Mathf.Sin(a) * (r + 2), c - Mathf.Cos(a) * (r + 2), 4, H("#fff3c4"));
        }
        // Подписи секторов кругами-маркерами
        Marker(p, c, r, (Roulette.LOSS[0] + Roulette.LOSS[1]) / 2, H("#ffffff", 0.9f), false);
        Marker(p, c, r, (Roulette.X19[0] + Roulette.X19[1]) / 2, H("#ffffff", 0.9f), true);
    });

    static void Marker(Painter p, float c, float r, float deg, Color col, bool plus)
    {
        float a = deg * Mathf.Deg2Rad, mr = r - 130;
        float x = c + Mathf.Sin(a) * mr, y = c - Mathf.Cos(a) * mr;
        p.Circle(x, y, 34, H("#000000", 0.25f));
        p.Line(x - 16, y, x + 16, y, 7, col);
        if (plus) p.Line(x, y - 16, x, y + 16, 7, col);
    }

    public static Sprite WheelHub() => Cached("hub", 200, p =>
    {
        p.Circle(100, 100, 96, H("#2a1a12"));
        p.Circle(100, 100, 84, H("#e8c27a"));
        p.Circle(100, 100, 60, H("#c9993a"));
        p.Circle(84, 84, 18, H("#fff3c4", 0.6f));
    });

    public static Sprite Pointer() => Cached("pointer", 140, p =>
    {
        p.Poly(H("#000000", 0.35f), 30, 14, 110, 14, 70, 136);
        p.Poly(H("#f4f1ea"), 30, 6, 110, 6, 70, 128);
        p.Circle(70, 30, 12, H("#d6264f"));
    });

    // ===== СЛОТЫ =====
    public static Sprite Slot(SlotSymbol s, bool blur = false)
    {
        int i = Data.C.SLOT_SYMBOLS.IndexOf(s);
        return Cached("slot" + i + blur, 200, p =>
        {
            switch (i)
            {
                case 0: // вишни
                    p.Path(6, H("#3a7a2a"), 70, 120, 100, 40, 140, 110);
                    p.Ellipse(110, 40, 26, 10, H("#4a9a3a"));
                    p.Circle(68, 136, 34, H("#c8102e"));
                    p.Circle(138, 126, 34, H("#e0213f"));
                    p.Circle(58, 124, 9, H("#ffffff", 0.6f));
                    p.Circle(128, 114, 9, H("#ffffff", 0.6f));
                    break;
                case 1: // лимон
                    p.Ellipse(100, 106, 70, 52, H("#f2d43a"));
                    p.Ellipse(36, 106, 12, 9, H("#e8c42a"));
                    p.Ellipse(164, 106, 12, 9, H("#e8c42a"));
                    p.Ellipse(84, 88, 24, 10, H("#ffffff", 0.45f));
                    break;
                case 2: // колокол
                    p.Poly(H("#f2b42a"), 60, 140, 140, 140, 126, 70, 100, 44, 74, 70);
                    p.Circle(100, 52, 28, H("#f2b42a"));
                    p.RoundRect(46, 136, 108, 18, 8, H("#d9962a"));
                    p.Circle(100, 160, 14, H("#a86a1a"));
                    p.Circle(100, 34, 8, H("#a86a1a"));
                    break;
                case 3: // звезда
                    var pts = new float[20];
                    for (int k = 0; k < 10; k++)
                    {
                        float a = -Mathf.PI / 2 + k * Mathf.PI / 5, rr = k % 2 == 0 ? 80 : 34;
                        pts[2 * k] = 100 + Mathf.Cos(a) * rr;
                        pts[2 * k + 1] = 106 + Mathf.Sin(a) * rr;
                    }
                    p.Poly(H("#ffcf2a"), pts);
                    p.Circle(86, 88, 10, H("#ffffff", 0.5f));
                    break;
                case 4: // алмаз
                    p.Poly(H("#4fd1ff"), 40, 80, 70, 40, 130, 40, 160, 80, 100, 170);
                    p.Poly(H("#9fe8ff"), 70, 40, 130, 40, 116, 80, 84, 80);
                    p.Poly(H("#2aa8e0"), 84, 80, 116, 80, 100, 170);
                    break;
                default: // семёрка
                    p.Poly(H("#d6264f"), 40, 36, 160, 36, 160, 66, 104, 170, 70, 170, 122, 66, 40, 66);
                    p.Poly(H("#ff5a7a"), 46, 42, 154, 42, 154, 52, 46, 52);
                    break;
            }
            if (blur) p.Rect(0, 0, 200, 200, H("#f5f0e6", 0.45f));
        });
    }

    // ===== РАКЕТА =====
    public static Sprite CrashSky() => Cached("crashsky", 512, p =>
    {
        p.VGrad(0, 0, 512, 512, H("#0b0a2a"), H("#3a1450"));
        var r = new System.Random(3);
        for (int i = 0; i < 90; i++) p.Circle((float)r.NextDouble() * 512, (float)r.NextDouble() * 400, (float)r.NextDouble() * 1.8f + 0.4f, H("#ffffff", 0.7f));
        for (int i = 1; i < 6; i++) p.Line(0, 512 - i * 90, 512, 512 - i * 90, 1, H("#ffffff", 0.07f));
        p.Ellipse(256, 540, 360, 80, H("#2a0e3a"));
    });

    public static Sprite Rocket() => Cached("rocket", 200, p =>
    {
        // Ракета смотрит вверх-вправо (поворот задаёт интерфейс)
        p.Poly(H("#ff8a2a"), 80, 150, 120, 150, 100, 196);
        p.Poly(H("#ffd23f"), 88, 150, 112, 150, 100, 180);
        p.RoundRect(74, 50, 52, 104, 22, H("#e8eef4"));
        p.Poly(H("#d6264f"), 74, 30 + 30, 126, 60, 100, 6);
        p.Circle(100, 86, 13, H("#2a6be0"));
        p.Circle(100, 86, 8, H("#9fd8ff"));
        p.Poly(H("#d6264f"), 74, 110, 52, 156, 74, 146);
        p.Poly(H("#d6264f"), 126, 110, 148, 156, 126, 146);
    });

    public static Sprite Boom() => Cached("boom", 256, p =>
    {
        var pts = new float[32];
        for (int k = 0; k < 16; k++)
        {
            float a = k * Mathf.PI / 8, rr = k % 2 == 0 ? 120 : 64;
            pts[2 * k] = 128 + Mathf.Cos(a) * rr;
            pts[2 * k + 1] = 128 + Mathf.Sin(a) * rr;
        }
        p.Poly(H("#ff5a2a"), pts);
        for (int k = 0; k < 16; k++) { pts[2 * k] = 128 + (pts[2 * k] - 128) * 0.62f; pts[2 * k + 1] = 128 + (pts[2 * k + 1] - 128) * 0.62f; }
        p.Poly(H("#ffd23f"), pts);
        p.Circle(128, 128, 30, H("#fff6d0"));
    });

    // ===== ВЕЩИ (цель игры) =====
    // Готовая иконка из Resources/Icons (рендер 3D-модели, Tools/Blender) — или null
    static Sprite Icon3D(string name)
    {
        string key = "icon3d" + name;
        if (cache.TryGetValue(key, out var s)) return s;
        var tex = Resources.Load<Texture2D>("Icons/" + name);
        s = tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.zero);
        cache[key] = s;
        return s;
    }

    public static Sprite Item(string id) => Icon3D("item_" + id) ?? ItemDrawn(id);

    static Sprite ItemDrawn(string id) => Cached("item" + id, 128, p =>
    {
        p.Scale = 1f;
        switch (id)
        {
            case "tshirt":
                p.Poly(H("#e8483f"), 34, 30, 50, 22, 78, 22, 94, 30, 116, 48, 102, 66, 92, 58, 92, 110, 36, 110, 36, 58, 26, 66, 12, 48);
                p.Ellipse(64, 24, 14, 6, H("#b8302a"));
                p.Circle(64, 70, 12, H("#ffffff", 0.85f));
                break;
            case "jeans":
                p.Poly(H("#3a6ab0"), 30, 24, 98, 24, 106, 100, 70, 104, 64, 60, 58, 104, 22, 100);
                p.Rect(30, 24, 68, 10, H("#2a5290"));
                p.Path(2, H("#f2c12e"), 40, 40, 44, 52);
                p.Path(2, H("#f2c12e"), 88, 40, 84, 52);
                break;
            case "hoodie":
                p.Poly(H("#6d4fc2"), 30, 34, 98, 34, 118, 64, 104, 76, 98, 66, 98, 114, 30, 114, 30, 66, 24, 76, 10, 64);
                p.Ellipse(64, 34, 26, 16, H("#4f37a0"));
                p.Ellipse(64, 34, 14, 9, H("#2a1a5a"));
                p.Line(58, 44, 56, 70, 3, H("#ece8f8"));
                p.Line(70, 44, 72, 70, 3, H("#ece8f8"));
                p.RoundRect(44, 82, 40, 20, 6, H("#5a40b0"));
                break;
            case "sneakers":
                p.Poly(H("#f4f4f4"), 14, 70, 44, 40, 64, 46, 80, 64, 114, 72, 116, 90, 14, 90);
                p.RoundRect(10, 86, 108, 14, 6, H("#d6264f"));
                p.Path(3, H("#3a6ab0"), 40, 56, 60, 76, 90, 76);
                p.Line(46, 50, 56, 56, 3, H("#333"));
                p.Line(52, 44, 62, 50, 3, H("#333"));
                break;
            case "headphones":
                p.ClipBottom = 66;
                p.Ring(64, 64, 40, 9, H("#2a2a33"));
                p.NoClip();
                p.RoundRect(14, 58, 26, 44, 10, H("#d6264f"));
                p.RoundRect(88, 58, 26, 44, 10, H("#d6264f"));
                p.RoundRect(20, 64, 14, 32, 6, H("#2a2a33"));
                p.RoundRect(94, 64, 14, 32, 6, H("#2a2a33"));
                break;
            case "watch":
                p.RoundRect(46, 6, 36, 116, 10, H("#2a2a33"));
                p.RoundRect(34, 34, 60, 60, 16, H("#4a4a55"));
                p.RoundRect(40, 40, 48, 48, 12, H("#0e1a2a"));
                p.Path(3, H("#4dd17a"), 46, 66, 54, 66, 58, 56, 64, 74, 68, 62, 82, 62);
                break;
            default: // smartphone
                p.RoundRect(36, 8, 56, 112, 12, H("#1a1a22"));
                p.RoundRect(41, 16, 46, 94, 6, H("#4ab0ff"));
                p.Poly(H("#7b5cff"), 41, 70, 87, 40, 87, 110, 41, 110);
                p.RoundRect(56, 11, 16, 3, 1, H("#333"));
                break;
        }
    });

    // ===== ТОВАРЫ ЛАВКИ =====
    public static Sprite Product(Product pr)
    {
        int i = Data.C.GROCERY_PRODUCTS.IndexOf(pr);
        return Icon3D("product_" + i) ?? Cached("product" + i, 128, p =>
        {
            switch (i)
            {
                case 0: // яблоки
                    p.Circle(48, 72, 28, H("#d6342f"));
                    p.Circle(80, 72, 28, H("#e8483f"));
                    p.Line(64, 46, 68, 30, 4, H("#5a3a1a"));
                    p.Ellipse(80, 30, 12, 6, H("#4a9a3a"));
                    p.Circle(40, 62, 6, H("#ffffff", 0.5f));
                    break;
                case 1: // морковь
                    p.Poly(H("#f08a24"), 30, 40, 54, 30, 104, 108);
                    p.Path(5, H("#4a9a3a"), 40, 34, 26, 12);
                    p.Path(5, H("#4a9a3a"), 44, 32, 46, 8);
                    p.Path(2, H("#c96a14"), 50, 52, 60, 50);
                    p.Path(2, H("#c96a14"), 64, 72, 74, 70);
                    break;
                case 2: // картошка
                    p.Ellipse(64, 70, 42, 30, H("#c9a35a"));
                    p.Circle(50, 62, 3, H("#8a6a32"));
                    p.Circle(76, 76, 3, H("#8a6a32"));
                    p.Circle(66, 58, 2.5f, H("#8a6a32"));
                    break;
                case 3: // помидоры
                    p.Circle(64, 72, 36, H("#e8483f"));
                    p.Poly(H("#3a8a2e"), 64, 36, 50, 44, 58, 46, 64, 40, 70, 46, 78, 44);
                    p.Circle(50, 60, 7, H("#ffffff", 0.45f));
                    break;
                case 4: // огурцы
                    p.RoundRect(18, 52, 92, 26, 13, H("#3a8a2e"));
                    p.RoundRect(26, 74, 84, 22, 11, H("#4a9a3a"));
                    for (int k = 0; k < 5; k++) p.Circle(30 + k * 16, 64, 1.8f, H("#d8f0c8"));
                    break;
                case 5: // бананы
                    p.Path(14, H("#f2d43a"), 24, 40, 38, 86, 76, 100, 106, 86);
                    p.Path(12, H("#e8c42a"), 30, 34, 50, 74, 84, 84, 108, 72);
                    p.Line(20, 36, 30, 30, 6, H("#6a5a2a"));
                    break;
                case 6: // лук
                    p.Ellipse(64, 76, 36, 32, H("#c98a4a"));
                    p.Poly(H("#c98a4a"), 52, 50, 76, 50, 64, 24);
                    p.Path(2, H("#a86a2a"), 64, 46, 56, 76, 64, 104);
                    p.Path(2, H("#a86a2a"), 64, 46, 74, 76, 64, 104);
                    break;
                default: // лимоны
                    p.Ellipse(64, 68, 40, 28, H("#f2d43a"));
                    p.Ellipse(26, 68, 8, 6, H("#e8c42a"));
                    p.Ellipse(102, 68, 8, 6, H("#e8c42a"));
                    p.Ellipse(52, 58, 14, 6, H("#ffffff", 0.45f));
                    break;
            }
        });
    }

    public static Sprite Parcel() => Cached("parcel", 200, p =>
    {
        p.Poly(H("#b88a54"), 30, 70, 100, 40, 170, 70, 170, 160, 100, 190, 30, 160);
        p.Poly(H("#d9b47a"), 30, 70, 100, 40, 170, 70, 100, 100);
        p.Poly(H("#a07440"), 100, 100, 170, 70, 170, 160, 100, 190);
        p.Poly(H("#f2e6c8"), 60, 56, 76, 50, 146, 80, 130, 86);
        p.RoundRect(44, 110, 44, 30, 3, H("#ffffff"));
        p.Line(50, 120, 80, 120, 3, H("#333"));
        p.Line(50, 130, 72, 130, 3, H("#333"));
    });

    public static Sprite Receipt() => Cached("receipt", 200, p =>
    {
        // Широкая бумажная лента с зубчатым краем; текст чека кладёт поверх MiniView.Receipt
        var pts = new List<float> { 14, 8, 186, 8, 186, 182 };
        for (int x = 186; x > 14; x -= 12)
        {
            pts.Add(x - 6); pts.Add(192);
            pts.Add(Mathf.Max(14, x - 12)); pts.Add(182);
        }
        p.Poly(H("#d8d2c4"), pts.Select((v, i) => i % 2 == 0 ? v + 2 : v + 3).ToArray());
        p.Poly(H("#fbf8f0"), pts.ToArray());
        for (int x = 26; x < 176; x += 10)
        {
            p.Line(x, 24, x + 5, 24, 2, H("#8a8a90", 0.5f));
            p.Line(x, 168, x + 5, 168, 2, H("#8a8a90", 0.5f));
        }
    });

    // ===== ЗНАЧКИ HUD =====
    public static Sprite Icon(string name) => Cached("icon" + name, 64, p =>
    {
        var w = H("#f1eefc");
        switch (name)
        {
            case "day":
                p.RoundRect(8, 12, 48, 44, 8, w);
                p.Rect(8, 12, 48, 12, H("#f87171"));
                p.Rect(18, 6, 6, 12, w);
                p.Rect(40, 6, 6, 12, w);
                p.Rect(16, 32, 8, 8, H("#2a2547"));
                p.Rect(28, 32, 8, 8, H("#2a2547"));
                p.Rect(40, 32, 8, 8, H("#2a2547"));
                break;
            case "money":
                p.Circle(32, 32, 26, H("#facc15"));
                p.Circle(32, 32, 19, H("#e0a80a"));
                p.Line(28, 20, 28, 44, 4, H("#fff3c4"));
                p.Path(4, H("#fff3c4"), 28, 20, 38, 22, 38, 30, 28, 32);
                p.Line(24, 38, 36, 38, 3, H("#fff3c4"));
                break;
            case "addiction":
                p.RoundRect(10, 10, 44, 44, 10, H("#a855f7"));
                p.Circle(22, 22, 4, w);
                p.Circle(42, 42, 4, w);
                p.Circle(32, 32, 4, w);
                p.Circle(42, 22, 4, w);
                p.Circle(22, 42, 4, w);
                break;
            case "satiety":
                p.Circle(32, 34, 24, w);
                p.Circle(32, 34, 16, H("#e8e0d0"));
                p.Line(6, 10, 6, 54, 4, w);
                p.Line(58, 10, 58, 54, 4, w);
                break;
            case "bag":
                p.RoundRect(12, 18, 40, 40, 12, H("#4ab0e0"));
                p.Ring(32, 18, 10, 4, H("#4ab0e0"));
                p.RoundRect(20, 34, 24, 14, 4, H("#2a7aa8"));
                break;
            case "soundOn":
                p.Poly(w, 10, 24, 22, 24, 34, 12, 34, 52, 22, 40, 10, 40);
                p.Path(4, w, 42, 22, 48, 32, 42, 42);
                p.Path(4, w, 48, 14, 58, 32, 48, 50);
                break;
            case "soundOff":
                p.Poly(w, 10, 24, 22, 24, 34, 12, 34, 52, 22, 40, 10, 40);
                p.Line(42, 22, 58, 42, 4, H("#f87171"));
                p.Line(58, 22, 42, 42, 4, H("#f87171"));
                break;
            case "restart":
                var arc = new List<float>();
                for (int k = 0; k <= 20; k++)
                {
                    float a = (60 + k * 13.5f) * Mathf.Deg2Rad;
                    arc.Add(32 + Mathf.Cos(a) * 20);
                    arc.Add(32 - Mathf.Sin(a) * 20);
                }
                p.Path(6, w, arc.ToArray());
                p.Poly(w, 30, 4, 46, 12, 30, 22);
                break;
        }
    });
}
