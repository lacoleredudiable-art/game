"""Katlı sol kanadı ana dosyaya tak, aynala (sağ), kemikler + Poz_Kanat_{L,R}_{Acik,Supurme_Sonu}.
python kanat2_birlestir.py -- <ana.blend> <kanat_katli.blend> <kanat.json>   (ana dosyaya yazar)"""
import sys, json, math
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index("--") + 1:]
MAIN, WING, JS = a
bpy.ops.wm.open_mainfile(filepath=MAIN)
arm = bpy.data.objects["ejder_on"]
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
def nrm(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
def rap(s): print("RAPOR " + s)
Jd = json.load(open(JS))
ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for pb in arm.pose.bones: pb.matrix_basis.identity()
for n in ["ejder_kanatlar", "ejder_kanat_L", "ejder_kanat_R"]:
    if n in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[n])
for act in list(bpy.data.actions):
    if act.name.startswith("Poz_Kanat_"): bpy.data.actions.remove(act)
NAMES = [f"Kanat_L_{i:02d}" for i in range(1, 7)]
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm.data.edit_bones
for b in list(eb):
    if b.name.startswith("Kanat_"): eb.remove(b)
for side, sx in (("L", 1), ("R", -1)):
    for n in NAMES:
        d = Jd[n]; nn = n.replace("_L_", f"_{side}_"); e = eb.new(nn)
        h = np.array(d["head"]); t = np.array(d["tail"]); h[0] *= sx; t[0] *= sx
        e.head = a2b(h); e.tail = a2b(t); e.roll = 0
    for n in NAMES:
        d = Jd[n]; nn = n.replace("_L_", f"_{side}_"); e = eb[nn]
        par = d["parent"]; e.parent = eb["Omurga_2"] if par in ("Kok", "Omurga_2") else eb[par.replace("_L_", f"_{side}_")]
        e.use_connect = par not in ("Kok", "Omurga_2") and (e.parent.tail - e.head).length < 1e-3
bpy.ops.object.mode_set(mode="OBJECT")

with bpy.data.libraries.load(WING, link=False) as (src, dst): dst.objects = ["ejder_kanat_L"]
wl = dst.objects[0]; bpy.context.scene.collection.objects.link(wl)
wr = wl.copy(); wr.data = wl.data.copy(); wr.name = "ejder_kanat_R"; bpy.context.scene.collection.objects.link(wr)
bm = bmesh.new(); bm.from_mesh(wr.data)
for v in bm.verts: v.co.x = -v.co.x
for f in bm.faces: f.normal_flip()
bm.to_mesh(wr.data); bm.free()
for vg in wr.vertex_groups:
    if "_L_" in vg.name: vg.name = vg.name.replace("_L_", "_R_")
for o in bpy.context.view_layer.objects: o.select_set(False)
wl.select_set(True); wr.select_set(True); bpy.context.view_layer.objects.active = wl; bpy.ops.object.join()
w = bpy.context.active_object; w.name = "ejder_kanatlar"; w.data.name = "ejder_kanatlar"
# çift yüzlü zar: kopya yüzler ters normal, 2 cm içe
bm = bmesh.new(); bm.from_mesh(w.data); bm.normal_update()
orig = list(bm.faces); dup = bmesh.ops.duplicate(bm, geom=orig)
nf = [g for g in dup["geom"] if isinstance(g, bmesh.types.BMFace)]; nv = [g for g in dup["geom"] if isinstance(g, bmesh.types.BMVert)]
for f in nf: f.normal_flip()
bm.normal_update()
for v in nv: v.co = v.co + v.normal * 0.02
bm.to_mesh(w.data); bm.free()
w.parent = arm; w.matrix_parent_inverse = arm.matrix_world.inverted()
for m in list(w.modifiers): w.modifiers.remove(m)
md = w.modifiers.new("Armature", "ARMATURE"); md.object = arm
rap(f"kanatlar: {sum(len(p.vertices)-2 for p in w.data.polygons)} üçgen, gruplar {sorted(vg.name for vg in w.vertex_groups)}")

# pozlar
CH = [("01", "S", "E"), ("02", "E", "W"), ("03", "W", "T1"), ("04", "W", "T2"), ("05", "W", "T3"), ("06", "W", "T4")]
def rest_joints(side):
    B = arm.data.bones; f = lambda n: b2a(B[f"Kanat_{side}_{n}"].head_local); t = lambda n: b2a(B[f"Kanat_{side}_{n}"].tail_local)
    return {"S": f("01"), "E": t("01"), "W": t("02"), "T1": t("03"), "T2": t("04"), "T3": t("05"), "T4": t("06")}
def L(side, b): return arm.data.bones[f"Kanat_{side}_{b}"].length
POSE = {
    "Acik":         {"01": (-0.75, 0.2, 0.62), "02": (-0.92, 0.3, 0.25), "03": (-0.7, 0.15, 0.7), "04": (-0.95, 0.3, 0.15),
                     "05": (-0.8, 0.6, -0.05), "06": (-0.45, 0.85, -0.3)},
    "Supurme_Sonu": {"01": (-0.6, -0.55, 0.58), "02": (-0.4, -0.9, -0.15), "03": (0.1, -0.85, -0.5), "04": (-0.05, -0.75, -0.65),
                     "05": (-0.25, -0.7, -0.62), "06": (-0.45, -0.55, -0.58)},
}
def joints_from(side, dirs):
    sx = 1 if side == "L" else -1; m = np.array([sx, 1, 1]); Jr = rest_joints(side)
    Q = {"S": Jr["S"]}; Q["E"] = Q["S"] + L(side, "01") * nrm(np.array(dirs["01"]) * m); Q["W"] = Q["E"] + L(side, "02") * nrm(np.array(dirs["02"]) * m)
    for b, k in (("03", "T1"), ("04", "T2"), ("05", "T3"), ("06", "T4")): Q[k] = Q["W"] + L(side, b) * nrm(np.array(dirs[b]) * m)
    return Q
def wnorm(Q, side, hint):
    n = nrm(np.cross(Q["T1"] - Q["W"], Q["T4"] - Q["W"])); return n if n @ hint >= 0 else -n
HINT = {"rest": (-1.0, 0, 0.0), "Acik": (0.6, 0, 0.8), "Supurme_Sonu": (0, 0, 1.0)}
def frame(xd, n):
    x = nrm(xd); z = n - (n @ x) * x
    if np.linalg.norm(z) < 1e-6: z = np.array([0, 0, 1.0]) - x[2] * x
    z = nrm(z); y = np.cross(z, x); return np.stack([x, y, z], 1)
MREF = np.diag([-1.0, -1.0, 1.0])
def set_pose(side, Q, pn):
    m = np.array([1 if side == "L" else -1, 1, 1])
    Jr = rest_joints(side); nR = wnorm(Jr, side, np.array(HINT["rest"]) * m); nT = wnorm(Q, side, np.array(HINT[pn]) * m)
    for b, p, q in CH:
        n = f"Kanat_{side}_{b}"; pb = arm.pose.bones[n]; rest = arm.data.bones[n].matrix_local
        Rm = frame(Q[q] - Q[p], nT) @ frame(Jr[q] - Jr[p], nR).T; Rb = MREF @ Rm @ MREF.T
        R4 = Matrix(((Rb[0, 0], Rb[0, 1], Rb[0, 2], 0), (Rb[1, 0], Rb[1, 1], Rb[1, 2], 0), (Rb[2, 0], Rb[2, 1], Rb[2, 2], 0), (0, 0, 0, 1)))
        pb.matrix = Matrix.Translation(a2b(Q[p])) @ R4 @ Matrix.Translation(-rest.translation) @ rest
        bpy.context.view_layer.update()
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
V0 = np.array([b2a(v.co) for v in w.data.vertices])
for side in "LR":
    for pn, dirs in POSE.items():
        for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        Q = joints_from(side, dirs); set_pose(side, Q, pn)
        act = bpy.data.actions.new(f"Poz_Kanat_{side}_{pn}"); act.use_fake_user = True; ad.action = act
        for b, _, _ in CH:
            q = arm.pose.bones[f"Kanat_{side}_{b}"]; q.keyframe_insert("rotation_quaternion", frame=1); q.keyframe_insert("location", frame=1)
        dg = bpy.context.evaluated_depsgraph_get(); ev = w.evaluated_get(dg); me = ev.to_mesh()
        Pw = np.array([b2a(w.matrix_world @ v.co) for v in me.vertices]); ev.to_mesh_clear()
        sel = np.sign(V0[:, 0]) == (-1 if side == "L" else 1); Ps = Pw[sel]
        rap(f"Poz_Kanat_{side}_{pn}: X {Ps[:,0].min():.1f}..{Ps[:,0].max():.1f} Z {Ps[:,1].min():.1f}..{Ps[:,1].max():.1f} H {Ps[:,2].min():.1f}..{Ps[:,2].max():.1f}")
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
bpy.ops.object.mode_set(mode="OBJECT")
ad.action = bpy.data.actions["Poz_Yatis"]
bpy.ops.wm.save_as_mainfile(filepath=MAIN)
