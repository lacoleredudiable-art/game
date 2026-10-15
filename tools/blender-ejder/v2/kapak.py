"""(a) Baş mesh'inin gövde içinde kalan boyun kökünü (dinlenmede Z > ZCUT) sil, kapat.
(b) Gövdedeki büyük açık döngüleri (kütük kesiği) kubbe kapakla ört: halka + merkez, dışa bombe, komşu UV, en yakın sınır
köşesinin ağırlıkları. python kapak.py -- <ejder.blend> <ZCUT>"""
import sys
import bpy, bmesh
import numpy as np
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]; B = a[0]; ZCUT = float(a[1])
bpy.ops.wm.open_mainfile(filepath=B)
def b2a(v): return np.array([-v.x, -v.y, v.z])
def a2b(p): return Vector((-p[0], -p[1], p[2]))

# (a) boyun kökü
h = bpy.data.objects["ejder_bas"]; bm = bmesh.new(); bm.from_mesh(h.data)
dead = [v for v in bm.verts if b2a(h.matrix_world @ v.co)[1] > ZCUT]
n0 = len(bm.faces); bmesh.ops.delete(bm, geom=dead, context="VERTS")
uvl = bm.loops.layers.uv.active; before = set(bm.faces)
res = bmesh.ops.holes_fill(bm, edges=[e for e in bm.edges if e.is_boundary], sides=0)
tr = bmesh.ops.triangulate(bm, faces=res["faces"])
for f in tr["faces"]:
    for l in f.loops:
        oth = [x for x in l.vert.link_loops if x.face in before]
        if oth: src = oth[0][uvl].uv.copy(); break
    for l in f.loops: l[uvl].uv = src
    f.material_index = 0
bm.to_mesh(h.data); bm.free()
print(f"RAPOR baş: boyun kökü kesildi Z>{ZCUT}: {n0} -> {len(h.data.polygons)} yüz")

# (b) gövde kapakları
g = bpy.data.objects["ejder_govde"]; me = g.data
bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
dl = bm.verts.layers.deform.verify(); uvl = bm.loops.layers.uv.active
bnd = [e for e in bm.edges if e.is_boundary]
adj = {}
for e in bnd:
    a_, b_ = e.verts; adj.setdefault(a_, []).append(b_); adj.setdefault(b_, []).append(a_)
seen = set(); loops = []
for s in adj:
    if s in seen: continue
    # sıralı döngü yürüyüşü
    loop = [s]; seen.add(s); prev = None; cur = s
    while True:
        nxt = [x for x in adj[cur] if x is not prev and x not in seen]
        if not nxt: break
        prev, cur = cur, nxt[0]; loop.append(cur); seen.add(cur)
    loops.append(loop)
loops.sort(key=len, reverse=True)
made = 0
for loop in loops:
    if len(loop) < 3: continue
    P = np.array([b2a(v.co) for v in loop]); c = P.mean(0); rad = np.linalg.norm(P - c, axis=1).mean()
    if rad < 0.3:
        try: bmesh.ops.contextual_create(bm, geom=loop)
        except Exception: pass
        continue
    # döngü normali: PCA en küçük eksen, gövde merkezinden dışa yönlü
    _, _, Vt = np.linalg.svd(P - c); n = Vt[2]
    if n @ (c - np.array([0, 32.0, 6.0])) < 0: n = -n
    lf = [l for v in loop for l in v.link_loops]
    src_uv = lf[0][uvl].uv.copy() if lf else None
    ring = []
    for v, p in zip(loop, P):
        q = c + (p - c) * 0.55 + n * rad * 0.22
        nv = bm.verts.new(a2b(q)); nv[dl].clear()
        for k, w in v[dl].items(): nv[dl][k] = w
        ring.append(nv)
    cv = bm.verts.new(a2b(c + n * rad * 0.32))
    for k, w in loop[0][dl].items(): cv[dl][k] = w
    newf = []
    m = len(loop)
    for i in range(m):
        j = (i + 1) % m
        try: newf.append(bm.faces.new([loop[i], loop[j], ring[j], ring[i]]))
        except ValueError: pass
        try: newf.append(bm.faces.new([ring[i], ring[j], cv]))
        except ValueError: pass
    for f in newf:
        if src_uv is not None:
            for l in f.loops: l[uvl].uv = src_uv
    made += 1
    print(f"RAPOR kapak: {m} kenarlı döngü, merkez {np.round(c,1)}, yarıçap {rad:.2f}")
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
left = sum(1 for e in bm.edges if e.is_boundary)
bm.to_mesh(me); bm.free()
print(f"RAPOR gövde: {made} kubbe kapak; kalan açık kenar {left}; üçgen {sum(len(p.vertices)-2 for p in me.polygons)}")
bpy.ops.wm.save_as_mainfile(filepath=B)
