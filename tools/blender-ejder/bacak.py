"""bacak.glb (aynalı bacak çifti) -> sol/sağ ön bacak, baş iskeletine eklenir -> ejder_on.
Model: pençe +X'e bakar, Y yukarı, bacaklar Z = ±0.3. Bir bacak (Z>0) alınır, aynalanır.
Arena: X doğu, Z kuzey, H yukarı; Blender (X, -Z, H).
"""
import sys, math
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix

SRC, BLEND, OUT = sys.argv[sys.argv.index("--") + 1:][:3]
S = 10.0                 # pençe 5,2 x 4,2 m
PAW_X, PAW_Z = 8.0, 14.5 # pençe referansı (model x=0) arena konumu
LEG_ZC = 0.30            # modeldeki bacak ekseni (z)
Y_KNEE = 0.45            # bunun üstü uzatılıp göğse yatırılır
STRETCH, LEAN = 2.0, 1.2
TARGET_TRIS = 4000       # bacak başına

# Unity (sol el): X doğu, Y yukarı, Z kuzey. Blender (sağ el) karşılığı: doğu = -X, kuzey = -Y, yukarı = +Z.
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])

def m2a(x, y, z, side):  # side -1 sol (batı), +1 sağ (doğu)
    yy = y if y < Y_KNEE else Y_KNEE + (y - Y_KNEE) * STRETCH
    lean = max(0.0, y - Y_KNEE) * S * LEAN
    return np.array([side * (PAW_X + (z - LEG_ZC) * S), PAW_Z - x * S + lean, yy * S])

bpy.ops.wm.open_mainfile(filepath=BLEND)
arm = bpy.data.objects["ejder_on_iskelet"]; head = bpy.data.objects["ejder_bas"]
existing = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=SRC)
leg = [o for o in bpy.data.objects if o not in existing and o.type == "MESH"][0]
bpy.ops.object.select_all(action="DESELECT"); leg.select_set(True); bpy.context.view_layer.objects.active = leg
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM"); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in [o for o in bpy.data.objects if o not in existing and o is not leg]: bpy.data.objects.remove(o)

me = leg.data; bm = bmesh.new(); bm.from_mesh(me)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
# Blender içe aktarım: model (x,y,z) -> (x,-z,y)
kill = []
for f in bm.faces:
    c = f.calc_center_median(); mx, my, mz = c.x, c.z, -c.y
    if mz < 0.02 or my > 0.72 or (my > 0.5 and mx > 0.08):
        kill.append(f)
bmesh.ops.delete(bm, geom=kill, context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
for v in bm.verts:
    mx, my, mz = v.co.x, v.co.z, -v.co.y
    v.co = a2b(m2a(mx, my, mz, -1))
bm.to_mesh(me); bm.free()
t0 = sum(len(p.vertices) - 2 for p in me.polygons)
d = leg.modifiers.new("d", "DECIMATE"); d.ratio = min(1, TARGET_TRIS / t0); d.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier="d")
t1 = sum(len(p.vertices) - 2 for p in me.polygons)
leg.name = leg.data.name = "ejder_bacak_L"
for m in leg.data.materials: m.name = "ejder_bacak_Mat"

# --- bacak eksenini ölç (arena) ---
V = np.array([b2a(v.co) for v in me.vertices])
def band(h0, h1):
    m = (V[:, 2] >= h0) & (V[:, 2] < h1)
    return np.array([np.median(V[m, 0]), np.median(V[m, 1]), (h0 + h1) / 2])
paw_tip = V[V[:, 1].argmin()]
J = {
    "omuz":  band(9.0, 10.5),
    "dirsek": band(4.0, 5.0),
    "bilek": band(1.2, 2.0),
}
J["uc"] = np.array([J["bilek"][0], paw_tip[1] + 0.8, 0.5])
print("RAPOR eklemler", {k: np.round(v, 1).tolist() for k, v in J.items()}, f"üçgen {t0}->{t1}")

# --- iskelete Gogus ve bacak kemikleri ---
bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm.data.edit_bones
g = eb.new("Gogus"); g.head = a2b((0, 27, 9)); g.tail = a2b((0, 21, 9)); g.roll = 0
eb["Boyun_1"].parent = g; eb["Boyun_1"].use_connect = False
CH = [("Omuz", "omuz", None), ("Kol_Ust", "omuz", "dirsek"), ("Kol_Alt", "dirsek", "bilek"), ("Pence", "bilek", "uc")]
for side, sx in (("L", 1), ("R", -1)):
    def P(k): p = J[k].copy(); p[0] *= sx; return p
    o = eb.new(f"Omuz_{side}"); top = P("omuz") + np.array([0, 3.0, 1.0])
    o.head = a2b(top); o.tail = a2b(P("omuz")); o.parent = g
    prev = o
    for n, a, b in CH[1:]:
        e = eb.new(f"{n}_{side}"); e.head = a2b(P(a)); e.tail = a2b(P(b)); e.parent = prev; e.use_connect = True; e.roll = 0; prev = e
bpy.ops.object.mode_set(mode="OBJECT")

# --- ağırlıklar (sol), sonra aynala ---
names = ["Omuz_L", "Kol_Ust_L", "Kol_Alt_L", "Pence_L"]
segs = [(J["omuz"] + np.array([0, 3.0, 1.0]), J["omuz"]), (J["omuz"], J["dirsek"]), (J["dirsek"], J["bilek"]), (J["bilek"], J["uc"])]
W = np.zeros((len(V), 4)); bd = np.full(len(V), np.inf); bs = np.zeros(len(V), int); bt = np.zeros(len(V))
for s, (a, b) in enumerate(segs):
    ab = b - a; t = np.clip(((V - a) @ ab) / (ab @ ab), 0, 1); dd = np.linalg.norm(V - (a + t[:, None] * ab), axis=1)
    m = dd < bd; bd[m] = dd[m]; bs[m] = s; bt[m] = t[m]
for i in range(len(V)):
    s, t = bs[i], bt[i]
    if V[i, 2] < 1.4: W[i, 3] = 1; continue
    W[i, s] = 1
    if t < 0.25 and s > 0: w = (0.25 - t) / 0.5; W[i, s - 1] = w; W[i, s] = 1 - w
    elif t > 0.75 and s < 3: w = (t - 0.75) / 0.5; W[i, s + 1] = w; W[i, s] = 1 - w
for j, n in enumerate(names):
    vg = leg.vertex_groups.new(name=n)
    for i in np.nonzero(W[:, j] > 1e-4)[0]: vg.add([int(i)], float(W[i, j]), "REPLACE")
# aynala -> sağ
legR = leg.copy(); legR.data = leg.data.copy(); bpy.context.collection.objects.link(legR)
legR.name = legR.data.name = "ejder_bacak_R"
bm = bmesh.new(); bm.from_mesh(legR.data)
for v in bm.verts: v.co.x = -v.co.x
bmesh.ops.reverse_faces(bm, faces=bm.faces); bm.to_mesh(legR.data); bm.free()
for vg in legR.vertex_groups: vg.name = vg.name[:-2] + "_R"
for o in (leg, legR):
    o.parent = arm; md = o.modifiers.new("Armature", "ARMATURE"); md.object = arm
# başı da Gogus'a bağlı tut (ağırlıklar değişmedi)

# --- pençe pozları: analitik 2 kemik IK (Kol_Ust, Kol_Alt) + Pence açısı ---
bones = arm.data.bones
def ik_pose(side, wrist, pitch=0.0, yaw=0.0):
    sx = 1 if side == "L" else -1
    nu, nl, npw = f"Kol_Ust_{side}", f"Kol_Alt_{side}", f"Pence_{side}"
    Sh = b2a(bones[nu].head_local); a = bones[nu].length; b = bones[nl].length
    W_ = np.array(wrist, float); dvec = W_ - Sh; dist = np.linalg.norm(dvec)
    dist_c = min(dist, (a + b) * 0.999); u = dvec / dist
    pole = np.array([0, 1.0, 0])                    # dirsek arkaya (kuzeye) bükülür
    pole = pole - (pole @ u) * u; pole /= np.linalg.norm(pole)
    x = (a * a - b * b + dist_c * dist_c) / (2 * dist_c); h = math.sqrt(max(a * a - x * x, 0))
    K = Sh + u * x + pole * h; Wc = Sh + u * dist_c
    pts = {nu: (Sh, K), nl: (K, Wc)}
    for n, (p0, p1) in pts.items():
        pb = arm.pose.bones[n]; rest = bones[n].matrix_local
        d0 = (bones[n].tail_local - bones[n].head_local).normalized(); d1 = (a2b(p1) - a2b(p0)).normalized()
        R = d0.rotation_difference(d1).to_matrix().to_4x4()
        pb.matrix = Matrix.Translation(a2b(p0)) @ R @ Matrix.Translation(-rest.translation) @ rest
        bpy.context.view_layer.update()
    rest = bones[npw].matrix_local
    R = Matrix.Rotation(math.radians(yaw * sx), 4, "Z") @ Matrix.Rotation(math.radians(pitch), 4, "X")
    arm.pose.bones[npw].matrix = Matrix.Translation(a2b(Wc)) @ R @ Matrix.Translation(-rest.translation) @ rest
    bpy.context.view_layer.update()
    return dist - dist_c

bil = {s: b2a(bones[f"Pence_{s}"].head_local) for s in ("L", "R")}
LEG_POSES = [
    # ad, taraf, bilek kayması (dx dışa +, dz kuzey +, dh), pitch, yaw
    ("Pence_{s}_Havada", (0.0, 1.0, 4.0), 30, 0),
    ("Pence_{s}_Yerde", (0.0, -0.5, 0.0), -4, 0),
    ("Pence_{s}_Acik", (3.0, -0.5, 2.0), 10, -40),
    ("Pence_{s}_Supurme_Sonu", (-3.0, -1.5, 0.5), -2, 30),
]
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
rep = []
for s in ("L", "R"):
    out = -1 if s == "L" else 1
    for name, (dx, dz, dh), pit, yw in LEG_POSES:
        for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        w = bil[s] + np.array([dx * out, dz, dh])
        miss = ik_pose(s, w, pit, yw)
        act = bpy.data.actions.new("Poz_" + name.format(s=s)); act.use_fake_user = True
        arm.animation_data_create(); arm.animation_data.action = act
        for bn in (f"Kol_Ust_{s}", f"Kol_Alt_{s}", f"Pence_{s}"):
            pb = arm.pose.bones[bn]; pb.keyframe_insert("location", frame=1); pb.keyframe_insert("rotation_quaternion", frame=1)
        rep.append(f"{act.name}: erişilemeyen {miss:.2f} m")
for pb in arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
bpy.ops.object.mode_set(mode="OBJECT")
arm.animation_data.action = bpy.data.actions["Poz_Yatis"]
arm.name = "ejder_on"
bpy.ops.wm.save_as_mainfile(filepath=f"{OUT}/ejder_on.blend")
bpy.ops.object.select_all(action="DESELECT")
for o in (arm, head, leg, legR): o.select_set(True)
bpy.ops.export_scene.fbx(filepath=f"{OUT}/ejder_on.fbx", use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, path_mode="COPY")
VL = np.array([b2a(v.co) for v in leg.data.vertices])
print(f"RAPOR sol bacak X {VL[:,0].min():.1f}..{VL[:,0].max():.1f} Z {VL[:,1].min():.1f}..{VL[:,1].max():.1f} H {VL[:,2].min():.1f}..{VL[:,2].max():.1f}")
tot = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in (head, leg, legR))
print(f"RAPOR ejder_on toplam üçgen {tot}")
print("\n".join("RAPOR " + r for r in rep))
