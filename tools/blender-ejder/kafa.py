"""ejder_kafa.glb -> ejder_on (baş + boyun) iskeletli, yatış pozunda, arena koordinatlarında.

Model (glTF): baş +X'e bakar, boyun dik, taban Y=0. Ölçek: kafatası arkasından buruna ~9,6 m (KE-2.3).
Arena: X doğu, Z kuzey, H yukarı. Blender: (X, -Z, H).
"""
import sys, math
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix

SRC, OUT = sys.argv[sys.argv.index("--") + 1:][:2]
S = 22.5
SNOUT_X = 0.488
TARGET_TRIS = 14000

def m2a(x, y, z):            # model -> arena (dinlenme: dik boyun)
    return np.array([z * S, 7.5 + (SNOUT_X - x) * S, y * S])

def a2b(p):                  # arena -> blender
    return Vector((p[0], -p[1], p[2]))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
obj = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
bpy.context.view_layer.objects.active = obj; obj.select_set(True)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.context.scene.objects):
    if o is not obj: bpy.data.objects.remove(o)
me = obj.data
bm = bmesh.new(); bm.from_mesh(me)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
for v in bm.verts:                       # Blender içe aktarım: model (x,y,z) -> (x,-z,y)
    mx, my, mz = v.co.x, v.co.z, -v.co.y
    v.co = a2b(m2a(mx, my, mz))
bm.to_mesh(me); bm.free()
tris = sum(len(p.vertices) - 2 for p in me.polygons)
if tris > TARGET_TRIS:
    d = obj.modifiers.new("d", "DECIMATE"); d.ratio = TARGET_TRIS / tris; d.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier="d")
tris2 = sum(len(p.vertices) - 2 for p in me.polygons)

# --- dinlenme iskeleti: boyun merkez çizgisi (model x,y; z=0) ---
neck_m = [(-0.17, 0.00), (-0.24, 0.18), (-0.27, 0.36), (-0.24, 0.54), (-0.12, 0.70), (0.06, 0.64)]
head_m = [(0.06, 0.64), (0.45, 0.60)]
P = [m2a(x, y, 0) for x, y in neck_m] + [m2a(*head_m[1], 0)]
names = ["Boyun_1", "Boyun_2", "Boyun_3", "Boyun_4", "Boyun_5", "Bas"]

bpy.ops.object.armature_add(location=(0, 0, 0))
arm = bpy.context.active_object; arm.name = arm.data.name = "ejder_on_iskelet"
bpy.ops.object.mode_set(mode="EDIT")
eb = arm.data.edit_bones; eb.remove(eb[0])
prev = None
for i, n in enumerate(names):
    b = eb.new(n); b.head = a2b(P[i]); b.tail = a2b(P[i + 1])
    b.roll = 0
    if prev: b.parent = prev; b.use_connect = True
    prev = b
bpy.ops.object.mode_set(mode="OBJECT")

# --- ağırlıklar: merkez çizgisine izdüşüm, eklem yakınında yumuşak geçiş ---
Pa = np.array(P)
V = np.array([(v.co.x, -v.co.y, v.co.z) for v in me.vertices])  # arena
nb = len(names)
W = np.zeros((len(V), nb))
best_d = np.full(len(V), np.inf); best_s = np.zeros(len(V), int); best_t = np.zeros(len(V))
for s in range(nb):
    a, b = Pa[s], Pa[s + 1]; ab = b - a
    t = np.clip(((V - a) @ ab) / (ab @ ab), 0, 1)
    d = np.linalg.norm(V - (a + t[:, None] * ab), axis=1)
    m = d < best_d; best_d[m] = d[m]; best_s[m] = s; best_t[m] = t[m]
# baş bölgesi (modelde x>0.05 ve y>0.40) kesin Bas
mx = SNOUT_X - (V[:, 1] - 7.5) / S; my = V[:, 2] / S
head_mask = (mx > 0.05) & (my > 0.40)
for i in range(len(V)):
    s, t = best_s[i], best_t[i]
    if head_mask[i]:
        W[i, nb - 1] = 1.0; continue
    W[i, s] = 1.0
    if t < 0.3 and s > 0:
        w = (0.3 - t) / 0.6; W[i, s - 1] = w; W[i, s] = 1 - w
    elif t > 0.7 and s < nb - 1:
        w = (t - 0.7) / 0.6; W[i, s + 1] = w; W[i, s] = 1 - w
for j, n in enumerate(names):
    vg = obj.vertex_groups.new(name=n)
    for i in np.nonzero(W[:, j] > 1e-4)[0]:
        vg.add([int(i)], float(W[i, j]), "REPLACE")
obj.parent = arm
mod = obj.modifiers.new("Armature", "ARMATURE"); mod.object = arm

# --- yatış pozu: baş yere, boyun gövdeye kıvrılır ---
L = [np.linalg.norm(Pa[i + 1] - Pa[i]) for i in range(5)]
neck_len = sum(L)
J = Pa[5].copy(); J_target = np.array([0.0, J[1], (0.64 - 0.439) * S])   # çene yerde
def bez(p0, p1, p2, p3, t):
    return ((1 - t) ** 3)[:, None] * p0 + (3 * (1 - t) ** 2 * t)[:, None] * p1 + (3 * (1 - t) * t ** 2)[:, None] * p2 + (t ** 3)[:, None] * p3
best = None
for zb in np.arange(24, 45, 0.25):
    B = np.array([0.0, zb, 10.0])
    c1 = J_target + np.array([0, 3.0, 6.0]); c2 = B + np.array([0, -6.0, 3.0])
    tt = np.linspace(0, 1, 400); pts = bez(J_target, c1, c2, B, tt)
    ln = np.sum(np.linalg.norm(np.diff(pts, axis=0), axis=1))
    if best is None or abs(ln - neck_len) < abs(best[0] - neck_len): best = (ln, zb, pts)
ln, zb, pts = best
seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
Q = [J_target]; acc = 0.0
for l in reversed(L):                    # J'den tabana doğru
    acc += l; Q.append(pts[np.searchsorted(seg, acc).clip(max=len(pts) - 1)])
Q = Q[::-1]                              # Q0 taban ... Q5 = J
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
for i, n in enumerate(names):
    pb = arm.pose.bones[n]
    rest = arm.data.bones[n].matrix_local.copy()
    if i < 5:
        d0 = Vector(a2b(Pa[i + 1]) - a2b(Pa[i])).normalized(); d1 = Vector(a2b(Q[i + 1]) - a2b(Q[i])).normalized()
        R = d0.rotation_difference(d1).to_matrix().to_4x4()
        M = Matrix.Translation(a2b(Q[i])) @ R @ Matrix.Translation(-rest.translation) @ rest
    else:
        M = Matrix.Translation(a2b(J_target) - rest.translation) @ rest   # baş yatay kalır, aşağı iner
    pb.matrix = M
    bpy.context.view_layer.update()
bpy.ops.object.mode_set(mode="OBJECT")
# pozu dinlenme yap
bpy.context.view_layer.objects.active = obj
bpy.ops.object.modifier_apply(modifier="Armature")
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE"); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode="OBJECT")
mod = obj.modifiers.new("Armature", "ARMATURE"); mod.object = arm
obj.name = obj.data.name = "ejder_bas"
for mat in obj.data.materials: mat.name = "ejder_bas_Mat"
for mat in obj.data.materials:
    for nd in mat.node_tree.nodes:
        if nd.type == "TEX_IMAGE" and nd.image:
            nd.image.filepath_raw = f"{OUT}/ejder_bas_albedo.png"; nd.image.file_format = "PNG"; nd.image.save()

Va = np.array([(obj.matrix_world @ v.co) for v in me.vertices]); Va = np.c_[Va[:, 0], -Va[:, 1], Va[:, 2]]
print(f"RAPOR üçgen {tris} -> {tris2}; boyun uzunluğu {neck_len:.1f} m, eğri {ln:.1f} m, taban Z {zb:.1f}")
print(f"RAPOR baş+boyun X {Va[:,0].min():.1f}..{Va[:,0].max():.1f} Z {Va[:,1].min():.1f}..{Va[:,1].max():.1f} H {Va[:,2].min():.1f}..{Va[:,2].max():.1f}")
hm = Va[:, 1] < 17.5
print(f"RAPOR baş: Z {Va[hm,1].min():.1f}..17.5, H {Va[hm,2].min():.1f}..{Va[hm,2].max():.1f}")
for z in (18, 20, 22, 25):
    m = abs(Va[:, 1] - z) < 0.5
    if m.any(): print(f"RAPOR boyun Z={z}: H {Va[m,2].min():.1f}..{Va[m,2].max():.1f}")
bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=f"{OUT}/ejder_bas.fbx", use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                         bake_anim=False, path_mode="COPY", mesh_smooth_type="FACE")
bpy.ops.wm.save_as_mainfile(filepath=f"{OUT}/ejder_bas.blend")
print("RAPOR tamam")
