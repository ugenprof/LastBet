# Общие инструменты генератора моделей «Последней ставки» (запускается внутри Blender).
# Стиль: «игрушечная диорама» — мягкие округлые формы, матовые цвета обычного мира,
# глянец и неоновое свечение — только у всего, что связано со ставками.
import bpy, bmesh, math
from mathutils import Matrix, Vector

ROOT = r"C:\AI\LastBet"
TAU = math.tau


# ---------- цвета и материалы ----------

def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def to_hex(c):
    return '#' + ''.join('%02x' % max(0, min(255, round(v * 255))) for v in c)


def mix(a, b, t):
    a, b = rgb(a), rgb(b)
    return to_hex(tuple(x + (y - x) * t for x, y in zip(a, b)))


def darken(h, t=0.25):
    return mix(h, '#000000', t)


def lighten(h, t=0.25):
    return mix(h, '#ffffff', t)


def mat(hexc, rough=0.62, metal=0.0, emit=0.0, coat=0.0, name=None):
    """Один материал на сочетание параметров: модели делят материалы, в Unity их будет немного."""
    key = name or 'LB_%s_r%02d_m%02d_e%02d_c%02d' % (hexc.lstrip('#'), rough * 100, metal * 100, emit * 10, coat * 10)
    m = bpy.data.materials.get(key)
    if m:
        return m
    m = bpy.data.materials.new(key)
    m.use_nodes = True
    col = tuple(srgb_to_lin(v) for v in rgb(hexc)) + (1.0,)
    bsdf = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = col
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    if emit:
        bsdf.inputs['Emission Color'].default_value = col
        bsdf.inputs['Emission Strength'].default_value = emit
    if coat:
        bsdf.inputs['Coat Weight'].default_value = coat
        bsdf.inputs['Coat Roughness'].default_value = 0.08
    m.diffuse_color = col
    return m


# Палитра мира (матовое) и мира ставок (глянец + неон)
INK = '#1d1a2b'          # тёмный «чернильный» — глаза, оправы, контуры
WHITE = '#f4f1ea'
GOLD = '#e8b84a'
NEON_PINK = '#ff3d8b'
NEON_CYAN = '#3de0ff'
NEON_VIOLET = '#9b5cff'
CASINO_DARK = '#241b3a'


# ---------- матрицы ----------

def M(loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    if not isinstance(scale, (tuple, list)):
        scale = (scale,) * 3
    R = (Matrix.Rotation(math.radians(rot[2]), 4, 'Z') @
         Matrix.Rotation(math.radians(rot[1]), 4, 'Y') @
         Matrix.Rotation(math.radians(rot[0]), 4, 'X'))
    S = Matrix.Diagonal((scale[0], scale[1], scale[2], 1))
    return Matrix.Translation(loc) @ R @ S


def look_dir(d):
    """Поворот, переводящий локальную ось -Y (лицо детали) в направление d."""
    d = Vector(d).normalized()
    yaw = math.atan2(d.x, -d.y)
    pitch = -math.asin(max(-1, min(1, d.z)))
    return Matrix.Rotation(yaw, 4, 'Z') @ Matrix.Rotation(pitch, 4, 'X')


# ---------- примитивы (в единичных размерах, bmesh) ----------

def g_sphere(seg=24, rings=14):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    return bm


def g_cyl(r1=1.0, r2=1.0, depth=1.0, seg=24, caps=True):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=caps, cap_tris=False, segments=seg,
                          radius1=r1, radius2=r2, depth=depth)
    return bm


def g_box(sx, sy, sz, bevel=0.0, segs=3):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * sx, v.co.y * sy, v.co.z * sz))
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=bm.verts[:] + bm.edges[:], offset=bevel, segments=segs,
                        affect='EDGES', profile=0.5, clamp_overlap=True)
    return bm


def g_rcyl(r, depth, bevel, seg=28, segs=3, r_top=None):
    """Цилиндр со скруглёнными кромками (шайба, фишка, туловище)."""
    bm = g_cyl(r, r if r_top is None else r_top, depth, seg)
    rims = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-6
            and abs(abs(e.verts[0].co.z) - depth / 2) < 1e-6
            and Vector((e.verts[0].co.x, e.verts[0].co.y)).length > 1e-4]
    bmesh.ops.bevel(bm, geom=rims, offset=bevel, segments=segs, affect='EDGES', profile=0.5, clamp_overlap=True)
    return bm


def g_torus(R, r, seg=32, rseg=10, arc=TAU, start=0.0):
    bm = bmesh.new()
    closed = arc >= TAU - 1e-6
    n = seg if closed else seg + 1
    rings = []
    for i in range(n):
        a = start + arc * i / seg
        c, s = math.cos(a), math.sin(a)
        ring = []
        for j in range(rseg):
            b = TAU * j / rseg
            rr = R + r * math.cos(b)
            ring.append(bm.verts.new((rr * c, rr * s, r * math.sin(b))))
        rings.append(ring)
    for i in range(seg):
        a, b = rings[i], rings[(i + 1) % n]
        for j in range(rseg):
            bm.faces.new((a[j], b[j], b[(j + 1) % rseg], a[(j + 1) % rseg]))
    if not closed:
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
    return bm


def g_cone(r, h, seg=16):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=True, segments=seg, radius1=r, radius2=0.0, depth=h)
    bmesh.ops.translate(bm, vec=(0, 0, h / 2), verts=bm.verts)
    return bm


def g_lathe(profile, seg=32):
    """Тело вращения по профилю [(радиус, z), ...] снизу вверх."""
    bm = bmesh.new()
    rings = []
    for (r, z) in profile:
        ring = []
        for i in range(seg):
            a = TAU * i / seg
            ring.append(bm.verts.new((r * math.cos(a), r * math.sin(a), z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(seg):
            bm.faces.new((a[i], a[(i + 1) % seg], b[(i + 1) % seg], b[i]))
    if profile[0][0] > 1e-5:
        bm.faces.new(list(reversed(rings[0])))
    if profile[-1][0] > 1e-5:
        bm.faces.new(rings[-1])
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    return bm


def g_extrude(points, depth):
    """Плоская фигура по контуру [(x, z)] толщиной depth по оси Y."""
    bm = bmesh.new()
    front = [bm.verts.new((x, -depth / 2, z)) for x, z in points]
    back = [bm.verts.new((x, depth / 2, z)) for x, z in points]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    n = len(points)
    for i in range(n):
        bm.faces.new((front[i], back[i], back[(i + 1) % n], front[(i + 1) % n]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def cut_plane(bm, co, no):
    """Ровный срез: убирает часть, в которую смотрит нормаль no."""
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-5,
                           plane_co=co, plane_no=no, clear_outer=True)
    return bm


def cut(bm, keep):
    """Удаляет вершины, для которых keep(co) ложно."""
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not keep(v.co)], context='VERTS')
    return bm


# ---------- сборка модели ----------

class Model:
    """Одна модель = один меш с несколькими материалами (удобно для Unity)."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []

    def add(self, g, matrix, material, smooth=True):
        if material not in self.mats:
            self.mats.append(material)
        mi = self.mats.index(material)
        bmesh.ops.transform(g, matrix=matrix, verts=g.verts)
        for f in g.faces:
            f.material_index = mi
            f.smooth = smooth
        me = bpy.data.meshes.new('_tmp')
        g.to_mesh(me)
        g.free()
        self.bm.from_mesh(me)
        bpy.data.meshes.remove(me)

    # удобные обёртки
    def sphere(self, loc, scale, m, rot=(0, 0, 0), seg=24, rings=14):
        self.add(g_sphere(seg, rings), M(loc, rot, scale), m)

    def limb(self, p1, p2, r, m, r2=None, seg=16):
        p1, p2 = Vector(p1), Vector(p2)
        d = p2 - p1
        rot = d.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        r2 = r if r2 is None else r2
        self.add(g_cyl(r, r2, d.length, seg), Matrix.Translation((p1 + p2) / 2) @ rot, m)
        self.sphere(p1, r, m, seg=seg, rings=10)
        self.sphere(p2, r2, m, seg=seg, rings=10)

    def finish(self, collection=None, loc=(0, 0, 0)):
        me = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(m)
        ob = bpy.data.objects.new(self.name, me)
        (collection or bpy.context.scene.collection).objects.link(ob)
        ob.location = loc
        return ob


def collection(name, parent=None):
    c = bpy.data.collections.get(name)
    if not c:
        c = bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(c)
    return c


def clear_collection(c):
    for ob in list(c.objects):
        me = ob.data
        bpy.data.objects.remove(ob)
        if me and me.users == 0 and isinstance(me, bpy.types.Mesh):
            bpy.data.meshes.remove(me)


def g_tube(points, radii, seg=12, ridge=0.0):
    """Гладкая трубка вдоль ломаной points с радиусами radii; ridge — рёбра (у банана 5 граней)."""
    bm = bmesh.new()
    pts = [Vector(p) for p in points]
    rings = []
    for i, (p, r) in enumerate(zip(pts, radii)):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = t.to_track_quat('Z', 'Y')
        ring = []
        for k in range(seg):
            a = TAU * k / seg
            rr = r * (1 - ridge * (0.5 + 0.5 * math.cos(a * 5)))
            ring.append(bm.verts.new(p + q @ Vector((math.cos(a) * rr, math.sin(a) * rr, 0))))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for k in range(seg):
            bm.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def g_blob(fn, seg=40, rings=28):
    """Гладкое тело из сферы: fn(Vector) -> Vector меняет каждую точку единичной сферы."""
    bm = g_sphere(seg, rings)
    for v in bm.verts:
        v.co = fn(v.co.copy())
    return bm
