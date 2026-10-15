"""Göğüs önünde kalp oyuğu: yüzeyde elips açıklık -> içe 1.8 m koyu çanak (ejder_ic_Mat), kalp çanağın içine,
4 plaka açıklığın önüne (menteşeler X ±2.35). Kalp/Plaka kemikleri ve nesneleri birlikte taşınır.
python kalp_yer.py -- <ejder.blend> <H merkez>"""
import sys, math
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
a = sys.argv[sys.argv.index("--") + 1:]; B = a[0]; HC = float(a[1])
bpy.ops.wm.open_mainfile(filepath=B)
def b2a(v): return np.array([-v.x, -v.y, v.z])
def a2b(p): return Vector((-p[0], -p[1], p[2]))
arm = bpy.data.objects["ejder_on"]; g = bpy.data.objects["ejder_govde"]
ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.view_layer.update()

bm = bmesh.new(); bm.from_mesh(g.data); bm.normal_update()
tree = BVHTree.FromBMesh(bm)
hit = tree.ray_cast(a2b((0, 0, HC)), a2b((0, 1, 0)) - a2b((0, 0, 0)))
ZS = b2a(hit[0])[1]; print(f"RAPOR göğüs yüzeyi X0 H{HC}: Z {ZS:.2f}")
RX, RH = 2.5, 2.4
dead = []
for f in bm.faces:
    c = b2a(f.calc_center_median()); n = b2a(f.normal)
    if ((c[0] / RX) ** 2 + ((c[2] - HC) / RH) ** 2) < 1 and c[1] < ZS + 2.5 and n[1] < -0.2: dead.append(f)
bmesh.ops.delete(bm, geom=dead, context="FACES")
loose = [v for v in bm.verts if not v.link_faces]; bmesh.ops.delete(bm, geom=loose, context="VERTS")
# açıklığın sınır döngüsü: merkeze en yakın döngü
bnd = [e for e in bm.edges if e.is_boundary]
adj = {}
for e in bnd:
    x, y = e.verts; adj.setdefault(x, []).append(y); adj.setdefault(y, []).append(x)
seen = set(); loops = []
for s in adj:
    if s in seen: continue
    loop = [s]; seen.add(s); prev = None; cur = s
    while True:
        nx = [q for q in adj[cur] if q is not prev and q not in seen]
        if not nx: break
        prev, cur = cur, nx[0]; loop.append(cur); seen.add(cur)
    loops.append(loop)
cen = np.array([0, ZS, HC])
loop = min(loops, key=lambda L: np.linalg.norm(np.mean([b2a(v.co) for v in L], 0) - cen))
mats = [m.name for m in g.data.materials]
icm = bpy.data.materials.get("ejder_ic_Mat")
if icm is None:
    icm = bpy.data.materials.new("ejder_ic_Mat"); icm.use_nodes = True
    icm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.010, 0.011, 0.014, 1)
    icm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.95
if "ejder_ic_Mat" not in mats: g.data.materials.append(icm); mats.append("ejder_ic_Mat")
ic = mats.index("ejder_ic_Mat")
dl = bm.verts.layers.deform.verify(); uvl = bm.loops.layers.uv.active
P = np.array([b2a(v.co) for v in loop]); c = P.mean(0)
DEPTH = 1.8
ring = []
for v, p in zip(loop, P):
    q = c + (p - c) * 0.75 + np.array([0, DEPTH, 0]); nv = bm.verts.new(a2b(q))
    for k, w in v[dl].items(): nv[dl][k] = w
    ring.append(nv)
cv = bm.verts.new(a2b(c + np.array([0, DEPTH + 0.6, 0])))
for k, w in loop[0][dl].items(): cv[dl][k] = w
m = len(loop); newf = []
for i in range(m):
    j = (i + 1) % m
    for vs in ([loop[i], loop[j], ring[j], ring[i]], [ring[i], ring[j], cv]):
        try: newf.append(bm.faces.new(vs))
        except ValueError: pass
uv0 = loop[0].link_loops[0][uvl].uv.copy()
for f in newf:
    f.material_index = ic
    for l in f.loops: l[uvl].uv = uv0
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(g.data); bm.free()
print(f"RAPOR oyuk: {len(dead)} yüz açıldı, {m} kenarlı ağız, çanak {len(newf)} yüz, derinlik {DEPTH}")

# kalp ve plakaları taşı
HEART_OLD = b2a(arm.data.bones["Kalp"].head_local)
HEART_NEW = np.array([0.0, ZS + 1.0, HC])
d = HEART_NEW - HEART_OLD; d[1] += 0.0
# plakalar yüzeyin hemen önünde olsun: eski düzende plaka merkezi kalpten 1.7 önde; yeni: kalpten 1.0+0.6 önde
names = ["Kalp"] + [f"Plaka_{i}" for i in range(1, 5)]
objs = {"Kalp": bpy.data.objects["ejder_kalp"]}
for i in range(1, 5): objs[f"Plaka_{i}"] = bpy.data.objects[f"ejder_plaka_{i}"]
wm = {n: o.matrix_world.copy() for n, o in objs.items()}
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm.data.edit_bones
for n in names:
    dd = d.copy()
    if n.startswith("Plaka"): dd[1] += 0.1
    eb[n].head = eb[n].head + (a2b(dd) - a2b((0, 0, 0))); eb[n].tail = eb[n].tail + (a2b(dd) - a2b((0, 0, 0)))
bpy.ops.object.mode_set(mode="OBJECT"); bpy.context.view_layer.update()
for n, o in objs.items():
    dd = d.copy()
    if n.startswith("Plaka"): dd[1] += 0.1
    o.matrix_world = Matrix.Translation(a2b(dd) - a2b((0, 0, 0))) @ wm[n]
bpy.context.view_layer.update()
pc = np.mean([b2a(objs[f"Plaka_{i}"].matrix_world.translation) for i in range(1, 5)], 0)
print(f"RAPOR kalp {np.round(b2a(objs['Kalp'].matrix_world.translation),2)}, plaka ortası {np.round(pc,2)} (yüzey Z {ZS:.2f})")
ad.action = bpy.data.actions["Poz_Yatis"]
bpy.ops.wm.save_as_mainfile(filepath=B)
