"""Omuz teşhisi: parçalar düz renk (bacak kırmızı, baş mavi, gövde gri, kalp/plaka sarı), sissiz, aydınlık.
python omuz_bak.py -- <blend> <out> <poz> cam..."""
import sys, math, bpy
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]; BLEND, OUT, POZ = a[:3]; CAMS = a[3:]
bpy.ops.wm.open_mainfile(filepath=BLEND)
arm = bpy.data.objects["ejder_on"]; ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for p in POZ.split(","):
    tr = ad.nla_tracks.new(); tr.strips.new(p, 1, bpy.data.actions["Poz_" + p])
COL = {"ejder_bacak_L": (0.8, 0.15, 0.1), "ejder_bacak_R": (0.8, 0.35, 0.1), "ejder_bas": (0.15, 0.3, 0.8),
       "ejder_govde": (0.5, 0.5, 0.5), "ejder_kalp": (0.9, 0.8, 0.1)}
for o in bpy.data.objects:
    if o.type != "MESH": continue
    c = COL.get(o.name, (0.9, 0.8, 0.1) if o.name.startswith("ejder_plaka") else (0.4, 0.6, 0.4))
    m = bpy.data.materials.new("dbg_" + o.name); m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*c, 1)
    o.data.materials.clear(); o.data.materials.append(m)
sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 800, 450
wd = bpy.data.worlds.new("w"); sc.world = wd; wd.use_nodes = True; wd.node_tree.nodes["Background"].inputs[1].default_value = 0.8
bpy.ops.object.light_add(type="SUN"); s = bpy.context.active_object; s.data.energy = 3; s.rotation_euler = (math.radians(40), 0, math.radians(200))
import os
for n in os.environ.get("GIZLE", "").split(","):
    if n in bpy.data.objects: bpy.data.objects[n].hide_render = True
s.rotation_euler = tuple(float(x) for x in os.environ.get("GUNES", "0.7,0,3.4").split(","))
sc.frame_set(1)
def A(x, z, h): return Vector((-x, -z, h))
for c in CAMS:
    cn, rest = c.split("="); pos, rest = rest.split(">"); look, fov = rest.split("@")
    p = [float(x) for x in pos.split(",")]; l = [float(x) for x in look.split(",")]
    bpy.ops.object.camera_add(location=A(*p)); cam = bpy.context.active_object
    cam.rotation_euler = (A(*l) - A(*p)).to_track_quat("-Z", "Y").to_euler(); cam.data.angle = math.radians(float(fov))
    sc.camera = cam; sc.render.filepath = f"{OUT}/omuz_{POZ.replace(',','+')}_{cn}.png"; bpy.ops.render.render(write_still=True)
