"""Bütün iskelet için birleşik pozlar ve 4 sinema klibi.
Bir 'tarif': göğüs (pitch°, kalkma m, ileri m), baş eklemi J + açılar, pençe hedefleri, çene°, plaka°.
Boyun tabanı göğüsle hareket eder; boyun uzunluğu korunur; pençeler IK ile hedefte kalır."""
import sys, math
import bpy
import numpy as np
from mathutils import Vector, Matrix

_a = sys.argv[sys.argv.index("--") + 1:]; BLEND = _a[0]; NECK_DB = float(_a[1]) if len(_a) > 1 else 0.0
bpy.ops.wm.open_mainfile(filepath=BLEND)
arm = bpy.data.objects["ejder_on"]; bones = arm.data.bones
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
NECK = ["Boyun_3", "Boyun_4", "Boyun_5"]          # Boyun_1-2 gövde içinde, göğüsle sabit
L = [bones[n].length for n in NECK]; LN = sum(L)
WRIST0 = {s: b2a(bones[f"Pence_{s}"].head_local) for s in "LR"}
J0 = b2a(bones["Bas"].head_local)

def bez(p0, p1, p2, p3, t):
    t = t[:, None]; return (1-t)**3*p0 + 3*(1-t)**2*t*p1 + 3*(1-t)*t**2*p2 + t**3*p3

def reset():
    for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()

def set_world(name, p0, p1, extra=Matrix.Identity(4)):
    """kemiği armatür uzayında p0'dan p1'e yönelt (dinlenme yönünden en kısa dönüş)."""
    pb = arm.pose.bones[name]; rest = bones[name].matrix_local
    d0 = (bones[name].tail_local - bones[name].head_local).normalized(); d1 = (a2b(p1) - a2b(p0)).normalized()
    R = d0.rotation_difference(d1).to_matrix().to_4x4()
    pb.matrix = Matrix.Translation(a2b(p0)) @ extra @ R @ Matrix.Translation(-rest.translation) @ rest
    bpy.context.view_layer.update()

def build(g_pitch=0.0, g_up=0.0, g_fwd=0.0, J=None, pitch=0.0, yaw=0.0, roll=0.0,
          paws=None, cene=0.0, plaka=0.0):
    reset()
    # göğüs: kendi başı etrafında pitch, sonra öteleme (yukarı, ileri=güney)
    # omurga: eğim üç kemiğe eşit dağılır (Omurga_1, Omurga_2, Gogus), kalan yükselme göğse öteleme
    R = Matrix.Rotation(math.radians(g_pitch / 3.0), 4, "X")      # + : ön yüz yukarı
    M = Matrix.Identity(4)
    for k, n in enumerate(["Omurga_1", "Omurga_2", "Gogus"]):
        rest = bones[n].matrix_local; h = M @ rest.translation
        M = Matrix.Translation(h) @ R @ Matrix.Translation(-h) @ M
        if n == "Gogus":
            lift = (M @ bones["Gogus"].matrix_local.translation).z - bones["Gogus"].matrix_local.translation.z
            M = Matrix.Translation(a2b((0, -g_fwd, g_up - lift)) - a2b((0, 0, 0))) @ M
        arm.pose.bones[n].matrix = M @ rest
        bpy.context.view_layer.update()
    # boyun: taban = göğüsle gelen Boyun_1 başı
    B = b2a(arm.pose.bones["Boyun_3"].head) + np.array([0, 0, -NECK_DB])   # boyun omuzdan çıkar (gövde omurgasıyla hizalı)
    J = np.array(J if J is not None else J0, float)
    Rh = (Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(pitch), 4, "X") @ Matrix.Rotation(math.radians(roll), 4, "Y"))
    fwd = b2a((Rh @ Vector((0, 1, 0, 0))).to_3d()) - b2a(Vector((0, 0, 0)))
    tB = np.array([0, -0.7, 0.7]); tB /= np.linalg.norm(tB); tJ = fwd / np.linalg.norm(fwd)
    tt = np.linspace(0, 1, 500); best = None
    for k in np.linspace(0.3, 15, 160):
        pts = bez(B, B + tB * k, J - tJ * k, J, tt); ln = np.sum(np.linalg.norm(np.diff(pts, axis=0), axis=1))
        if best is None or abs(ln - LN) < abs(best[0] - LN): best = (ln, pts)
    ln, pts = best; seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    Q = [B]; acc = 0
    for l in L: acc += l; Q.append(pts[min(np.searchsorted(seg, acc), len(pts) - 1)])
    for i, n in enumerate(NECK): set_world(n, Q[i], Q[i + 1])
    QN = len(NECK)
    restb = bones["Bas"].matrix_local
    arm.pose.bones["Bas"].matrix = Matrix.Translation(a2b(Q[QN])) @ Rh @ Matrix.Translation(-restb.translation) @ restb
    bpy.context.view_layer.update()
    # pençeler: IK (Kol_Ust, Kol_Alt), Pence yatay kalır + pitch
    paws = paws or {}
    for s in "LR":
        tgt, ppitch = paws.get(s, (WRIST0[s], 0.0))
        nu, nl = f"Kol_Ust_{s}", f"Kol_Alt_{s}"
        Sh = b2a(arm.pose.bones[nu].head); a, b = bones[nu].length, bones[nl].length
        W = np.array(tgt, float); dv = W - Sh; dist = np.linalg.norm(dv); u = dv / dist; dc = min(dist, (a + b) * 0.999)
        pv = np.array([(-1 if s == "L" else 1) * 0.4, 1.0, -0.35]); pole = pv - (pv @ u) * u; pole /= np.linalg.norm(pole)
        x = (a * a - b * b + dc * dc) / (2 * dc); h = math.sqrt(max(a * a - x * x, 0)); K = Sh + u * x + pole * h; Wc = Sh + u * dc
        set_world(nu, Sh, K); set_world(nl, K, Wc)
        rp = bones[f"Pence_{s}"].matrix_local
        arm.pose.bones[f"Pence_{s}"].matrix = Matrix.Translation(a2b(Wc)) @ Matrix.Rotation(math.radians(ppitch), 4, "X") @ Matrix.Translation(-rp.translation) @ rp
        bpy.context.view_layer.update()
    if cene:
        arm.pose.bones["Cene"].rotation_quaternion = Matrix.Rotation(math.radians(-cene), 4, "X").to_quaternion()
    if plaka:
        for i, sx in zip(range(1, 5), (-1, 1, -1, 1)):
            pbp = arm.pose.bones[f"Plaka_{i}"]; rp = bones[f"Plaka_{i}"].matrix_local; parent = arm.pose.bones["Gogus"].matrix @ bones["Gogus"].matrix_local.inverted()
            pbp.matrix = parent @ Matrix.Translation(rp.translation) @ Matrix.Rotation(math.radians(plaka * sx), 4, "Z") @ Matrix.Translation(-rp.translation) @ rp
    bpy.context.view_layer.update()
    return {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}

ALL = [pb.name for pb in arm.pose.bones]
def key_pose(act, pose, frame, names=None):
    arm.animation_data.action = act
    for n in (names or ALL):
        pb = arm.pose.bones[n]; pb.matrix_basis = pose[n]
        pb.keyframe_insert("location", frame=frame); pb.keyframe_insert("rotation_quaternion", frame=frame)

def new_action(name):
    if name in bpy.data.actions: bpy.data.actions.remove(bpy.data.actions[name])
    a = bpy.data.actions.new(name); a.use_fake_user = True; return a

bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
arm.animation_data_create(); arm.animation_data.action = None
UP = dict(g_pitch=12, g_up=3.5, g_fwd=0.5)
DOWN = dict(g_pitch=-6, g_up=-1.3, g_fwd=0.8)
def paw_up(s, dz=1.0, dh=5.0, pit=30): return {s: (WRIST0[s] + np.array([0, dz, dh]), pit)}
POSES = {
    "Dogrulmus":               build(**UP, J=(0, 16.5, 15.0), pitch=-25),
    "Dogrulmus_Nefes":         build(**UP, J=(0, 18.0, 16.5), pitch=-8),
    "Dogrulmus_Pence_L_Havada": build(**UP, J=(0, 16.5, 15.0), pitch=-25, paws=paw_up("L")),
    "Dogrulmus_Pence_R_Havada": build(**UP, J=(0, 16.5, 15.0), pitch=-25, paws=paw_up("R")),
    "Gogus_Yerde":             build(**DOWN, J=(0, 16.0, 10.0), pitch=-20,
                                     paws={s: (WRIST0[s] + np.array([(1 if s == "R" else -1) * 1.2, -0.3, 0]), -4) for s in "LR"}),
}
HEAD = [("Yatis", (0, J0[1], J0[2]), 0, 0, 0), ("Nefes_Cekme", (0, 17.6, 5.4), 6, 0, 0),
        ("Bas_Sola_Cekik", (-5.0, 18.0, 6.3), 4, -35, -6), ("Bas_Saga_Cekik", (5.0, 18.0, 6.3), 4, 35, 6),
        ("Bas_Kalkik", (0, 18.0, 10.3), 12, 0, 0), ("Bas_Yere_Vurmus", (0, 16.6, 4.6), -4, 0, 0),
        ("Bas_Goge", (0, 21.0, 15.0), 60, 0, 0), ("Sendeleme", (2.0, 17.0, 5.0), -4, 8, 22)]
NB = ["Boyun_1", "Boyun_2"] + NECK + ["Bas"]
rep = []
for n, J_, pit, yw, rl in HEAD:
    pz = build(J=J_, pitch=pit, yaw=yw, roll=rl); act = new_action(f"Poz_{n}"); key_pose(act, pz, 1, NB)
    rep.append(f"Poz_{n}: baş H {b2a(arm.pose.bones['Bas'].head)[2]:.1f}")
for n, p in POSES.items():
    act = new_action(f"Poz_{n}"); key_pose(act, p, 1)
    bpy.context.view_layer.update()
    rep.append(f"Poz_{n}: baş H {b2a(arm.pose.bones['Bas'].head)[2]:.1f}")

# --- sinema klipleri (30 fps) ---
YAT = build(); UYKU = build(J=(0, J0[1], J0[2] - 0.35), pitch=-3)
KALK = build(J=(0, 18.0, 8.5), pitch=14); KALK_AGIZ = build(J=(0, 18.0, 8.5), pitch=14, cene=20)
NEF = build(J=(0, 17.6, 5.4), pitch=6)
DOG = POSES["Dogrulmus"]; DOG_AGIZ = build(**UP, J=(0, 17.0, 16.0), pitch=10, cene=22)
GY = POSES["Gogus_Yerde"]; GY_PL = build(**DOWN, J=(0, 16.0, 10.0), pitch=-20, plaka=8,
                                         paws={s: (WRIST0[s] + np.array([(1 if s == "R" else -1) * 1.2, -0.3, 0]), -4) for s in "LR"})
OLUM1 = build(**DOWN, J=(0, 16.5, 11.5), pitch=-5, cene=8)
OLUM2 = build(g_pitch=-10, g_up=-2.4, g_fwd=1.0, J=(2.5, 15.5, 3.4), pitch=-6, yaw=10, roll=24,
              paws={s: (WRIST0[s] + np.array([(1 if s == "R" else -1) * 2.0, -0.6, 0]), -8) for s in "LR"})
CLIPS = {
    "Klip_Uyanis":     [(1, UYKU), (35, UYKU), (60, NEF), (78, KALK), (84, KALK_AGIZ), (104, KALK_AGIZ), (120, YAT)],
    "Klip_Dogrulus":   [(1, YAT), (25, NEF), (75, DOG), (82, DOG_AGIZ), (100, DOG_AGIZ), (120, DOG)],
    "Klip_Gogus_Yere": [(1, DOG), (20, DOG), (70, GY), (95, GY_PL), (120, GY_PL)],
    "Klip_Olum":       [(1, GY), (25, OLUM1), (70, OLUM2), (90, OLUM2)],
}
for n, keys in CLIPS.items():
    act = new_action(n)
    for f, p in keys: key_pose(act, p, f)
    rep.append(f"{n}: {keys[-1][0]} kare ({keys[-1][0]/30:.1f} s)")
reset(); bpy.ops.object.mode_set(mode="OBJECT")
arm.animation_data.action = bpy.data.actions["Poz_Yatis"]
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print("\n".join("RAPOR " + r for r in rep))
