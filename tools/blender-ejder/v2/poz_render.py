"""ejder.blend'i bir ya da birkaç poz üst üste (NLA) ile çizer.
python poz_render.py -- <blend> <çıkış> <ad> <pozlar,virgüllü> <faz:R,B,G> <kamera...>
kamera: 'ad=x,z,h>x,z,h@fov'  (arena koordinatı)"""
import sys, math
import bpy
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]
BLEND, OUT, NAME, POZ, FAZ = a[:5]; CAMS = a[5:]
bpy.ops.wm.open_mainfile(filepath=BLEND)
sc = bpy.context.scene
def A(x, z, y=0.0): return Vector((-x, -z, y))
def hexcol(h):
    h = h.lstrip("#"); c = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)
arm = bpy.data.objects["ejder_on"]
ad = arm.animation_data or arm.animation_data_create(); ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
FRAME = 1
for p in [p for p in POZ.split(",") if p]:
    if "@" in p: p, fr = p.split("@"); FRAME = int(fr)
    act = [x for x in bpy.data.actions if x.name.endswith(p if p.startswith("Klip_") else "Poz_" + p)][0]
    tr = ad.nla_tracks.new(); st = tr.strips.new(p, 1, act); st.blend_type = "REPLACE"
fz = [float(x) for x in FAZ.split(",")]; r, b, g = fz[:3]; ak = fz[3] if len(fz) > 3 else 0.0
for m in bpy.data.materials:
    if m.use_nodes and "agizKor" in m.node_tree.nodes: m.node_tree.nodes["agizKor"].outputs[0].default_value = ak
for m in bpy.data.materials:
    if m.use_nodes and "fazR" in m.node_tree.nodes:
        m.node_tree.nodes["fazR"].inputs[1].default_value = r
        m.node_tree.nodes["fazB"].inputs[1].default_value = b
        m.node_tree.nodes["goz"].inputs[1].default_value = g
gm = bpy.data.materials.new("zemin"); gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*hexcol("#26282C"), 1)
bpy.ops.mesh.primitive_circle_add(vertices=64, radius=150, fill_type="NGON"); bpy.context.active_object.data.materials.append(gm)
bpy.ops.object.light_add(type="SUN"); sun = bpy.context.active_object
sun.data.energy = 3.0; sun.data.color = hexcol("#F2C79A")
az, el = math.radians(315), math.radians(20)
d = Vector((-math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
sun.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
wd = bpy.data.worlds.new("w"); sc.world = wd; wd.use_nodes = True
wd.node_tree.nodes["Background"].inputs[0].default_value = (*hexcol("#4A5260"), 1)
wd.node_tree.nodes["Background"].inputs[1].default_value = 0.6
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 16; sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = 800, 450
sc.view_layers[0].use_pass_z = True
sc.render.image_settings.media_type = "MULTI_LAYER_IMAGE"; sc.render.image_settings.file_format = "OPEN_EXR_MULTILAYER"
sc.frame_set(FRAME)
for c in CAMS:
    cn, rest = c.split("="); pos, rest = rest.split(">"); look, fov = rest.split("@")
    p = [float(x) for x in pos.split(",")]; l = [float(x) for x in look.split(",")]
    bpy.ops.object.camera_add(location=A(*p)); cam = bpy.context.active_object
    cam.rotation_euler = (A(*l) - A(*p)).to_track_quat("-Z", "Y").to_euler()
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle = math.radians(float(fov)); cam.data.clip_end = 500
    sc.camera = cam; sc.render.filepath = f"{OUT}/{NAME}_{cn}.exr"; bpy.ops.render.render(write_still=True)
