"""ejder_govde.blend + gri kutu yer tutucuları -> oyun kamerasından ve üstten önizleme."""
import sys, math
import bpy
from mathutils import Vector

_a = sys.argv[sys.argv.index("--") + 1:]; BLEND, OUT = _a[:2]; EXTRA = _a[2:]
bpy.ops.wm.open_mainfile(filepath=BLEND)
sc = bpy.context.scene

def A(x, z, y=0.0):            # arena (X doğu, Z kuzey, Y yukarı) -> Blender (doğu = -X)
    return Vector((-x, -z, y))

def hexcol(h, a=1.0):
    h = h.lstrip("#"); c = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    c = [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]
    return (*c, a)

def mat(name, hexc, emis=None, strength=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = hexcol(hexc)
    b.inputs["Roughness"].default_value = 0.9
    if emis:
        b.inputs["Emission Color"].default_value = hexcol(emis)
        b.inputs["Emission Strength"].default_value = strength
    return m

def box(name, cx, cz, w, d, h, m, y0=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=A(cx, cz, y0 + h / 2))
    o = bpy.context.active_object; o.name = name
    o.scale = (w, d, h); o.data.materials.append(m); return o

stone = mat("tas", "#5E6672"); rock = mat("kaya", "#3A4048"); kor = mat("kor", "#FF7A1A", "#FF7A1A", 8)
gnd = mat("zemin", "#26282C"); ply = mat("oyuncu", "#A9A49A")

# zemin
bpy.ops.mesh.primitive_circle_add(vertices=96, radius=120, fill_type="NGON", location=(0, 0, 0))
bpy.context.active_object.data.materials.append(gnd)
# duvar halkası (kaya, 7 m)
bpy.ops.mesh.primitive_cylinder_add(vertices=96, radius=20, depth=7, location=(0, 0, 3.5))
w = bpy.context.active_object; w.data.materials.append(rock)
bpy.ops.mesh.primitive_cylinder_add(vertices=96, radius=18, depth=8, location=(0, 0, 3.5))
cut = bpy.context.active_object
mod = w.modifiers.new("b", "BOOLEAN"); mod.object = cut; mod.operation = "DIFFERENCE"
bpy.context.view_layer.objects.active = w; bpy.ops.object.modifier_apply(modifier="b")
bpy.data.objects.remove(cut)
# kuzey sektörü duvarı kaldır (gövde orada duvar) : 330..30 derece
bpy.ops.mesh.primitive_cube_add(size=1, location=A(0, 20, 3.5)); k = bpy.context.active_object; k.scale = (21, 8, 10)
mod = w.modifiers.new("b", "BOOLEAN"); mod.object = k; mod.operation = "DIFFERENCE"
bpy.context.view_layer.objects.active = w; bpy.ops.object.modifier_apply(modifier="b"); bpy.data.objects.remove(k)

# taşlar
for (x, z, h) in [(-9, 4, 8), (9, 4, 7), (0, -10, 6)]:
    box("tas", x, z, 2, 1.5, h, stone)
# baş: gerçek model varsa onu al, yoksa yer tutucu
POZ = "Poz_Yatis"
for fp in EXTRA:
    if fp.startswith("poz="): POZ = fp[4:]; continue
    bpy.ops.import_scene.fbx(filepath=fp)
for o in bpy.data.objects:
    if o.type == "ARMATURE" and o.animation_data:
        acts = [a for a in bpy.data.actions if a.name.endswith(POZ)]
        if acts: o.animation_data.action = acts[0]
EXTRA = [e for e in EXTRA if not e.startswith("poz=")]
if not EXTRA:
    box("bas", 0, 12, 6, 9, 7, rock)
if not EXTRA:
    for x in (-7, 7):
        box("pence", x, 9, 3, 4, 4, rock)
if not EXTRA:
    for x in (-1.6, 1.6):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=A(x, 7.6, 4.8)); bpy.context.active_object.data.materials.append(kor)
# oyuncu 1,78 m
bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=1.78, location=A(-3, -9, 0.89))
bpy.context.active_object.data.materials.append(ply)

# ışık: SI-3.1 güneş azimut 315°, yükseklik 20°
bpy.ops.object.light_add(type="SUN"); sun = bpy.context.active_object
sun.data.energy = 3.0; sun.data.color = hexcol("#F2C79A")[:3]
az, el = math.radians(315), math.radians(20)
d = Vector((-math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))  # güneşe doğru yön
sun.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
# dünya: sis rengi + hacimsel sis (SI-4.1, yoğunluk 0,012)
wd = bpy.data.worlds.new("w"); sc.world = wd; wd.use_nodes = True
nt = wd.node_tree; bg = nt.nodes["Background"]; bg.inputs[0].default_value = hexcol("#4A5260"); bg.inputs[1].default_value = 0.6
# sis: render sonrası derinlikten üstel (SI-4.1): f = 1 - exp(-0.012 d)
sc.view_layers[0].use_pass_z = True

sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 16
sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = 1280, 640
sc.view_settings.view_transform = "AgX"

def shot(name, loc, look, fov_v=55, ortho=None):
    bpy.ops.object.camera_add(location=loc); cam = bpy.context.active_object
    cam.rotation_euler = (look - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    if ortho:
        cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho
    else:
        cam.data.sensor_fit = "VERTICAL"; cam.data.angle = math.radians(fov_v)
    cam.data.clip_end = 400
    sc.camera = cam
    sc.render.image_settings.media_type = "MULTI_LAYER_IMAGE"; sc.render.image_settings.file_format = "OPEN_EXR_MULTILAYER"
    sc.render.filepath = f"{OUT}/{name}.exr"
    bpy.ops.render.render(write_still=True)

# oyun kamerası: oyuncu (0,-14), 6 m geride, 30° eğim, göğüs 1,2 m (O-5.1–5.4)
p = A(-3, -9, 1.2); fwd = Vector((0, -math.cos(math.radians(30)), -math.sin(math.radians(30))))  # arena kuzey = Blender -Y
cam_loc = p - fwd * 6
shot("oyun_kamerasi_baslangic", cam_loc, cam_loc + fwd * 10)
# kilitli kamera: boss başına bakar
shot("oyun_kamerasi_kilitli", cam_loc, A(0, 12, 6))
shot("genel", A(-38, -30, 22), A(0, 18, 7), fov_v=45)
shot("yandan", A(-40, 12, 6), A(0, 18, 7), fov_v=50)
