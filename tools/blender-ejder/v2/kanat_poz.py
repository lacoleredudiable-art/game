"""Kanat dinlenme indirme + Poz_Kanat_{L,R}_{Acik,Supurme_Sonu} yeniden kurulum (18 KE-6.6, [K] 17 Ekim).
1) Dinlenme: her kanat, gövdeye bağlandığı iç kenar çizgisi çevresinde dışa/aşağı DOWN derece döndürülür
   (mesh + Kanat_* kemikleri); hedef: dinlenmede en yüksek nokta ~20 m (README açık iş 2).
   Dosyada `kanat_indirme` özelliği tutulur; tekrar çalıştırmada yalnız fark uygulanır.
2) Acik (hazırlık): kanat3_birlestir.py ile aynı yönler, yeni dinlenmeye göre.
3) Supurme_Basi -> Supurme_Sonu (0,4 s etkin süre): kanat önce kuzeybatıda yere iner, sonra güneye süpürür;
   iki poz arası geçiş batı üçte biri 6 m altından örter (görünen = vuran). Sonda kanat güney kenara (Z -18)
   ulaşır, toprağa girmez; zincir 42 m yetmediği için süpürmede kanat 1,0'dan SWEEP_SCALE katına büyür ([K] 17 Ekim).
python kanat_poz.py -- <ejder.blend> [DOWN=15]   (dosyaya yazar)"""
import sys, math
import bpy
import numpy as np
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index("--") + 1:]
MAIN = a[0]; DOWN = float(a[1]) if len(a) > 1 else 15.0
SWEEP_SCALE = 1.2
bpy.ops.wm.open_mainfile(filepath=MAIN)
arm = bpy.data.objects["ejder_on"]; w = bpy.data.objects["ejder_kanatlar"]
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
def nrm(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
def rap(s): print("RAPOR " + s)
def rot_axis(ax, ang):
    ax = nrm(ax); K = np.array([[0, -ax[2], ax[1]], [ax[2], 0, -ax[0]], [-ax[1], ax[0], 0]])
    return np.eye(3) + math.sin(ang) * K + (1 - math.cos(ang)) * K @ K
ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.view_layer.update()
DONE = float(arm.get("kanat_indirme", 0.0)); DELTA = DOWN - DONE
rap(f"indirme: dosyada {DONE:.0f}°, hedef {DOWN:.0f}°, uygulanan {DELTA:.0f}°")
NAMES = [f"Kanat_{{}}_{i:02d}" for i in range(1, 7)]

# --- 1) dinlenme indirme ---
# iç kenar çizgisi: Kanat_01 başı (omuz) ile Omurga_2'ye en çok bağlı köşelerin doğrultusu
P = np.array([b2a(v.co) for v in w.data.vertices])
gi = {vg.name: vg.index for vg in w.vertex_groups}
def wt(v, name):
    for g in v.groups:
        if g.group == gi.get(name): return g.weight
    return 0.0
wo = np.array([wt(v, "Omurga_2") for v in w.data.vertices])
for side, sx in (("L", -1), ("R", 1)):
    sel = np.sign(P[:, 0]) == sx
    S = b2a(arm.data.bones[f"Kanat_{side}_01"].head_local)
    root = P[sel & (wo > 0.5)]
    c = root.mean(0); _, _, Vt = np.linalg.svd(root - c); ax = Vt[0]
    if ax[1] < 0: ax = -ax                       # kuzeye bakan eksen
    best = None
    for sg in ((1, -1) if DELTA >= 0 else (-1, 1)):
        R = rot_axis(ax, sg * math.radians(DELTA)); Q = S + (P[sel] - S) @ R.T
        hi = Q[:, 2].max() if DELTA >= 0 else -Q[:, 2].max()
        if best is None or hi < best[0]: best = (hi, R)
    R = best[1]
    rap(f"{side}: eksen {np.round(ax,2)}, kök köşe {len(root)}, en yüksek {P[sel][:,2].max():.1f} -> {best[0]:.1f}")
    P[sel] = S + (P[sel] - S) @ R.T
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT")
    for n in NAMES:
        e = arm.data.edit_bones[n.format(side)]
        h = S + R @ (b2a(e.head) - S); t = S + R @ (b2a(e.tail) - S); e.head = a2b(h); e.tail = a2b(t)
    bpy.ops.object.mode_set(mode="OBJECT")
for v, p in zip(w.data.vertices, P): v.co = a2b(p)
w.data.update(); arm["kanat_indirme"] = DOWN

# --- 2-3) pozlar ---
for act in list(bpy.data.actions):
    if act.name.startswith("Poz_Kanat_"): bpy.data.actions.remove(act)
CH = [("01", "S", "E"), ("02", "E", "W"), ("03", "W", "T1"), ("04", "W", "T2"), ("05", "W", "T3"), ("06", "W", "T4")]
def rest_joints(side):
    B = arm.data.bones; f = lambda n: b2a(B[f"Kanat_{side}_{n}"].head_local); t = lambda n: b2a(B[f"Kanat_{side}_{n}"].tail_local)
    return {"S": f("01"), "E": t("01"), "W": t("02"), "T1": t("03"), "T2": t("04"), "T3": t("05"), "T4": t("06")}
def L(side, b): return arm.data.bones[f"Kanat_{side}_{b}"].length
ACIK = {"01": (-0.75, 0.2, 0.62), "02": (-0.92, 0.3, 0.25), "03": (-0.7, 0.15, 0.7), "04": (-0.95, 0.3, 0.15),
        "05": (-0.8, 0.6, -0.05), "06": (-0.45, 0.85, -0.3)}
# süpürme: hedef noktalar (sol kanat, arena); yönler bunlardan, boylar kemik boyu × ölçek
BASI = {"E": (-13.0, 24.0, 7.0), "W": (-16.0, 17.0, 3.2), "T1": (-4.5, 10.0, 2.4), "T2": (-6.0, 0.0, 2.4),
        "T3": (-12.0, -2.0, 3.0), "T4": (-17.0, 6.0, 4.6)}
SONU = {"E": (-10.0, 18.0, 10.0), "W": (-11.0, 7.0, 4.5), "T1": (-3.5, -8.0, 2.6), "T2": (-5.5, -17.5, 2.8),
        "T3": (-11.5, -14.0, 3.6), "T4": (-16.5, -6.0, 4.2)}
def sweep_dirs(S, TGT):
    d = {"01": nrm(np.array(TGT["E"]) - S), "02": nrm(np.array(TGT["W"]) - TGT["E"])}
    for b, k in (("03", "T1"), ("04", "T2"), ("05", "T3"), ("06", "T4")): d[b] = nrm(np.array(TGT[k]) - TGT["W"])
    return d
def joints_from(side, dirs, s):
    m = np.array([1 if side == "L" else -1, 1, 1]); Jr = rest_joints(side)
    Q = {"S": Jr["S"]}; Q["E"] = Q["S"] + s * L(side, "01") * nrm(np.array(dirs["01"]) * m)
    Q["W"] = Q["E"] + s * L(side, "02") * nrm(np.array(dirs["02"]) * m)
    for b, k in (("03", "T1"), ("04", "T2"), ("05", "T3"), ("06", "T4")): Q[k] = Q["W"] + s * L(side, b) * nrm(np.array(dirs[b]) * m)
    return Q
def wnorm(Q, hint):
    n = nrm(np.cross(Q["T1"] - Q["W"], Q["T4"] - Q["W"])); return n if n @ hint >= 0 else -n
HINT = {"rest": (-1.0, 0, 0.0), "Acik": (0.6, 0, 0.8), "Supurme_Basi": (0, 0, 1.0), "Supurme_Sonu": (0, 0, 1.0)}
def frame(xd, n):
    x = nrm(xd); z = n - (n @ x) * x
    if np.linalg.norm(z) < 1e-6: z = np.array([0, 0, 1.0]) - x[2] * x
    z = nrm(z); y = np.cross(z, x); return np.stack([x, y, z], 1)
MREF = np.diag([-1.0, -1.0, 1.0])
def set_pose(side, Q, pn, s):
    m = np.array([1 if side == "L" else -1, 1, 1])
    Jr = rest_joints(side); nR = wnorm(Jr, np.array(HINT["rest"]) * m); nT = wnorm(Q, np.array(HINT[pn]) * m)
    for b, p, q in CH:
        n = f"Kanat_{side}_{b}"; pb = arm.pose.bones[n]; rest = arm.data.bones[n].matrix_local
        Rm = s * (frame(Q[q] - Q[p], nT) @ frame(Jr[q] - Jr[p], nR).T); Rb = MREF @ Rm @ MREF.T
        R4 = Matrix(((Rb[0, 0], Rb[0, 1], Rb[0, 2], 0), (Rb[1, 0], Rb[1, 1], Rb[1, 2], 0), (Rb[2, 0], Rb[2, 1], Rb[2, 2], 0), (0, 0, 0, 1)))
        pb.matrix = Matrix.Translation(a2b(Q[p])) @ R4 @ Matrix.Translation(-rest.translation) @ rest
        bpy.context.view_layer.update()
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
V0 = np.array([b2a(v.co) for v in w.data.vertices])
def measure(side):
    dg = bpy.context.evaluated_depsgraph_get(); ev = w.evaluated_get(dg); me = ev.to_mesh()
    Pw = np.array([b2a(w.matrix_world @ v.co) for v in me.vertices]); ev.to_mesh_clear()
    return Pw[np.sign(V0[:, 0]) == (-1 if side == "L" else 1)]
for side in "LR":
    for pn in ("Acik", "Supurme_Basi", "Supurme_Sonu"):
        for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        s = SWEEP_SCALE if pn == "Supurme_Sonu" else 1.0
        S0 = rest_joints("L")["S"]
        dirs = {"Acik": ACIK, "Supurme_Basi": sweep_dirs(S0, BASI), "Supurme_Sonu": sweep_dirs(S0, SONU)}[pn]
        Q = joints_from(side, dirs, s); set_pose(side, Q, pn, s)
        act = bpy.data.actions.new(f"Poz_Kanat_{side}_{pn}"); act.use_fake_user = True; ad.action = act
        try: ad.action_slot = act.slots[0]
        except Exception: pass
        for b, _, _ in CH:
            q = arm.pose.bones[f"Kanat_{side}_{b}"]
            for path in ("rotation_quaternion", "location", "scale"): q.keyframe_insert(path, frame=1)
        Ps = measure(side)
        rap(f"Poz_Kanat_{side}_{pn}: X {Ps[:,0].min():.1f}..{Ps[:,0].max():.1f} Z {Ps[:,1].min():.1f}..{Ps[:,1].max():.1f} "
            f"H {Ps[:,2].min():.2f}..{Ps[:,2].max():.1f} (en alçak {np.round(Ps[Ps[:,2].argmin()],1)}, yer altı %{(Ps[:,2]<0.3).mean()*100:.2f}); uç T2 {np.round(Q['T2'],1)}")
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()
Ps = np.array([b2a(v.co) for v in w.data.vertices])
rap(f"dinlenme kanat: X {Ps[:,0].min():.1f}..{Ps[:,0].max():.1f} Z {Ps[:,1].min():.1f}..{Ps[:,1].max():.1f} H {Ps[:,2].min():.1f}..{Ps[:,2].max():.1f}")
bpy.ops.object.mode_set(mode="OBJECT")
ad.action = bpy.data.actions["Poz_Yatis"]
try: ad.action_slot = bpy.data.actions["Poz_Yatis"].slots[0]
except Exception: pass
bpy.ops.wm.save_as_mainfile(filepath=MAIN)
