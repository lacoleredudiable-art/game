"""Omurga: Kok -> Omurga_1 -> Omurga_2 -> Gogus. Gövdenin gövde (bacak/kuyruk dışı) ağırlığı Z'ye göre dört kemiğe
yumuşak dağıtılır (şapka fonksiyonları). python omurga_kemik.py -- <ejder.blend>"""
import sys
import bpy
import numpy as np
from mathutils import Vector
B = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.open_mainfile(filepath=B)
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
arm = bpy.data.objects["ejder_on"]; g = bpy.data.objects["ejder_govde"]
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm.data.edit_bones
o1 = eb.get("Omurga_1") or eb.new("Omurga_1"); o1.head = a2b((0, 43, 8.5)); o1.tail = a2b((0, 36, 9.0)); o1.parent = eb["Kok"]; o1.roll = 0
o2 = eb.get("Omurga_2") or eb.new("Omurga_2"); o2.head = a2b((0, 36, 9.0)); o2.tail = a2b((0, 29, 9.0)); o2.parent = o1; o2.use_connect = True; o2.roll = 0
eb["Gogus"].parent = o2
bpy.ops.object.mode_set(mode="OBJECT")
ZK = {"Kok": 47.0, "Omurga_1": 39.5, "Omurga_2": 32.0, "Gogus": 25.0}
names = list(ZK); zs = np.array([ZK[n] for n in names])
def hats(z):
    w = np.zeros(4)
    if z >= zs[0]: w[0] = 1; return w
    if z <= zs[-1]: w[-1] = 1; return w
    for i in range(3):
        if zs[i] >= z >= zs[i + 1]:
            t = (zs[i] - z) / (zs[i] - zs[i + 1]); w[i] = 1 - t; w[i + 1] = t; return w
for n in names[1:3]:
    if n not in g.vertex_groups: g.vertex_groups.new(name=n)
G = {n: g.vertex_groups[n] for n in names}
gi = {vg.index: vg.name for vg in g.vertex_groups}
cnt = np.zeros(4)
for v in g.data.vertices:
    T = sum(x.weight for x in v.groups if gi[x.group] in ("Kok", "Gogus", "Omurga_1", "Omurga_2"))
    if T <= 1e-4: continue
    w = hats(b2a(v.co)[1]) * T
    for k, n in enumerate(names):
        if w[k] > 1e-3: G[n].add([v.index], float(w[k]), "REPLACE"); cnt[k] += w[k] > 0.5
        else: G[n].remove([v.index])
print("RAPOR omurga kemikleri; gövde ağırlığı >0.5 köşe:", dict(zip(names, cnt.astype(int))))
# kanat kemikleri (varsa) omuz omurgasına bağlansın
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm.data.edit_bones
for b in eb:
    if b.name.startswith("Kanat_") and b.parent and b.parent.name == "Kok": b.parent = eb["Omurga_2"]
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.wm.save_as_mainfile(filepath=B)
