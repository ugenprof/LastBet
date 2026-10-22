// Фоны сцен, нарисованные кодом: плоская иллюстрация в сумеречной гамме, лёгкое зерно и виньетка.
// Холст 960×540 единиц (текстура 1280×720). Имена — как поле bg у сцен (school, home, alley, ...).
// Левая часть кадра важнее: справа фон закрыт панелью с текстом.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Backgrounds
{
    const float U = 960, V = 540, SCALE = 1280f / 960f;

    static Color H(string hex, float a = 1f) => Painter.Hex(hex, a);
    static Color Sh(Color c, float k) => Painter.Shade(c, k);
    static Color Al(Color c, float a) => Painter.A(c, a);

    public static readonly string[] All =
    {
        "school", "school-dark", "classroom", "home", "night", "kitchen", "casino", "alley", "hospital", "busy_street",
        "vegetable_shop", "canteen", "warehouse", "shawarma", "park", "mall", "yard", "car", "bus_stop", "prison",
        "electronic_store", "cloth_store"
    };

    // Фон: готовый — сразу, иначе null и рисуется в фоне (urgent — вперёд очереди)
    public static Texture2D Get(string name, bool urgent = false)
    {
        name = Normalize(name);
        return ArtQueue.Get("bg:" + name, PW, PH, () => Draw(name), urgent);
    }

    // Нарисовать сразу (для проверок без Unity)
    public static Texture2D GetNow(string name)
    {
        name = Normalize(name);
        return ArtQueue.GetNow("bg:" + name, PW, PH, () => Draw(name));
    }

    static string Normalize(string name) => string.IsNullOrEmpty(name) || Array.IndexOf(All, name) < 0 ? "school" : name;

    const int PW = 1280, PH = 720;

    // Центр неонового круга на фоне казино — доля экрана от левого нижнего угла (рисунок: 250, 290 из 960×540)
    public static readonly Vector2 CasinoRing = new Vector2(250f / U, 1f - 290f / V);

    // Пиксели фона; можно вызывать в фоновом потоке
    public static Color32[] Draw(string name)
    {
        var p = new Painter(PW, PH, SCALE);
        var rnd = new System.Random(Seed(name));
        switch (name)
        {
            case "school": Corridor(p, rnd, false); break;
            case "school-dark": Corridor(p, rnd, true); break;
            case "classroom": Classroom(p, rnd); break;
            case "home": Home(p, rnd); break;
            case "night": Night(p, rnd); break;
            case "kitchen": Kitchen(p, rnd); break;
            case "casino": CasinoBg(p, rnd); break;
            case "alley": Alley(p, rnd); break;
            case "hospital": Hospital(p, rnd); break;
            case "busy_street": Street(p, rnd); break;
            case "vegetable_shop": Vegetables(p, rnd); break;
            case "canteen": Canteen(p, rnd); break;
            case "warehouse": Warehouse(p, rnd); break;
            case "shawarma": Shawarma(p, rnd); break;
            case "park": Park(p, rnd); break;
            case "mall": Mall(p, rnd); break;
            case "yard": Yard(p, rnd); break;
            case "car": Car(p, rnd); break;
            case "bus_stop": BusStop(p, rnd); break;
            case "prison": Station(p, rnd); break;
            case "electronic_store": Electronics(p, rnd); break;
            case "cloth_store": Clothes(p, rnd); break;
        }
        p.Grain(0.035f, name.Length * 7919);
        p.Vignette(0.55f);
        return p.Pixels32();
    }

    // Зерно случайности по имени — одинаковое при каждом запуске (string.GetHashCode в Unity не постоянен)
    static int Seed(string name)
    {
        int h = 17;
        foreach (var c in name) h = h * 31 + c;
        return h;
    }

    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // ===== ОБЩИЕ ДЕТАЛИ =====
    static void Room(Painter p, Color wallTop, Color wallBottom, Color floorTop, Color floorBottom, float horizon)
    {
        p.VGrad(0, 0, U, horizon, wallTop, wallBottom);
        p.VGrad(0, horizon, U, V - horizon, floorTop, floorBottom);
        p.Rect(0, horizon - 10, U, 10, Sh(wallBottom, -0.25f)); // плинтус
    }

    static void Window(Painter p, float x, float y, float w, float h, Color skyTop, Color skyBottom, Color frame)
    {
        p.RoundRect(x - 8, y - 8, w + 16, h + 16, 6, frame);
        p.VGrad(x, y, w, h, skyTop, skyBottom);
        p.Rect(x + w / 2 - 4, y, 8, h, frame);
        p.Rect(x, y + h * 0.42f, w, 7, frame);
        p.Rect(x - 14, y + h + 6, w + 28, 10, Sh(frame, 0.15f)); // подоконник
    }

    static void Skyline(Painter p, System.Random r, float baseY, float minH, float maxH, Color body, Color lit, float litChance, float x0 = 0, float x1 = U)
    {
        float x = x0 - 10;
        while (x < x1)
        {
            float w = R(r, 40, 90), h = R(r, minH, maxH);
            p.Rect(x, baseY - h, w, h + 2, body);
            for (float wy = baseY - h + 12; wy < baseY - 10; wy += 16)
                for (float wx = x + 8; wx < x + w - 10; wx += 14)
                    if (r.NextDouble() < litChance) p.Rect(wx, wy, 6, 8, lit);
            x += w + R(r, 0, 8);
        }
    }

    static void Stars(Painter p, System.Random r, float x, float y, float w, float h, int n)
    {
        for (int i = 0; i < n; i++) p.Circle(x + R(r, 0, w), y + R(r, 0, h), R(r, 0.6f, 1.6f), H("#ffffff", R(r, 0.3f, 0.9f)));
    }

    static void Lamp(Painter p, float x, float y, Color light, float radius)
    {
        p.Glow(x, y + 20, radius, Al(light, 0.55f));
        p.RoundRect(x - 26, y - 4, 52, 14, 6, H("#2a2730"));
        p.Ellipse(x, y + 12, 20, 6, Sh(light, 0.4f));
    }

    static void Silhouette(Painter p, float x, float baseY, float h, Color c)
    {
        p.Circle(x, baseY - h + h * 0.12f, h * 0.12f, c);
        p.RoundRect(x - h * 0.16f, baseY - h * 0.76f, h * 0.32f, h * 0.5f, h * 0.12f, c);
        p.RoundRect(x - h * 0.12f, baseY - h * 0.3f, h * 0.1f, h * 0.3f, 3, c);
        p.RoundRect(x + h * 0.02f, baseY - h * 0.3f, h * 0.1f, h * 0.3f, 3, c);
    }

    // ===== ШКОЛА: КОРИДОР =====
    static void Corridor(Painter p, System.Random r, bool dark)
    {
        float k = dark ? -0.62f : 0f;
        Color wallT = Sh(H("#b9d3cf"), k), wallB = Sh(H("#8fb5b1"), k), floorT = Sh(H("#a67c5b"), k), floorB = Sh(H("#6e4f3a"), k);
        Room(p, wallT, wallB, floorT, floorB, 360);
        // Плитка на полу — сходится к центру
        for (int i = -8; i <= 8; i++) p.Line(480 + i * 18, 360, 480 + i * 150, 540, 2, Al(Sh(floorB, -0.3f), 0.5f));
        for (float y = 380; y < 540; y += (y - 330) * 0.35f) p.Line(0, y, U, y, 2, Al(Sh(floorB, -0.3f), 0.4f));
        // Верхняя часть стены светлее, панель снизу
        p.Rect(0, 210, U, 150, Sh(wallB, -0.08f));
        p.Rect(0, 206, U, 6, Sh(wallB, -0.3f));
        // Окна
        for (int i = 0; i < 3; i++)
            Window(p, 560 + i * 140, 50, 100, 120, dark ? H("#101a2c") : H("#cfe6f5"), dark ? H("#1d2a40") : H("#f6efd9"), Sh(H("#e8e4dc"), k));
        // Шкафчики
        for (int i = 0; i < 9; i++)
        {
            float x = 20 + i * 58;
            var c = Sh(i % 2 == 0 ? H("#4f7fa8") : H("#5b8cb5"), k);
            p.RoundRect(x, 196, 54, 164, 4, c);
            p.Rect(x + 4, 196, 2, 164, Sh(c, -0.25f));
            for (int v = 0; v < 3; v++) p.Rect(x + 14, 212 + v * 8, 26, 3, Sh(c, -0.3f));
            p.RoundRect(x + 40, 270, 6, 16, 2, Sh(c, 0.4f));
        }
        // Доска объявлений
        p.RoundRect(180, 70, 220, 100, 6, Sh(H("#b98a5a"), k));
        p.RoundRect(188, 78, 204, 84, 4, Sh(H("#d9b380"), k));
        string[] papers = { "#fdfdf5", "#ffe58a", "#bfe3ff", "#ffc6c6" };
        for (int i = 0; i < 6; i++)
        {
            float x = 196 + i * 32 + R(r, -4, 4), y = 86 + (i % 2) * 22;
            p.Rect(x, y, 26, 34, Sh(H(papers[i % 4]), k));
            p.Circle(x + 13, y + 3, 2.5f, H("#d33"));
        }
        // Часы
        p.Circle(460, 110, 26, Sh(H("#f4f1ea"), k));
        p.Ring(460, 110, 26, 4, Sh(H("#3a3a3a"), k));
        p.Line(460, 110, 460, 94, 3, H("#333"));
        p.Line(460, 110, 472, 116, 3, H("#333"));
        if (dark)
        {
            p.RoundRect(60, 120, 80, 30, 4, H("#3a0d0d"));
            p.Glow(100, 135, 120, H("#ff3b3b", 0.35f));
            p.RoundRect(66, 126, 68, 18, 3, H("#ff5a4f"));
        }
        else
        {
            for (int i = 0; i < 3; i++) p.Poly(H("#fff8e0", 0.08f), 560 + i * 140, 180, 660 + i * 140, 180, 760 + i * 140 - 60, 540, 520 + i * 140 - 120, 540);
        }
    }

    // ===== КЛАСС =====
    static void Classroom(Painter p, System.Random r)
    {
        Room(p, H("#d8cdb0"), H("#bfae8a"), H("#8a6a4c"), H("#5a4330"), 330);
        // Доска
        p.RoundRect(150, 60, 420, 200, 6, H("#6b4b32"));
        p.Rect(160, 70, 400, 180, H("#2f5a44"));
        p.Path(2.5f, H("#e8f0e8", 0.75f), 190, 110, 230, 100, 270, 112, 310, 104);
        p.Path(2.5f, H("#e8f0e8", 0.7f), 190, 150, 300, 150);
        p.Path(2.5f, H("#e8f0e8", 0.6f), 190, 180, 260, 176, 290, 190, 360, 182);
        p.Line(400, 120, 520, 210, 2.5f, H("#e8f0e8", 0.5f));
        p.Circle(470, 150, 34, H("#2f5a44"));
        p.Ring(470, 150, 34, 2.5f, H("#e8f0e8", 0.6f));
        p.Rect(160, 250, 400, 10, H("#8b6a4c"));
        // Окна слева
        Window(p, 20, 60, 90, 180, H("#a7d1ef"), H("#e9f3e3"), H("#efe9dc"));
        p.Poly(H("#fff6d6", 0.12f), 20, 250, 110, 250, 300, 540, 60, 540);
        // Парты рядами
        for (int row = 0; row < 3; row++)
        {
            float y = 360 + row * 58, s = 1 + row * 0.25f;
            for (int i = 0; i < 4; i++)
            {
                float x = 40 + i * 230 - row * 30;
                p.Poly(H("#b5835a"), x, y, x + 150 * s, y, x + 160 * s, y + 18 * s, x - 10, y + 18 * s);
                p.Rect(x, y + 18 * s, 6, 40 * s, H("#3d3d45"));
                p.Rect(x + 140 * s, y + 18 * s, 6, 40 * s, H("#3d3d45"));
            }
        }
        p.Circle(640, 80, 22, H("#f4f1ea"));
        p.Ring(640, 80, 22, 4, H("#333"));
    }

    // ===== ДОМ: ГОСТИНАЯ ВЕЧЕРОМ =====
    static void Home(Painter p, System.Random r)
    {
        Room(p, H("#6f4f5f"), H("#4d3545"), H("#5a4032"), H("#33241c"), 380);
        for (float x = 0; x < U; x += 36) p.Rect(x, 0, 14, 370, H("#ffffff", 0.035f)); // обои
        // Окно с закатом
        Window(p, 120, 60, 170, 190, H("#3b3f78"), H("#f19a6b"), H("#d8c8b5"));
        var rr = new System.Random(5);
        Skyline(p, rr, 250, 20, 70, H("#2a2440"), H("#ffd27a"), 0.25f, 120, 290);
        p.Rect(112, 52, 186, 8, H("#d8c8b5"));
        p.RoundRect(96, 40, 36, 240, 10, H("#8a3a4a"));  // шторы
        p.RoundRect(278, 40, 36, 240, 10, H("#8a3a4a"));
        // Диван
        p.RoundRect(330, 300, 300, 110, 30, H("#3f5d7a"));
        p.RoundRect(350, 250, 260, 90, 26, H("#4a6b8a"));
        p.RoundRect(310, 290, 50, 120, 20, H("#365270"));
        p.RoundRect(600, 290, 50, 120, 20, H("#365270"));
        p.RoundRect(380, 270, 70, 50, 14, H("#d9a441"));  // подушки
        p.RoundRect(500, 272, 70, 48, 14, H("#c25e4a"));
        // Торшер
        p.Line(720, 180, 720, 420, 5, H("#2b2420"));
        p.Ellipse(720, 424, 30, 7, H("#2b2420"));
        p.Poly(H("#f4d8a0"), 690, 180, 750, 180, 765, 130, 675, 130);
        p.Glow(720, 170, 260, H("#ffcf7a", 0.45f));
        // Картины
        p.RoundRect(380, 80, 90, 70, 4, H("#c9a66b"));
        p.Rect(388, 88, 74, 54, H("#6f9a8a"));
        p.Poly(H("#4e7a62"), 388, 142, 420, 110, 445, 130, 462, 115, 462, 142);
        p.RoundRect(500, 96, 60, 74, 4, H("#c9a66b"));
        p.Rect(507, 103, 46, 60, H("#e8d8c4"));
        p.Circle(530, 125, 12, H("#c98a7a"));
        // Ковёр
        p.Ellipse(480, 470, 260, 40, H("#7a3a3a"));
        p.Ellipse(480, 470, 230, 30, H("#9a4a44"));
    }

    // ===== НОЧЬ: КОМНАТА ГЕРОЯ =====
    static void Night(Painter p, System.Random r)
    {
        Room(p, H("#1b2340"), H("#121830"), H("#141826"), H("#090b12"), 390);
        // Окно с луной и городом
        Window(p, 90, 50, 220, 220, H("#0b1230"), H("#24305a"), H("#2c3550"));
        Stars(p, r, 90, 50, 220, 120, 40);
        p.Circle(250, 100, 22, H("#f3efd2"));
        p.Circle(260, 94, 20, H("#0f1736"));
        Skyline(p, r, 270, 30, 110, H("#0a0e1e"), H("#ffcf6a", 0.9f), 0.18f, 90, 310);
        p.Poly(H("#a9b8ff", 0.06f), 90, 270, 310, 270, 420, 540, 40, 540); // лунный свет на полу
        // Кровать
        p.RoundRect(360, 300, 360, 130, 18, H("#2a3a6a"));
        p.RoundRect(350, 250, 40, 180, 10, H("#3a2a26"));
        p.RoundRect(380, 290, 110, 40, 16, H("#e3e6f2"));
        p.RoundRect(470, 310, 260, 70, 24, H("#36508a"));
        // Телефон светится на одеяле
        p.Glow(600, 320, 150, H("#7ee8ff", 0.5f));
        p.RoundRect(586, 300, 28, 46, 5, H("#0b0b10"));
        p.RoundRect(589, 304, 22, 38, 3, H("#9ff3ff"));
        // Стол и тумбочка
        p.RoundRect(760, 280, 160, 14, 4, H("#3a2e2a"));
        p.Rect(770, 294, 10, 110, H("#2a2220"));
        p.Rect(900, 294, 10, 110, H("#2a2220"));
        p.RoundRect(800, 230, 70, 50, 6, H("#22283a"));       // монитор
        p.Rect(806, 236, 58, 38, H("#151a28"));
        // Постеры
        p.Rect(420, 90, 80, 110, H("#5a2a6a"));
        p.Circle(460, 135, 22, H("#e86aa0"));
        p.Rect(540, 70, 90, 70, H("#2a5a6a"));
        p.Line(550, 120, 620, 90, 4, H("#ffd166"));
    }

    // ===== КУХНЯ =====
    static void Kitchen(Painter p, System.Random r)
    {
        Room(p, H("#e6c79a"), H("#c9a273"), H("#7a5a44"), H("#4a372a"), 380);
        // Плитка-фартук
        for (float y = 170; y < 260; y += 22)
            for (float x = 0; x < U; x += 22) p.Rect(x + 1, y + 1, 20, 20, H("#f2e6cf"));
        p.Rect(0, 166, U, 4, H("#b88a5a"));
        // Шкафы
        for (int i = 0; i < 5; i++)
        {
            p.RoundRect(20 + i * 120, 30, 112, 130, 6, H("#7e9a7a"));
            p.RoundRect(30 + i * 120, 40, 92, 110, 4, H("#8fab8a"));
            p.RoundRect(110 + i * 120, 120, 6, 24, 3, H("#d9cfa8"));
        }
        // Столешница и нижние шкафы
        p.Rect(0, 260, 620, 18, H("#5a4a3e"));
        p.Rect(0, 278, 620, 100, H("#6f8a6a"));
        for (int i = 0; i < 5; i++) p.RoundRect(14 + i * 122, 290, 110, 80, 4, H("#7e9a7a"));
        // Плита и чайник
        p.Rect(330, 250, 120, 12, H("#2a2a2e"));
        p.Ellipse(370, 250, 22, 5, H("#444"));
        p.RoundRect(350, 210, 44, 40, 14, H("#c94a3a"));
        p.Line(392, 222, 408, 214, 5, H("#c94a3a"));
        // Окно
        Window(p, 680, 60, 180, 170, H("#5b6fa8"), H("#f2b37a"), H("#efe6da"));
        // Стол с тарелками и лампа
        p.Ellipse(480, 460, 230, 46, H("#9a6a44"));
        p.Ellipse(480, 452, 230, 44, H("#b57e52"));
        p.Ellipse(410, 446, 40, 12, H("#f5f1ea"));
        p.Ellipse(410, 446, 26, 7, H("#d9a441"));
        p.Ellipse(550, 446, 40, 12, H("#f5f1ea"));
        p.Ellipse(550, 446, 26, 7, H("#c25e4a"));
        p.Line(480, 0, 480, 140, 3, H("#2a2420"));
        p.Poly(H("#c94a3a"), 450, 140, 510, 140, 530, 180, 430, 180);
        p.Glow(480, 220, 300, H("#ffd38a", 0.4f));
    }

    // ===== КАЗИНО (приложение): неон =====
    static void CasinoBg(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#1a0630"), H("#07040f"));
        for (int i = 0; i < 26; i++)
            p.Glow(R(r, 0, U), R(r, 0, V), R(r, 30, 110), Al(i % 3 == 0 ? H("#ff3fa4") : i % 3 == 1 ? H("#7b5cff") : H("#ffd23f"), R(r, 0.15f, 0.4f)));
        // Неоновые дуги и линии (центр — CasinoRing: туда ставится колесо рулетки)
        p.Ring(250, 290, 190, 5, H("#ff3fa4", 0.8f));
        p.Ring(250, 290, 160, 2.5f, H("#7b5cff", 0.7f));
        p.Glow(250, 290, 260, H("#ff3fa4", 0.18f));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            p.Line(250 + Mathf.Cos(a) * 160, 290 + Mathf.Sin(a) * 160, 250 + Mathf.Cos(a) * 190, 290 + Mathf.Sin(a) * 190, 3, H("#ffd23f", 0.6f));
        }
        // Фишки
        for (int i = 0; i < 7; i++)
        {
            float x = R(r, 40, 560), y = R(r, 380, 520);
            var c = i % 2 == 0 ? H("#d6264f") : H("#2b6be0");
            p.Ellipse(x, y + 4, 34, 12, Sh(c, -0.4f));
            p.Ellipse(x, y, 34, 12, c);
            p.Ellipse(x, y, 22, 7, H("#ffffff", 0.85f));
        }
        // Карты
        p.RoundRect(520, 120, 90, 130, 8, H("#f4f1ea"));
        p.Circle(565, 185, 20, H("#d6264f"));
        p.RoundRect(600, 140, 90, 130, 8, H("#f4f1ea"));
        p.Poly(H("#111"), 645, 180, 665, 205, 645, 230, 625, 205);
        p.Line(0, 360, U, 360, 2, H("#7b5cff", 0.5f));
    }

    // ===== ПОДВОРОТНЯ =====
    static void Alley(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#0b0f1c"), H("#05060a"));
        Stars(p, r, 280, 0, 400, 80, 18);
        // Стены уходят в перспективу
        p.Poly(H("#3a2a2a"), 0, 0, 300, 120, 300, 400, 0, 540);
        p.Poly(H("#2c2428"), 960, 0, 640, 120, 640, 400, 960, 540);
        for (int i = 0; i < 12; i++)
        {
            float y = 40 + i * 40;
            p.Line(0, y, 300, 120 + i * 24, 1.6f, H("#000000", 0.35f));
            p.Line(960, y, 640, 120 + i * 24, 1.6f, H("#000000", 0.35f));
        }
        // Гаражи в глубине
        for (int i = 0; i < 4; i++)
        {
            float x = 310 + i * 82;
            p.Rect(x, 260, 76, 140, H("#4a4f5a"));
            p.Rect(x + 6, 276, 64, 124, H("#5a606c"));
            for (float y = 286; y < 396; y += 12) p.Line(x + 8, y, x + 68, y, 1.4f, H("#3a3f48"));
        }
        p.Rect(300, 400, 340, 140, H("#1a1c22"));
        p.Poly(H("#15161b"), 0, 540, 300, 400, 640, 400, 960, 540);
        // Фонарь и лужи
        p.Line(340, 120, 340, 260, 4, H("#1a1a1e"));
        p.Line(340, 124, 380, 124, 4, H("#1a1a1e"));
        p.Glow(380, 140, 230, H("#ffb84d", 0.45f));
        p.Ellipse(380, 134, 14, 6, H("#ffe2a0"));
        p.Ellipse(420, 470, 120, 16, H("#ffb84d", 0.15f));
        p.Ellipse(250, 510, 90, 10, H("#8ab4ff", 0.08f));
        // Граффити
        p.Path(5, H("#c94ad6", 0.7f), 60, 300, 90, 260, 120, 310, 150, 270, 180, 300);
        p.Path(4, H("#4ad6c9", 0.6f), 70, 340, 160, 330);
        p.Path(5, H("#e0c14a", 0.6f), 800, 280, 830, 250, 860, 290, 890, 260);
        // Мусорный бак
        p.RoundRect(560, 360, 70, 60, 6, H("#2e4a3a"));
        p.Rect(552, 352, 86, 12, H("#24382d"));
    }

    // ===== БОЛЬНИЦА =====
    static void Hospital(Painter p, System.Random r)
    {
        Room(p, H("#cfe3e2"), H("#a9c9c8"), H("#9fb1b4"), H("#6f8287"), 380);
        p.Rect(0, 250, U, 6, H("#7fa7a6"));
        // Окно с жалюзи
        p.RoundRect(110, 60, 220, 170, 4, H("#e8efef"));
        p.VGrad(120, 70, 200, 150, H("#bcd8ee"), H("#f2f2e6"));
        for (float y = 74; y < 220; y += 12) p.Rect(120, y, 200, 6, H("#e9eef0", 0.85f));
        // Кровать
        p.RoundRect(360, 300, 330, 100, 14, H("#e8eef2"));
        p.RoundRect(360, 290, 330, 30, 10, H("#8fb7d6"));
        p.RoundRect(370, 270, 100, 34, 14, H("#ffffff"));
        p.Rect(350, 240, 10, 200, H("#9aa6ad"));
        p.Rect(690, 260, 10, 180, H("#9aa6ad"));
        p.Line(360, 300, 690, 300, 4, H("#9aa6ad"));
        // Капельница
        p.Line(760, 90, 760, 440, 4, H("#8a949a"));
        p.Ellipse(760, 444, 30, 6, H("#8a949a"));
        p.Line(740, 96, 780, 96, 4, H("#8a949a"));
        p.RoundRect(746, 100, 28, 50, 8, H("#dff1ff", 0.9f));
        p.Rect(750, 120, 20, 26, H("#ffe2b0", 0.8f));
        p.Path(2, H("#b8c8d0"), 760, 150, 750, 220, 700, 290);
        // Монитор
        p.RoundRect(820, 160, 110, 80, 6, H("#2a3238"));
        p.Rect(828, 168, 94, 64, H("#0e1a14"));
        p.Path(2.2f, H("#4dff8f"), 832, 200, 850, 200, 858, 184, 866, 214, 874, 200, 918, 200);
        p.Glow(875, 200, 110, H("#4dff8f", 0.15f));
        p.Rect(0, 0, U, 18, H("#f4fbfb"));
        p.Glow(480, 10, 400, H("#ffffff", 0.25f));
    }

    // ===== УЛИЦА У МЕТРО =====
    static void Street(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, 300, H("#8fb8dc"), H("#e9d9c0"));
        Skyline(p, r, 330, 120, 260, H("#8a8fa3"), H("#d9e4f0"), 0.3f);
        Skyline(p, r, 340, 60, 150, H("#6a6f84"), H("#f6e7b0"), 0.2f);
        // Тротуар и дорога
        p.Rect(0, 330, U, 70, H("#b7aea0"));
        for (float x = 0; x < U; x += 60) p.Line(x, 330, x - 30, 400, 1.5f, H("#9a9184"));
        p.Rect(0, 400, U, 140, H("#4a4a52"));
        for (int i = 0; i < 8; i++) p.Rect(300 + i * 46, 410, 26, 120, H("#e8e8e8", 0.85f)); // зебра
        // Вход в метро
        p.RoundRect(60, 200, 170, 130, 8, H("#5d6a7a"));
        p.Rect(74, 230, 142, 100, H("#2b3340"));
        p.Circle(145, 176, 28, H("#d62b3a"));
        p.Poly(H("#ffffff"), 129, 190, 133, 164, 145, 180, 157, 164, 161, 190, 155, 190, 153, 176, 145, 186, 137, 176, 135, 190);
        // Деревья
        for (int i = 0; i < 3; i++)
        {
            float x = 300 + i * 220;
            p.Rect(x - 5, 260, 10, 72, H("#5a4030"));
            p.Circle(x, 240, 36, H("#4d8a4f"));
            p.Circle(x - 22, 256, 26, H("#5a9a5a"));
            p.Circle(x + 24, 254, 28, H("#437a45"));
        }
        // Прохожие вдали
        for (int i = 0; i < 9; i++) Silhouette(p, R(r, 240, 900), R(r, 360, 395), R(r, 50, 70), H("#3a3f52", R(r, 0.45f, 0.75f)));
        // Автобус
        p.RoundRect(620, 330, 300, 110, 18, H("#e0b83a"));
        for (int i = 0; i < 5; i++) p.RoundRect(636 + i * 56, 346, 44, 40, 4, H("#2a3a4a"));
        p.Circle(670, 444, 20, H("#1a1a1e"));
        p.Circle(860, 444, 20, H("#1a1a1e"));
    }

    // ===== ОВОЩНАЯ ЛАВКА =====
    static void Vegetables(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#e9d6b0"), H("#a37e55"));
        // Полосатый навес
        for (int i = 0; i < 12; i++) p.Poly(i % 2 == 0 ? H("#2f8a5a") : H("#f3efe2"), i * 80, 0, i * 80 + 80, 0, i * 80 + 80, 90, i * 80, 90);
        for (int i = 0; i < 12; i++) p.Circle(i * 80 + 40, 90, 40, i % 2 == 0 ? H("#2f8a5a") : H("#f3efe2"));
        p.Rect(0, 0, U, 14, H("#1f6a42"));
        // Прилавок с ящиками
        string[][] produce =
        {
            new[] { "#d6342f", "#e8483f" }, new[] { "#f08a24", "#f39c3a" }, new[] { "#c9a35a", "#b58c48" },
            new[] { "#e8483f", "#c02a2a" }, new[] { "#4a9a3a", "#3a8a2e" }, new[] { "#f2d43a", "#e8c42a" }
        };
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < 4; i++)
            {
                float x = 30 + i * 150, y = 250 + row * 110;
                p.RoundRect(x, y + 40, 140, 60, 4, H("#8a5a32"));
                for (int s = 0; s < 3; s++) p.Rect(x, y + 50 + s * 18, 140, 3, H("#6a4222"));
                var pr = produce[(row * 4 + i) % produce.Length];
                for (int k = 0; k < 9; k++) p.Circle(x + 16 + (k % 5) * 27 + (k / 5) * 13, y + 40 - (k / 5) * 16, 14, H(pr[k % 2]));
                p.RoundRect(x + 46, y + 74, 48, 22, 4, H("#fdfaf0"));
                p.Line(x + 54, y + 85, x + 86, y + 85, 3, H("#333", 0.6f));
            }
        // Весы
        p.RoundRect(660, 300, 120, 60, 8, H("#e8e8e8"));
        p.Ellipse(720, 300, 60, 10, H("#c8c8c8"));
        p.RoundRect(690, 320, 60, 24, 4, H("#2a3a2a"));
        p.Rect(0, 470, U, 70, H("#6a4a2e"));
    }

    // ===== СТОЛОВАЯ =====
    static void Canteen(Painter p, System.Random r)
    {
        Room(p, H("#f0dcb0"), H("#d9bf8a"), H("#a88a64"), H("#6e5940"), 360);
        // Раздача
        p.Rect(0, 230, 560, 20, H("#b8bec4"));
        p.Rect(0, 250, 560, 110, H("#cfd4d8"));
        for (int i = 0; i < 6; i++)
        {
            p.RoundRect(20 + i * 90, 200, 70, 32, 6, H("#9aa2a8"));
            p.Ellipse(55 + i * 90, 204, 28, 7, i % 2 == 0 ? H("#d9a441") : H("#c25e4a"));
        }
        p.Glow(280, 210, 280, H("#fff0c8", 0.35f));
        // Меню
        p.RoundRect(120, 40, 300, 130, 6, H("#3a2e26"));
        for (int i = 0; i < 5; i++) p.Line(140, 70 + i * 20, 300 + (i % 3) * 30, 70 + i * 20, 4, H("#f2e6c8", 0.75f));
        // Столы
        for (int i = 0; i < 3; i++)
        {
            float y = 390 + i * 50;
            p.RoundRect(-20 + i * 20, y, 560, 20, 6, H("#e2c99a"));
            for (int k = 0; k < 6; k++) p.Ellipse(30 + k * 90 + i * 20, y + 4, 22, 6, H("#f8f6f0"));
        }
        Window(p, 640, 60, 120, 200, H("#a9cfe9"), H("#f2eacf"), H("#efe6da"));
        Window(p, 800, 60, 120, 200, H("#a9cfe9"), H("#f2eacf"), H("#efe6da"));
    }

    // ===== СКЛАД =====
    static void Warehouse(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#4a5258"), H("#22272b"));
        p.Rect(0, 400, U, 140, H("#3a3f44"));
        // Стеллажи с коробками
        for (int s = 0; s < 3; s++)
        {
            float x = 20 + s * 210;
            p.Rect(x, 60, 8, 340, H("#d9822b"));
            p.Rect(x + 180, 60, 8, 340, H("#d9822b"));
            for (int lvl = 0; lvl < 4; lvl++)
            {
                float y = 120 + lvl * 85;
                p.Rect(x, y, 188, 8, H("#2f6aa8"));
                float bx = x + 10;
                while (bx < x + 170)
                {
                    float w = R(r, 30, 60), h = R(r, 30, 60);
                    if (bx + w > x + 180) break;
                    p.Rect(bx, y - h, w, h, H("#c49a64"));
                    p.Rect(bx + w / 2 - 4, y - h, 8, h, H("#d9b47a"));
                    bx += w + 4;
                }
            }
        }
        // Конвейер
        p.RoundRect(640, 330, 320, 40, 18, H("#2a2e33"));
        for (float x = 650; x < 960; x += 30) p.Circle(x, 350, 10, H("#555b62"));
        p.Rect(700, 300, 50, 32, H("#c49a64"));
        p.Rect(800, 296, 60, 36, H("#b88a54"));
        // Лампы
        for (int i = 0; i < 4; i++)
        {
            float x = 120 + i * 240;
            p.Line(x, 0, x, 30, 2, H("#111"));
            p.Poly(H("#2a2e33"), x - 30, 46, x + 30, 46, x + 14, 28, x - 14, 28);
            p.Glow(x, 60, 200, H("#fff0c0", 0.25f));
        }
        // Полосы безопасности
        for (int i = 0; i < 24; i++) p.Poly(i % 2 == 0 ? H("#f2c12e") : H("#1a1a1a"), i * 40, 520, i * 40 + 40, 520, i * 40 + 20, 540, i * 40 - 20, 540);
    }

    // ===== ШАУРМИЧНАЯ =====
    static void Shawarma(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#6a2a1e"), H("#2a120c"));
        // Меню с подсветкой
        p.RoundRect(80, 40, 520, 130, 8, H("#1a0c08"));
        p.RoundRect(88, 48, 504, 114, 6, H("#f2c84a"));
        for (int i = 0; i < 3; i++)
        {
            p.RoundRect(100 + i * 166, 60, 150, 70, 6, H("#e2572e"));
            p.Ellipse(175 + i * 166, 95, 40, 18, H("#f6e0a8"));
            p.Line(120 + i * 166, 145, 230 + i * 166, 145, 5, H("#3a1a10"));
        }
        p.Glow(340, 100, 380, H("#ffcc55", 0.25f));
        // Вертел
        p.Line(700, 120, 700, 360, 6, H("#9aa2a8"));
        p.Poly(H("#a0582a"), 660, 150, 740, 150, 728, 330, 672, 330);
        for (int i = 0; i < 8; i++) p.Line(664 + i * 2, 160 + i * 22, 736 - i * 2, 166 + i * 22, 4, H("#7a3a18", 0.7f));
        p.RoundRect(770, 140, 30, 200, 6, H("#2a2a2e"));
        p.Glow(785, 240, 120, H("#ff6a2a", 0.35f));
        // Прилавок
        p.Rect(0, 360, U, 26, H("#c9c2b8"));
        p.Rect(0, 386, U, 154, H("#7a3a26"));
        for (float x = 0; x < U; x += 48) p.Rect(x, 386, 2, 154, H("#5a2a1a"));
        p.RoundRect(120, 320, 90, 44, 6, H("#2a2a30"));     // касса
        p.Rect(130, 328, 70, 22, H("#4dd17a"));
        p.RoundRect(420, 330, 80, 30, 10, H("#f2efe6"));    // тарелка с лавашом
        p.Ellipse(460, 330, 30, 10, H("#e8c27a"));
    }

    // ===== ПАРК =====
    static void Park(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, 340, H("#7fb7e6"), H("#f2e6c8"));
        p.Circle(800, 90, 40, H("#fff3c4"));
        p.Glow(800, 90, 160, H("#fff3c4", 0.4f));
        // Колесо обозрения
        p.Ring(250, 190, 140, 6, H("#5a6a8a"));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            float x = 250 + Mathf.Cos(a) * 140, y = 190 + Mathf.Sin(a) * 140;
            p.Line(250, 190, x, y, 2.5f, H("#5a6a8a"));
            p.RoundRect(x - 12, y, 24, 22, 5, i % 3 == 0 ? H("#e85a5a") : i % 3 == 1 ? H("#f2c12e") : H("#4ab0e0"));
        }
        p.Poly(H("#4a5a7a"), 250, 190, 190, 350, 310, 350);
        // Холмы и деревья
        p.Ellipse(200, 420, 420, 120, H("#6aa85a"));
        p.Ellipse(760, 420, 420, 110, H("#5a9a4e"));
        p.Rect(0, 400, U, 140, H("#5a9a4e"));
        for (int i = 0; i < 6; i++)
        {
            float x = 420 + i * 100 + R(r, -20, 20), y = 330 + R(r, -10, 10);
            p.Rect(x - 6, y, 12, 70, H("#5a4030"));
            p.Circle(x, y - 20, 44, H(i % 2 == 0 ? "#3f7a42" : "#4d8a4f"));
            p.Circle(x - 24, y, 30, H("#3a7040"));
        }
        // Дорожка и фонарь
        p.Poly(H("#d8c8a0"), 380, 540, 520, 400, 580, 400, 620, 540);
        p.Line(660, 300, 660, 460, 4, H("#2a2a30"));
        p.Circle(660, 296, 10, H("#fff3c4"));
        p.RoundRect(700, 440, 120, 14, 4, H("#8a5a32"));      // скамейка
        p.Rect(710, 454, 6, 20, H("#2a2a30"));
        p.Rect(804, 454, 6, 20, H("#2a2a30"));
    }

    // ===== ТОРГОВЫЙ ЦЕНТР =====
    static void Mall(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#f2e8dc"), H("#b8a894"));
        // Стеклянный купол
        p.Poly(H("#cfe4f2"), 0, 0, U, 0, 760, 90, 200, 90);
        for (int i = 0; i < 9; i++) p.Line(i * 120, 0, 200 + i * 70, 90, 2, H("#9ab0c0"));
        // Два этажа витрин
        for (int floor = 0; floor < 2; floor++)
        {
            float y = 110 + floor * 170;
            p.Rect(0, y + 130, U, 14, H("#d8d0c4"));
            for (int i = 0; i < 6; i++)
            {
                float x = 10 + i * 160;
                var c = H(new[] { "#e85a7a", "#4ab0e0", "#f2c12e", "#6ac26a", "#a06ae0", "#f08a3a" }[(i + floor * 2) % 6]);
                p.Rect(x, y, 150, 130, Sh(c, -0.35f));
                p.Rect(x + 8, y + 26, 134, 100, H("#fff6e8", 0.85f));
                p.Rect(x, y, 150, 22, c);
                for (int k = 0; k < 3; k++) p.RoundRect(x + 20 + k * 40, y + 60, 28, 56, 8, Al(c, 0.6f));
                p.Glow(x + 75, y + 70, 90, H("#ffffff", 0.25f));
            }
            p.Rect(0, y + 144, U, 6, H("#9ab0c0", 0.8f)); // стеклянное ограждение
        }
        // Эскалатор
        p.Poly(H("#8a8f96"), 560, 460, 620, 460, 860, 260, 800, 260);
        p.Poly(H("#b8bec4"), 570, 455, 610, 455, 850, 265, 810, 265);
        p.Rect(0, 460, U, 80, H("#d8cfc2"));
        for (float x = 0; x < U; x += 80) p.Line(x, 460, x - 60, 540, 1.5f, H("#c4b8a8"));
    }

    // ===== ДВОР =====
    static void Yard(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, 300, H("#9cc0de"), H("#e9e0cc"));
        // Панельные дома
        for (int b = 0; b < 3; b++)
        {
            float x = -20 + b * 330, top = 40 + b % 2 * 30;
            p.Rect(x, top, 300, 320, H(b % 2 == 0 ? "#c8c0b4" : "#b8b4ac"));
            for (float y = top + 16; y < 330; y += 34)
                for (float wx = x + 14; wx < x + 290; wx += 36)
                {
                    p.Rect(wx, y, 22, 22, r.NextDouble() < 0.25 ? H("#f6e2a0") : H("#6a7e96"));
                    p.Rect(wx - 2, y + 22, 26, 3, H("#9a948a"));
                }
        }
        p.Rect(0, 340, U, 200, H("#7a9a5a"));
        p.Rect(0, 340, U, 20, H("#9a948a"));
        // Футбольная коробка
        p.Rect(80, 380, 360, 120, H("#5a8a4a"));
        p.Rect(80, 380, 360, 120, Al(H("#ffffff"), 0.05f));
        for (float x = 80; x <= 440; x += 20) p.Line(x, 360, x, 500, 1.2f, H("#ddd", 0.5f));
        p.Line(80, 360, 440, 360, 3, H("#3a4a5a"));
        p.Rect(80, 360, 4, 140, H("#3a4a5a"));
        p.Rect(436, 360, 4, 140, H("#3a4a5a"));
        p.Circle(260, 470, 10, H("#f4f4f4"));
        // Качели и горка
        p.Path(5, H("#d9482e"), 560, 500, 600, 380, 640, 500);
        p.Line(600, 380, 700, 380, 5, H("#d9482e"));
        p.Path(5, H("#d9482e"), 660, 500, 700, 380, 740, 500);
        p.Line(640, 380, 640, 450, 2, H("#333"));
        p.Rect(628, 450, 28, 6, H("#f2c12e"));
        p.Poly(H("#3a8ad9"), 780, 400, 800, 400, 900, 500, 870, 500);
        // Скамейка у подъезда
        p.RoundRect(480, 330, 70, 40, 4, H("#5a3a2a"));
        p.RoundRect(470, 316, 90, 16, 4, H("#4a6a8a"));
    }

    // ===== МАШИНА ПАПЫ (вечер) =====
    static void Car(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#0e1a2a"), H("#05080e"));
        // Вид через лобовое стекло
        p.VGrad(60, 40, 840, 280, H("#1a2a48"), H("#3a2a3a"));
        Skyline(p, r, 260, 30, 120, H("#0a0e18"), H("#ffcf6a"), 0.2f, 60, 900);
        p.Poly(H("#22232a"), 300, 320, 660, 320, 520, 230, 440, 230);
        for (int i = 0; i < 4; i++) p.Rect(474 - i * 2, 240 + i * 22, 12 + i * 4, 12, H("#f2f2e0", 0.8f));
        for (int i = 0; i < 6; i++)
        {
            float x = 120 + i * 140;
            p.Glow(x, 120 + R(r, -20, 20), 60, H("#ffb84d", 0.5f));
        }
        p.Glow(520, 250, 60, H("#ff3a3a", 0.5f)); // стоп-сигнал впереди
        // Рама и приборная панель
        p.Poly(H("#121418"), 0, 0, 60, 40, 60, 320, 0, 360);
        p.Poly(H("#121418"), 960, 0, 900, 40, 900, 320, 960, 360);
        p.Rect(0, 0, U, 40, H("#121418"));
        p.Poly(H("#1c1f26"), 0, 320, 960, 320, 960, 540, 0, 540);
        p.RoundRect(80, 340, 300, 90, 30, H("#262a33"));
        p.Ring(170, 385, 34, 4, H("#4dd1ff", 0.7f));
        p.Ring(290, 385, 34, 4, H("#4dd1ff", 0.7f));
        p.Glow(230, 385, 160, H("#4dd1ff", 0.12f));
        // Руль
        p.Ring(230, 470, 120, 18, H("#0c0d10"));
        p.Rect(130, 462, 200, 16, H("#0c0d10"));
        p.RoundRect(600, 360, 120, 70, 8, H("#22262e"));  // магнитола
        p.Rect(612, 372, 96, 30, H("#2a6a8a"));
    }

    // ===== ОСТАНОВКА =====
    static void BusStop(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, 360, H("#3a4a7a"), H("#e8946a"));
        Skyline(p, r, 330, 40, 160, H("#2a2a44"), H("#ffd27a"), 0.2f);
        p.Rect(0, 330, U, 60, H("#8a8a90"));
        p.Rect(0, 390, U, 150, H("#3a3a42"));
        for (float x = 0; x < U; x += 120) p.Rect(x, 460, 60, 8, H("#e8e8e8", 0.7f));
        // Павильон
        p.Rect(80, 120, 420, 16, H("#4a5a6a"));
        p.Rect(90, 136, 8, 200, H("#4a5a6a"));
        p.Rect(482, 136, 8, 200, H("#4a5a6a"));
        p.Rect(98, 136, 384, 170, H("#cfe6f2", 0.25f));
        p.RoundRect(110, 150, 120, 150, 4, H("#f2c12e"));  // реклама
        p.Rect(120, 160, 100, 70, H("#e2572e"));
        p.RoundRect(250, 280, 200, 14, 4, H("#6a4a3a"));    // лавка
        p.Rect(260, 294, 6, 36, H("#333"));
        p.Rect(434, 294, 6, 36, H("#333"));
        // Знак
        p.Line(560, 140, 560, 340, 5, H("#555"));
        p.Circle(560, 130, 26, H("#2b6be0"));
        p.RoundRect(546, 118, 28, 22, 4, H("#ffffff"));
        p.Glow(700, 200, 120, H("#ffd27a", 0.3f));
    }

    // ===== ОТДЕЛЕНИЕ ПОЛИЦИИ =====
    static void Station(Painter p, System.Random r)
    {
        Room(p, H("#9aa6b2"), H("#7a8692"), H("#5a6066"), H("#33373c"), 380);
        p.Rect(0, 260, U, 120, H("#5a6a7a"));
        // Окно с решёткой
        p.Rect(120, 70, 180, 140, H("#2a3442"));
        p.VGrad(130, 80, 160, 120, H("#4a5a74"), H("#6a7a90"));
        for (int i = 0; i < 6; i++) p.Rect(136 + i * 28, 80, 6, 120, H("#2a2e33"));
        p.Rect(130, 136, 160, 6, H("#2a2e33"));
        // Стенд с листовками
        p.RoundRect(360, 70, 200, 150, 4, H("#3a4a6a"));
        for (int i = 0; i < 4; i++) p.Rect(372 + (i % 2) * 94, 82 + (i / 2) * 68, 82, 60, H("#eceae2"));
        p.Rect(380, 90, 40, 6, H("#2a4a8a"));
        // Стол дежурного
        p.Rect(560, 330, 360, 20, H("#6a4a32"));
        p.Rect(570, 350, 340, 100, H("#5a3e2a"));
        p.RoundRect(620, 270, 90, 60, 4, H("#2a2e33"));
        p.Rect(628, 278, 74, 44, H("#3a6a8a"));
        p.Rect(760, 310, 70, 20, H("#eceae2"));
        // Скамья и холодный свет
        p.RoundRect(60, 400, 360, 20, 4, H("#3a3f48"));
        p.Rect(80, 420, 10, 50, H("#2a2e33"));
        p.Rect(390, 420, 10, 50, H("#2a2e33"));
        for (int i = 0; i < 3; i++)
        {
            p.RoundRect(150 + i * 300, 10, 160, 10, 4, H("#f0f8ff"));
            p.Glow(230 + i * 300, 18, 220, H("#e0f0ff", 0.25f));
        }
    }

    // ===== МАГАЗИН ЭЛЕКТРОНИКИ =====
    static void Electronics(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#e8eef4"), H("#a8b4c4"));
        p.Rect(0, 0, U, 50, H("#1f4a8a"));
        p.Rect(0, 50, U, 6, H("#4ab0ff"));
        // Стенды с экранами
        for (int s = 0; s < 3; s++)
        {
            float x = 30 + s * 220;
            p.RoundRect(x, 90, 200, 300, 6, H("#2a3442"));
            for (int lvl = 0; lvl < 3; lvl++)
            {
                float y = 110 + lvl * 92;
                p.Rect(x + 6, y + 70, 188, 6, H("#4a5a6a"));
                for (int k = 0; k < 3; k++)
                {
                    float dx = x + 18 + k * 60;
                    bool phone = lvl == 0;
                    float w = phone ? 26 : 48, hh = phone ? 50 : 34;
                    p.RoundRect(dx, y + 70 - hh, w, hh, 4, H("#111418"));
                    var screen = H(new[] { "#4ab0ff", "#7b5cff", "#4dd17a", "#ff6a8a" }[(k + lvl + s) % 4]);
                    p.RoundRect(dx + 3, y + 73 - hh, w - 6, hh - 8, 2, screen);
                    p.Glow(dx + w / 2, y + 70 - hh / 2, 40, Al(screen, 0.25f));
                    p.Rect(dx, y + 78, 30, 10, H("#f2c12e"));
                }
            }
        }
        // Большой телевизор
        p.RoundRect(700, 80, 230, 140, 6, H("#111418"));
        p.VGrad(708, 88, 214, 124, H("#ff6a3a"), H("#7b5cff"));
        p.Circle(815, 150, 30, H("#ffd23f", 0.8f));
        p.Rect(0, 400, U, 140, H("#f2f4f6"));
        for (float x = 0; x < U; x += 96) p.Line(x, 400, x - 40, 540, 1.5f, H("#d8dde2"));
        p.RoundRect(700, 300, 230, 100, 8, H("#f2f4f6"));  // касса
        p.Rect(700, 300, 230, 12, H("#1f4a8a"));
    }

    // ===== МАГАЗИН ОДЕЖДЫ =====
    static void Clothes(Painter p, System.Random r)
    {
        p.VGrad(0, 0, U, V, H("#f2dcd6"), H("#c49a92"));
        p.Rect(0, 420, U, 120, H("#e8d8c8"));
        // Вешалки с одеждой
        for (int rack = 0; rack < 2; rack++)
        {
            float x0 = 30 + rack * 330, y = 120;
            p.Line(x0, y, x0 + 300, y, 5, H("#8a8a90"));
            p.Line(x0 + 10, y, x0 + 10, 420, 5, H("#8a8a90"));
            p.Line(x0 + 290, y, x0 + 290, 420, 5, H("#8a8a90"));
            for (int i = 0; i < 9; i++)
            {
                float x = x0 + 24 + i * 30;
                var c = H(new[] { "#e85a7a", "#4a7ab0", "#f2c12e", "#6ac26a", "#3a3a44", "#a06ae0", "#f08a3a", "#f4f4f4", "#2f8a8a" }[(i + rack * 4) % 9]);
                p.Path(2, H("#555"), x, y, x, y + 10);
                p.Poly(c, x - 14, y + 12, x + 14, y + 12, x + 18, y + 120 + (i % 3) * 20, x - 18, y + 120 + (i % 3) * 20);
                p.Rect(x - 1, y + 12, 2, 100, Sh(c, -0.25f));
            }
        }
        // Манекен
        p.Circle(780, 130, 22, H("#e8e0d8"));
        p.RoundRect(740, 156, 80, 130, 30, H("#3a6ab0"));
        p.Rect(776, 286, 8, 120, H("#8a8a90"));
        p.Ellipse(780, 410, 40, 8, H("#8a8a90"));
        // Зеркало
        p.RoundRect(870, 80, 70, 300, 30, H("#c9a66b"));
        p.RoundRect(878, 88, 54, 284, 26, H("#dfeef4"));
        p.Line(890, 120, 910, 100, 3, H("#ffffff", 0.8f));
        for (int i = 0; i < 4; i++) p.Glow(150 + i * 230, 30, 160, H("#fff0e8", 0.4f));
    }
}
