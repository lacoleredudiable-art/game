"""Ağıza diş: dudak çizgisi (ejder_agiz_Mat ile baş malzemesi arasındaki kenar) boyunca konik, hafif geriye kıvrık dişler.
Üst sıra Bas'a, alt sıra Cene'ye bağlı; öndeki 2. diş köpek dişi. Baş malzemesini kullanır (UV: dokunun kemik
renkli, korsuz bir noktası) -> ek çizim çağrısı yok.
python disler.py -- <ejder.blend> <bas_albedo.png> <bas_kor.png>"""
import sys, math
import bpy, bmesh
import numpy as np
from mathutils import Vector
from PIL import Image

a = sys.argv[sys.argv.index("--") + 1:]
BLEND, ALB, KOR = a
bpy.ops.wm.open_mainfile(filepath=BLEND)
head = bpy.data.objects["ejder_bas"]; me = head.data
def b2a(v): return np.array([-v.x, -v.y, v.z])
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def nrm(v): v = np.asarray(v, float); return v / np.linalg.norm(v)

if head.get("disler"):
    raise SystemExit("dişler zaten var")
mats = [m.name for m in me.materials]; ai = mats.index("ejder_agiz_Mat")
gi = {g.name: g.index for g in head.vertex_groups}; cene = gi["Cene"]

bm = bmesh.new(); bm.from_mesh(me); dl = bm.verts.layers.deform.active
lip = {"ust": [], "alt": []}
for e in bm.edges:
    fs = e.link_faces
    if len(fs) == 2 and (fs[0].material_index == ai) != (fs[1].material_index == ai):
        for v in e.verts:
            p = b2a(head.matrix_world @ v.co); w = v[dl].get(cene, 0.0)
            lip["alt" if w > 0.5 else "ust"].append(p)
bm.free()
for k in lip: lip[k] = np.unique(np.round(np.array(lip[k]), 4), axis=0)
print("RAPOR dudak köşesi:", {k: len(v) for k, v in lip.items()})

# UV: albedo'da kemik rengine (#5B5A57) en yakın, korsuz, düzgün bir nokta
A = np.asarray(Image.open(ALB).convert("RGB"), float); K = np.asarray(Image.open(KOR).convert("RGB"), float)
tgt = np.array([0x5B, 0x5A, 0x57], float)
d = np.linalg.norm(A - tgt, axis=2) + (K.sum(2) > 0) * 1e3
from scipy.ndimage import uniform_filter, minimum_filter
dd = uniform_filter(d, 7) + (minimum_filter(-(K.sum(2) == 0).astype(float), 7) > -1) * 1e3
iy, ix = np.unravel_index(np.argmin(dd), dd.shape); SZ = A.shape[0]
UVT = ((ix + 0.5) / SZ, 1 - (iy + 0.5) / SZ)
print("RAPOR diş UV", UVT, "renk", A[iy, ix])

V, F, G = [], [], []
def tooth(base, down, length, r, inward, bone):
    """base: dudak noktası; down: diş yönü; hafif geriye (+Z) ve içe kıvrık."""
    ax = nrm(down); side = nrm(np.cross(ax, [0, 1.0, 0])) if abs(ax[1]) < 0.9 else np.array([1.0, 0, 0])
    up = np.cross(side, ax)
    c0 = base - ax * 0.25 * length + inward * 0.12      # köke gömülü
    ring = lambda c, rr: [c + rr * (math.cos(2 * math.pi * k / 6) * side + math.sin(2 * math.pi * k / 6) * up) for k in range(6)]
    mid = c0 + ax * 0.6 * length + np.array([0, 0.10 * length, 0]) + inward * 0.05
    tip = c0 + ax * length + np.array([0, 0.25 * length, 0]) + inward * 0.08
    i0 = len(V); V.extend(ring(c0, r)); V.extend(ring(mid, r * 0.55)); V.append(tip)
    tf = []
    for k in range(6):
        k1 = (k + 1) % 6
        tf.append((i0 + k, i0 + k1, i0 + 6 + k1, i0 + 6 + k)); tf.append((i0 + 6 + k, i0 + 6 + k1, i0 + 12))
    q = tf[0]; n = np.cross(V[q[1]] - V[q[0]], V[q[3]] - V[q[0]])
    if n @ (V[q[0]] - c0) < 0: tf = [tuple(reversed(f)) for f in tf]
    F.extend(tf)
    G.extend([bone] * 13)

n_t = 0
for row, bone, sign in (("ust", "Bas", -1), ("alt", "Cene", 1)):
    P = lip[row]
    for sx in (-1, 1):
        S = P[np.sign(P[:, 0]) == sx]
        if len(S) < 4: continue
        S = S[np.argsort(S[:, 1])]
        z0, z1 = S[0, 1], S[-1, 1]
        n = 9 if row == "ust" else 8
        off = 0.0 if row == "ust" else 0.5
        for i in range(n):
            z = z0 + (z1 - z0) * (0.04 + 0.86 * (i + off) / n)
            j = np.argmin(np.abs(S[:, 1] - z)); p = S[j]
            inward = np.array([-sx, 0, 0.0]) * 1.0
            fang = (i == 1 and row == "ust") or (i == 0 and row == "alt")
            L = (1.5 if fang else 0.95 - 0.45 * i / n) * (1.0 if row == "ust" else 0.85)
            r = 0.26 if fang else 0.17
            down = np.array([0.12 * -sx, 0.0, sign * 1.0])
            tooth(p, down, L, r, inward * 0.6, bone); n_t += 1

mt = bpy.data.meshes.new("ejder_dis"); mt.from_pydata([tuple(a2b(v)) for v in V], [], [f for f in F if f])
mt.update(); ob = bpy.data.objects.new("ejder_dis", mt); bpy.context.scene.collection.objects.link(ob)
uvl = mt.uv_layers.new(name="UVMap")
for l in uvl.data: l.uv = UVT
for b in ("Bas", "Cene"):
    vg = ob.vertex_groups.new(name=b); vg.add([i for i, g in enumerate(G) if g == b], 1.0, "REPLACE")
ob.vertex_groups.new(name="ejder_dis").add(list(range(len(V))), 1.0, "REPLACE")
mt.materials.append(me.materials[0])
ca = mt.color_attributes.new("AgizDerinlik", "BYTE_COLOR", "POINT") if "AgizDerinlik" in me.color_attributes else None
if ca:
    for c in ca.data: c.color = (0, 0, 0, 1)
for o in bpy.context.view_layer.objects: o.select_set(False)
ob.select_set(True); head.select_set(True); bpy.context.view_layer.objects.active = head
bpy.ops.object.join()
head["disler"] = 1
head.vertex_groups.remove(head.vertex_groups["ejder_dis"])
tri = sum(len(p.vertices) - 2 for p in head.data.polygons)
print(f"RAPOR {n_t} diş, {len(F)} yüz eklendi; baş toplam {tri} üçgen")
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
