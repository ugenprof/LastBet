# Персонажи по CharacterDef из Resources/Data/characters.json (+ варианты героя из heroes.json, прохожие из passers.json).
# Пропорции «чиби»: большая голова, короткое туловище — как у 2D-персонажей игры.
# Лицо смотрит в -Y, ноги на z = 0, рост подростка ~1.4 (взрослые — по полю scale).
#
# Герой собирается из частей (отдельные объекты внутри одной модели, игра включает нужные — Actor3D.SetOutfit):
#   base — голова, шея, кисти; top_default / pants_default / shoes_default — своя одежда;
#   вещи из рюкзака: top_tshirt, top_hoodie, pants_jeans, shoes_sneakers, acc_headphones, acc_watch, acc_smartphone.
# Остальные персонажи — один меш. Прохожие — с реквизитом (поле props в passers.json) и позой под него.
import json, math, os
import bpy
from mathutils import Vector, Matrix
import lb_core as C
from lb_core import M, mat, mix, darken, lighten, INK, WHITE, GOLD

HZ = 1.12                      # центр головы
HR = (0.25, 0.23, 0.24)        # полуоси головы
FEMALE_HAIR = {'bun', 'bob', 'long', 'ponytail', 'scarf', 'granny', 'braid', 'pigtails', 'wavy'}

# Причёски только для 3D (2D-портреты рисуют своё): разнообразие среди второстепенных персонажей
HAIR_3D = {
    'max': 'sidepart', 'denis': 'mohawk', 'kirill': 'curly', 'senior': 'quiff', 'lena': 'braid',
    'dima': 'mullet', 'kolya': 'slick', 'coatman': 'slick', 'doctor': 'sidepart', 'pasha_mom': 'wavy',
    'pasha_dad': 'slick', 'passer_student': 'curly', 'passer_busstop': 'sidepart',
    'passer_business': 'slick', 'passer_gloomy': 'wavy', 'passer_mother': 'pigtails',
}

# Вещи из рюкзака на герое: цвета — как у значков вещей (Art/GameArt.Item)
ITEM_LOOK = {
    'tshirt': {'top': '#e8483f', 'accent': WHITE},
    'hoodie': {'top': '#2f2b3a', 'accent': '#f4f1ea', 'trim': '#9b7bff'},
    'jeans': {'pants': '#3a6ab0'},
}


def head_point(x, z, out=0.0):
    """Точка на передней поверхности головы и нормаль в ней."""
    rx, ry, rz = HR
    t = 1 - (x / rx) ** 2 - ((z - HZ) / rz) ** 2
    y = -ry * math.sqrt(max(0.0, t))
    n = Vector((x / rx ** 2, y / ry ** 2, (z - HZ) / rz ** 2)).normalized()
    return Vector((x, y, z)) + n * out, n


def on_face(m, g, x, z, out, scale, material, roll=0.0, smooth=True):
    p, n = head_point(x, z, out)
    R = C.look_dir(n) @ Matrix.Rotation(math.radians(roll), 4, 'Y')
    S = Matrix.Diagonal(tuple(scale) + (1,))
    m.add(g, Matrix.Translation(p) @ R @ S, material, smooth)


def hair_cap(m, hm, grow=1.06, front=0.10, back=-0.13):
    """Шапка волос: сфера чуть больше головы, срезанная наклонной плоскостью (чёлка выше затылка)."""
    rx, ry, rz = HR
    g = C.g_sphere(40, 24)
    f, b = front / rz, back / rz
    C.cut_plane(g, (0, 0, (f + b) / 2), (0, b - f, -2))
    m.add(g, M((0, 0.005, HZ), scale=(rx * grow, ry * grow, rz * grow)), hm)


def fringe(m, hm, parts):
    for x, r in parts:
        p, n = head_point(x, HZ + 0.135, 0.018)
        m.add(C.g_sphere(16, 10), Matrix.Translation(p) @ C.look_dir(n) @ M(rot=(0, r, 0), scale=(0.075, 0.035, 0.06)), hm)


def side_volume(m, hm, low, grow=1.12):
    rx, ry, rz = HR
    g = C.g_sphere(28, 18)
    C.cut(g, lambda co: co.y > -0.25)
    m.add(g, M((0, 0.03, HZ - 0.02), scale=(rx * grow, ry * 1.08, rz * 1.08)), hm)
    for s in (-1, 1):
        m.limb((s * 0.24, -0.02, HZ - 0.02), (s * 0.235, 0.0, low), 0.055, hm, r2=0.06)


def build_hair(m, d, style):
    hc = d.get('hair', '#3a2a20')
    hm = mat(hc, 0.55)
    rx, ry, rz = HR
    hat = bool(d.get('hat'))
    if style == 'bald':
        # венчик волос на затылке и висках
        g = C.g_sphere(28, 18)
        C.cut(g, lambda co: co.y > -0.35 and -0.35 < co.z < 0.15)
        m.add(g, M((0, 0.01, HZ), scale=(rx * 1.04, ry * 1.04, rz * 1.04)), hm)
        return
    if style == 'scarf':
        sc = d.get('scarf', '#8a4f7d')
        sm = mat(sc, 0.75)
        g = C.g_sphere(48, 32)
        # открытое лицо: срез спереди-снизу
        C.cut_plane(g, (0, -0.42, 0.0), (0, -0.82, -0.57))
        m.add(g, M((0, 0.01, HZ + 0.01), scale=(rx * 1.13, ry * 1.12, rz * 1.13)), sm)
        # узел под подбородком и складка платка сзади
        m.sphere((0, -0.1, HZ - 0.25), (0.06, 0.05, 0.045), sm)
        m.add(C.g_cone(0.09, 0.2, 12), M((0, 0.17, HZ - 0.12), (155, 0, 0), (1, 0.6, 1)), sm)
        m.sphere((0, -0.2, HZ + 0.16), (0.12, 0.04, 0.03), hm)
        return
    if style == 'buzz':
        hair_cap(m, hm, grow=1.025, front=0.12, back=-0.1)
        return
    if style == 'dad':
        hair_cap(m, hm, grow=1.05, front=0.15, back=-0.12)
        return
    if style == 'granny':
        hair_cap(m, hm, grow=1.07, front=0.09, back=-0.1)
        m.sphere((0, 0.06, HZ + 0.27), 0.085, hm)
        m.add(C.g_torus(0.07, 0.015, 24, 8), M((0, 0.06, HZ + 0.22)), mat(darken(hc, 0.2), 0.5))
        # волны седых волос по бокам
        for s in (-1, 1):
            m.sphere((s * 0.21, -0.02, HZ + 0.1), (0.07, 0.09, 0.07), hm)
        return
    if style == 'slick':
        # зачёс назад: гладко, без чёлки, с бороздками от расчёски
        hair_cap(m, hm, grow=1.06, front=0.16, back=-0.13)
        for x in (-0.08, 0.0, 0.08):
            m.limb((x, -0.13, HZ + 0.2), (x * 1.2, 0.12, HZ + 0.22), 0.012, mat(lighten(hc, 0.12), 0.4))
        return
    if style == 'mohawk':
        hair_cap(m, hm, grow=1.02, front=0.13, back=-0.1)
        # полоса прядей от лба к затылку: спереди видна широким гребнем, а не одним шипом
        m.sphere((0, -0.01, HZ + 0.23), (0.07, 0.2, 0.05), hm)
        for i in range(7):
            t = i / 6
            y, z = -0.18 + t * 0.36, HZ + 0.24 - (t - 0.4) ** 2 * 0.25
            for dx in (-0.035, 0.035):
                m.add(C.g_cone(0.04, 0.13 - abs(t - 0.4) * 0.07, 10), M((dx, y, z), (-30 + t * 60, dx * 300, 0)), hm)
        return
    if style == 'afro':
        g = C.g_sphere(32, 20)
        C.cut_plane(g, (0, -0.35, 0), (0, -0.75, -0.66))
        m.add(g, M((0, 0.03, HZ + 0.08), scale=(0.34, 0.31, 0.3)), hm)
        return
    if style == 'curly':
        hair_cap(m, hm, grow=1.05, front=0.1, back=-0.12)
        # шапка из кудряшек
        import random
        rnd = random.Random(len(d.get('hair', '')) + len(d.get('skin', '')))
        for i in range(34):
            a = rnd.uniform(0, math.tau)
            el = rnd.uniform(0.15, 1.35)
            v = Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el)))
            if v.y < -0.45 and v.z < 0.55:
                continue
            p = Vector((v.x * rx * 1.08, v.y * ry * 1.08, v.z * rz * 1.06 + HZ))
            m.sphere(p, 0.05 + rnd.uniform(0, 0.015), hm, seg=12, rings=8)
        return

    hair_cap(m, hm, grow=1.07, front=0.08 if style not in ('neat', 'sidepart', 'quiff') else 0.1, back=-0.14)
    if hat and style in ('messy', 'neat', 'bun', 'quiff', 'pigtails'):
        style = 'short'     # под шапкой не видно ни ирокеза, ни пучка
    if not hat and style in ('short', 'neat', 'messy', 'bob', 'long', 'ponytail', 'bun', 'braid', 'pigtails', 'wavy', 'mullet'):
        fringe(m, hm, [(-0.13, 0.0), (-0.04, 8), (0.06, -5), (0.14, 6)] if style != 'neat' else [(-0.12, 0), (-0.03, 0)])
    if style == 'neat':
        m.sphere((0.07, -0.08, HZ + 0.2), (0.15, 0.13, 0.07), hm, rot=(0, -12, 0))
    if style == 'sidepart':
        # косой пробор: прядь через лоб на одну сторону
        p, n = head_point(0.04, HZ + 0.14, 0.03)
        m.add(C.g_sphere(20, 12), Matrix.Translation(p) @ C.look_dir(n) @ M(rot=(0, 22, 0), scale=(0.17, 0.05, 0.065)), hm)
    if style == 'quiff':
        # кок: объём надо лбом, зачёсанный вверх-назад
        m.sphere((0.0, -0.13, HZ + 0.24), (0.15, 0.11, 0.09), hm, rot=(-25, 0, 0))
        m.sphere((0.0, -0.02, HZ + 0.26), (0.16, 0.13, 0.08), hm)
    if style == 'messy':
        for x, y, tilt in [(-0.12, -0.05, -28), (-0.03, -0.1, -8), (0.07, -0.06, 14),
                           (0.15, 0.02, 30), (-0.05, 0.08, -15), (0.06, 0.1, 18)]:
            m.add(C.g_cone(0.06, 0.15, 10), M((x, y, HZ + 0.2), (tilt * 0.3 - 10, tilt, 0)), hm)
    if style in ('bob', 'long'):
        side_volume(m, hm, HZ - 0.2 if style == 'bob' else HZ - 0.42)
        if style == 'long':
            m.add(C.g_box(0.4, 0.09, 0.4, 0.04), M((0, 0.14, HZ - 0.24)), hm)
    if style == 'wavy':
        # волнистые до плеч: локоны-шарики по бокам и сзади
        side_volume(m, hm, HZ - 0.12, grow=1.14)
        for s in (-1, 1):
            for k in range(4):
                m.sphere((s * (0.25 + (k % 2) * 0.03), 0.02 + k * 0.02, HZ - 0.08 - k * 0.075), 0.06, hm)
        for k in range(5):
            m.sphere(((k - 2) * 0.09, 0.2, HZ - 0.2 - (k % 2) * 0.04), 0.07, hm)
    if style == 'mullet':
        m.add(C.g_box(0.32, 0.1, 0.24, 0.05), M((0, 0.17, HZ - 0.17), (12, 0, 0)), hm)
    if style == 'ponytail':
        # хвост сбоку-сзади — виден и спереди; пряди у лица
        tie = mat('#7d5ce0', 0.5)
        m.add(C.g_torus(0.035, 0.014, 20, 8), M((0.17, 0.17, HZ + 0.13), (90, 0, 40)), tie)
        m.limb((0.19, 0.19, HZ + 0.12), (0.3, 0.16, HZ - 0.05), 0.055, hm, r2=0.045)
        m.limb((0.3, 0.16, HZ - 0.05), (0.29, 0.12, HZ - 0.2), 0.045, hm, r2=0.025)
        for s in (-1, 1):
            m.limb((s * 0.215, -0.1, HZ + 0.06), (s * 0.225, -0.08, HZ - 0.1), 0.028, hm, r2=0.02)
    if style == 'pigtails':
        tie = mat('#e05a8a', 0.5)
        for s in (-1, 1):
            m.add(C.g_torus(0.032, 0.013, 20, 8), M((s * 0.24, 0.05, HZ + 0.08), (0, 90, 0)), tie)
            m.limb((s * 0.27, 0.06, HZ + 0.07), (s * 0.33, 0.06, HZ - 0.12), 0.05, hm, r2=0.03)
    if style == 'braid':
        # коса через плечо: цепочка «звеньев»
        side_volume(m, hm, HZ - 0.12)
        for k in range(6):
            t = k / 5
            m.sphere((0.2 + t * 0.03, -0.08 - t * 0.04, HZ - 0.14 - t * 0.36), (0.05 - t * 0.012, 0.045, 0.055), hm, rot=(0, (k % 2) * 30 - 15, 0))
        m.add(C.g_torus(0.025, 0.01, 16, 6), M((0.23, -0.12, HZ - 0.47)), mat('#e05a8a', 0.5))
    if style == 'bun':
        m.sphere((0, 0.12, HZ + 0.23), 0.085, hm)


def build_hat(m, d):
    hat = d.get('hat')
    if not hat:
        return
    hm = mat(d.get('hatColor', '#333333'), 0.6)
    rx, ry, rz = HR
    if hat == 'cap':
        g = C.g_sphere(28, 16)
        C.cut(g, lambda co: co.z > -0.02)
        m.add(g, M((0, 0.01, HZ + 0.06), scale=(rx * 1.1, ry * 1.12, rz * 0.95)), hm)
        m.add(C.g_rcyl(0.17, 0.025, 0.01, 28), M((0, -0.27, HZ + 0.075), (10, 0, 0), (1, 0.85, 1)), hm)
        m.sphere((0, 0.01, HZ + 0.29), 0.025, mat(darken(d.get('hatColor', '#333'), 0.3)))
    elif hat == 'flatcap':
        m.add(C.g_rcyl(0.27, 0.09, 0.04, 28), M((0, 0.0, HZ + 0.17), (-6, 0, 0), (1, 1.05, 1)), hm)
        m.add(C.g_rcyl(0.13, 0.02, 0.009, 24), M((0, -0.24, HZ + 0.13), (10, 0, 0), (1, 0.6, 1)), hm)
    elif hat == 'police':
        m.add(C.g_rcyl(0.25, 0.14, 0.02, 28, r_top=0.29), M((0, 0, HZ + 0.2)), hm)
        m.add(C.g_rcyl(0.255, 0.04, 0.01, 28), M((0, 0, HZ + 0.13)), mat('#b03030', 0.5))
        m.add(C.g_rcyl(0.15, 0.025, 0.01, 24), M((0, -0.24, HZ + 0.1), (8, 0, 0), (1, 0.6, 1)), mat(INK, 0.3, coat=1))
        m.sphere((0, -0.27, HZ + 0.21), (0.035, 0.012, 0.035), mat(GOLD, 0.3, 0.9))


def build_face(m, d, style):
    skin = d['skin']
    hc = d.get('hair', '#3a2a20')
    ink = mat(INK, 0.25, coat=1)
    white = mat(WHITE, 0.3)
    sm = mat(skin, 0.6)
    old = bool(d.get('wrinkles'))
    for s in (-1, 1):
        m.sphere((s * 0.245, 0.0, HZ - 0.01), (0.04, 0.03, 0.055), sm)
    for s in (-1, 1):
        x = s * 0.088
        # у пожилых глаза поменьше и с веком — сразу видно возраст
        k = 0.82 if old else 1.0
        on_face(m, C.g_sphere(20, 12), x, HZ - 0.01, -0.006, (0.05 * k, 0.02, 0.058 * k), white)
        on_face(m, C.g_sphere(20, 12), x + s * 0.004, HZ - 0.02, 0.006, (0.034 * k, 0.016, 0.042 * k), ink)
        on_face(m, C.g_sphere(12, 8), x + 0.012, HZ + 0.002, 0.018, (0.011, 0.008, 0.011), white)
        if old:
            on_face(m, C.g_sphere(16, 8), x, HZ + 0.025, 0.008, (0.05, 0.012, 0.022), mat(darken(skin, 0.05), 0.6))
            # «гусиные лапки» у глаз
            for dz, roll in ((0.012, 20), (-0.012, -20)):
                on_face(m, C.g_box(1, 1, 1, 0.3), x + s * 0.07, HZ - 0.01 + dz, -0.002, (0.03, 0.01, 0.005),
                        mat(darken(skin, 0.16), 0.7), roll=-s * roll)
        if style in FEMALE_HAIR and not d.get('glasses') and not old:
            for dx, dz, roll in ((0.045, 0.035, 35), (0.05, 0.015, 65)):
                on_face(m, C.g_box(1, 1, 1, 0.3), x + s * dx, HZ - 0.01 + dz, 0.0, (0.022, 0.012, 0.008), ink, roll=-s * roll)
        on_face(m, C.g_box(1, 1, 1, 0.3), x, HZ + 0.075, 0.004, (0.07, 0.022, 0.018), mat(darken(hc, 0.15), 0.6),
                roll=-s * 6)
    on_face(m, C.g_sphere(16, 10), 0.0, HZ - 0.055, 0.0, (0.028, 0.025, 0.026), mat(darken(skin, 0.06), 0.6))
    blush = mat(mix(skin, '#ff7b8a', 0.45), 0.7)
    for s in (-1, 1):
        on_face(m, C.g_sphere(16, 8), s * 0.15, HZ - 0.07, -0.004, (0.038, 0.01, 0.024), blush)
    mouth = mat('#7a2a3a', 0.4)
    p, n = head_point(0.0, HZ - 0.12, 0.004)
    m.add(C.g_torus(0.042, 0.009, 16, 8, arc=math.pi * 0.7, start=math.pi * 1.15),
          Matrix.Translation(p) @ C.look_dir(n) @ M(rot=(90, 0, 0), loc=(0, 0, 0.03)), mouth)
    if old:
        wm = mat(darken(skin, 0.14), 0.7)
        for k, z in enumerate((HZ + 0.115, HZ + 0.14)):
            on_face(m, C.g_box(1, 1, 1, 0.3), 0, z, -0.002, (0.1 - k * 0.02, 0.012, 0.006), wm)
        # носогубные складки
        for s in (-1, 1):
            on_face(m, C.g_box(1, 1, 1, 0.3), s * 0.06, HZ - 0.1, -0.002, (0.008, 0.01, 0.045), wm, roll=s * 20)
    if d.get('stubble'):
        g = C.g_sphere(40, 24)
        C.cut_plane(g, (0, 0, -0.25), (0, 0, 1))
        C.cut_plane(g, (0, 0.05, 0), (0, 1, 0))
        m.add(g, M((0, 0, HZ), scale=(HR[0] * 1.01, HR[1] * 1.012, HR[2] * 1.01)), mat(mix(skin, hc, 0.35), 0.8))
    if d.get('beard'):
        g = C.g_sphere(40, 24)
        C.cut_plane(g, (0, 0, -0.1), (0, -0.45, 1))
        C.cut_plane(g, (0, 0.05, 0), (0, 1, 0))
        m.add(g, M((0, -0.01, HZ - 0.01), scale=(HR[0] * 1.05, HR[1] * 1.09, HR[2] * 1.1)), mat(hc, 0.7))
        p, n = head_point(0.0, HZ - 0.12, 0.03)
        m.add(C.g_torus(0.035, 0.009, 16, 8, arc=math.pi * 0.7, start=math.pi * 1.15),
              Matrix.Translation(p) @ C.look_dir(n) @ M(rot=(90, 0, 0), loc=(0, 0, 0.025)), mouth)
    if d.get('mustache'):
        for s in (-1, 1):
            on_face(m, C.g_sphere(16, 10), s * 0.035, HZ - 0.085, 0.012, (0.045, 0.02, 0.018), mat(darken(hc, 0.1), 0.6),
                    roll=s * 14)
    if d.get('glasses'):
        fr = mat(INK, 0.25, 0.3, coat=1)
        for s in (-1, 1):
            p, n = head_point(s * 0.088, HZ - 0.01, 0.028)
            m.add(C.g_torus(0.056, 0.008, 28, 8), Matrix.Translation(p) @ C.look_dir(n) @ M(rot=(90, 0, 0)), fr)
            m.limb((s * 0.145, -0.17, HZ + 0.005), (s * 0.24, 0.02, HZ + 0.01), 0.007, fr)
        m.limb((-0.035, -0.247, HZ), (0.035, -0.247, HZ), 0.008, fr)
    if d.get('earrings'):
        for s in (-1, 1):
            m.sphere((s * 0.25, -0.01, HZ - 0.075), 0.016, mat(GOLD, 0.25, 1.0))


# ===================== ТУЛОВИЩЕ И ОДЕЖДА =====================

def torso_r(z):
    """Радиус туловища на высоте z (сужается к плечам)."""
    t = max(0.0, min(1.0, (z - 0.44) / 0.38))
    return 0.205 - 0.033 * t


def torso_front(z):
    return -torso_r(z) * 0.78


def torso_profile(grow=0.0, low=None):
    """Профиль туловища для g_lathe: частые кольца, чтобы оболочки резались аккуратно."""
    pts = []
    if low is not None:
        pts.append((0.205 + grow, low))
        z = low + 0.01
    else:
        pts += [(0.0, 0.41), (0.17 + grow, 0.41), (0.195 + grow, 0.418)]
        z = 0.44
    while z <= 0.82:
        pts.append((torso_r(z) + grow, z))
        z += 0.008
    pts += [(0.168 + grow, 0.845), (0.15 + grow, 0.862), (0.11 + grow, 0.872), (0.0, 0.876)]
    return pts


def poly_planes(poly):
    """Плоскости-стенки выпуклого многоугольника [(x, z)] против часовой стрелки (вид спереди)."""
    out = []
    for i, (x1, z1) in enumerate(poly):
        x2, z2 = poly[(i + 1) % len(poly)]
        out.append(((x1, 0, z1), (z2 - z1, 0, -(x2 - x1))))
    return out


def rect(x0, z0, x1, z1):
    return [(x0, z0), (x1, z0), (x1, z1), (x0, z1)]


def shell(m, material, poly=None, z=None, x=None, front=True, grow=0.008, low=None):
    """Деталь одежды, повторяющая форму туловища, с ровными краями: срез плоскостями.
    poly — выпуклый контур спереди; z / x — пояс (z0, z1) / полоса по x (x0, x1); front — только спереди."""
    g = C.g_lathe(torso_profile(grow, low), seg=64)
    planes = poly_planes(poly) if poly else []
    if z:
        planes += [((0, 0, z[0]), (0, 0, -1)), ((0, 0, z[1]), (0, 0, 1))]
    if x:
        planes += [((x[0], 0, 0), (-1, 0, 0)), ((x[1], 0, 0), (1, 0, 0))]
    if front:
        planes.append(((0, 0, 0), (0, 1, 0)))
    for co, no in planes:
        C.cut_plane(g, co, no)
    m.add(g, M((0, 0.01, 0), scale=(1, 0.78, 1)), material)


def circle(cx, cz, r, n=24):
    return [(cx + r * math.cos(math.tau * i / n), cz + r * math.sin(math.tau * i / n)) for i in range(n)]


class Parts:
    """Куда класть детали: у героя — отдельные модели по частям, у остальных — всё в одну."""

    def __init__(self, base, top=None, pants=None, shoes=None):
        self.base = base
        self.top = top or base
        self.pants = pants or base
        self.shoes = shoes or base


# Позы: куда тянутся руки (плечо, локоть, кисть) и ноги (бедро, колено, стопа). s = -1 / +1 — сторона
def arm_pose(pose, s):
    sh = Vector((s * 0.205, 0.01, 0.79))
    if pose in ('handlebar', 'stroller'):
        hand = Vector((s * 0.16, -0.36 if pose == 'handlebar' else -0.3, 0.64 if pose == 'handlebar' else 0.6))
        return sh, (sh + hand) / 2 + Vector((s * 0.05, 0, -0.04)), hand
    if pose == 'run':
        fwd = s > 0
        el = Vector((s * 0.25, -0.08 if fwd else 0.1, 0.62))
        hand = Vector((s * 0.21, -0.2 if fwd else 0.16, 0.7 if fwd else 0.56))
        return sh, el, hand
    if pose == 'phone' and s > 0:
        return sh, Vector((s * 0.33, -0.02, 0.8)), Vector((s * 0.235, -0.06, 1.06))
    if pose == 'cane' and s > 0:
        return sh, Vector((s * 0.27, -0.04, 0.62)), Vector((s * 0.28, -0.12, 0.5))
    return sh, Vector((s * 0.235, 0.005, 0.66)), Vector((s * 0.255, 0.0, 0.53))


def leg_pose(pose, s):
    hip = Vector((s * 0.085, 0, 0.42))
    if pose == 'ride':
        return hip, Vector((s * 0.1, -0.2, 0.42)), Vector((s * 0.1, -0.14, 0.08 if s > 0 else 0.16))
    if pose == 'run':
        fwd = s < 0
        return hip, Vector((s * 0.09, -0.12 if fwd else 0.08, 0.28)), Vector((s * 0.085, -0.1 if fwd else 0.16, 0.09 if fwd else 0.16))
    return hip, (hip + Vector((s * 0.085, 0, 0.09))) / 2, Vector((s * 0.085, 0, 0.09))


def build_legs(P, d, pose, pants_col, skin, shorts=False, skirt=False, stripe=None):
    legm = mat('#4a4052', 0.8) if skirt else mat(pants_col, 0.8)
    sm = mat(skin, 0.6)
    for s in (-1, 1):
        hip, knee, foot = leg_pose(pose, s)
        if shorts:
            P.pants.limb(hip, knee, 0.074, legm, r2=0.07)
            P.pants.limb(knee, foot, 0.05, sm)
        else:
            P.pants.limb(hip, knee, 0.068, legm)
            P.pants.limb(knee, foot, 0.068, legm)
        if stripe:
            P.pants.limb(hip + Vector((s * 0.066, 0, -0.02)), knee + Vector((s * 0.066, 0, 0)), 0.011, stripe)
    if skirt:
        P.pants.add(C.g_rcyl(0.24, 0.3, 0.03, 32, r_top=0.19), M((0, 0.01, 0.33), scale=(1, 0.82, 1)), mat('#3d3a4f', 0.8))
    else:
        P.pants.add(C.g_rcyl(0.214, 0.12, 0.04, 32), M((0, 0.01, 0.43), scale=(1, 0.8, 1)), legm)


def build_shoes(P, pose, sneakers, accent, sole=None, swoosh=None):
    shoe = mat(WHITE if sneakers else '#2a2730', 0.5 if sneakers else 0.35, coat=0 if sneakers else 0.6)
    for s in (-1, 1):
        _, _, foot = leg_pose(pose, s)
        at = foot + Vector((0, -0.03, -0.045))
        P.shoes.add(C.g_box(0.12, 0.2, 0.085, 0.035), M(at + Vector((0, 0, 0.0)), scale=1), shoe)
        if sneakers:
            P.shoes.add(C.g_box(0.124, 0.205, 0.025, 0.01), M(at + Vector((0, 0, -0.033))), mat(sole or '#c9c3d6', 0.5, coat=0.3 if sole else 0))
            P.shoes.add(C.g_box(0.06, 0.03, 0.03, 0.01), M(at + Vector((0, -0.1, 0.03))), mat(accent, 0.6))
            if swoosh:
                P.shoes.add(C.g_box(0.006, 0.12, 0.02, 0.003), M(at + Vector((s * 0.062, 0.01, 0.0)), (0, 0, 0), 1), mat(swoosh, 0.5))


def build_arms(P, d, pose, style, top, acc, skin):
    tm = mat(top, 0.75)
    am = mat(acc, 0.65)
    sm = mat(skin, 0.6)
    short = style == 'tshirt'
    cuff = am if style in ('coat', 'tracksuit', 'uniform', 'jacket') else mat(darken(top, 0.18), 0.75)
    for s in (-1, 1):
        sh, el, hand = arm_pose(pose, s)
        P.top.sphere((s * 0.18, 0.01, 0.8), (0.085, 0.08, 0.075), tm)
        if short:
            P.top.limb(sh, sh + (el - sh) * 0.7, 0.066, tm)
            P.top.limb(sh + (el - sh) * 0.7, el, 0.048, sm)
            P.top.limb(el, hand, 0.048, sm)
        else:
            P.top.limb(sh, el, 0.058, tm, r2=0.056)
            P.top.limb(el, hand + (el - hand).normalized() * 0.03, 0.056, tm, r2=0.054)
            d_ = (hand - el).normalized()
            cuff_at = hand - d_ * 0.03
            rot = d_.to_track_quat('Z', 'Y').to_matrix().to_4x4()
            P.top.add(C.g_rcyl(0.064, 0.035, 0.012, 18), Matrix.Translation(cuff_at) @ rot, cuff)
        if style == 'tracksuit':
            P.top.limb(sh + Vector((s * 0.045, 0.02, 0)), el + Vector((s * 0.05, 0, 0)), 0.01, am)
        P.base.sphere(hand + Vector((0, -0.005, -0.03)), 0.05, sm)


def build_top(P, d, style, top, acc, skin):
    """Туловище и всё, что на нём (без рук)."""
    m = P.top
    tm = mat(top, 0.75)
    am = mat(acc, 0.65)
    dark_top = mat(darken(top, 0.12), 0.75)
    fy = torso_front
    m.add(C.g_lathe(torso_profile(), seg=48), M((0, 0.01, 0), scale=(1, 0.78, 1)), tm)

    if style in ('hoodie', 'hoodie_item'):
        m.add(C.g_torus(0.11, 0.04, 28, 10), M((0, 0.03, 0.855), (-10, 0, 0), (1.15, 0.95, 1)),
              mat(d.get('trim', top), 0.6) if style == 'hoodie_item' else dark_top)
        for s in (-1, 1):
            m.limb((s * 0.035, fy(0.83) - 0.02, 0.83), (s * 0.04, fy(0.66) - 0.014, 0.66), 0.007, am)
            m.sphere((s * 0.04, fy(0.655) - 0.014, 0.655), 0.013, am)
        shell(m, mat(darken(top, 0.08), 0.75), [(-0.1, 0.455), (0.1, 0.455), (0.075, 0.56), (-0.075, 0.56)], grow=0.014)
        if style == 'hoodie_item':
            # новое худи из магазина: графит, фиолетовая подкладка капюшона, кант и полоса на груди
            trim = mat(d.get('trim', top), 0.55)
            shell(m, trim, z=(0.43, 0.455), front=False, grow=0.01)
            shell(m, trim, z=(0.69, 0.72), front=False, grow=0.008)
            shell(m, mat(WHITE, 0.5), z=(0.72, 0.728), front=False, grow=0.009)
    elif style == 'sweater':
        m.add(C.g_torus(0.07, 0.022, 24, 8), M((0, 0.0, 0.855), scale=(1, 0.95, 1)), dark_top)
        stripe = mat(lighten(top, 0.3), 0.75)
        shell(m, stripe, z=(0.6, 0.64), front=False, grow=0.006)
        shell(m, stripe, z=(0.565, 0.58), front=False, grow=0.006)
    elif style in ('cardigan', 'jacket', 'coat'):
        lo = 0.62 if style != 'coat' else 0.66

        def vw(z):
            return (z - lo) / (0.87 - lo) * 0.085
        w = vw(0.9)
        shell(m, am, [(0, lo), (w, 0.9), (-w, 0.9)], grow=0.004)
        if style == 'jacket' and not d.get('noTie'):
            tie = mat(d.get('tie', '#8a2b2b'), 0.45)
            shell(m, tie, [(0, 0.6), (0.032, 0.66), (0.018, 0.8), (-0.018, 0.8), (-0.032, 0.66)], grow=0.012)
            shell(m, tie, rect(-0.024, 0.795, 0.024, 0.84), grow=0.016)
        edge = mat(darken(top, 0.18), 0.7)
        for s in (-1, 1):
            q = [(0, lo), (s * 0.016, lo), (s * (w + 0.016), 0.9), (s * w, 0.9)]
            shell(m, edge, q if s > 0 else q[::-1], grow=0.01)
        shell(m, edge, rect(-0.007, 0.39, 0.007, lo + 0.004), grow=0.01, low=0.4)
        for z in (0.56, 0.48):
            m.sphere((0.022, fy(z) - 0.016, z), (0.012, 0.007, 0.012),
                     mat(GOLD, 0.35, 0.6) if style == 'cardigan' else mat(INK, 0.35, coat=0.5))
        if style == 'coat':
            for xr in ((0.06, 0.15), (-0.15, -0.06)):
                shell(m, mat(darken(top, 0.06), 0.7), rect(xr[0], 0.46, xr[1], 0.53), grow=0.012)
            if d.get('stethoscope'):
                st = mat('#4a4f5c', 0.3, coat=1)
                m.add(C.g_torus(0.1, 0.01, 28, 8, arc=math.pi * 1.1, start=math.pi * 0.95),
                      M((0, fy(0.78) - 0.02, 0.8), (90, 0, 0)), st)
                m.limb((0.1, fy(0.72) - 0.02, 0.78), (0.09, fy(0.64) - 0.02, 0.64), 0.008, st)
                m.sphere((0.09, fy(0.62) - 0.02, 0.62), (0.025, 0.012, 0.025), mat('#c9ced8', 0.2, 1.0))
    elif style == 'tshirt':
        m.add(C.g_torus(0.065, 0.015, 24, 8), M((0, 0.0, 0.85)), mat(darken(top, 0.15), 0.75))
        shell(m, mat(acc, 0.55), circle(0, 0.66, 0.06), grow=0.006)
        if d.get('print', 'plus') == 'plus':
            shell(m, mat(WHITE, 0.5), rect(-0.009, 0.624, 0.009, 0.696), grow=0.012)
            shell(m, mat(WHITE, 0.5), rect(-0.036, 0.651, 0.036, 0.669), grow=0.012)
    elif style in ('shirt', 'blouse'):
        col = mat(acc, 0.6)
        for s in (-1, 1):
            m.add(C.g_extrude([(0, 0.0), (s * 0.075, 0.03), (s * 0.06, -0.06)], 0.012),
                  M((s * 0.012, fy(0.84) - 0.012, 0.84), (-25, 0, 0)), col)
        if style == 'shirt':
            shell(m, mat(darken(top, 0.1), 0.7), rect(-0.014, 0.4, 0.014, 0.9), grow=0.004)
            for z in (0.74, 0.64, 0.54):
                m.sphere((0, fy(z) - 0.012, z), (0.01, 0.006, 0.01), col)
            shell(m, mat(darken(top, 0.06), 0.7), rect(0.06, 0.66, 0.13, 0.73), grow=0.008)
        else:
            m.sphere((0, fy(0.8) - 0.016, 0.8), (0.02, 0.01, 0.02), mat(GOLD, 0.25, 1.0))
    elif style == 'apron':
        ap = mat(acc, 0.75)
        shell(m, ap, rect(-0.14, 0.32, 0.14, 0.625), grow=0.014, low=0.3)
        shell(m, ap, rect(-0.085, 0.6, 0.085, 0.8), grow=0.014)
        shell(m, mat(darken(acc, 0.1), 0.75), rect(-0.08, 0.44, 0.08, 0.52), grow=0.022, low=0.3)
        for s in (-1, 1):
            m.limb((s * 0.08, fy(0.8) - 0.012, 0.8), (s * 0.06, 0.0, 0.88), 0.008, ap)
        m.add(C.g_torus(0.07, 0.02, 24, 8), M((0, 0.0, 0.85)), dark_top)
    elif style == 'vest':
        vest = mat('#f39a1e', 0.55)
        refl = mat('#e9edf2', 0.25, 0.5)
        for xr in ((0.035, 1), (-1, -0.035)):
            shell(m, vest, z=(0.3, 0.85), x=xr, front=False, grow=0.014)
            for zr in ((0.49, 0.515), (0.57, 0.595)):
                shell(m, refl, z=zr, x=xr, front=False, grow=0.02)
    elif style == 'uniform':
        ep = mat(acc, 0.5)
        for s in (-1, 1):
            m.add(C.g_box(0.1, 0.05, 0.02, 0.008), M((s * 0.19, 0.01, 0.865), (0, s * 12, 0)), ep)
        for xr in ((0.05, 0.13), (-0.13, -0.05)):
            shell(m, mat(darken(top, 0.15), 0.7), rect(xr[0], 0.64, xr[1], 0.71), grow=0.008)
        shell(m, mat(darken(top, 0.2), 0.7), rect(-0.006, 0.4, 0.006, 0.9), grow=0.006)
        m.sphere((0.09, fy(0.76) - 0.012, 0.76), (0.022, 0.01, 0.022), mat(GOLD, 0.25, 1.0))
        m.add(C.g_torus(0.07, 0.022, 24, 8), M((0, 0.0, 0.855)), mat(darken(top, 0.25), 0.7))
        shell(m, mat(INK, 0.35, coat=1), z=(0.44, 0.48), front=False, grow=0.01)
        m.add(C.g_box(0.05, 0.02, 0.04, 0.006), M((0, fy(0.46) - 0.014, 0.46)), mat('#c0c4cc', 0.25, 1.0))
    elif style == 'tracksuit':
        shell(m, am, rect(-0.005, 0.4, 0.005, 0.9), grow=0.004)
        m.add(C.g_torus(0.075, 0.024, 24, 8), M((0, 0.0, 0.855)), tm)
        m.sphere((0, fy(0.83) - 0.012, 0.83), (0.012, 0.008, 0.018), mat('#c0c4cc', 0.2, 1.0))
    elif style == 'overalls':
        bib = mat(acc, 0.8)
        shell(m, bib, z=(0.39, 0.475), front=False, grow=0.012, low=0.38)
        shell(m, bib, rect(-0.1, 0.46, 0.1, 0.65), grow=0.012)
        shell(m, mat(darken(acc, 0.15), 0.8), rect(-0.045, 0.53, 0.045, 0.6), grow=0.02)
        for s in (-1, 1):
            m.limb((s * 0.08, fy(0.64) - 0.012, 0.64), (s * 0.11, -0.04, 0.86), 0.014, bib)
            m.sphere((s * 0.08, fy(0.635) - 0.024, 0.635), 0.016, mat(GOLD, 0.3, 1.0))
        m.add(C.g_torus(0.065, 0.016, 24, 8), M((0, 0.0, 0.85)), mat(darken(top, 0.15), 0.75))


def outfit_colors(d):
    style = d.get('topStyle', 'hoodie')
    female = d.get('hairStyle') in FEMALE_HAIR
    adult = d.get('scale', 1.0) >= 1.08 or bool(d.get('wrinkles'))
    if style == 'overalls':
        pants = d.get('accent', WHITE)
    elif style == 'tracksuit':
        pants = d['top']
    elif adult:
        pants = '#3a3d48' if style not in ('jacket', 'uniform') else darken(d['top'], 0.15)
    else:
        pants = '#3b4a6e'
    skirt = female and adult and style in ('cardigan', 'blouse', 'apron')
    return pants, skirt, not adult


def build_body(P, d, pose):
    style = d.get('topStyle', 'hoodie')
    top, acc, skin = d['top'], d.get('accent', WHITE), d['skin']
    pants, skirt, sneakers = outfit_colors(d)
    props = d.get('props', [])
    build_shoes(P, pose, sneakers, top)
    build_legs(P, d, pose, pants, skin, shorts='shorts' in props, skirt=skirt,
               stripe=mat(acc, 0.65) if style == 'tracksuit' else None)
    build_top(P, d, style, top, acc, skin)
    build_arms(P, d, pose, style, top, acc, skin)
    P.base.add(C.g_cyl(0.055, 0.055, 0.12, 16), M((0, 0.01, 0.89)), mat(skin, 0.6))


def build_head(m, d, key):
    style = HAIR_3D.get(key, d.get('hairStyle', 'short'))
    m.sphere((0, 0, HZ), HR, mat(d['skin'], 0.6), seg=40, rings=24)
    build_face(m, d, style)
    build_hair(m, d, style)
    build_hat(m, d)


# ===================== РЕКВИЗИТ ПРОХОЖИХ =====================

def pose_for(props):
    if 'bike' in props:
        return 'ride'
    if 'scooter' in props:
        return 'handlebar'
    if 'stroller' in props:
        return 'stroller'
    if 'run' in props:
        return 'run'
    if 'phone_ear' in props:
        return 'phone'
    if 'cane' in props:
        return 'cane'
    return 'stand'


def headphones(m, cups='#d6264f', band='#2a2a33'):
    bm = mat(band, 0.4, coat=0.6)
    m.add(C.g_torus(0.275, 0.016, 40, 10, arc=math.pi, start=0), M((0, 0.0, HZ + 0.02), (90, 0, 0), (1, 1, 1.05)), bm)
    for s in (-1, 1):
        m.add(C.g_rcyl(0.07, 0.06, 0.02, 28), M((s * 0.275, 0.0, HZ - 0.01), (0, 90, 0), (1, 1.2, 1)), mat(cups, 0.3, coat=0.8))
        m.add(C.g_rcyl(0.058, 0.03, 0.012, 24), M((s * 0.255, 0.0, HZ - 0.01), (0, 90, 0), (1, 1.2, 1)), mat('#2a2a33', 0.9))


def phone_mesh(m, at, rot, screen='#4ab0ff'):
    R = M(at, rot)
    m.add(C.g_box(0.06, 0.012, 0.12, 0.008), R, mat('#1a1a22', 0.2, 0.3, coat=1))
    m.add(C.g_box(0.052, 0.004, 0.108, 0.006), R @ M((0, -0.006, 0)), mat(screen, 0.15, emit=2.5))


VEHICLES = ('bike', 'scooter', 'stroller')


def build_props(m, d, props, vehicles):
    """vehicles=False — то, что на человеке (поднимается вместе с ним); True — транспорт на земле."""
    props = [p for p in props if (p in VEHICLES) == vehicles]
    _, _, hand_r = arm_pose(pose_for(d.get('props', [])), 1)
    _, _, hand_l = arm_pose(pose_for(d.get('props', [])), -1)
    metal = mat('#c9ced8', 0.25, 1.0)
    dark = mat('#2a2a33', 0.5)
    if 'headphones' in props:
        headphones(m, '#4ab0ff', '#1d1a2b')
    if 'helmet' in props:
        hm = mat(d.get('hatColor', '#2a9ab0'), 0.3, coat=0.8)
        g = C.g_sphere(32, 18)
        C.cut_plane(g, (0, 0, -0.05), (0, 0.25, -1))
        m.add(g, M((0, 0.02, HZ + 0.06), scale=(0.29, 0.29, 0.25)), hm)
        for k in (-1, 0, 1):
            m.limb((k * 0.08, -0.2, HZ + 0.22), (k * 0.09, 0.22, HZ + 0.2), 0.012, mat(WHITE, 0.4))
    if 'cane' in props:
        wood = mat('#6b4423', 0.5, coat=0.3)
        m.limb(hand_r + Vector((0.0, -0.02, -0.02)), Vector((hand_r.x + 0.02, hand_r.y - 0.06, 0.0)), 0.014, wood)
        m.add(C.g_torus(0.035, 0.013, 16, 8, arc=math.pi, start=0), M(hand_r + Vector((-0.03, -0.02, 0.0)), (90, 0, 0)), wood)
    if 'avoska' in props:
        # авоська: сетка-мешок с апельсинами и батоном
        net = mat('#e8c33a', 0.6)
        c = hand_l + Vector((0, 0, -0.17))
        g = C.g_sphere(14, 10)
        m.add(g, M(c, scale=(0.1, 0.07, 0.12)), mat('#e8c33a', 0.7))
        for k, (dx, dz) in enumerate(((-0.04, -0.03), (0.04, -0.04), (0.0, 0.03))):
            m.sphere(c + Vector((dx, -0.055, dz)), 0.042, mat('#f08a24', 0.5))
        m.limb(c + Vector((-0.02, 0.0, 0.1)), c + Vector((-0.07, 0.0, 0.2)), 0.02, mat('#d9a35a', 0.6))
        for s in (-1, 1):
            m.limb(c + Vector((s * 0.06, 0, 0.09)), hand_l, 0.006, net)
    if 'bags' in props:
        for s, h in ((1, hand_r), (-1, hand_l)):
            c = h + Vector((s * 0.04, 0, -0.17))
            m.add(C.g_box(0.16, 0.08, 0.22, 0.03), M(c), mat(WHITE if s > 0 else '#f2c12e', 0.7))
            m.sphere(c + Vector((0, -0.01, 0.13)), (0.04, 0.03, 0.03), mat('#6aa84f', 0.6))   # зелень из пакета
            m.limb(c + Vector((-0.05, 0, 0.1)), h, 0.007, mat(WHITE, 0.6))
            m.limb(c + Vector((0.05, 0, 0.1)), h, 0.007, mat(WHITE, 0.6))
    if 'briefcase' in props:
        c = hand_l + Vector((0, 0, -0.14))
        m.add(C.g_box(0.08, 0.26, 0.19, 0.02), M(c), mat('#4a3020', 0.4, coat=0.6))
        m.add(C.g_torus(0.035, 0.008, 16, 6, arc=math.pi, start=0), M(c + Vector((0, 0, 0.1)), (90, 0, 90)), dark)
        m.add(C.g_box(0.085, 0.03, 0.02, 0.005), M(c + Vector((0, -0.03, 0.05))), mat(GOLD, 0.3, 1.0))
    if 'phone_ear' in props:
        phone_mesh(m, hand_r + Vector((-0.02, -0.02, 0.03)), (0, -80, 0), '#1e3a5a')
    if 'backpack' in props:
        bp = mat('#2a9d8f', 0.75)
        m.add(C.g_box(0.3, 0.16, 0.34, 0.07), M((0, 0.23, 0.6)), bp)
        m.add(C.g_box(0.22, 0.06, 0.14, 0.04), M((0, 0.32, 0.53)), mat(darken('#2a9d8f', 0.15), 0.75))
        for s in (-1, 1):
            m.limb((s * 0.1, 0.12, 0.8), (s * 0.12, -0.13, 0.78), 0.016, mat('#1d2b3a', 0.7))
    if 'stroller' in props:
        # коляска: люлька с капюшоном на колёсах, ручка у рук мамы
        body_c = Vector((0, -0.62, 0.5))
        m.add(C.g_rcyl(0.2, 0.24, 0.08, 28), M(body_c, (90, 0, 0), (1, 0.9, 1.0)), mat('#7aa6c8', 0.7))
        g = C.g_sphere(24, 14)
        C.cut_plane(g, (0, 0, 0), (0, 0, -1))
        C.cut_plane(g, (0, 0, 0), (0, -1, 0))
        m.add(g, M(body_c + Vector((0, 0.05, 0.05)), (0, 0, 0), (0.2, 0.2, 0.22)), mat('#5b8bb0', 0.7))
        m.sphere(body_c + Vector((0, -0.04, 0.12)), 0.06, mat('#f2c9a0', 0.6))     # малыш
        m.sphere(body_c + Vector((0, -0.06, 0.17)), (0.065, 0.06, 0.04), mat('#f4b6c8', 0.7))  # шапочка
        for s in (-1, 1):
            m.limb(body_c + Vector((s * 0.16, 0.12, 0.0)), Vector((s * 0.16, -0.32, 0.6)), 0.012, metal)
            for y in (-0.1, 0.12):
                m.limb(body_c + Vector((s * 0.14, y, -0.08)), Vector((s * 0.17, -0.62 + y * 1.3, 0.1)), 0.01, metal)
                m.add(C.g_torus(0.08, 0.022, 24, 8), M((s * 0.19, -0.62 + y * 1.3, 0.09), (0, 90, 0)), dark)
        m.limb((-0.17, -0.32, 0.6), (0.17, -0.32, 0.6), 0.016, dark)
    if 'scooter' in props:
        deck_z = 0.03
        m.add(C.g_box(0.13, 0.55, 0.035, 0.015), M((0, 0.0, deck_z)), mat('#2a2a33', 0.5, coat=0.5))
        m.add(C.g_box(0.12, 0.5, 0.008, 0.003), M((0, 0.0, deck_z + 0.02)), mat('#d6264f', 0.5))
        for y in (-0.3, 0.3):
            m.add(C.g_rcyl(0.045, 0.035, 0.012, 20), M((0, y, 0.045), (0, 90, 0)), mat('#ffd23f', 0.4))
        m.limb((0, -0.3, 0.06), (0, -0.36, 0.7), 0.016, metal)
        m.limb((-0.19, -0.36, 0.7), (0.19, -0.36, 0.7), 0.014, dark)
    if 'bike' in props:
        frame = mat(d.get('top', '#f2c12e'), 0.35, 0.3, coat=0.6)
        tire = mat('#1d1a22', 0.7)
        R, wy, wz = 0.24, 0.48, 0.24
        for y in (-wy, wy):
            m.add(C.g_torus(R, 0.03, 40, 10), M((0, y, wz), (0, 90, 0)), tire)
            m.add(C.g_torus(R - 0.03, 0.006, 32, 6), M((0, y, wz), (0, 90, 0)), metal)
            for k in range(6):
                a = k * math.pi / 3
                m.limb((0, y, wz), (0, y + math.cos(a) * (R - 0.03), wz + math.sin(a) * (R - 0.03)), 0.004, metal)
            m.sphere((0, y, wz), 0.025, metal)
        crank = Vector((0, 0.0, 0.2))
        seat = Vector((0, 0.08, 0.5))
        head = Vector((0, -0.3, 0.68))
        for a, b in ((Vector((0, wy, wz)), crank), (crank, seat), (seat, Vector((0, wy, wz))),
                     (crank, head), (seat + Vector((0, -0.02, -0.06)), head), (head, Vector((0, -wy, wz)))):
            m.limb(a, b, 0.018, frame)
        m.add(C.g_box(0.1, 0.18, 0.04, 0.02), M(seat + Vector((0, 0.02, 0.04))), dark)
        m.limb(head, Vector((0, -0.3, 0.78)), 0.014, metal)
        m.limb((-0.2, -0.3, 0.78), (0.2, -0.3, 0.78), 0.014, dark)
        m.sphere(crank, 0.04, metal)
        for s in (-1, 1):
            m.add(C.g_box(0.07, 0.04, 0.012, 0.004), M(crank + Vector((s * 0.1, -0.06 * s, -0.04 * s))), dark)


# ===================== ВЕЩИ ИЗ РЮКЗАКА НА ГЕРОЕ =====================

def build_item_parts(key, d, coll):
    """Отдельные объекты-части: игра включает их, когда вещь надета."""
    parts = {}
    skin = d['skin']

    def new(name):
        parts[name] = C.Model('%s__%s' % (key, name))
        return parts[name]

    # футболка: красная с белым кругом — как значок вещи
    t = dict(d, **ITEM_LOOK['tshirt'], print='circle')
    P = Parts(new('top_tshirt'))
    build_top(P, t, 'tshirt', t['top'], t['accent'], skin)
    build_arms(P, t, 'stand', 'tshirt', t['top'], t['accent'], skin)
    # кисти уже в base — у футболки рук до локтя хватает
    h = dict(d, **ITEM_LOOK['hoodie'])
    P = Parts(new('top_hoodie'))
    build_top(P, h, 'hoodie_item', h['top'], h['accent'], skin)
    build_arms(P, h, 'stand', 'hoodie', h['top'], h['accent'], skin)
    # шорты: джинсовые, с жёлтой строчкой
    P = Parts(new('pants_jeans'))
    build_legs(P, d, 'stand', ITEM_LOOK['jeans']['pants'], skin, shorts=True)
    stitch = mat('#f2c12e', 0.6)
    for s in (-1, 1):
        P.pants.add(C.g_torus(0.075, 0.006, 24, 6), M((s * 0.086, 0, 0.27)), stitch)
    # кроссовки: белые, красная подошва, синяя полоса
    P = Parts(new('shoes_sneakers'))
    build_shoes(P, 'stand', True, '#d6264f', sole='#d6264f', swoosh='#3a6ab0')
    headphones(new('acc_headphones'))
    # часы на левом запястье
    w = new('acc_watch')
    _, el, hand = arm_pose('stand', -1)
    wrist = hand + (el - hand).normalized() * 0.045
    rot = (el - hand).normalized().to_track_quat('Z', 'Y').to_matrix().to_4x4()
    w.add(C.g_torus(0.06, 0.013, 24, 8), Matrix.Translation(wrist) @ rot, mat('#2a2a33', 0.6))
    w.add(C.g_box(0.05, 0.03, 0.055, 0.012), Matrix.Translation(wrist + Vector((-0.012, -0.05, 0))) @ M(rot=(0, 0, -15)),
          mat('#4a4a55', 0.25, 0.8))
    w.add(C.g_box(0.04, 0.006, 0.045, 0.01), Matrix.Translation(wrist + Vector((-0.012, -0.066, 0))) @ M(rot=(0, 0, -15)),
          mat('#4dd17a', 0.2, emit=2))
    # смартфон в правой руке экраном к герою
    p = new('acc_smartphone')
    _, _, hand = arm_pose('stand', 1)
    phone_mesh(p, hand + Vector((-0.02, -0.06, 0.02)), (-15, 0, -20), '#7b5cff')
    return parts


# ===================== СБОРКА =====================

def build_character(key, d, coll):
    s = d.get('scale', 1.0)
    props = d.get('props', [])
    pose = pose_for(props)
    hero = key.startswith('hero_')
    if not hero:
        m = C.Model('ch_' + key)
        P = Parts(m)
        build_body(P, d, pose)
        build_head(m, d, key)
        build_props(m, d, props, vehicles=False)
        # на велосипеде и самокате — приподнят: сидит на седле / стоит на деке (транспорт добавляется после)
        lift = {'ride': (0.14, 0.08), 'handlebar': (0.06, 0.0)}.get(pose)
        if lift:
            for v in m.bm.verts:
                v.co.z += lift[0]
                v.co.y += lift[1]
        build_props(m, d, props, vehicles=True)
        ob = m.finish(coll)
        ob.scale = (s, s, s)
        return ob
    # Герой: части отдельными объектами под общим пустым родителем
    root = bpy.data.objects.new('ch_' + key, None)
    coll.objects.link(root)
    base = C.Model('ch_%s__base' % key)
    P = Parts(base, C.Model('ch_%s__top_default' % key), C.Model('ch_%s__pants_default' % key),
              C.Model('ch_%s__shoes_default' % key))
    build_body(P, d, 'stand')
    build_head(base, d, key)
    models = [base, P.top, P.pants, P.shoes] + list(build_item_parts('ch_' + key, d, coll).values())
    for mdl in models:
        ob = mdl.finish(coll)
        ob.parent = root
    root.scale = (s, s, s)
    return root


def load_defs():
    p = os.path.join(C.ROOT, 'Assets', 'Resources', 'Data')
    with open(os.path.join(p, 'characters.json'), encoding='utf-8') as f:
        chars = json.load(f)
    with open(os.path.join(p, 'passers.json'), encoding='utf-8') as f:
        passers = json.load(f)
    with open(os.path.join(p, 'heroes.json'), encoding='utf-8') as f:
        for g, looks in json.load(f).items():
            for i, d in enumerate(looks):
                chars['hero_%s_%d' % (g, i + 1)] = d
    return chars, {v['model']: v for v in passers.values()}


def build_all(only=None):
    chars, passers = load_defs()
    coll = C.collection('Characters')
    C.clear_collection(coll)
    obs = []
    for k, d in list(chars.items()) + list(passers.items()):
        if only and k not in only:
            continue
        obs.append(build_character(k, d, coll))
    for i, ob in enumerate(obs):
        ob.location = ((i % 10) * 0.9, (i // 10) * 1.4, 0)
    return obs


def show_row(r, per=10):
    """Для просмотра: оставляет видимым только r-й ряд коллекции Characters."""
    obs = sorted([o for o in C.collection('Characters').objects if o.parent is None],
                 key=lambda o: (round(o.location.y, 2), o.location.x))
    for i, o in enumerate(obs):
        vis = (i // per) == r
        for x in [o] + list(o.children):
            x.hide_render = x.hide_viewport = not vis
    return [o.name for o in obs if (obs.index(o) // per) == r]
