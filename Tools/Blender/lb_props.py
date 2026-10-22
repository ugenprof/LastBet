# Предметы: вещи из рюкзака (catalog.json → ITEMS), мир ставок (рулетка, слоты, ракета, фишки),
# предметы обычной жизни (школа, дом, подработки). Цвета — как у 2D-иконок (Art/GameArt.cs).
# Обычный мир — матовый; всё, что про ставки, — глянец, золото и неоновое свечение.
import math, random
import bpy, bmesh
from mathutils import Matrix, Vector
import lb_core as C
from lb_core import M, mat, darken, lighten, INK, WHITE, GOLD, NEON_PINK, NEON_CYAN, NEON_VIOLET, CASINO_DARK


def text_mesh(body, size=0.1, depth=0.01, bevel=0.003):
    """Текст как bmesh (лежит в плоскости XZ, лицом к -Y)."""
    cu = bpy.data.curves.new('_txt', 'FONT')
    cu.body = body
    cu.size = size
    cu.extrude = depth
    cu.bevel_depth = bevel
    cu.align_x = 'CENTER'
    cu.align_y = 'CENTER'
    ob = bpy.data.objects.new('_txt', cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = ob.evaluated_get(dg).to_mesh()
    bm = bmesh.new()
    bm.from_mesh(me)
    ob.evaluated_get(dg).to_mesh_clear()
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, 'X'), verts=bm.verts)
    return bm


def soft_extrude(points, depth, bevel):
    """Плоская фигура со скруглёнными кромками — для одежды «как вырезанной из фетра»."""
    g = C.g_extrude(points, depth)
    bmesh.ops.bevel(g, geom=[e for e in g.edges], offset=bevel, segments=3, affect='EDGES',
                    profile=0.5, clamp_overlap=True)
    return g


# ===================== ВЕЩИ ИЗ РЮКЗАКА =====================

def item_tshirt():
    m = C.Model('item_tshirt')
    red = mat('#e8483f', 0.85)
    pts = [(-0.12, 0.0), (0.12, 0.0), (0.12, 0.2), (0.165, 0.17), (0.215, 0.235), (0.13, 0.31),
           (0.055, 0.31), (-0.055, 0.31), (-0.13, 0.31), (-0.215, 0.235), (-0.165, 0.17), (-0.12, 0.2)]
    m.add(soft_extrude(pts, 0.04, 0.012), M(), red)
    m.add(C.g_torus(0.05, 0.012, 24, 8, arc=math.pi, start=math.pi), M((0, -0.021, 0.31), (90, 0, 0)),
          mat('#b8302a', 0.8))
    m.add(C.g_rcyl(0.045, 0.006, 0.002, 28), M((0, -0.022, 0.16), (90, 0, 0)), mat(WHITE, 0.6))
    return m


def item_jeans():
    m = C.Model('item_jeans')
    den = mat('#3a6ab0', 0.85)
    # контур невыпуклый — собираем из двух штанин и пояса
    m.add(soft_extrude([(-0.13, 0.3), (-0.145, 0.03), (-0.01, 0.03), (0.0, 0.3)], 0.05, 0.012), M(), den)
    m.add(soft_extrude([(0.0, 0.3), (0.01, 0.03), (0.145, 0.03), (0.13, 0.3)], 0.05, 0.012), M(), den)
    m.add(C.g_box(0.27, 0.056, 0.04, 0.01), M((0, 0, 0.29)), mat('#2a5290', 0.8))
    m.add(C.g_box(0.03, 0.06, 0.03, 0.006), M((0, -0.002, 0.29)), mat('#c9ced8', 0.25, 1.0))
    st = mat('#f2c12e', 0.6)
    for s in (-1, 1):
        m.limb((s * 0.09, -0.027, 0.25), (s * 0.075, -0.027, 0.2), 0.004, st)
        m.limb((s * 0.12, -0.027, 0.06), (s * 0.03, -0.027, 0.06), 0.004, st)
    return m


def item_hoodie():
    m = C.Model('item_hoodie')
    top = '#2f2b3a'      # как на герое (lb_characters.ITEM_LOOK)
    trim = '#9b7bff'
    tm = mat(top, 0.85)
    body = [(-0.13, 0.0), (0.13, 0.0), (0.13, 0.22), (0.16, 0.3), (0.13, 0.33), (-0.13, 0.33), (-0.16, 0.3), (-0.13, 0.22)]
    m.add(soft_extrude(body, 0.06, 0.015), M(), tm)
    for s in (-1, 1):
        m.limb((s * 0.15, 0, 0.31), (s * 0.24, 0, 0.08), 0.042, tm)
        m.add(C.g_rcyl(0.045, 0.03, 0.01, 18), M((s * 0.245, 0, 0.06), (0, s * 22, 0)), mat(darken(top, 0.18), 0.85))
    m.add(C.g_torus(0.075, 0.035, 28, 10), M((0, 0.01, 0.33), (-20, 0, 0), (1.1, 1, 1)), mat(trim, 0.6))
    m.sphere((0, 0.012, 0.335), (0.06, 0.02, 0.035), mat('#1a1626', 0.9))
    m.add(C.g_box(0.262, 0.064, 0.025, 0.008), M((0, 0, 0.24)), mat(trim, 0.6))
    m.add(C.g_box(0.262, 0.064, 0.025, 0.008), M((0, 0, 0.012)), mat(trim, 0.6))
    for s in (-1, 1):
        m.limb((s * 0.03, -0.04, 0.3), (s * 0.034, -0.04, 0.2), 0.005, mat('#ece8f8', 0.6))
        m.sphere((s * 0.034, -0.04, 0.195), 0.01, mat('#ece8f8', 0.6))
    m.add(C.g_box(0.16, 0.012, 0.07, 0.008), M((0, -0.033, 0.07)), mat(darken(top, 0.2), 0.85))
    return m


def item_sneakers():
    m = C.Model('item_sneakers')
    for i, x in enumerate((-0.075, 0.075)):
        y = -0.03 if i else 0.03
        up = mat('#f4f4f4', 0.55)
        m.add(C.g_box(0.11, 0.3, 0.08, 0.04), M((x, y, 0.065)), up)
        m.sphere((x, y + 0.08, 0.11), (0.055, 0.08, 0.06), up)
        m.add(C.g_box(0.116, 0.308, 0.03, 0.014), M((x, y, 0.017)), mat('#d6264f', 0.45, coat=0.4))
        m.add(C.g_rcyl(0.05, 0.02, 0.008, 20), M((x, y + 0.1, 0.165)), mat('#e6e2ea', 0.6))
        # «галочка» и шнурки
        s = 1 if x > 0 else -1
        m.add(soft_extrude([(-0.09, 0.0), (0.08, 0.035), (0.09, 0.05), (-0.08, 0.02)], 0.01, 0.003),
              M((x + s * 0.057, y, 0.06), (0, 0, 90 * s)), mat('#3a6ab0', 0.5))
        for k in range(3):
            m.limb((x - 0.022, y - 0.07 + k * 0.03, 0.106), (x + 0.022, y - 0.07 + k * 0.03, 0.106),
                   0.005, mat('#333340', 0.6))
    return m


def item_headphones():
    m = C.Model('item_headphones')
    dark = mat('#2a2a33', 0.4, coat=0.6)
    red = mat('#d6264f', 0.35, coat=0.8)
    m.add(C.g_torus(0.15, 0.016, 40, 10, arc=math.pi, start=0), M((0, 0, 0.12), (90, 0, 0)), dark)
    m.add(C.g_torus(0.15, 0.022, 28, 10, arc=math.pi * 0.5, start=math.pi * 0.25), M((0, 0, 0.12), (90, 0, 0), (1, 1.2, 1)),
          mat('#3a3a46', 0.8))
    for s in (-1, 1):
        m.add(C.g_rcyl(0.065, 0.05, 0.018, 28), M((s * 0.155, 0, 0.08), (0, 90, 0), (1, 1.25, 1)), red)
        m.add(C.g_rcyl(0.055, 0.03, 0.012, 28), M((s * 0.125, 0, 0.08), (0, 90, 0), (1, 1.25, 1)), mat('#2a2a33', 0.9))
        m.limb((s * 0.15, 0, 0.12), (s * 0.155, 0, 0.13), 0.012, dark)
    return m


def item_watch():
    m = C.Model('item_watch')
    strap = mat('#2a2a33', 0.7)
    m.add(C.g_torus(0.07, 0.016, 40, 8), M((0, 0, 0.07), (90, 0, 0), (1, 1, 1.0)), strap)
    m.add(C.g_box(0.1, 0.03, 0.12, 0.025), M((0, -0.07, 0.07)), mat('#4a4a55', 0.25, 0.8))
    m.add(C.g_box(0.082, 0.01, 0.1, 0.02), M((0, -0.083, 0.07)), mat('#0e1a2a', 0.1, coat=1))
    pulse = [(-0.032, 0.0), (-0.012, 0.0), (-0.004, 0.022), (0.006, -0.024), (0.014, 0.008), (0.032, 0.008)]
    for a, b in zip(pulse, pulse[1:]):
        m.limb((a[0], -0.09, 0.07 + a[1]), (b[0], -0.09, 0.07 + b[1]), 0.0025, mat('#4dd17a', 0.4, emit=3))
    m.add(C.g_rcyl(0.008, 0.012, 0.003, 12), M((0.054, -0.07, 0.09), (0, 90, 0)), mat('#c9ced8', 0.2, 1.0))
    return m


def phone(m, screen_hex, emit=2.5, loc=(0, 0, 0)):
    x, y, z = loc
    m.add(C.g_box(0.11, 0.016, 0.22, 0.012), M((x, y, z + 0.11)), mat('#1a1a22', 0.2, 0.3, coat=1))
    m.add(C.g_box(0.098, 0.004, 0.2, 0.01), M((x, y - 0.0075, z + 0.11)), mat(screen_hex, 0.15, emit=emit))
    m.add(C.g_box(0.03, 0.004, 0.006, 0.002), M((x, y - 0.0098, z + 0.205)), mat('#111116', 0.3))
    m.add(C.g_rcyl(0.009, 0.006, 0.002, 16), M((x - 0.03, y + 0.01, z + 0.19), (90, 0, 0)), mat('#333340', 0.1, coat=1))


def item_smartphone():
    m = C.Model('item_smartphone')
    phone(m, '#4ab0ff')
    # фиолетовая «волна» обоев
    m.add(soft_extrude([(-0.048, 0.015), (0.048, 0.015), (0.048, 0.07), (-0.048, 0.13)], 0.002, 0.0005),
          M((0, -0.0105, 0)), mat('#7b5cff', 0.2, emit=2))
    return m


def item_backpack():
    """Рюкзак героя — цель игры: собрать в него все вещи."""
    m = C.Model('item_backpack')
    top = '#6d4fc2'
    body = mat(top, 0.8)
    m.add(C.g_box(0.34, 0.2, 0.42, 0.09), M((0, 0, 0.23)), body)
    m.sphere((0, 0, 0.42), (0.17, 0.1, 0.07), body)
    m.add(C.g_box(0.26, 0.08, 0.17, 0.05), M((0, -0.11, 0.15)), mat(darken(top, 0.12), 0.8))
    zip_m = mat(GOLD, 0.3, 1.0)
    m.limb((-0.1, -0.152, 0.215), (0.1, -0.152, 0.215), 0.005, mat('#2a1a5a', 0.6))
    m.add(C.g_box(0.016, 0.01, 0.03, 0.004), M((0.08, -0.158, 0.2)), zip_m)
    m.add(C.g_torus(0.045, 0.012, 20, 8, arc=math.pi, start=0), M((0, 0, 0.47), (90, 0, 0)), mat('#2a1a5a', 0.6))
    for s in (-1, 1):
        m.add(C.g_box(0.05, 0.03, 0.36, 0.012), M((s * 0.09, 0.11, 0.24), (6, 0, 0)), mat('#2a1a5a', 0.7))
    # значок с семёркой на кармане — привет из казино
    m.add(C.g_rcyl(0.028, 0.008, 0.003, 24), M((-0.08, -0.155, 0.14), (90, 0, 0)), mat('#ffd23f', 0.3, 0.6))
    return m


# ===================== МИР СТАВОК =====================

ROULETTE_LOSS = 192.6     # Roulette.LOSS / X19 / X20 в градусах (Games/Roulette.cs)
ROULETTE_X20 = 358.2


def casino_roulette():
    m = C.Model('casino_roulette')
    wood = mat('#2a1a12', 0.35, coat=1)
    goldm = mat('#e8c27a', 0.25, 1.0)
    # чаша
    m.add(C.g_lathe([(0.0, 0.0), (0.5, 0.0), (0.56, 0.03), (0.58, 0.09), (0.55, 0.11), (0.5, 0.1), (0.0, 0.1)], 64),
          M(), wood)
    m.add(C.g_torus(0.555, 0.018, 64, 10), M((0, 0, 0.105)), goldm)
    # сектора: 30 по 12° (проигрыш — красные, ×1.9 — зелёные, ×20 — золотая полоска)
    cols = {'loss': ('#b3243f', '#8e1830'), 'win': ('#1f9a62', '#167a4c')}
    R0, R1 = 0.12, 0.47
    for i in range(30):
        a0, a1 = i * 12.0, (i + 1) * 12.0
        for kind, lo, hi in (('loss', 0, ROULETTE_LOSS), ('win', ROULETTE_LOSS, ROULETTE_X20), ('x20', ROULETTE_X20, 360)):
            s0, s1 = max(a0, lo), min(a1, hi)
            if s1 - s0 < 0.05:
                continue
            if kind == 'x20':
                col = mat('#ffd23f', 0.2, 0.6, emit=1.5)
            else:
                col = mat(cols[kind][i % 2], 0.3, coat=0.8)
            n = max(2, int((s1 - s0) / 3))
            pts = [(0.0, 0.0)]
            for k in range(n + 1):
                a = math.radians(90 - (s0 + (s1 - s0) * k / n))      # 0° — верх, по часовой
                pts.append((R1 * math.cos(a), R1 * math.sin(a)))
            g = C.g_extrude([(x, y) for x, y in pts], 0.02)
            m.add(g, M((0, 0, 0.1), (90, 0, 0)), col, smooth=False)
    for i in range(30):
        a = math.radians(90 - i * 12)
        m.limb((R0 * math.cos(a), R0 * math.sin(a), 0.115), (R1 * math.cos(a), R1 * math.sin(a), 0.115), 0.004, goldm)
    m.add(C.g_torus(R1, 0.012, 64, 8), M((0, 0, 0.112)), goldm)
    # огоньки по ободу
    for i in range(24):
        a = math.tau * i / 24
        m.sphere((0.555 * math.cos(a), 0.555 * math.sin(a), 0.13), 0.013, mat('#fff3c4', 0.2, emit=6), seg=12, rings=8)
    # ступица
    m.add(C.g_lathe([(0.0, 0.1), (0.13, 0.1), (0.13, 0.13), (0.09, 0.15), (0.03, 0.17), (0.02, 0.24), (0.0, 0.25)], 48),
          M(), goldm)
    m.sphere((0, 0, 0.255), 0.03, mat('#c9993a', 0.2, 1.0))
    # шарик и стрелка
    m.sphere((0.3, 0.2, 0.135), 0.022, mat(WHITE, 0.1, coat=1), seg=20, rings=12)
    m.limb((0, 0.6, 0.0), (0, 0.6, 0.2), 0.016, goldm)
    m.add(C.g_extrude([(-0.035, 0.0), (0.035, 0.0), (0.0, -0.11)], 0.025), M((0, 0.6, 0.2), (90, 0, 0)),
          mat('#f4f1ea', 0.3))
    m.sphere((0, 0.585, 0.215), 0.014, mat('#d6264f', 0.3, emit=2))
    return m


def casino_slot_machine():
    m = C.Model('casino_slot_machine')
    body = mat(CASINO_DARK, 0.3, 0.2, coat=1)
    gold = mat(GOLD, 0.25, 1.0)
    pink = mat(NEON_PINK, 0.2, emit=5)
    cyan = mat(NEON_CYAN, 0.2, emit=4)
    m.add(C.g_box(0.62, 0.45, 0.62, 0.06), M((0, 0, 0.31)), body)                 # тумба
    m.add(C.g_box(0.6, 0.42, 0.6, 0.08), M((0, 0.02, 0.9)), body)                  # корпус с барабанами
    m.add(C.g_rcyl(0.3, 0.42, 0.08, 48), M((0, 0.02, 1.18), (90, 0, 0), (1, 0.55, 1)), body)  # купол
    m.add(C.g_box(0.5, 0.22, 0.04, 0.015), M((0, -0.14, 0.64), (-20, 0, 0)), mat('#3a2c58', 0.4, coat=1))
    # окно с барабанами
    for z in (0.765, 1.075):
        m.add(C.g_box(0.52, 0.05, 0.035, 0.012), M((0, -0.215, z)), gold)
    for x in (-0.245, 0.245):
        m.add(C.g_box(0.035, 0.05, 0.34, 0.012), M((x, -0.215, 0.92)), gold)
    for x in (-0.075, 0.075):
        m.add(C.g_box(0.012, 0.03, 0.3, 0.004), M((x, -0.2, 0.92)), gold)
    reels = [('#e0213f', '7'), ('#ffcf2a', '7'), ('#4fd1ff', '7')]
    for k, (col, sym) in enumerate(reels):
        x = -0.15 + k * 0.15
        m.add(C.g_cyl(0.16, 0.16, 0.13, 32), M((x, -0.05, 0.92), (0, 90, 0)), mat('#f5f0e6', 0.4))
        m.add(text_mesh(sym, 0.13, 0.01, 0.003), M((x, -0.215, 0.915)), mat('#d6264f', 0.3, emit=0.6))
    m.add(C.g_box(0.48, 0.02, 0.006, 0.002), M((0, -0.222, 0.92)), mat('#ff5a7a', 0.2, emit=4))
    # неоновая вывеска «777» и рамки
    m.add(text_mesh('777', 0.16, 0.02, 0.006), M((0, -0.25, 1.24)), pink)
    m.add(C.g_torus(0.29, 0.01, 64, 8, arc=math.pi, start=0), M((0, -0.22, 1.11), (90, 0, 0), (1, 0.55, 1)), cyan)
    for z in (0.73, 1.115):
        m.add(C.g_box(0.6, 0.02, 0.02, 0.008), M((0, -0.235, z)), cyan)
    # кнопки
    for k, col in enumerate(('#ff3d8b', '#ffd23f', '#3de0ff')):
        m.add(C.g_rcyl(0.035, 0.03, 0.01, 20), M((-0.12 + k * 0.12, -0.2, 0.67), (70, 0, 0)), mat(col, 0.2, emit=3))
    # рычаг
    m.add(C.g_rcyl(0.05, 0.06, 0.015, 24), M((0.33, 0.02, 0.86), (0, 90, 0)), gold)
    m.limb((0.36, 0.02, 0.86), (0.4, 0.0, 1.22), 0.016, mat('#c9ced8', 0.2, 1.0))
    m.sphere((0.4, 0.0, 1.25), 0.05, mat('#e0213f', 0.15, coat=1))
    # лоток для монет
    m.add(C.g_box(0.3, 0.1, 0.05, 0.015), M((0, -0.22, 0.4)), gold)
    m.add(C.g_box(0.26, 0.07, 0.03, 0.01), M((0, -0.225, 0.42)), mat('#120c20', 0.5))
    return m


def casino_rocket():
    """Ракета из игры «Ракета» (crash): летит, пока не взорвётся."""
    m = C.Model('casino_rocket')
    m.add(C.g_lathe([(0.0, 0.12), (0.07, 0.12), (0.1, 0.18), (0.115, 0.3), (0.112, 0.45), (0.095, 0.55)], 40), M(),
          mat('#e8eef4', 0.3, coat=0.6))
    m.add(C.g_lathe([(0.095, 0.55), (0.07, 0.62), (0.035, 0.68), (0.0, 0.7)], 40), M(), mat('#d6264f', 0.25, coat=1))
    m.add(C.g_torus(0.113, 0.01, 40, 8), M((0, 0, 0.3)), mat('#d6264f', 0.25, coat=1))
    m.add(C.g_rcyl(0.045, 0.02, 0.008, 28), M((0, -0.105, 0.42), (90, 0, 0)), mat('#2a6be0', 0.2, coat=1))
    m.add(C.g_rcyl(0.032, 0.022, 0.006, 28), M((0, -0.11, 0.42), (90, 0, 0)), mat('#9fd8ff', 0.1, emit=2))
    for k in range(3):
        a = math.radians(90 + k * 120)
        fin = C.g_extrude([(0.0, 0.0), (0.12, -0.06), (0.11, 0.04), (0.0, 0.16)], 0.02)
        m.add(fin, Matrix.Translation((0.09 * math.cos(a), 0.09 * math.sin(a), 0.12)) @ Matrix.Rotation(a, 4, 'Z'),
              mat('#d6264f', 0.25, coat=1))
    m.add(C.g_lathe([(0.065, 0.13), (0.075, 0.09), (0.06, 0.07)], 32), M(), mat('#4a4f5c', 0.3, 0.8))
    # пламя
    m.add(C.g_lathe([(0.06, 0.07), (0.07, 0.03), (0.05, -0.06), (0.0, -0.2)], 32), M(), mat('#ff8a2a', 0.5, emit=5))
    m.add(C.g_lathe([(0.035, 0.07), (0.042, 0.03), (0.026, -0.04), (0.0, -0.12)], 32), M((0, 0, -0.005)),
          mat('#ffd23f', 0.5, emit=8))
    return m


def casino_chips():
    m = C.Model('casino_chips')
    stacks = [((0, 0), '#9b5cff', 7), ((0.1, 0.03), '#d6264f', 4), ((0.04, -0.1), '#1f9a62', 3), ((-0.1, -0.04), '#2a2a33', 5)]
    for (x, y), col, n in stacks:
        for k in range(n):
            z = 0.008 + k * 0.0165
            rot = k * 17
            m.add(C.g_rcyl(0.045, 0.015, 0.004, 32), M((x, y, z), (0, 0, rot)), mat(col, 0.25, coat=1))
            for j in range(6):
                a = math.radians(rot + j * 60)
                m.add(C.g_box(0.016, 0.008, 0.0152, 0.002),
                      Matrix.Translation((x + 0.0425 * math.cos(a), y + 0.0425 * math.sin(a), z)) @ Matrix.Rotation(a + math.pi / 2, 4, 'Z'),
                      mat(WHITE, 0.4))
        top = 0.008 + (n - 1) * 0.0165
        m.add(C.g_rcyl(0.03, 0.002, 0.0008, 28), M((x, y, top + 0.008)), mat(lighten(col, 0.25), 0.3))
    # монеты
    for k, (x, y) in enumerate([(0.13, -0.08), (0.15, -0.02), (-0.03, 0.1)]):
        m.add(C.g_rcyl(0.025, 0.006, 0.0025, 24), M((x, y, 0.003 + (k == 0) * 0.006)), mat(GOLD, 0.25, 1.0))
    return m


def casino_bet_phone():
    """Телефон с открытым приложением ставок — с него всё начинается."""
    m = C.Model('casino_bet_phone')
    phone(m, '#2a1450', emit=1.5)
    m.add(C.g_rcyl(0.032, 0.002, 0.0008, 32), M((0, -0.0105, 0.13), (90, 0, 0)), mat('#ffd23f', 0.2, emit=3))
    for k, col in enumerate(('#b3243f', '#1f9a62', '#b3243f', '#1f9a62')):
        a = math.radians(45 + k * 90)
        m.add(C.g_rcyl(0.012, 0.0025, 0.0006, 16), M((0.022 * math.cos(a), -0.0115, 0.13 + 0.022 * math.sin(a)), (90, 0, 0)),
              mat(col, 0.2, emit=3))
    m.add(C.g_box(0.07, 0.003, 0.022, 0.006), M((0, -0.011, 0.05)), mat(NEON_PINK, 0.2, emit=4))
    m.add(text_mesh('x20', 0.03, 0.002, 0.0006), M((0, -0.0115, 0.19)), mat('#ffd23f', 0.2, emit=4))
    return m


def casino_cash():
    """Пачка денег — выигрыш."""
    m = C.Model('casino_cash')
    note = mat('#8fbf9f', 0.7)
    for k in range(6):
        m.add(C.g_box(0.16, 0.075, 0.006, 0.002), M((0.003 * (k % 2), 0.002 * k, 0.003 + k * 0.0065), (0, 0, k * 3 - 6)), note)
    m.add(C.g_box(0.03, 0.08, 0.042, 0.004), M((0, 0, 0.021)), mat('#e6dccb', 0.6))
    m.add(C.g_box(0.15, 0.065, 0.001, 0.0), M((0, 0, 0.0425)), mat('#a6d1b3', 0.6))
    m.add(C.g_rcyl(0.018, 0.001, 0.0, 24), M((0.045, 0, 0.0435)), mat('#5e8f70', 0.6))
    return m


# ===================== ОБЫЧНАЯ ЖИЗНЬ =====================

def prop_school_desk():
    m = C.Model('prop_school_desk')
    wood = mat('#c49a6c', 0.6)
    metal = mat('#4a6a8a', 0.4, 0.5)
    m.add(C.g_box(1.0, 0.5, 0.04, 0.015), M((0, 0, 0.74)), wood)
    for sx in (-1, 1):
        m.add(C.g_box(0.04, 0.4, 0.04, 0.012), M((sx * 0.42, 0, 0.06)), metal)
        m.limb((sx * 0.42, 0, 0.06), (sx * 0.42, 0, 0.72), 0.02, metal)
    m.add(C.g_box(0.9, 0.38, 0.02, 0.008), M((0, 0.02, 0.6)), mat(darken('#c49a6c', 0.15), 0.6))
    # тетрадь и ручка
    m.add(C.g_box(0.2, 0.15, 0.012, 0.004), M((-0.2, -0.05, 0.767), (0, 0, 8)), mat('#4a8ac0', 0.7))
    m.add(C.g_box(0.19, 0.14, 0.006, 0.002), M((-0.2, -0.05, 0.775), (0, 0, 8)), mat(WHITE, 0.8))
    m.limb((0.0, -0.08, 0.77), (0.13, -0.03, 0.77), 0.006, mat('#d6264f', 0.4))
    return m


def prop_school_chair():
    m = C.Model('prop_school_chair')
    wood = mat('#c49a6c', 0.6)
    metal = mat('#4a6a8a', 0.4, 0.5)
    m.add(C.g_box(0.42, 0.4, 0.035, 0.014), M((0, 0, 0.44)), wood)
    m.add(C.g_box(0.42, 0.03, 0.2, 0.014), M((0, 0.2, 0.7), (-6, 0, 0)), wood)
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.limb((sx * 0.18, sy * 0.17, 0.0), (sx * 0.18, sy * 0.17, 0.43), 0.016, metal)
        m.limb((sx * 0.18, 0.18, 0.45), (sx * 0.18, 0.21, 0.7), 0.014, metal)
    return m


def prop_locker():
    m = C.Model('prop_locker')
    blue = '#4a78a8'
    for k in range(2):
        x = -0.21 + k * 0.42
        m.add(C.g_box(0.4, 0.45, 1.8, 0.02), M((x, 0, 0.9)), mat(blue, 0.45, 0.3))
        m.add(C.g_box(0.36, 0.02, 1.7, 0.012), M((x, -0.226, 0.9)), mat(lighten(blue, 0.08), 0.45, 0.3))
        for j in range(4):
            m.add(C.g_box(0.2, 0.01, 0.015, 0.005), M((x, -0.238, 1.55 - j * 0.04)), mat(darken(blue, 0.35), 0.5))
        m.add(C.g_box(0.03, 0.03, 0.12, 0.01), M((x + 0.13, -0.245, 1.0)), mat('#c9ced8', 0.2, 1.0))
    return m


def prop_bed():
    m = C.Model('prop_bed')
    wood = mat('#8a5a3a', 0.55)
    m.add(C.g_box(1.0, 2.0, 0.25, 0.04), M((0, 0, 0.25)), wood)
    m.add(C.g_box(1.0, 0.08, 0.75, 0.04), M((0, 1.0, 0.45)), wood)
    m.add(C.g_box(0.94, 1.92, 0.18, 0.08), M((0, 0, 0.45)), mat('#f0ede6', 0.85))
    m.add(C.g_box(0.98, 1.3, 0.12, 0.06), M((0, -0.32, 0.56)), mat('#5a6fb8', 0.85))
    m.add(C.g_box(0.98, 0.12, 0.1, 0.05), M((0, 0.3, 0.57)), mat(lighten('#5a6fb8', 0.25), 0.85))
    m.add(C.g_box(0.6, 0.3, 0.12, 0.06), M((0, 0.72, 0.6)), mat(WHITE, 0.9))
    for sx in (-1, 1):
        m.add(C.g_box(0.06, 0.06, 0.14, 0.02), M((sx * 0.45, -0.95, 0.07)), wood)
    # телефон светится в темноте
    phone(m, '#4ab0ff', 3.0, loc=(0.25, 0.5, 0.55))
    return m


def prop_bench():
    m = C.Model('prop_bench')
    wood = mat('#b5763f', 0.6)
    iron = mat('#2f3540', 0.4, 0.6)
    for k in range(3):
        m.add(C.g_box(1.4, 0.11, 0.035, 0.012), M((0, -0.13 + k * 0.13, 0.45)), wood)
    for k in range(2):
        m.add(C.g_box(1.4, 0.035, 0.1, 0.012), M((0, 0.24, 0.62 + k * 0.14), (-12, 0, 0)), wood)
    for sx in (-1, 1):
        m.add(C.g_extrude([(-0.2, 0.0), (0.2, 0.0), (0.2, 0.43), (-0.2, 0.43)], 0.04),
              M((sx * 0.6, 0.0, 0), (0, 0, 90)), iron)
        m.limb((sx * 0.6, 0.22, 0.43), (sx * 0.6, 0.27, 0.85), 0.02, iron)
    return m


def prop_street_lamp():
    m = C.Model('prop_street_lamp')
    iron = mat('#2f3540', 0.4, 0.6)
    m.add(C.g_lathe([(0.0, 0.0), (0.14, 0.0), (0.13, 0.08), (0.06, 0.14), (0.05, 0.3), (0.0, 0.3)], 32), M(), iron)
    m.limb((0, 0, 0.3), (0, 0, 2.6), 0.035, iron, r2=0.028)
    m.add(C.g_torus(0.25, 0.022, 32, 8, arc=math.pi * 0.5, start=math.pi * 0.5), M((0.25, 0, 2.6), (90, 0, 0)), iron)
    m.add(C.g_lathe([(0.0, 0.0), (0.06, 0.0), (0.16, -0.08), (0.17, -0.12), (0.0, -0.12)], 32), M((0.42, 0, 2.85)),
          mat('#3a414f', 0.4, 0.5))
    m.sphere((0.42, 0, 2.73), (0.12, 0.12, 0.05), mat('#ffe2a0', 0.3, emit=8))
    return m


def prop_shawarma():
    m = C.Model('prop_shawarma')
    m.add(C.g_lathe([(0.0, 0.0), (0.05, 0.005), (0.06, 0.05), (0.062, 0.2), (0.0, 0.22)], 28), M(), mat('#e8c58a', 0.7))
    m.add(C.g_lathe([(0.064, 0.0), (0.066, 0.12), (0.0, 0.125)], 28), M((0, 0, -0.002)), mat('#f2efe8', 0.8))
    for k, col in enumerate(('#6aa84f', '#d84a3a', '#9a5a32')):
        a = math.radians(k * 120)
        m.sphere((0.03 * math.cos(a), 0.03 * math.sin(a), 0.215), (0.03, 0.03, 0.02), mat(col, 0.6))
    return m


def prop_vegetable_crate():
    m = C.Model('prop_vegetable_crate')
    wood = mat('#c8a06a', 0.7)
    for z in (0.04, 0.14):
        for s in (-1, 1):
            m.add(C.g_box(0.5, 0.02, 0.07, 0.006), M((0, s * 0.17, z)), wood)
            m.add(C.g_box(0.02, 0.34, 0.07, 0.006), M((s * 0.25, 0, z)), wood)
    m.add(C.g_box(0.5, 0.34, 0.015, 0.004), M((0, 0, 0.008)), wood)
    rnd = random.Random(7)
    kinds = [('#d43a2f', 0.045), ('#e98a2a', 0.04), ('#c9a46a', 0.042), ('#e2483a', 0.04)]
    for i in range(14):
        col, r = kinds[i % len(kinds)]
        x = -0.19 + (i % 5) * 0.095 + rnd.uniform(-0.01, 0.01)
        y = -0.11 + (i // 5) * 0.11 + rnd.uniform(-0.01, 0.01)
        m.sphere((x, y, 0.13 + rnd.uniform(0, 0.03)), (r, r, r * 0.92), mat(col, 0.5))
    return m


def prop_delivery_box():
    m = C.Model('prop_delivery_box')
    m.add(C.g_box(0.42, 0.34, 0.4, 0.04), M((0, 0, 0.2)), mat('#f2c12e', 0.6))
    m.add(C.g_box(0.43, 0.345, 0.02, 0.008), M((0, 0, 0.3)), mat(darken('#f2c12e', 0.2), 0.6))
    m.add(C.g_rcyl(0.07, 0.006, 0.002, 28), M((0, -0.172, 0.17), (90, 0, 0)), mat('#2a2a33', 0.6))
    for s in (-1, 1):
        m.add(C.g_box(0.04, 0.03, 0.36, 0.01), M((s * 0.1, 0.18, 0.22)), mat('#2a2a33', 0.7))
    return m


def prop_flyers():
    m = C.Model('prop_flyers')
    cols = ['#ff7a59', '#4ab0ff', '#ffd23f']
    for k in range(12):
        m.add(C.g_box(0.15, 0.21, 0.004, 0.001), M((0.002 * (k % 3), 0.002 * (k % 2), 0.002 + k * 0.004), (0, 0, k % 4 - 2)),
              mat(WHITE, 0.85))
    m.add(C.g_box(0.13, 0.08, 0.002, 0.0), M((0, 0.04, 0.05)), mat(cols[0], 0.6))
    m.add(C.g_box(0.1, 0.02, 0.002, 0.0), M((0, -0.03, 0.05)), mat('#2a2a33', 0.6))
    m.add(C.g_box(0.08, 0.012, 0.002, 0.0), M((0, -0.06, 0.05)), mat('#2a2a33', 0.6))
    return m


ITEMS = [item_tshirt, item_jeans, item_hoodie, item_sneakers, item_headphones, item_watch, item_smartphone, item_backpack]
CASINO = [casino_roulette, casino_slot_machine, casino_rocket, casino_chips, casino_bet_phone, casino_cash]
WORLD = [prop_school_desk, prop_school_chair, prop_locker, prop_bed, prop_bench, prop_street_lamp,
         prop_shawarma, prop_vegetable_crate, prop_delivery_box, prop_flyers]


def build_all():
    out = {}
    for cname, fns in (('Items', ITEMS), ('Casino', CASINO), ('World', WORLD), ('Produce', PRODUCE)):
        coll = C.collection(cname)
        C.clear_collection(coll)
        x = 0.0
        obs = []
        for fn in fns:
            ob = fn().finish(coll)
            obs.append(ob)
        # раскладка в ряд для просмотра
        y = {'Items': -2.0, 'Casino': -4.0, 'World': -7.0, 'Produce': -9.0}[cname]
        for ob in obs:
            w = max(ob.dimensions.x, 0.2)
            ob.location = (x + w / 2, y, 0)
            x += w + 0.25
        out[cname] = obs
    return out


def show_only(*names):
    """Для просмотра: скрывает все коллекции моделей, кроме указанных."""
    for cname in ('Characters', 'Items', 'Casino', 'World', 'Produce'):
        c = bpy.data.collections.get(cname)
        if c:
            for o in c.objects:
                o.hide_viewport = o.hide_render = cname not in names


def _apple(co):
    r2 = co.x * co.x + co.y * co.y
    if co.z > 0:
        co.z -= 0.3 * math.exp(-r2 / 0.07)
    else:
        co.z += 0.16 * math.exp(-r2 / 0.05)
    k = 1 + 0.07 * co.z
    return Vector((co.x * k, co.y * k, co.z * 0.92))


def _tomato(co):
    a = math.atan2(co.y, co.x)
    r2 = co.x * co.x + co.y * co.y
    k = 1 + 0.045 * math.cos(a * 5) * math.sqrt(r2)
    z = co.z * 0.78
    if z > 0:
        z -= 0.14 * math.exp(-r2 / 0.06)
    return Vector((co.x * k, co.y * k, z))


def _lemon(co):
    tip = max(0.0, abs(co.z) - 0.7) / 0.3
    return Vector((co.x * (1 - 0.15 * tip), co.y * (1 - 0.15 * tip), co.z * (1 + 0.32 * tip * tip)))


# ===================== ПРОДУКТЫ ОВОЩНОЙ ЛАВКИ (catalog.json → GROCERY_PRODUCTS, тот же порядок) =====================

def _leaf(m, at, rot, scale=0.05, col='#4f9a3a'):
    g = C.g_sphere(16, 10)
    m.add(g, M(at, rot, (scale * 0.45, scale * 0.12, scale)), mat(col, 0.55))


def prod_apples():
    m = C.Model('prod_apples')
    for (x, y, z, r, col) in ((-0.065, 0.02, 0.07, 0.075, '#d93b30'), (0.07, 0.03, 0.068, 0.072, '#e2483a'),
                              (0.0, -0.05, 0.13, 0.07, '#c92f2a')):
        m.add(C.g_blob(_apple), M((x, y, z), scale=r), mat(col, 0.35, coat=0.6))
        m.limb((x, y, z + r * 0.55), (x + 0.008, y, z + r * 1.05), 0.006, mat('#5a3a1e', 0.6))
    _leaf(m, (0.012, -0.05, 0.215), (0, 50, 30))
    return m


def prod_carrot():
    m = C.Model('prod_carrot')
    for k, (x, y, rot) in enumerate(((-0.04, 0.0, -14), (0.045, 0.03, 10))):
        body = C.g_lathe([(0.0, -0.17), (0.012, -0.15), (0.03, -0.05), (0.04, 0.06), (0.036, 0.1), (0.0, 0.11)], 24)
        R = M((x, y, 0.13), (0, rot, 0))
        m.add(body, R, mat('#f07a1e', 0.55))
        for z in (-0.08, -0.02, 0.04):
            m.add(C.g_torus(0.034 if z > 0 else 0.026 + (z + 0.08) * 0.1, 0.0035, 20, 5), R @ M((0, 0, z)), mat('#d9631a', 0.6))
        for a in (-25, 0, 25):
            m.add(C.g_cone(0.012, 0.12, 8), R @ M((0, 0, 0.1), (a * 0.4, a, 0)), mat('#4f9a3a', 0.55))
    return m


def prod_potato():
    m = C.Model('prod_potato')
    for (x, y, z, sx, sy, sz, rot) in ((-0.05, 0.01, 0.055, 0.08, 0.06, 0.055, 15), (0.06, -0.01, 0.05, 0.07, 0.055, 0.05, -20)):
        m.sphere((x, y, z), (sx, sy, sz), mat('#c9a46a', 0.85), rot=(0, 10, rot))
        for k in range(4):
            a = k * 1.7
            m.sphere((x + math.cos(a) * sx * 0.6, y - sy * 0.85, z + math.sin(a) * sz * 0.5), 0.006, mat('#8a6a3a', 0.9))
    return m


def prod_tomato():
    m = C.Model('prod_tomato')
    for (x, y, r) in ((-0.06, 0.02, 0.07), (0.065, 0.0, 0.065)):
        m.add(C.g_blob(_tomato), M((x, y, r * 0.8), scale=r), mat('#e2372c', 0.3, coat=0.7))
        for k in range(5):
            a = k * 72
            m.add(C.g_sphere(10, 6), M((x, y, r * 1.47), (0, 0, a), (0.035, 0.009, 0.004)) @ M((0.6, 0, 0)), mat('#3f8a33', 0.5))
        m.limb((x, y, r * 1.45), (x, y, r * 1.62), 0.005, mat('#3f8a33', 0.5))
    return m


def prod_cucumber():
    m = C.Model('prod_cucumber')
    import random
    rnd = random.Random(3)
    for (x, rot) in ((-0.035, 8), (0.04, -6)):
        R = M((x, 0, 0.04), (90, 0, rot))
        m.add(C.g_rcyl(0.035, 0.24, 0.03, 20), R, mat('#3f8f3a', 0.45, coat=0.4))
        for k in range(14):
            a = rnd.uniform(0, math.tau)
            m.sphere(R.to_translation() + Vector((math.cos(a) * 0.035, rnd.uniform(-0.1, 0.1), 0.04 + math.sin(a) * 0.035 - 0.04)),
                     0.006, mat('#5fb04f', 0.5))
        m.sphere(R @ Vector((0, 0, 0.12)), 0.012, mat('#c9b04a', 0.6))
    return m


def prod_banana():
    """Связка бананов: гладкие изогнутые плоды с гранями, коричневые кончики, общий черешок."""
    m = C.Model('prod_banana')
    yel = mat('#f5cf3a', 0.45, coat=0.3)
    brown = mat('#6b4a24', 0.6)
    stem = Vector((-0.12, 0.0, 0.17))
    for dy, dz, tilt in ((-0.04, 0.0, -10), (0.0, 0.02, 0), (0.04, 0.0, 10)):
        R = M(rot=(0, 0, tilt))
        pts, radii = [], []
        n = 24
        for i in range(n + 1):
            t = i / n
            a = math.radians(205 + t * 105)
            pts.append(R @ Vector((0.16 * math.cos(a) + 0.02, dy, 0.16 * math.sin(a) + 0.2 + dz)))
            radii.append(0.034 * (0.3 + 0.7 * math.sin(math.pi * min(1.0, 0.08 + t * 1.0))))
        m.add(C.g_tube(pts, radii, 15, ridge=0.08), Matrix.Identity(4), yel)
        m.sphere(pts[-1], radii[-1] * 1.05, brown, seg=10, rings=6)
        m.limb(pts[0], stem, radii[0] * 0.9, mat('#c9a43a', 0.5), r2=0.012, seg=10)
    m.limb(stem, stem + Vector((-0.02, 0, 0.05)), 0.016, brown, seg=10)
    return m


def prod_onion():
    m = C.Model('prod_onion')
    for (x, y, r) in ((-0.055, 0.01, 0.07), (0.06, -0.01, 0.062)):
        m.add(C.g_lathe([(0.0, -0.75), (0.5, -0.7), (0.95, -0.25), (1.0, 0.15), (0.75, 0.6), (0.3, 0.95), (0.08, 1.25), (0.0, 1.35)], 32),
              M((x, y, r * 0.75), scale=r), mat('#c98a3a', 0.45, coat=0.3))
        for k in range(6):
            a = k * math.pi / 3
            m.limb((x + math.cos(a) * r * 0.25, y + math.sin(a) * r * 0.25, r * 0.0), (x + math.cos(a) * r * 0.4, y + math.sin(a) * r * 0.4, r * 0.75 + r * 0.95),
                   0.0025, mat('#a86a2a', 0.5))
        for a in (0, 120, 240):
            m.add(C.g_cone(0.004, 0.025, 6), M((x, y, r * 0.05), (160, 0, a)), mat('#e8dcc0', 0.7))
    return m


def prod_lemon():
    m = C.Model('prod_lemon')
    for (x, y, rot) in ((-0.05, 0.0, 70), (0.055, 0.02, 110)):
        m.add(C.g_blob(_lemon), M((x, y, 0.055), (0, rot, 0), (0.055, 0.055, 0.065)), mat('#f5d43a', 0.4, coat=0.4))
    _leaf(m, (0.0, 0.02, 0.11), (0, 70, 20), 0.045)
    return m


PRODUCE = [prod_apples, prod_carrot, prod_potato, prod_tomato, prod_cucumber, prod_banana, prod_onion, prod_lemon]
