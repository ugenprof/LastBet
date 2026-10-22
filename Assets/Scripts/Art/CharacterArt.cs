// Персонажи по пояс, нарисованные кодом: крупная голова, плоская заливка с тенью сбоку (цел-шейдинг).
// Внешность — CharacterDef (Resources/Data/characters.json). Эмоции: normal | happy | sad | angry.
// Варианты для анимации: глаза закрыты (моргание), рот открыт (речь). Готовые текстуры кэшируются.
using System.Collections.Generic;
using UnityEngine;

public static class CharacterArt
{
    public const int UW = 200, UH = 320;   // холст в единицах
    const float SCALE = 2f;                // 400×640 пикселей

    public struct Look
    {
        public bool tired, pale, blink, talk;
    }

    static readonly Color PaleSkin = Painter.Hex("#e9dccf");
    static readonly Color Ink = Painter.Hex("#2a2333");
    static readonly Color MouthDark = Painter.Hex("#6e2a35");
    static readonly Color Tongue = Painter.Hex("#d9707a");
    static readonly Color Gold = Painter.Hex("#e3b341");

    // Готовая текстура или null (тогда она рисуется в фоне, urgent — вперёд очереди)
    public static Texture2D Get(string key, CharacterDef c, string mood, Look look, bool urgent = true)
    {
        string k = "ch:" + key + "|" + mood + "|" + look.tired + look.pale + look.blink + look.talk;
        return ArtQueue.Get(k, PW, PH, () => Pixels(c, mood, look), urgent);
    }

    const int PW = (int)(UW * SCALE), PH = (int)(UH * SCALE);

    // Нарисовать сразу (главный поток)
    public static Texture2D Draw(CharacterDef c, string mood, Look look) => Painter.MakeTexture(PW, PH, Pixels(c, mood, look));

    // Пиксели персонажа; можно вызывать в фоновом потоке
    public static Color32[] Pixels(CharacterDef c, string mood, Look look)
    {
        var p = new Painter(PW, PH, SCALE);
        var skin = look.pale ? PaleSkin : Painter.Hex(c.skin);
        var hair = Painter.Hex(c.hair);
        var top = Painter.Hex(c.top);
        var accent = c.accent != null ? Painter.Hex(c.accent) : Painter.Shade(top, 0.5f);

        HairBack(p, c, hair);
        Body(p, c, skin, top, accent);
        Head(p, c, skin, hair, mood, look);
        HairFront(p, c, hair);
        if (c.glasses) Glasses(p);
        Hat(p, c);
        return p.Pixels32();
    }

    // ===== ТЕЛО =====
    static void Body(Painter p, CharacterDef c, Color skin, Color top, Color accent)
    {
        var dark = Painter.Shade(top, -0.28f);
        // Шея
        p.RoundRect(84, 150, 32, 52, 12, Painter.Shade(skin, -0.14f));
        // Плечи и руки (тёмные по краям), корпус светлее, правая сторона в тени
        p.RoundRect(8, 196, 184, 170, 56, dark);
        p.RoundRect(18, 190, 164, 176, 52, top);
        p.ClipLeft = 128;
        p.RoundRect(18, 190, 164, 176, 52, Painter.Shade(top, -0.12f));
        p.NoClip();
        // Складки рукавов
        p.Line(46, 250, 44, 330, 3, Painter.A(dark, 0.7f));
        p.Line(154, 250, 156, 330, 3, Painter.A(dark, 0.7f));

        switch (c.topStyle)
        {
            case "hoodie":
                p.Ellipse(100, 196, 50, 18, dark);                     // капюшон за шеей
                p.Ellipse(100, 200, 28, 10, Painter.Shade(skin, -0.14f));
                p.Line(88, 206, 85, 252, 3.5f, accent);
                p.Line(112, 206, 115, 252, 3.5f, accent);
                p.Circle(85, 254, 3.5f, accent);
                p.Circle(115, 254, 3.5f, accent);
                p.RoundRect(66, 292, 68, 40, 12, Painter.Shade(top, -0.18f)); // карман
                break;
            case "sweater":
                p.Ellipse(100, 196, 30, 12, dark);
                p.Ellipse(100, 194, 22, 8, Painter.Shade(skin, -0.14f));
                p.Rect(22, 262, 156, 10, Painter.Shade(top, 0.2f));     // полоска узора
                p.Rect(22, 278, 156, 4, Painter.Shade(top, 0.2f));
                break;
            case "cardigan":
                p.Poly(accent, 78, 192, 122, 192, 100, 276);
                p.Line(80, 194, 70, 366, 4, dark);
                p.Line(120, 194, 130, 366, 4, dark);
                for (int i = 0; i < 4; i++) p.Circle(77 - i * 1.5f, 238 + i * 30, 3.4f, dark);
                break;
            case "jacket":
                p.Poly(accent, 80, 192, 120, 192, 100, 272);
                if (!c.noTie) p.Poly(c.tie != null ? Painter.Hex(c.tie) : dark, 95, 204, 105, 204, 109, 262, 100, 276, 91, 262);
                p.Poly(dark, 80, 192, 70, 196, 88, 262, 100, 278);
                p.Poly(dark, 120, 192, 130, 196, 112, 262, 100, 278);
                p.Circle(100, 300, 3.5f, dark);
                p.Circle(100, 330, 3.5f, dark);
                break;
            case "tshirt":
                p.Ellipse(100, 196, 26, 10, dark);
                p.Ellipse(100, 194, 21, 7, Painter.Shade(skin, -0.14f));
                p.Circle(100, 268, 24, accent);
                p.Line(88, 268, 112, 268, 5, top);
                p.Line(100, 256, 100, 280, 5, top);
                break;
            case "shirt":
                p.Poly(accent, 78, 190, 96, 214, 100, 198);
                p.Poly(accent, 122, 190, 104, 214, 100, 198);
                p.Line(100, 204, 100, 366, 2, dark);
                for (int i = 0; i < 5; i++) p.Circle(100, 226 + i * 28, 2.6f, dark);
                p.RoundRect(116, 236, 30, 26, 4, Painter.Shade(top, -0.08f));
                break;
            case "blouse":
                p.Poly(accent, 78, 190, 96, 220, 100, 200);
                p.Poly(accent, 122, 190, 104, 220, 100, 200);
                p.Circle(100, 206, 5, Gold);
                break;
            case "apron":
                p.Ellipse(100, 196, 26, 10, dark);
                p.RoundRect(56, 226, 88, 150, 14, accent);
                p.Line(62, 230, 74, 196, 5, accent);
                p.Line(138, 230, 126, 196, 5, accent);
                p.RoundRect(78, 290, 44, 30, 8, Painter.Shade(accent, -0.1f));
                break;
            case "vest":
                var vest = Painter.Hex("#f39c12");
                p.Poly(vest, 26, 210, 84, 196, 92, 366, 22, 366);
                p.Poly(vest, 174, 210, 116, 196, 108, 366, 178, 366);
                var refl = Painter.Hex("#e8eef3");
                p.Rect(22, 270, 70, 9, refl);
                p.Rect(108, 270, 70, 9, refl);
                p.Rect(22, 300, 70, 9, refl);
                p.Rect(108, 300, 70, 9, refl);
                break;
            case "coat":
                p.Poly(accent, 82, 192, 118, 192, 100, 260);
                p.Poly(Painter.Shade(top, -0.1f), 82, 192, 70, 198, 90, 300, 100, 262);
                p.Poly(Painter.Shade(top, -0.1f), 118, 192, 130, 198, 110, 300, 100, 262);
                if (c.stethoscope)
                {
                    var st = Painter.Hex("#4a4f58");
                    p.Path(3.5f, st, 84, 196, 76, 240, 86, 270, 100, 276);
                    p.Path(3.5f, st, 116, 196, 124, 240, 114, 270, 100, 276);
                    p.Circle(100, 282, 8, Painter.Hex("#9aa3ad"));
                }
                p.RoundRect(118, 300, 34, 30, 5, Painter.Shade(top, -0.06f));
                break;
            case "uniform":
                p.Ellipse(100, 196, 26, 10, dark);
                p.RoundRect(28, 196, 40, 12, 4, accent);              // погоны
                p.RoundRect(132, 196, 40, 12, 4, accent);
                p.Line(100, 204, 100, 366, 2.5f, dark);
                p.Circle(130, 250, 9, Gold);                          // значок
                p.RoundRect(56, 236, 34, 24, 4, dark);
                p.RoundRect(110, 270, 34, 24, 4, dark);
                break;
            case "tracksuit":
                p.Ellipse(100, 196, 28, 12, dark);
                p.Line(100, 204, 100, 366, 3, Painter.Shade(top, 0.25f));
                p.Line(30, 220, 22, 366, 6, accent);                  // лампасы на рукавах
                p.Line(40, 222, 34, 366, 6, accent);
                p.Line(170, 220, 178, 366, 6, accent);
                p.Line(160, 222, 166, 366, 6, accent);
                break;
            case "overalls":
                p.RoundRect(66, 250, 68, 130, 10, accent);
                p.Line(72, 254, 60, 196, 8, accent);
                p.Line(128, 254, 140, 196, 8, accent);
                p.Circle(74, 258, 4, Gold);
                p.Circle(126, 258, 4, Gold);
                p.RoundRect(84, 276, 32, 22, 4, Painter.Shade(accent, -0.15f));
                break;
        }
    }

    // ===== ГОЛОВА И ЛИЦО =====
    static void Head(Painter p, CharacterDef c, Color skin, Color hair, string mood, Look look)
    {
        var shadow = Painter.Shade(skin, -0.13f);
        // Уши
        p.Ellipse(51, 120, 10, 14, shadow);
        p.Ellipse(149, 120, 10, 14, shadow);
        if (c.earrings)
        {
            p.Circle(51, 138, 3.5f, Gold);
            p.Circle(149, 138, 3.5f, Gold);
        }
        // Голова: тень справа, свет слева
        p.Ellipse(100, 110, 50, 58, shadow);
        p.Ellipse(97, 109, 46, 56, skin);

        var brow = Painter.Shade(hair, -0.25f);
        if (c.hairStyle == "bald" || c.hairStyle == "scarf") brow = Painter.Shade(hair, -0.1f);

        // Морщины на лбу
        if (c.wrinkles)
        {
            p.Path(1.6f, Painter.A(Painter.Shade(skin, -0.3f), 0.7f), 80, 80, 100, 77, 120, 80);
            p.Path(1.6f, Painter.A(Painter.Shade(skin, -0.3f), 0.7f), 84, 87, 100, 85, 116, 87);
        }

        // Щетина и борода — под ртом
        if (c.stubble)
        {
            p.ClipTop = 128;
            p.Ellipse(98, 128, 44, 38, Painter.A(hair, 0.22f));
            p.NoClip();
        }
        if (c.beard)
        {
            p.ClipTop = 126;
            p.Ellipse(99, 132, 48, 40, hair);
            p.NoClip();
        }

        // Круги под глазами (зависимость)
        if (look.tired)
        {
            var bag = Painter.A(Painter.Hex("#6b4f86"), 0.45f);
            p.Path(2.2f, bag, 70, 130, 78, 133, 88, 130);
            p.Path(2.2f, bag, 112, 130, 122, 133, 130, 130);
        }

        Eyes(p, mood, look, skin);
        Brows(p, mood, brow);

        // Нос
        p.Path(2.2f, Painter.Shade(skin, -0.25f), 100, 122, 96, 134, 102, 136);

        // Румянец
        if (mood == "happy")
        {
            p.Ellipse(72, 140, 9, 5, Painter.A(Painter.Hex("#e8798a"), 0.35f));
            p.Ellipse(126, 140, 9, 5, Painter.A(Painter.Hex("#e8798a"), 0.35f));
        }

        if (c.mustache) p.Poly(hair, 84, 144, 92, 139, 100, 141, 108, 139, 116, 144, 108, 147, 100, 145, 92, 147);

        Mouth(p, mood, look.talk);
    }

    static void Eyes(Painter p, string mood, Look look, Color skin)
    {
        float[] xs = { 79, 119 };
        float y = 120;
        foreach (var x in xs)
        {
            if (look.blink)
            {
                p.Path(2.6f, Ink, x - 8, y + 1, x, y + 4, x + 8, y + 1);
                continue;
            }
            float ry = mood == "happy" ? 8f : mood == "angry" ? 7f : 9.5f;
            p.Ellipse(x, y, 8.5f, ry, Color.white);
            p.Ellipse(x + 0.5f, y + 1, 6f, ry - 2f, Ink);
            p.Circle(x + 2.5f, y - 2, 2.2f, Color.white);
            // Верхнее веко — линия
            p.Path(2.2f, Ink, x - 9, y - ry + 2, x, y - ry - 0.5f, x + 9, y - ry + 2);
            if (mood == "happy")
            {
                // Прищур: нижняя часть глаза закрыта щекой
                p.ClipTop = y + 3;
                p.Ellipse(x, y + 10, 11, 8, skin);
                p.NoClip();
            }
        }
    }

    static void Brows(Painter p, string mood, Color c)
    {
        switch (mood)
        {
            case "happy":
                p.Path(3.6f, c, 68, 104, 79, 98, 91, 102);
                p.Path(3.6f, c, 109, 102, 121, 98, 132, 104);
                break;
            case "sad":
                p.Path(3.6f, c, 68, 106, 80, 104, 91, 98);
                p.Path(3.6f, c, 109, 98, 120, 104, 132, 106);
                break;
            case "angry":
                p.Path(3.8f, c, 68, 99, 80, 102, 92, 108);
                p.Path(3.8f, c, 108, 108, 120, 102, 132, 99);
                break;
            default:
                p.Path(3.4f, c, 68, 104, 79, 101, 91, 103);
                p.Path(3.4f, c, 109, 103, 121, 101, 132, 104);
                break;
        }
    }

    static void Mouth(Painter p, string mood, bool talk)
    {
        if (talk)
        {
            float rx = mood == "happy" ? 11 : 8, ry = mood == "sad" ? 5 : 7;
            p.Ellipse(100, 154, rx, ry, MouthDark);
            p.ClipTop = 156;
            p.Ellipse(100, 160, rx - 3, ry - 1, Tongue);
            p.NoClip();
            return;
        }
        switch (mood)
        {
            case "happy":
                p.ClipTop = 150;
                p.Ellipse(100, 150, 14, 11, MouthDark);
                p.ClipTop = 156;
                p.Ellipse(100, 162, 9, 6, Tongue);
                p.NoClip();
                p.Rect(88, 150, 24, 3, Color.white);
                break;
            case "sad":
                p.Path(3f, MouthDark, 88, 158, 100, 152, 112, 158);
                break;
            case "angry":
                p.Path(3.4f, MouthDark, 88, 156, 100, 154, 112, 157);
                break;
            default:
                p.Path(3f, MouthDark, 88, 152, 100, 157, 112, 152);
                break;
        }
    }

    static void Glasses(Painter p)
    {
        var frame = Painter.Hex("#24202b");
        p.Ring(79, 120, 15, 3.2f, frame);
        p.Ring(119, 120, 15, 3.2f, frame);
        p.Line(94, 119, 104, 119, 3, frame);
        p.Line(64, 118, 52, 114, 3, frame);
        p.Line(134, 118, 148, 114, 3, frame);
        p.Ellipse(74, 114, 4, 2.4f, Painter.A(Color.white, 0.5f));
        p.Ellipse(114, 114, 4, 2.4f, Painter.A(Color.white, 0.5f));
    }

    // ===== ВОЛОСЫ =====
    static void HairBack(Painter p, CharacterDef c, Color hair)
    {
        var dark = Painter.Shade(hair, -0.2f);
        switch (c.hairStyle)
        {
            case "long":
                p.RoundRect(44, 70, 112, 210, 50, dark);
                break;
            case "bob":
                p.RoundRect(42, 66, 116, 116, 46, dark);
                break;
            case "ponytail":
                p.Ellipse(158, 136, 16, 44, dark);
                p.Ellipse(152, 92, 9, 9, Painter.Hex(c.top));
                break;
            case "bun":
                if (c.hat == null) p.Circle(100, 48, 22, dark);
                break;
            case "granny":
                p.Circle(100, 52, 18, dark);
                break;
            case "scarf":
                var scarf = Painter.Hex(c.scarf);
                p.Ellipse(100, 112, 62, 70, Painter.Shade(scarf, -0.15f));
                p.Poly(Painter.Shade(scarf, -0.15f), 60, 160, 140, 160, 120, 214, 80, 214);
                break;
        }
    }

    static void HairFront(Painter p, CharacterDef c, Color hair)
    {
        var lit = Painter.Shade(hair, 0.12f);
        switch (c.hairStyle)
        {
            case "bald":
                p.Ellipse(53, 108, 7, 16, hair);
                p.Ellipse(147, 108, 7, 16, hair);
                p.Ellipse(84, 70, 12, 6, Painter.A(Color.white, 0.25f)); // блик на макушке
                return;
            case "scarf":
                var scarf = Painter.Hex(c.scarf);
                p.ClipBottom = 92;
                p.Ellipse(100, 104, 56, 54, scarf);
                p.NoClip();
                p.Path(2.5f, Painter.Shade(scarf, 0.35f), 56, 90, 78, 82, 100, 80, 122, 82, 144, 90);
                p.Poly(scarf, 88, 166, 112, 166, 100, 186);
                return;
            case "buzz":
                p.ClipBottom = 84;
                p.Ellipse(98, 104, 51, 54, Painter.A(hair, 0.92f));
                p.NoClip();
                return;
        }

        float fringe = c.hairStyle == "dad" ? 82 : c.hairStyle == "neat" ? 88 : c.hairStyle == "granny" ? 86 : 94;
        // Шапка волос
        p.ClipBottom = fringe;
        p.Ellipse(99, 102, 55, 52, hair);
        p.NoClip();
        // Пряди у висков
        if (c.hairStyle != "dad" && c.hairStyle != "granny")
        {
            p.RoundRect(46, 84, 12, 40, 6, hair);
            p.RoundRect(142, 84, 12, 40, 6, hair);
        }

        switch (c.hairStyle)
        {
            case "messy":
                for (int i = 0; i < 6; i++)
                {
                    float x = 58 + i * 16;
                    p.Poly(hair, x, fringe - 6, x + 16, fringe - 6, x + 6, fringe + 10);
                }
                p.Poly(hair, 70, 58, 80, 34, 90, 54);
                p.Poly(hair, 96, 52, 112, 30, 118, 54);
                p.Poly(hair, 122, 58, 140, 44, 136, 66);
                break;
            case "neat":
                p.Ellipse(80, 82, 32, 13, lit);                     // зачёс набок
                p.Path(2f, Painter.Shade(hair, -0.3f), 112, 58, 116, 84);
                break;
            case "short":
                p.Poly(hair, 60, fringe - 4, 140, fringe - 4, 120, fringe + 6, 92, fringe + 2, 70, fringe + 8);
                break;
            case "bun":
            case "ponytail":
                p.Path(3f, Painter.Shade(hair, -0.25f), 100, 52, 92, 70, 74, 92);
                p.Poly(hair, 56, fringe - 6, 98, fringe - 8, 64, fringe + 14);
                break;
            case "long":
                p.RoundRect(44, 86, 18, 120, 9, hair);
                p.RoundRect(138, 86, 18, 120, 9, hair);
                p.Poly(hair, 62, fringe - 6, 104, fringe - 10, 70, fringe + 12);
                break;
            case "bob":
                p.Rect(56, fringe - 8, 88, 12, hair);                // прямая чёлка
                p.RoundRect(42, 86, 20, 90, 10, hair);
                p.RoundRect(138, 86, 20, 90, 10, hair);
                break;
            case "granny":
                p.Path(2f, Painter.Shade(hair, -0.2f), 70, 76, 86, 70, 100, 76, 114, 70, 130, 76);
                break;
        }
        p.Ellipse(80, 66, 14, 6, Painter.A(Color.white, 0.18f)); // блик
    }

    // ===== ГОЛОВНЫЕ УБОРЫ =====
    static void Hat(Painter p, CharacterDef c)
    {
        if (c.hat == null) return;
        var color = Painter.Hex(c.hatColor ?? "#333333");
        var dark = Painter.Shade(color, -0.3f);
        switch (c.hat)
        {
            case "cap":
                p.ClipBottom = 86;
                p.Ellipse(99, 92, 56, 50, color);
                p.NoClip();
                p.Ellipse(100, 88, 62, 9, dark);                    // козырёк
                p.Circle(100, 44, 4, dark);
                p.Path(2, Painter.A(dark, 0.6f), 100, 46, 100, 84);
                break;
            case "flatcap":
                p.ClipBottom = 84;
                p.Ellipse(100, 94, 58, 44, color);
                p.NoClip();
                p.Ellipse(104, 84, 60, 8, dark);
                break;
            case "police":
                p.RoundRect(46, 46, 108, 38, 14, color);
                p.Rect(48, 74, 104, 10, Painter.Hex("#b23b3b"));
                p.Ellipse(100, 88, 54, 7, Painter.Hex("#141414"));
                p.Circle(100, 62, 7, Gold);
                break;
        }
    }
}
