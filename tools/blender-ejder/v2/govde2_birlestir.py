"""Ana dosyaya yeni gövdeyi bağla. Eski gövde/bacak/kanat çıkar; ön bacak kemikleri sfenks konumuna; arka bacak
ve kuyruk kemikleri eklenir; bacak pozları (Pence_*) yeni kemiklerle yeniden üretilir.
python govde2_birlestir.py -- <ana.blend> <govde_sfenks.blend> <kemik.json> <çıkış.blend>"""
import sys, json, math
import bpy
import numpy as np
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index("--") + 1:]
MAIN, BODY, JS, OUT = a
bpy.ops.wm.open_mainfile(filepath=MAIN)
arm = bpy.data.objects["ejder_on"]
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
J = json.load(open(JS))
ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for pb in arm.pose.bones: pb.matrix_basis.identity()

for n in ["ejder_bacak_L", "ejder_bacak_R", "ejder_govde"]:
    if n in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[n])
for act in list(bpy.data.actions):
    if act.name.startswith(("Poz_Kanat_", "Poz_Pence_")): bpy.data.actions.remove(act)

bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm.data.edit_bones
for b in list(eb):
    if b.name.startswith("Kanat_"): eb.remove(b)
for n, d in J.items():
    if n in ("Kok", "Gogus"):
        h = b2a(eb[n].head); assert np.linalg.norm(h - np.array(d["head"])) < 0.05, (n, h, d["head"])
        continue
    b = eb.get(n) or eb.new(n)
    b.use_connect = False
    b.head = a2b(d["head"]); b.tail = a2b(d["tail"]); b.roll = 0
for n, d in J.items():
    if n in ("Kok", "Gogus"): continue
    b = eb[n]; b.parent = eb[d["parent"]] if d["parent"] else None
    if b.parent and (b.parent.tail - b.head).length < 1e-3: b.use_connect = True
bpy.ops.object.mode_set(mode="OBJECT")

with bpy.data.libraries.load(BODY, link=False) as (src, dst): dst.objects = ["ejder_govde"]
g = dst.objects[0]; bpy.context.scene.collection.objects.link(g)
g.parent = arm; g.matrix_parent_inverse = arm.matrix_world.inverted()
for m in list(g.modifiers): g.modifiers.remove(m)
md = g.modifiers.new("Armature", "ARMATURE"); md.object = arm
missing = [vg.name for vg in g.vertex_groups if vg.name not in arm.data.bones]
print("RAPOR gövde bağlandı; iskelette olmayan grup:", missing, "üçgen", sum(len(p.vertices) - 2 for p in g.data.polygons))

# --- bacak pozları (bacak.py tarifi; dirsek kutbu sfenks dinlenmesine uygun) ---
bones = arm.data.bones
def ik_pose(side, wrist, pitch=0.0, yaw=0.0):
    sx = 1 if side == "L" else -1; out = -1 if side == "L" else 1
    nu, nl, npw = f"Kol_Ust_{side}", f"Kol_Alt_{side}", f"Pence_{side}"
    Sh = b2a(arm.pose.bones[nu].head); a_ = bones[nu].length; b_ = bones[nl].length
    W_ = np.array(wrist, float); dv = W_ - Sh; dist = np.linalg.norm(dv); dc = min(dist, (a_ + b_) * 0.999); u = dv / dist
    pole = np.array([out * 0.4, 1.0, -0.35]); pole = pole - (pole @ u) * u; pole /= np.linalg.norm(pole)
    x = (a_ * a_ - b_ * b_ + dc * dc) / (2 * dc); h = math.sqrt(max(a_ * a_ - x * x, 0))
    K = Sh + u * x + pole * h; Wc = Sh + u * dc
    for n, (p0, p1) in {nu: (Sh, K), nl: (K, Wc)}.items():
        pb = arm.pose.bones[n]; rest = bones[n].matrix_local
        d0 = (bones[n].tail_local - bones[n].head_local).normalized(); d1 = (a2b(p1) - a2b(p0)).normalized()
        R = d0.rotation_difference(d1).to_matrix().to_4x4()
        pb.matrix = Matrix.Translation(a2b(p0)) @ R @ Matrix.Translation(-rest.translation) @ rest
        bpy.context.view_layer.update()
    rest = bones[npw].matrix_local
    R = Matrix.Rotation(math.radians(yaw * sx), 4, "Z") @ Matrix.Rotation(math.radians(pitch), 4, "X")
    arm.pose.bones[npw].matrix = Matrix.Translation(a2b(Wc)) @ Matrix.Translation(-rest.translation) @ R @ rest if False else \
        Matrix.Translation(a2b(Wc)) @ R @ Matrix.Translation(-rest.translation) @ rest
    bpy.context.view_layer.update()
    return dist - dc
bil = {s: b2a(bones[f"Pence_{s}"].head_local) for s in "LR"}
LEG_POSES = [("Pence_{s}_Havada", (0.0, 1.0, 4.0), 30, 0), ("Pence_{s}_Yerde", (0.0, -0.5, 0.0), -4, 0),
             ("Pence_{s}_Acik", (3.0, -0.5, 2.0), 10, -40), ("Pence_{s}_Supurme_Sonu", (-3.0, -1.5, 0.5), -2, 30)]
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
for s in "LR":
    out = -1 if s == "L" else 1
    for name, (dx, dz, dh), pit, yw in LEG_POSES:
        for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        miss = ik_pose(s, bil[s] + np.array([dx * out, dz, dh]), pit, yw)
        act = bpy.data.actions.new("Poz_" + name.format(s=s)); act.use_fake_user = True; ad.action = act
        for bn in (f"Kol_Ust_{s}", f"Kol_Alt_{s}", f"Pence_{s}"):
            pb = arm.pose.bones[bn]; pb.keyframe_insert("location", frame=1); pb.keyframe_insert("rotation_quaternion", frame=1)
        print(f"RAPOR {act.name}: erişilemeyen {miss:.2f} m")
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
bpy.ops.object.mode_set(mode="OBJECT")
ad.action = bpy.data.actions["Poz_Yatis"]
bpy.ops.wm.save_as_mainfile(filepath=OUT)
