"""Gövdenin ön yarısında sırt çizgisini alçalt (karın yerde kalır): H' = H - d(Z)·min(H/15,1)·(1 - ön bacak ağırlığı),
d(Z) = DMAX·clip((ZP - Z)/(ZP - Z1)). Kalp/plaka kemik ve nesneleri aynı kayma ile iner.
python omurga.py -- <ejder.blend> <DMAX>"""
import sys
import bpy
import numpy as np
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index("--") + 1:]; B = a[0]; DMAX = float(a[1]); ZP, Z1 = 34.0, 22.0
bpy.ops.wm.open_mainfile(filepath=B)
def b2a(v): return np.array([-v.x, -v.y, v.z])
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def d(Z): return DMAX * np.clip((ZP - Z) / (ZP - Z1), 0, 1)
arm = bpy.data.objects["ejder_on"]; g = bpy.data.objects["ejder_govde"]
ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.view_layer.update()
gi = {vg.index: vg.name for vg in g.vertex_groups}
n0 = 0
for v in g.data.vertices:
    wl = sum(x.weight for x in v.groups if gi[x.group].startswith(("Omuz", "Kol_", "Pence")))
    p = b2a(v.co); dh = d(p[1]) * min(max(p[2], 0) / 15.0, 1.0) * (1 - min(wl, 1))
    if dh > 1e-4: p[2] -= dh; v.co = a2b(p); n0 += 1
g.data.update()
# kalp + plakalar
names = ["Kalp"] + [f"Plaka_{i}" for i in range(1, 5)]
objs = {"Kalp": bpy.data.objects["ejder_kalp"], **{f"Plaka_{i}": bpy.data.objects[f"ejder_plaka_{i}"] for i in range(1, 5)}}
hp = b2a(arm.data.bones["Kalp"].head_local); dh = float(d(hp[1]) * min(hp[2] / 15.0, 1.0))
wm = {n: o.matrix_world.copy() for n, o in objs.items()}
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm.data.edit_bones
for n in names: eb[n].head.z -= dh; eb[n].tail.z -= dh
bpy.ops.object.mode_set(mode="OBJECT"); bpy.context.view_layer.update()
for n, o in objs.items(): o.matrix_world = Matrix.Translation((0, 0, -dh)) @ wm[n]
print(f"RAPOR omurga: {n0} köşe alçaldı (en çok {DMAX} m, Z<{Z1} tam); kalp/plakalar {dh:.2f} m indi")
ad.action = bpy.data.actions.get("Poz_Yatis")
bpy.ops.wm.save_as_mainfile(filepath=B)
