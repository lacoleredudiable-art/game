"""Tripo kanadı (sol, batı) -> katlı dinlenme pozunda, Kanat_L_01..06 + Kok ağırlıklı mesh.
Ham GLB'de sağdaki (+x) kanat alınır; ölçek SC; omuz (−7.2, 28, 13.2). Sırt parçası ve iç zarın alt kısmı kesilir.
İç kenar gövdenin sırt-yan çizgisine döndürülür (Kok'a bağlı kalır). Açık bağlama -> katlı poz -> sabitle.
python kanat2.py -- <kanat.glb> <çıkış.blend> <kemik.json>"""
import sys, math, json
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
a = sys.argv[sys.argv.index("--") + 1:]
GLB, OUT, JS = a[:3]
SC = 40.0
JR = {"S": (-0.46, 0.0, 0.38), "E": (-0.26, 0.0, 0.32), "W": (-0.06, 0.0, 0.51),
      "T1": (0.34, 0.0, 0.57), "T2": (0.47, 0.0, 0.29), "T3": (0.25, 0.0, 0.13), "T4": (-0.04, 0.0, 0.09)}
S_AR = np.array([-7.5, 28.0, 11.5]); EDGE_T = np.array([-0.2, 0.8, -0.6])     # iç kenar hedef yönü (arena)
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
def nrm(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
def raw2ar(p): p = np.asarray(p, float); return S_AR + SC * np.array([-(p[0] - JR["S"][0]), -(p[2] - JR["S"][2]), -(p[1] - JR["S"][1])])
def rap(s): print("RAPOR " + s)

bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=GLB)
o = [x for x in bpy.data.objects if x.type == "MESH"][0]; o.name = "ejder_kanat_L"; o.data.name = "ejder_kanat_L"
R0 = np.array([o.matrix_world @ v.co for v in o.data.vertices]); o.parent = None; o.matrix_world = Matrix.Identity(4)
for v, r in zip(o.data.vertices, R0): v.co = Vector(r)
bm = bmesh.new(); bm.from_mesh(o.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
def hw(y): return max(0.008, 0.05 + 0.13 * y)
def inner_cut(x, y, z):
    # yalnız iç bölge (sırt parçasına yakın, 4. parmağın içi): x < 0.24
    if x > 0.24: return False
    u = np.clip(x / 0.24, 0, 1); return z < 0.30 + 0.05 * math.sin(math.pi * u) * (1 - u) + 0.0 * u
dead = []
# kopuk adacıkları at
bm.faces.ensure_lookup_table(); seen = set(); isl = []
for f in bm.faces:
    if f.index in seen: continue
    st = [f]; cur = []; seen.add(f.index)
    while st:
        x = st.pop(); cur.append(x)
        for e in x.edges:
            for y in e.link_faces:
                if y.index not in seen: seen.add(y.index); st.append(y)
    isl.append(cur)
isl.sort(key=len, reverse=True)
bmesh.ops.delete(bm, geom=[f for c in isl if len(c) < 200 for f in c], context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
bm.to_mesh(o.data); bm.free()
dm = o.modifiers.new("dec", "DECIMATE"); dm.ratio = 20000 / max(1, len(o.data.polygons)); dm.use_collapse_triangulate = True
bpy.context.view_layer.objects.active = o; o.select_set(True); bpy.ops.object.modifier_apply(modifier="dec")
bm = bmesh.new(); bm.from_mesh(o.data)
# iç kenar (kesik çizgisi) noktaları, ham
edge_raw = np.array([v.co[:] for e in bm.edges if e.is_boundary for v in e.verts if v.co.x < -0.43 and v.co.z < 0.355])
bm.to_mesh(o.data); bm.free()
rap(f"kanat kesildi: {len(o.data.polygons)} yüz, adacıklar {[len(c) for c in isl[:6]]}")

# ham -> arena (açık, omuz yerinde)
P = np.array([raw2ar(v.co[:]) for v in o.data.vertices]); J = {k: raw2ar(p) for k, p in JR.items()}
E0 = np.array([raw2ar(p) for p in edge_raw]); c = E0.mean(0); _, _, Vt = np.linalg.svd(E0 - c); d = Vt[0]
if d[1] < 0: d = -d
top = E0[np.argmin((E0 - c) @ d)]; bot = E0[np.argmax((E0 - c) @ d)]
# döndürme: iç kenar yönü -> hedef; kalan serbestlik: T2 ucu olabildiğince dışa (-X) ve yukarı
def rot_to(u, v):
    u = nrm(u); v = nrm(v); ax = np.cross(u, v); s = np.linalg.norm(ax); cth = u @ v
    if s < 1e-9: return np.eye(3)
    ax /= s; K = np.array([[0, -ax[2], ax[1]], [ax[2], 0, -ax[0]], [-ax[1], ax[0], 0]])
    return np.eye(3) + math.sin(math.atan2(s, cth)) * K + (1 - cth) * K @ K
def rot_axis(ax, ang):
    ax = nrm(ax); K = np.array([[0, -ax[2], ax[1]], [ax[2], 0, -ax[0]], [-ax[1], ax[0], 0]])
    return np.eye(3) + math.sin(ang) * K + (1 - math.cos(ang)) * K @ K
R1 = rot_to(bot - top, EDGE_T); best = None
for ang in np.linspace(-math.pi, math.pi, 73):
    R = rot_axis(EDGE_T, ang) @ R1; t2 = R @ (J["T2"] - S_AR)
    sc_ = -t2[0] + 0.5 * t2[2]
    if best is None or sc_ > best[0]: best = (sc_, R)
R = best[1]
def tr(p): return S_AR + R @ (np.asarray(p) - S_AR)
P = np.array([tr(p) for p in P]); J = {k: tr(p) for k, p in J.items()}; top, bot = tr(top), tr(bot)
rap(f"iç kenar: {np.round(top,1)} -> {np.round(bot,1)} ({np.linalg.norm(bot-top):.1f} m)")
for v, p in zip(o.data.vertices, P): v.co = a2b(p)
o.data.update()

# kemikler (açık)
arm_d = bpy.data.armatures.new("fitk"); arm = bpy.data.objects.new("fitk", arm_d); bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm_d.edit_bones
k = eb.new("Omurga_2"); k.head = a2b((0, 36, 9.0)); k.tail = a2b((0, 29, 9.0))
CH = [("01", "S", "E", "Kok"), ("02", "E", "W", "01"), ("03", "W", "T1", "02"), ("04", "W", "T2", "02"), ("05", "W", "T3", "02"), ("06", "W", "T4", "02")]
for b, p, q, par in CH:
    e = eb.new(f"Kanat_L_{b}"); e.head = a2b(J[p]); e.tail = a2b(J[q]); e.roll = 0
    e.parent = eb["Omurga_2" if par == "Kok" else f"Kanat_L_{par}"]; e.use_connect = par != "Kok"
bpy.ops.object.mode_set(mode="OBJECT")
LEN = {b: float(np.linalg.norm(J[q] - J[p])) for b, p, q, _ in CH}
rap("kemik boyları " + ", ".join(f"{b}:{l:.1f}" for b, l in LEN.items()))

# --- kemikleri kalınlaştır (kendi eksenleri etrafında) ---
RB = {"01": 1.9, "02": 1.6, "03": 0.8, "04": 0.8, "05": 0.8, "06": 0.8}; KTB = {"01": 1.5, "02": 1.5, "03": 1.0, "04": 1.0, "05": 1.0, "06": 1.0}
BS = [(b, J[p], J[q]) for b, p, q, _ in CH]
def segd(P_, A, B):
    ab = B - A; t = np.clip(((P_ - A) @ ab) / (ab @ ab), 0, 1); F = A + t[:, None] * ab; return np.linalg.norm(P_ - F, axis=1), F
Ds = []; Fs = []
for b, A, Bq in BS:
    d_, F_ = segd(P, A, Bq); Ds.append(d_ / RB[b]); Fs.append(F_)
Ds = np.array(Ds); near = Ds.argmin(0); dn = Ds.min(0)
for i in range(len(P)):
    k = near[i]; x = dn[i]
    if x >= 2.2: continue
    F_ = Fs[k][i]; rvec = P[i] - F_; r = np.linalg.norm(rvec)
    if r < 1e-6: continue
    Rb = RB[BS[k][0]]
    KT = KTB[BS[k][0]]
    rn = r * KT if x < 1.0 else (Rb * KT + (r - Rb) * (2.2 * Rb - Rb * KT) / (1.2 * Rb))
    P[i] = F_ + rvec / r * rn
for v, p in zip(o.data.vertices, P): v.co = a2b(p)
o.data.update()
rap(f"kol 1.5x kalınlaştırıldı, parmaklar aynı; tüp köşesi {(dn < 1.0).sum()}")

# --- zarı parmak uçlarına uzat, zarın dışına taşan parmakları kısalt ---
W0 = J["W"].copy(); n_ = nrm(np.cross(J["T1"] - W0, J["T4"] - W0)); u_ = nrm(J["T1"] - W0); v_ = np.cross(n_, u_)
def angv(Q):
    q = np.atleast_2d(Q) - W0; return np.arctan2(q @ v_, q @ u_)
TIPS = ["T1", "T2", "T3", "T4"]
thk = [float(angv(J[t])[0]) for t in TIPS]
if thk[3] < 0: v_ = -v_; thk = [float(angv(J[t])[0]) for t in TIPS]
q = P - W0; inpl = q - np.outer(q @ n_, n_); r_all = np.linalg.norm(inpl, axis=1); th_all = angv(P)
tube = dn < 1.6
memb = ~tube
bins = np.linspace(thk[0] - 0.2, thk[3] + 0.2, 141); bw = bins[1] - bins[0]
r_edge = np.full(len(bins), np.nan)
for i, b0 in enumerate(bins):
    m = memb & (np.abs(th_all - b0) < bw)
    if m.any(): r_edge[i] = r_all[m].max()
ok = ~np.isnan(r_edge); r_edge = np.interp(bins, bins[ok], r_edge[ok])
def redge(th): return np.interp(th, bins, r_edge)
newtip = {}
for k, (bn, tname) in enumerate(zip(["03", "04", "05", "06"], TIPS)):
    dk = nrm(J[tname] - W0); Lk = np.linalg.norm(J[tname] - W0); rm = float(redge(thk[k])) + 0.6
    sel = tube & (near == (2 + k))
    t = (P[sel] - W0) @ dk; tn = np.where(t > rm, rm + (t - rm) * 0.3, t)
    P[sel] = P[sel] + np.outer(tn - t, dk)
    newtip[tname] = W0 + dk * min(Lk, rm + max(Lk - rm, 0) * 0.3)
rap("parmak kısaltma (m): " + ", ".join(f"{t}:{np.linalg.norm(J[t]-W0):.1f}->{np.linalg.norm(newtip[t]-W0):.1f}" for t in TIPS))
for t in TIPS: J[t] = newtip[t]
# oyukları doldur: zar kenarını komşu parmak uçlarını birleştiren kirişin %75'ine kadar dışa çek
def r_chord(th):
    out = np.zeros_like(th)
    for k in range(3):
        A = J[TIPS[k]] - W0; B = J[TIPS[k + 1]] - W0
        a2 = np.array([A @ u_, A @ v_]); b2 = np.array([B @ u_, B @ v_])
        m = (th >= thk[k]) & (th <= thk[k + 1])
        dvec = np.stack([np.cos(th[m]), np.sin(th[m])], 1); e = b2 - a2
        den = dvec[:, 0] * e[1] - dvec[:, 1] * e[0]
        out[m] = (a2[0] * e[1] - a2[1] * e[0]) / np.where(np.abs(den) < 1e-9, 1e-9, den)
    return out
fan = memb & (th_all > thk[0]) & (th_all < thk[3])
re = redge(th_all[fan]); rc = r_chord(th_all[fan]); f = np.maximum(1.0, (re + 0.75 * (rc - re)) / np.maximum(re, 1e-6))
g = 1 + (f - 1) * np.clip((r_all[fan] / np.maximum(re, 1e-6) - 0.25) / 0.75, 0, 1)
qf = P[fan] - W0; nn = np.outer(qf @ n_, n_); P[fan] = W0 + nn + (qf - nn) * g[:, None]
rap(f"zar uçlara uzatıldı: {fan.sum()} köşe, en çok {f.max():.2f}x")
for v, p in zip(o.data.vertices, P): v.co = a2b(p)
o.data.update()
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT")
for bn, t in zip(["03", "04", "05", "06"], TIPS): arm_d.edit_bones[f"Kanat_L_{bn}"].tail = a2b(J[t])
bpy.ops.object.mode_set(mode="OBJECT")
LEN = {b: float(np.linalg.norm(J[q] - J[p])) for b, p, q, _ in CH}
BS = [(b, J[p], J[q]) for b, p, q, _ in CH]

# --- ağırlık: tüp köşeleri en yakın kemiğe; zar köşeleri parmaklar arası açıya göre ---
names = [f"Kanat_L_{b}" for b, _, _ in BS] + ["Omurga_2"]
Wm = np.zeros((7, len(P)))
Ds = []
for b, A, Bq in BS:
    d_, _ = segd(P, A, Bq); Ds.append(d_)
de, _ = segd(P, top, bot); Ds.append(de); Ds = np.array(Ds)
Wt = 1.0 / np.maximum(Ds, 0.08) ** 6; Wt /= Wt.sum(0)                    # tüp (keskin)
Wi = 1.0 / np.maximum(Ds, 0.08) ** 3; Wi /= Wi.sum(0)                    # yumuşak (arka zar bölgesi)
W0 = J["W"]; n_ = nrm(np.cross(J["T1"] - W0, J["T4"] - W0)); u_ = nrm(J["T1"] - W0); v_ = np.cross(n_, u_)
def ang(p): q = p - W0; return math.atan2(q @ v_, q @ u_)
TH = [ang(J[t]) for t in ("T1", "T2", "T3", "T4")]
if TH[3] < 0: v_ = -v_; TH = [ang(J[t]) for t in ("T1", "T2", "T3", "T4")]
rap("parmak açıları " + ", ".join(f"{math.degrees(t):.0f}" for t in TH))
for i, p in enumerate(P):
    th = ang(p); r = np.linalg.norm(p - W0); w = np.zeros(7)
    if 0.0 <= th <= TH[3]:
        for k in range(3):
            if TH[k] <= th <= TH[k + 1]:
                t = (th - TH[k]) / (TH[k + 1] - TH[k]); w[2 + k] = 1 - t; w[3 + k] = t; break
        kw = np.clip(r / 3.0, 0, 1); w = w * kw; w[1] += 1 - kw                     # bileğe yakın: ön kol
    elif th < 0.0 and th > -1.2:
        kf = np.clip(-th / 0.6, 0, 1); w[2] = 1 - kf; w[1] = kf                      # ön kenar zarı: F1 -> ön kol
    else:
        w = Wi[:, i].copy()                                                          # arka zar: F4 / kol / gövde
    Wm[:, i] = w / w.sum()
mt = np.clip((1.5 * np.array([RB[b] * KTB[b] for b, _, _ in BS] + [1.0])[:, None] - Ds) / 1.0, 0, 1).max(0)
W = mt * Wt + (1 - mt) * Wm
W /= W.sum(0)
segs = [(n, None, None) for n in names]
for i, n in enumerate(names):
    vg = o.vertex_groups.new(name=n); idx = np.where(W[i] > 0.01)[0]
    for j in idx: vg.add([int(j)], float(W[i, j]), "REPLACE")
o.parent = arm; md = o.modifiers.new("Armature", "ARMATURE"); md.object = arm

# katlı dinlenme pozu (çerçeveli dönüş: kemik yönü + kanat normali)
FOLD = {"01": (-0.75, 0.3, 0.6), "02": (-0.55, 0.8, 0.3), "03": (-0.6, 0.65, 0.2), "04": (-0.6, 0.75, -0.25),
        "05": (-0.45, 0.55, -0.7), "06": (-0.3, 0.25, -0.92)}       # yarı açık: sırtın yanında kalkık yelken
def joints_from(dirs):
    Q = {"S": J["S"].copy()}
    Q["E"] = Q["S"] + LEN["01"] * nrm(dirs["01"]); Q["W"] = Q["E"] + LEN["02"] * nrm(dirs["02"])
    for b, t in (("03", "T1"), ("04", "T2"), ("05", "T3"), ("06", "T4")): Q[t] = Q["W"] + LEN[b] * nrm(dirs[b])
    return Q
def wnorm(Q): return nrm(np.cross(Q["T1"] - Q["W"], Q["T4"] - Q["W"]))
def frame(xd, n):
    x = nrm(xd); z = n - (n @ x) * x
    if np.linalg.norm(z) < 1e-6: z = np.array([0, 0, 1.0]) - x[2] * x
    z = nrm(z); y = np.cross(z, x); return np.stack([x, y, z], 1)
MREF = np.diag([-1.0, -1.0, 1.0])
def set_pose(Q, Jrest, hT, hR):
    nR = wnorm(Jrest); nT = wnorm(Q)
    if nR @ hR < 0: nR = -nR
    if nT @ hT < 0: nT = -nT
    for b, p, q, _ in CH:
        n = f"Kanat_L_{b}"; pb = arm.pose.bones[n]; rest = arm_d.bones[n].matrix_local
        Rm = frame(Q[q] - Q[p], nT) @ frame(Jrest[q] - Jrest[p], nR).T; Rb = MREF @ Rm @ MREF.T
        R4 = Matrix(((Rb[0, 0], Rb[0, 1], Rb[0, 2], 0), (Rb[1, 0], Rb[1, 1], Rb[1, 2], 0), (Rb[2, 0], Rb[2, 1], Rb[2, 2], 0), (0, 0, 0, 1)))
        pb.matrix = Matrix.Translation(a2b(Q[p])) @ R4 @ Matrix.Translation(-rest.translation) @ rest
        bpy.context.view_layer.update()
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
Qf = joints_from(FOLD); set_pose(Qf, J, np.array([-1.0, 0, 0.0]), R @ np.array([0, 0, 1.0]))
rap("katlı eklemler: " + ", ".join(f"{k}{np.round(v,1)}" for k, v in Qf.items()))
out = {pb.name: {"head": list(map(float, b2a(pb.head))), "tail": list(map(float, b2a(pb.tail))), "parent": pb.parent.name if pb.parent else None}
       for pb in arm.pose.bones if pb.name != "Omurga_2"}
out["_open"] = {k: list(map(float, v)) for k, v in J.items()}
out["_edge"] = [list(map(float, top)), list(map(float, bot))]
json.dump(out, open(JS, "w"), indent=1)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.objects.active = o; o.select_set(True); bpy.ops.object.modifier_apply(modifier="Armature")
# zar kıvrım gürültüsünü yumuşat (kemik tüplerine dokunma): Laplace, 6 tur
bmz = bmesh.new(); bmz.from_mesh(o.data); bmz.verts.ensure_lookup_table()
memb = (mt < 0.2)
Pz = np.array([v.co[:] for v in bmz.verts]); nb = [[e.other_vert(v).index for e in v.link_edges] for v in bmz.verts]
bnd = np.array([v.is_boundary for v in bmz.verts])
for _ in range(6):
    Q = Pz.copy()
    for i in np.where(memb & ~bnd)[0]:
        if nb[i]: Q[i] = Pz[i] * 0.5 + 0.5 * Pz[nb[i]].mean(0)
    Pz = Q
for v, p in zip(bmz.verts, Pz): v.co = p
bmz.to_mesh(o.data); bmz.free()
rap(f"zar yumuşatıldı: {int((memb & ~bnd).sum())} köşe")
o.parent = None; bpy.data.objects.remove(arm)
Pz = np.array([b2a(v.co) for v in o.data.vertices])
rap(f"katlı kanat: X {Pz[:,0].min():.1f}..{Pz[:,0].max():.1f} Z {Pz[:,1].min():.1f}..{Pz[:,1].max():.1f} H {Pz[:,2].min():.1f}..{Pz[:,2].max():.1f}; yer altı %{(Pz[:,2]<-0.2).mean()*100:.1f}")
bpy.ops.wm.save_as_mainfile(filepath=OUT)
