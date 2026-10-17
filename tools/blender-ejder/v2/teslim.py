"""Teslim (v2): kemiğe bağlı parçaları deriye çevir, bütçeye seyrelt (AL-1.19), ağırlıkları temizle (≤4 etki,
normalize), LOD1 (%50), kabul ölçümleri, ejder.fbx + kabul_raporu.txt yaz. Kaynak dosyaya dokunmaz.
Bütçe (LOD0): baş 13 000 · gövde (bacaklar, kuyruk dahil) 13 000 · kanatlar 4 000 tek yüz × 2 (çift yüz) ·
kalp + plakalar + pullar ~1 600 → ≤ 36 000.
python teslim.py -- <kaynak.blend> <çıkış klasörü> [<çalışma.blend>]"""
import sys, math, os
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = os.path.abspath(a[0]), os.path.abspath(a[1]); WORK = os.path.abspath(a[2]) if len(a) > 2 else None
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SRC)
arm = bpy.data.objects["ejder_on"]
def b2a(v): return np.array([-v.x, -v.y, v.z])
RAP = []
def rap(s): RAP.append(s); print("RAPOR " + s)
BUDGET = {"ejder_bas": 13000, "ejder_govde": 13000, "ejder_kanatlar": 4000}

ad = arm.animation_data; ad.action = None
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
arm.data.pose_position = "REST"; bpy.context.view_layer.update()
def deselect():
    for o in bpy.context.view_layer.objects: o.select_set(False)
def activate(o): deselect(); o.select_set(True); bpy.context.view_layer.objects.active = o
def tri(o): return sum(len(p.vertices) - 2 for p in o.data.polygons)

# --- 1) kemiğe bağlı -> deri (tek etki, ağırlık 1) ---
def to_skinned(o, bone):
    mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    activate(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
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
pl = bpy.context.active_object; pl.name = pl.data.name = "ejder_plakalar"
for m in [m for m in pl.modifiers if m.type == "ARMATURE"][1:]: pl.modifiers.remove(m)

# --- 2) bütçe ---
def decimate(o, target, sym=True):
    t0 = tri(o)
    for k in range(6):                         # tek geçiş hedefe inmezse (açık kenarlı zar) yineler; son çare simetrisiz
        t = tri(o)
        if t <= target * 1.01: break
        activate(o); dm = o.modifiers.new("dec", "DECIMATE"); dm.decimate_type = "COLLAPSE"; dm.ratio = target / t
        dm.use_symmetry = sym and k < 4; dm.symmetry_axis = "X"; dm.use_collapse_triangulate = True
        bpy.ops.object.modifier_move_to_index(modifier="dec", index=0); bpy.ops.object.modifier_apply(modifier="dec")
    rap(f"seyreltme {o.name}: {t0} -> {tri(o)}")
decimate(bpy.data.objects["ejder_bas"], BUDGET["ejder_bas"])
decimate(bpy.data.objects["ejder_govde"], BUDGET["ejder_govde"])
# kanatlar: çift yüz kopyasını at (kanat3_birlestir: ikinci yarı ters normal, 2 cm içe), seyrelt, yeniden çiftle
w = bpy.data.objects["ejder_kanatlar"]; n = len(w.data.polygons)
bm = bmesh.new(); bm.from_mesh(w.data); bm.faces.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[bm.faces[i] for i in range(n // 2, n)], context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
# zar ile kemik tüplerinin birleştiği kenarları 3+ yüz paylaşıyor; seyreltme bunlarda kilitleniyor (~10,5 bin
# üçgende duruyor). Ayır -> seyrelt -> 5 mm içindeki köşeleri yeniden kaynat.
nme = [e for e in bm.edges if not e.is_manifold and not e.is_boundary]; bmesh.ops.split_edges(bm, edges=nme)
bm.to_mesh(w.data); bm.free(); rap(f"kanat çift yüzü atıldı: {n} -> {len(w.data.polygons)}; ayrılan kenar {len(nme)}")
decimate(w, BUDGET["ejder_kanatlar"], sym=False)
bm = bmesh.new(); bm.from_mesh(w.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.005); bm.normal_update()
dup = bmesh.ops.duplicate(bm, geom=list(bm.faces))
for f in [g for g in dup["geom"] if isinstance(g, bmesh.types.BMFace)]: f.normal_flip()
bm.normal_update()
for v in [g for g in dup["geom"] if isinstance(g, bmesh.types.BMVert)]: v.co = v.co + v.normal * 0.02
bm.to_mesh(w.data); bm.free(); rap(f"kanat yeniden çift yüz: {tri(w)}")

PULS = sorted(o.name for o in bpy.data.objects if o.name.startswith("ejder_pul_"))
MESHES = ["ejder_bas", "ejder_govde", "ejder_kanatlar", "ejder_kalp", "ejder_plakalar"] + PULS

# --- 3) ağırlık temizliği ---
def clean_weights(o):
    activate(o); bones = {b.name for b in arm.data.bones}
    for g in list(o.vertex_groups):
        if g.name not in bones: o.vertex_groups.remove(g)
    bpy.ops.object.vertex_group_clean(group_select_mode="ALL", limit=0.01)
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    cnt = [len(v.groups) for v in o.data.vertices]; tot = [sum(g.weight for g in v.groups) for v in o.data.vertices]
    return max(cnt), cnt.count(0), sum(1 for t in tot if abs(t - 1) > 1e-3)
for nme in MESHES:
    mx, zero, bad = clean_weights(bpy.data.objects[nme])
    rap(f"ağırlık {nme}: en çok {mx} etki, ağırlıksız köşe {zero}, toplamı≠1 {bad}")

# --- 4) LOD1 ---
LOD = {}
for nme in ["ejder_bas", "ejder_govde", "ejder_kanatlar"]:
    o = bpy.data.objects[nme]; c = o.copy(); c.data = o.data.copy(); c.name = c.data.name = nme + "_LOD1"
    bpy.context.scene.collection.objects.link(c)
    for m in list(c.modifiers): c.modifiers.remove(m)
    activate(c); dm = c.modifiers.new("dec", "DECIMATE"); dm.decimate_type = "COLLAPSE"; dm.ratio = 0.5
    dm.use_symmetry = True; dm.symmetry_axis = "X"; dm.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier="dec")
    md = c.modifiers.new("Armature", "ARMATURE"); md.object = arm; clean_weights(c); LOD[nme] = c
    rap(f"LOD1 {nme}: {tri(o)} -> {tri(c)} üçgen")

# --- 5) kabul ölçümleri (dinlenme) ---
def bbox(o):
    P = np.array([b2a(o.matrix_world @ v.co) for v in o.data.vertices]); return P.min(0), P.max(0)
def open_edges(o):
    bm = bmesh.new(); bm.from_mesh(o.data); k = sum(1 for e in bm.edges if e.is_boundary); bm.free(); return k
t0 = sum(tri(bpy.data.objects[x]) for x in MESHES); t1 = sum(tri(c) for c in LOD.values()) + sum(tri(bpy.data.objects[x]) for x in MESHES if x not in LOD)
rap("bütçe LOD0: " + ", ".join(f"{x.replace('ejder_', '')} {tri(bpy.data.objects[x])}" for x in MESHES) + f" → toplam {t0} / 36000")
rap(f"bütçe LOD1: toplam {t1}")
for nme in MESHES + [c.name for c in LOD.values()]:
    o = bpy.data.objects[nme]; lo, hi = bbox(o)
    rap(f"{nme}: açık kenar {open_edges(o)}  X {lo[0]:.1f}..{hi[0]:.1f} Z {lo[1]:.1f}..{hi[1]:.1f} H {lo[2]:.1f}..{hi[2]:.1f}")

# --- 6) pozlu ölçüm ---
arm.data.pose_position = "POSE"
def posed_pts(nme):
    dg = bpy.context.evaluated_depsgraph_get(); o = bpy.data.objects[nme]; ev = o.evaluated_get(dg); me = ev.to_mesh()
    P = np.array([b2a(o.matrix_world @ v.co) for v in me.vertices]); ev.to_mesh_clear(); return P
for act in sorted(bpy.data.actions, key=lambda x: x.name):
    frames = [1] if act.name.startswith("Poz_") else [int(act.frame_range[0]), int(act.frame_range[1])]
    for f in frames:
        for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        ad.action = act
        try: ad.action_slot = act.slots[0]
        except Exception: pass
        bpy.context.scene.frame_set(f); bpy.context.view_layer.update()
        allp = np.vstack([posed_pts(x) for x in ["ejder_bas", "ejder_govde", "ejder_kanatlar"]]); Pb = posed_pts("ejder_bas")
        rap(f"poz {act.name}@{f}: baş H {Pb[:,2].min():.1f}..{Pb[:,2].max():.1f}, en güney Z {allp[:,1].min():.1f}, "
            f"yer altı (<-0,3 m) %{(allp[:, 2] < -0.3).mean() * 100:.2f}, X {allp[:,0].min():.1f}..{allp[:,0].max():.1f}")
ad.action = None; bpy.context.scene.frame_set(1)
if WORK: bpy.ops.wm.save_as_mainfile(filepath=WORK, copy=True)

# --- 7) seyrek poz aksiyonlarını doldur (yalnız FBX için): anahtarsız kemik = dinlenme ---
ALLB = [pb.name for pb in arm.pose.bones]
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
filled = 0
for act in bpy.data.actions:
    keyed = set()
    for lay in act.layers:
        for st in lay.strips:
            for cb in st.channelbags:
                for fc in cb.fcurves:
                    if fc.data_path.startswith('pose.bones["'): keyed.add(fc.data_path.split('"')[1])
    f0, f1 = int(act.frame_range[0]), int(act.frame_range[1]); ad.action = act
    try: ad.action_slot = act.slots[0]
    except Exception: pass
    for nme in ALLB:
        if nme in keyed: continue
        pb = arm.pose.bones[nme]; pb.matrix_basis = Matrix.Identity(4)
        for f in sorted({f0, f1}):
            for path in ("location", "rotation_quaternion", "scale"): pb.keyframe_insert(path, frame=f)
        filled += 1
rap(f"aksiyon doldurma: {filled} kemik-aksiyon çifti dinlenmeye sabitlendi")
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
ad.action = bpy.data.actions["Poz_Yatis"]
try: ad.action_slot = ad.action.slots[0]
except Exception: pass
bpy.context.scene.frame_set(1)

# --- 8) dışa aktar ---
deselect()
for o in [arm] + [bpy.data.objects[x] for x in MESHES] + list(LOD.values()): o.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=f"{OUT}/ejder.fbx", use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                         mesh_smooth_type="FACE", use_mesh_modifiers=True, bake_anim=True,
                         bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, path_mode="STRIP", embed_textures=False)
open(f"{OUT}/kabul_raporu.txt", "w", encoding="utf-8").write("\n".join(RAP) + "\n")
