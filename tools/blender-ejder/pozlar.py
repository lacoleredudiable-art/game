"""ejder_bas.blend -> baş/boyun pozları (AL-1.27–1.45), her biri tek karelik Action 'Poz_<Ad>'.
Boyun tabanı gövdeye sabit; boyun uzunluğu korunur; baş hedef konuma ve açıya gider.
Arena: X doğu, Z kuzey, H yukarı. Blender (X, -Z, H).
"""
import sys, math
import bpy
import numpy as np
from mathutils import Vector, Matrix, Euler

BLEND, OUT = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.open_mainfile(filepath=BLEND)
arm = bpy.data.objects["ejder_on_iskelet"]; body = bpy.data.objects["ejder_bas"]
NECK = ["Boyun_1", "Boyun_2", "Boyun_3", "Boyun_4", "Boyun_5"]
def a2b(p): return Vector((p[0], -p[1], p[2]))
def b2a(v): return np.array([v.x, -v.y, v.z])

bones = arm.data.bones
P = [b2a(bones[n].head_local) for n in NECK] + [b2a(bones["Bas"].head_local)]
L = [np.linalg.norm(P[i + 1] - P[i]) for i in range(5)]; LN = sum(L)
B = P[0]; J0 = P[5]

def bez(p0, p1, p2, p3, t):
    t = t[:, None]; return (1-t)**3*p0 + 3*(1-t)**2*t*p1 + 3*(1-t)*t**2*p2 + t**3*p3

def solve(J, head_fwd):
    """Taban B'den J'ye, uzunluğu LN olan eğri; eklemler Q0..Q5."""
    tB = np.array([0, -1.0, 0.35]); tB /= np.linalg.norm(tB)     # gövdeden öne ve hafif yukarı çıkar
    tJ = head_fwd / np.linalg.norm(head_fwd)
    tt = np.linspace(0, 1, 600); best = None
    for k in np.linspace(0.5, 25, 200):
        pts = bez(B, B + tB * k, J - tJ * k, J, tt)
        ln = np.sum(np.linalg.norm(np.diff(pts, axis=0), axis=1))
        if best is None or abs(ln - LN) < abs(best[0] - LN): best = (ln, pts)
    ln, pts = best
    seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    Q = [B]; acc = 0
    for l in L:
        acc += l; Q.append(pts[min(np.searchsorted(seg, acc), len(pts) - 1)])
    return Q, ln

def set_pose(J, pitch=0.0, yaw=0.0, roll=0.0):
    """J: baş ekleminin arena konumu; açılar derece (pitch + yukarı, yaw + doğuya bakar, roll + sağa yatar)."""
    R = (Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(pitch), 4, "X")
         @ Matrix.Rotation(math.radians(roll), 4, "Y"))
    fwd_b = (R @ Vector((0, 1, 0, 0))).to_3d()
    Q, ln = solve(np.array(J, float), b2a(fwd_b))
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    for i, n in enumerate(NECK):
        pb = arm.pose.bones[n]; rest = bones[n].matrix_local
        d0 = (bones[n].tail_local - bones[n].head_local).normalized()
        d1 = (a2b(Q[i + 1]) - a2b(Q[i])).normalized()
        Rb = d0.rotation_difference(d1).to_matrix().to_4x4()
        pb.matrix = Matrix.Translation(a2b(Q[i])) @ Rb @ Matrix.Translation(-rest.translation) @ rest
        bpy.context.view_layer.update()
    rest = bones["Bas"].matrix_local
    arm.pose.bones["Bas"].matrix = Matrix.Translation(a2b(Q[5])) @ R @ Matrix.Translation(-rest.translation) @ rest
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")
    return np.linalg.norm(Q[5] - np.array(J)), ln

# (ad, J arena konumu, pitch, yaw, roll) — J0 dinlenmede (0, 17.1, 4.3)
POSES = [
    ("Yatis",            (0, J0[1], J0[2]), 0, 0, 0),
    ("Nefes_Cekme",      (0, 17.6, 5.4), 6, 0, 0),
    ("Bas_Sola_Cekik",   (-5.0, 18.0, 6.3), 4, 35, -6),
    ("Bas_Saga_Cekik",   (5.0, 18.0, 6.3), 4, -35, 6),
    ("Bas_Kalkik",       (0, 18.0, 10.3), 12, 0, 0),
    ("Bas_Yere_Vurmus",  (0, 16.6, 4.6), -4, 0, 0),
    ("Bas_Goge",         (0, 21.0, 15.0), 60, 0, 0),
    ("Dogrulmus",        (0, 17.0, 15.0), -25, 0, 0),
    ("Dogrulmus_Nefes",  (0, 18.0, 16.0), -10, 0, 0),
    ("Gogus_Yerde",      (0, 16.5, 10.0), -20, 0, 0),
    ("Sendeleme",        (2.0, 17.0, 5.0), -4, 8, 22),
]
report = []
for name, J, pit, yw, rl in POSES:
    err, ln = set_pose(J, pit, yw, rl)
    act = bpy.data.actions.new(f"Poz_{name}"); act.use_fake_user = True
    arm.animation_data_create(); arm.animation_data.action = act
    for pb in arm.pose.bones:
        pb.keyframe_insert("location", frame=1); pb.keyframe_insert("rotation_quaternion", frame=1)
    # değerlendirilmiş mesh: silüet için köşe konumları
    dg = bpy.context.evaluated_depsgraph_get(); ev = body.evaluated_get(dg); me = ev.to_mesh()
    V = np.array([b2a(body.matrix_world @ v.co) for v in me.vertices]); ev.to_mesh_clear()
    np.save(f"{OUT}/poz_{name}.npy", V)
    report.append(f"{name}: J sapma {err:.2f} m, boyun eğrisi {ln:.1f}/{LN:.1f} m, burun önü Z {V[:,1].min():.1f}, en alçak H {V[:,2].min():.1f}, en yüksek H {V[:,2].max():.1f}")
arm.animation_data.action = bpy.data.actions["Poz_Yatis"]
bpy.ops.wm.save_as_mainfile(filepath=f"{OUT}/ejder_bas_pozlar.blend")
bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); body.select_set(True)
bpy.ops.export_scene.fbx(filepath=f"{OUT}/ejder_bas_pozlar.fbx", use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, path_mode="COPY")
print("\n".join("RAPOR " + r for r in report))
