"""Teslim: kemiğe bağlı parçaları deriye çevir, ağırlıkları temizle (≤4 etki, normalize), LOD1 (%50),
kabul ölçümleri, ejder.fbx + ejder.blend yaz.
python teslim.py -- <kaynak.blend> <çıkış klasörü>"""
import sys, math, os
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = a[0], a[1]
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SRC)
arm = bpy.data.objects["ejder_on"]
def b2a(v): return np.array([-v.x, -v.y, v.z])
RAP = []
def rap(s): RAP.append(s); print("RAPOR " + s)

arm.animation_data.action = bpy.data.actions["Poz_Yatis"]
for t in list(arm.animation_data.nla_tracks): arm.animation_data.nla_tracks.remove(t)
bpy.context.view_layer.update()
# dinlenme pozunda çalış
arm.data.pose_position = "REST"; bpy.context.view_layer.update()

def deselect():
    for o in bpy.context.view_layer.objects: o.select_set(False)

# --- 1) kemiğe bağlı -> deri (tek etki, ağırlık 1) ---
def to_skinned(o, bone):
    mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    deselect(); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    g = o.vertex_groups.new(name=bone); g.add(list(range(len(o.data.vertices))), 1.0, "REPLACE")
    if not o.data.uv_layers:
        bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(island_margin=0.02); bpy.ops.object.mode_set(mode="OBJECT")
    md = o.modifiers.new("Armature", "ARMATURE"); md.object = arm
    o.parent = arm; o.matrix_parent_inverse = arm.matrix_world.inverted()
to_skinned(bpy.data.objects["ejder_kalp"], "Kalp")
plates = [bpy.data.objects[f"ejder_plaka_{i}"] for i in range(1, 5)]
for i, o in enumerate(plates, 1): to_skinned(o, f"Plaka_{i}")
deselect()
for o in plates: o.select_set(True)
bpy.context.view_layer.objects.active = plates[0]; bpy.ops.object.join()
pl = bpy.context.active_object; pl.name = "ejder_plakalar"; pl.data.name = "ejder_plakalar"
mods = [m for m in pl.modifiers if m.type == "ARMATURE"]
for m in mods[1:]: pl.modifiers.remove(m)

# gövde bütçesi: kapak üçgenleri 10k'yı aştırdı -> simetrik hafif seyreltme
g = bpy.data.objects["ejder_govde"]; t0 = sum(len(p.vertices) - 2 for p in g.data.polygons)
if t0 > 9990:
    dm = g.modifiers.new("dec", "DECIMATE"); dm.decimate_type = "COLLAPSE"; dm.ratio = 9960 / t0
    dm.use_symmetry = True; dm.symmetry_axis = "X"; dm.use_collapse_triangulate = True
    bpy.ops.object.mode_set(mode="OBJECT"); deselect(); g.select_set(True); bpy.context.view_layer.objects.active = g
    bpy.ops.object.modifier_move_to_index(modifier="dec", index=0); bpy.ops.object.modifier_apply(modifier="dec")
    rap(f"gövde seyreltme: {t0} -> {sum(len(p.vertices) - 2 for p in g.data.polygons)}")

MESHES = ["ejder_bas", "ejder_bacak_L", "ejder_bacak_R", "ejder_govde", "ejder_kalp", "ejder_plakalar"]

# --- 2) ağırlık temizliği ---
def clean_weights(o):
    deselect(); o.select_set(True); bpy.context.view_layer.objects.active = o
    bones = {b.name for b in arm.data.bones}
    for g in list(o.vertex_groups):
        if g.name not in bones: o.vertex_groups.remove(g)
    bpy.ops.object.vertex_group_clean(group_select_mode="ALL", limit=0.01)
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    cnt = [len(v.groups) for v in o.data.vertices]
    tot = [sum(g.weight for g in v.groups) for v in o.data.vertices]
    bad = sum(1 for t in tot if abs(t - 1) > 1e-3)
    return max(cnt), cnt.count(0), bad
for n in MESHES:
    mx, zero, bad = clean_weights(bpy.data.objects[n])
    rap(f"ağırlık {n}: en çok {mx} etki, ağırlıksız köşe {zero}, toplamı≠1 {bad}")

# --- 3) LOD1 ---
def tri(o): return sum(len(p.vertices) - 2 for p in o.data.polygons)
LOD = {}
for n in ["ejder_bas", "ejder_bacak_L", "ejder_bacak_R", "ejder_govde"]:
    o = bpy.data.objects[n]
    c = o.copy(); c.data = o.data.copy(); c.name = n + "_LOD1"; c.data.name = n + "_LOD1"
    bpy.context.scene.collection.objects.link(c)
    for m in list(c.modifiers): c.modifiers.remove(m)
    dm = c.modifiers.new("dec", "DECIMATE"); dm.decimate_type = "COLLAPSE"; dm.ratio = 0.5
    dm.use_symmetry = n in ("ejder_bas", "ejder_govde"); dm.symmetry_axis = "X"; dm.use_collapse_triangulate = True
    deselect(); c.select_set(True); bpy.context.view_layer.objects.active = c
    bpy.ops.object.modifier_apply(modifier="dec")
    md = c.modifiers.new("Armature", "ARMATURE"); md.object = arm
    clean_weights(c)
    LOD[n] = c
    rap(f"LOD1 {n}: {tri(o)} -> {tri(c)} üçgen")
for o in [bpy.data.objects[n] for n in ["ejder_kalp", "ejder_plakalar"]]:
    pass   # küçük parçalar LOD1'de de aynı mesh (kod LODGroup'a iki seviyede de koyar)

# --- 4) kabul ölçümleri (dinlenme pozu) ---
def vol(o):
    me = o.data; V = np.array([v.co for v in me.vertices]); s = 0.0
    for p in me.polygons:
        idx = list(p.vertices)
        for k in range(1, len(idx) - 1):
            s += np.dot(V[idx[0]], np.cross(V[idx[k]], V[idx[k + 1]])) / 6
    return s
def bbox(o):
    P = np.array([b2a(o.matrix_world @ v.co) for v in o.data.vertices]); return P.min(0), P.max(0)
def open_edges(o):
    bm = bmesh.new(); bm.from_mesh(o.data); n = sum(1 for e in bm.edges if e.is_boundary); bm.free(); return n
ON = ["ejder_bas", "ejder_bacak_L", "ejder_bacak_R", "ejder_kalp", "ejder_plakalar"]
t_on = sum(tri(bpy.data.objects[n]) for n in ON); t_g = tri(bpy.data.objects["ejder_govde"])
rap(f"bütçe LOD0: ön (baş+bacak+kalp+plaka) {t_on} / 26000, gövde {t_g} / 10000, toplam {t_on + t_g} / 36000")
rap(f"bütçe LOD1: toplam {sum(tri(c) for c in LOD.values()) + 80 + 640}")
for n in MESHES + [c.name for c in LOD.values()]:
    o = bpy.data.objects[n]; lo, hi = bbox(o)
    rap(f"{n}: hacim {'+' if vol(o) > 0 else '-'}  açık kenar {open_edges(o)}  X {lo[0]:.1f}..{hi[0]:.1f} Z {lo[1]:.1f}..{hi[1]:.1f} H {lo[2]:.1f}..{hi[2]:.1f}")

# --- 5) pozlu ölçüm: her poz/klipte baş, pençe, kanat ucu, yer altına inen nokta ---
arm.data.pose_position = "POSE"
dg = bpy.context.evaluated_depsgraph_get()
def posed_pts(n):
    o = bpy.data.objects[n]; ev = o.evaluated_get(dg); me = ev.to_mesh()
    P = np.array([b2a(o.matrix_world @ v.co) for v in me.vertices]); ev.to_mesh_clear(); return P
CHECK = []
for act in sorted(bpy.data.actions, key=lambda x: x.name):
    frames = [1] if act.name.startswith("Poz_") else [int(act.frame_range[0]), int(act.frame_range[1])]
    for f in frames:
        for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        arm.animation_data.action = act; bpy.context.scene.frame_set(f); bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        allp = np.vstack([posed_pts(n) for n in ["ejder_bas", "ejder_bacak_L", "ejder_bacak_R", "ejder_govde"]])
        Pb = posed_pts("ejder_bas")
        under = (allp[:, 2] < -0.3).mean() * 100
        reach = allp[:, 1].min()
        CHECK.append(f"{act.name}@{f}: baş H üst {Pb[:,2].max():.1f}, en güney Z {reach:.1f}, yer altı %{under:.2f}, X {allp[:,0].min():.1f}..{allp[:,0].max():.1f}")
for c in CHECK: rap("poz " + c)
arm.animation_data.action = bpy.data.actions["Poz_Yatis"]; bpy.context.scene.frame_set(1)

# blend seyrek aksiyonlarla kaydedilir (katmanlama Blender'da çalışsın); doldurma yalnız FBX içindir
bpy.ops.wm.save_as_mainfile(filepath=f"{OUT}/ejder.blend")
# --- 6) seyrek poz aksiyonlarını doldur: anahtarsız kemik = dinlenme (sızıntı olmasın) ---
ALLB = [pb.name for pb in arm.pose.bones]
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
filled = 0
for act in bpy.data.actions:
    keyed = set()
    for fc in act.fcurves if hasattr(act, "fcurves") else []:
        if fc.data_path.startswith('pose.bones["'): keyed.add(fc.data_path.split('"')[1])
    if not hasattr(act, "fcurves"):
        for lay in act.layers:
            for st in lay.strips:
                for cb in st.channelbags:
                    for fc in cb.fcurves:
                        if fc.data_path.startswith('pose.bones["'): keyed.add(fc.data_path.split('"')[1])
    f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
    arm.animation_data.action = act
    for n in ALLB:
        if n in keyed: continue
        pb = arm.pose.bones[n]; pb.matrix_basis = Matrix.Identity(4)
        for f in sorted({f0, f1}):
            pb.keyframe_insert("location", frame=f); pb.keyframe_insert("rotation_quaternion", frame=f); pb.keyframe_insert("scale", frame=f)
        filled += 1
rap(f"aksiyon doldurma: {filled} kemik-aksiyon çifti dinlenmeye sabitlendi")
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
arm.animation_data.action = bpy.data.actions["Poz_Yatis"]; bpy.context.scene.frame_set(1)

# --- 7) dışa aktar ---
deselect()
for o in [arm] + [bpy.data.objects[n] for n in MESHES] + list(LOD.values()): o.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=f"{OUT}/ejder.fbx", use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                         mesh_smooth_type="FACE", use_mesh_modifiers=True, bake_anim=True,
                         bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, path_mode="STRIP", embed_textures=False)
open(f"{OUT}/kabul_raporu.txt", "w").write("\n".join(RAP) + "\n")
