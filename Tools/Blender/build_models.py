# Сборка всех моделей «Последней ставки»: генерация → FBX (персонажи — Assets/Resources/Models, остальное —
# Assets/Models) → обзорные картинки в Docs.
#
# Запуск без интерфейса:
#   blender --background --factory-startup --python Tools/Blender/build_models.py
# или из открытого Blender (Scripting → Run Script), или через MCP: exec(open(путь).read()).
#
# Модели стоят на z = 0, лицом к -Y Blender; в Unity после импорта смотрят вдоль +Z (transform.forward).
import os, sys, math
import bpy, mathutils

HERE = os.path.dirname(os.path.abspath(__file__)) if '__file__' in globals() else r"C:\AI\LastBet\Tools\Blender"
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import importlib
import lb_core, lb_characters, lb_props
for mod in (lb_core, lb_characters, lb_props):
    importlib.reload(mod)
C = lb_core

# Персонажи — в Resources: игра грузит их сама (Stage3D); остальное пока не используется и в сборку не попадает
OUT = {'Characters': os.path.join(C.ROOT, 'Assets', 'Resources', 'Models', 'Characters')}
OUT_OTHER = os.path.join(C.ROOT, 'Assets', 'Models')
DOCS = os.path.join(C.ROOT, 'Docs')
GROUPS = ('Characters', 'Items', 'Casino', 'World', 'Produce')


def setup_scene():
    sc = bpy.data.scenes.get('LastBet') or bpy.data.scenes.new('LastBet')
    if bpy.context.window:
        bpy.context.window.scene = sc
    try:
        sc.render.engine = 'BLENDER_EEVEE'
    except TypeError:
        pass
    vt = [i.identifier for i in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items]
    sc.view_settings.view_transform = 'AgX' if 'AgX' in vt else 'Filmic'
    if hasattr(sc, 'eevee') and hasattr(sc.eevee, 'taa_render_samples'):
        sc.eevee.taa_render_samples = 128
    w = bpy.data.worlds.get('LB_World') or bpy.data.worlds.new('LB_World')
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND')
    bg.inputs[0].default_value = (0.05, 0.045, 0.09, 1)   # тёмно-фиолетовый — как фон интерфейса игры
    bg.inputs[1].default_value = 1.2
    sc.world = w
    return sc


def export_fbx(ob, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    loc = ob.location.copy()
    # герой — пустой родитель с частями-детьми (одежда, вещи): выгружаются все, в Unity включаются по надетому
    every = [ob] + list(ob.children)
    hidden = [(o.hide_viewport, o.hide_render) for o in every]
    for o in every:
        o.hide_viewport = o.hide_render = False
    ob.location = (0, 0, 0)
    bpy.ops.object.select_all(action='DESELECT')
    for o in every:
        o.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False,
                             path_mode='STRIP')
    ob.location = loc
    for o, h in zip(every, hidden):
        o.hide_viewport, o.hide_render = h


def preview(sc, obs, path, cols, cell, view=(0.0, -1.0, 0.18), res=(1800, 1000), fit=False):
    """Обзорная картинка группы: сетка моделей, три источника света, ортокамера.
    fit — каждый предмет масштабируется под ячейку (как иконки); персонажи — в настоящем росте."""
    stash = {o: (o.location.copy(), o.hide_render, o.scale.copy()) for g in GROUPS for o in bpy.data.collections[g].objects}
    for g in GROUPS:
        for o in bpy.data.collections[g].objects:
            o.hide_render = True
    rows = math.ceil(len(obs) / cols)
    for i, o in enumerate(obs):
        o.hide_render = False
        for ch in o.children:
            ch.hide_render = not ch.name.endswith(('__base', '_default'))
        o.location = ((i % cols - (cols - 1) / 2) * cell[0], 0, -(i // cols) * cell[1])
        if fit:     # предмет — по центру ячейки (dimensions обновляется не сразу — считаем по bound_box)
            bb = [mathutils.Vector(v) for v in o.bound_box]
            lo = mathutils.Vector([min(v[a] for v in bb) for a in range(3)])
            hi = mathutils.Vector([max(v[a] for v in bb) for a in range(3)])
            k = min(cell[0], cell[1]) * 0.72 / max(hi - lo)
            o.scale = (k, k, k)
            mid = (lo + hi) / 2 * k
            o.location -= mathutils.Vector((mid.x, mid.y, mid.z - cell[1] * 0.42))
    tmp = []
    cam_d = bpy.data.cameras.new('_cam')
    cam_d.type = 'ORTHO'
    w, h = cols * cell[0], rows * cell[1]
    cam_d.ortho_scale = max(w, h * res[0] / res[1]) * 1.05
    cam = bpy.data.objects.new('_cam', cam_d)
    sc.collection.objects.link(cam)
    tmp.append(cam)
    d = mathutils.Vector(view).normalized()
    centre = mathutils.Vector((0, 0, -(rows - 1) * cell[1] / 2 + cell[1] * 0.42))
    cam.location = centre + d * 30          # view — направление от моделей к камере
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    for name, loc, energy, col in (('_key', (-6, -10, 9), 5.0, (1, 0.95, 0.88)), ('_fill', (8, -6, 3), 1.6, (0.75, 0.8, 1)),
                                   ('_rim', (0, 8, 7), 3.0, (0.85, 0.6, 1))):
        L = bpy.data.lights.new(name, 'SUN')
        L.energy = energy
        L.color = col
        L.angle = math.radians(12)
        o = bpy.data.objects.new(name, L)
        o.rotation_euler = (-mathutils.Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        sc.collection.objects.link(o)
        tmp.append(o)
    studio = bpy.data.collections.get('Studio')
    if studio:
        studio.hide_render = True
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    sc.render.filepath = path
    sc.render.image_settings.file_format = 'PNG'
    bpy.ops.render.render(write_still=True, scene=sc.name)
    for o in tmp:
        data = o.data
        bpy.data.objects.remove(o)
        (bpy.data.cameras if isinstance(data, bpy.types.Camera) else bpy.data.lights).remove(data)
    if studio:
        studio.hide_render = False
    for o, (loc, hr, scale) in stash.items():
        o.location, o.hide_render, o.scale = loc, hr, scale


ICONS = os.path.join(C.ROOT, 'Assets', 'Resources', 'Icons')
ITEM_IDS = {'item_tshirt': 'tshirt', 'item_jeans': 'jeans', 'item_hoodie': 'hoodie', 'item_sneakers': 'sneakers',
            'item_headphones': 'headphones', 'item_watch': 'watch', 'item_smartphone': 'smartphone', 'item_backpack': 'backpack'}


def render_icons(sc, items, produce, size=256, view=(0.5, -1.0, 0.55)):
    """Иконки с прозрачным фоном: Resources/Icons/item_<id>.png и product_<номер>.png (GameArt берёт их вместо рисунка)."""
    os.makedirs(ICONS, exist_ok=True)
    every = [o for g in GROUPS + ('Produce',) if bpy.data.collections.get(g) for o in bpy.data.collections[g].objects]
    stash = {o: o.hide_render for o in every}
    studio = bpy.data.collections.get('Studio')
    if studio:
        studio.hide_render = True
    cam_d = bpy.data.cameras.new('_icam')
    cam_d.type = 'ORTHO'
    cam = bpy.data.objects.new('_icam', cam_d)
    sc.collection.objects.link(cam)
    tmp = [cam]
    for name, loc, energy, col in (('_ik', (-5, -8, 9), 4.0, (1, 0.96, 0.9)), ('_if', (8, -5, 2), 1.5, (0.8, 0.85, 1)),
                                   ('_ir', (2, 8, 6), 2.5, (0.9, 0.7, 1))):
        L = bpy.data.lights.new(name, 'SUN')
        L.energy, L.color, L.angle = energy, col, math.radians(15)
        o = bpy.data.objects.new(name, L)
        o.rotation_euler = (-mathutils.Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        sc.collection.objects.link(o)
        tmp.append(o)
    sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    d = mathutils.Vector(view).normalized()
    rot = (-d).to_track_quat('-Z', 'Y')
    right, up = rot @ mathutils.Vector((1, 0, 0)), rot @ mathutils.Vector((0, 1, 0))
    jobs = [(o, ITEM_IDS[o.name] and 'item_' + ITEM_IDS[o.name]) for o in items if o.name in ITEM_IDS]
    jobs += [(o, 'product_%d' % i) for i, o in enumerate(produce)]
    for ob, fname in jobs:
        for o in every:
            o.hide_render = o is not ob
        corners = [ob.matrix_world @ mathutils.Vector(c) for c in ob.bound_box]
        cx = sum(corners, mathutils.Vector()) / 8
        xs = [(c - cx).dot(right) for c in corners]
        ys = [(c - cx).dot(up) for c in corners]
        cam_d.ortho_scale = max(max(xs) - min(xs), max(ys) - min(ys)) * 1.12
        cam.location = cx + right * (max(xs) + min(xs)) / 2 + up * (max(ys) + min(ys)) / 2 + d * 20
        cam.rotation_euler = rot.to_euler()
        sc.render.filepath = os.path.join(ICONS, fname + '.png')
        bpy.ops.render.render(write_still=True, scene=sc.name)
    for o in tmp:
        data = o.data
        bpy.data.objects.remove(o)
        (bpy.data.cameras if isinstance(data, bpy.types.Camera) else bpy.data.lights).remove(data)
    for o, h in stash.items():
        o.hide_render = h
    sc.render.film_transparent = False
    if studio:
        studio.hide_render = False


def main(export=True, previews=True, save=True):
    sc = setup_scene()
    chars = lb_characters.build_all()
    props = lb_props.build_all()
    groups = {'Characters': chars, **props}
    for g in GROUPS:
        for o in bpy.data.collections[g].objects:
            o.hide_viewport = o.hide_render = False
    if export:
        render_icons(sc, props['Items'], props['Produce'])
        for g, obs in groups.items():
            if g == 'Produce':
                continue        # продукты нужны только как иконки
            for o in obs:
                export_fbx(o, os.path.join(OUT.get(g) or os.path.join(OUT_OTHER, g), o.name + '.fbx'))
    if previews:
        preview(sc, chars, os.path.join(DOCS, 'models_characters.png'), 10, (0.78, 1.6), res=(2000, 1300))
        preview(sc, props['Items'] + props['Casino'] + props['World'], os.path.join(DOCS, 'models_props.png'), 8,
                (1.0, 1.0), view=(0.35, -1.0, 0.4), res=(2000, 1000), fit=True)
    if save:
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, 'lastbet_models.blend'), copy=True)
    print('models:', {g: len(v) for g, v in groups.items()})
    return groups


if __name__ == '__main__':
    main()
