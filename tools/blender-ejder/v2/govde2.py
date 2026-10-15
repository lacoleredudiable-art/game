"""Yeni Tripo gövdesi -> sfenks dinlenme pozu, oyun iskeletinin adlarıyla ağırlıklı.
1) GLB'yi arena koordinatına ölçekle (57x), ön bacakları uzat (k), ayakta pozda kemik kur ve ağırlık ver.
2) Sfenks pozuna bük (karın yere, ön bilek pençe bölgesine, arka bacak katlı, kuyruk batıya kıvrık), sabitle.
3) Kemiklerin sfenks konumlarını JSON'a yaz (ana iskelet bunlarla güncellenecek).
python govde2.py -- <govde.glb> <çıkış.blend> <kemik.json>"""
import sys, math, json
import bpy, bmesh
import numpy as np
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index("--") + 1:]
GLB, OUT, JS = a[:3]
S_ = 57.0; Z0 = 17.0; YF = -0.45; K = 1.30; DROP = 7.0
def a2b(p): return Vector((-p[0], -p[1], p[2]))
def b2a(v): return np.array([-v.x, -v.y, v.z])
def nrm(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
def raw2ar(x, y, z): return np.array([S_ * x, Z0 + (y - YF) * S_, S_ * z])
def rap(s): print("RAPOR " + s)

bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=GLB)
g = [o for o in bpy.data.objects if o.type == "MESH"][0]; g.name = "ejder_govde"; g.data.name = "ejder_govde"
me = g.data
R = np.array([g.matrix_world @ v.co for v in me.vertices])
g.parent = None; g.matrix_world = Matrix.Identity(4)
P = np.array([raw2ar(*r) for r in R])                      # arena, ayakta

# --- boyun kütüğünü göğse bastır (kesme yok, delik açılmaz) ---
bm = bmesh.new(); bm.from_mesh(me)
for v, p in zip(bm.verts, P): v.co = a2b(p)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
bm.to_mesh(me); bm.free()
P = np.array([b2a(v.co) for v in me.vertices])
ZC = Z0 + 0.06 * S_
f = np.clip((P[:, 2] - 0.22 * S_) / (0.05 * S_), 0, 1) * (P[:, 1] < ZC)
P[:, 1] = P[:, 1] - f * 0.7 * (P[:, 1] - ZC)
rap(f"kütük bastırıldı: {(f > 0).sum()} köşe; en ön Z {P[f > 0, 1].min():.1f}")
# --- ön bacak uzatma: bacak ekseni çevresinde, bağlantı yüksekliğinin altında ---
FCL = {s: (sx * 0.133 * S_, Z0 + (-0.300 - YF) * S_) for s, sx in (("L", -1), ("R", 1))}
HA = 0.17 * S_; HW = 0.04 * S_
for s, (cx, cz) in FCL.items():
    r = np.hypot(P[:, 0] - cx, P[:, 1] - (cz - 1.5))
    w = np.clip((5.0 - r) / 1.2, 0, 1) * (P[:, 2] < HA)
    H = P[:, 2]; Hn = np.where(H > HW, HA - (HA - H) * K, H - (K - 1) * (HA - HW))
    P[:, 2] = H + w * (Hn - H)
for v, p in zip(me.vertices, P): v.co = a2b(p)
me.update()

# --- ayakta eklemler (uzatılmış) ---
def fl(sx):
    S = np.array([sx * 0.130 * S_, Z0 + (-0.300 - YF) * S_, 0.22 * S_])
    E = np.array([sx * 0.135 * S_, Z0 + (-0.300 - YF) * S_, HA - (HA - 0.11 * S_) * K])
    W = np.array([sx * 0.133 * S_, Z0 + (-0.320 - YF) * S_, HA - (HA - HW) * K])
    T = np.array([sx * 0.140 * S_, Z0 + (-0.400 - YF) * S_, 0.005 * S_ - (K - 1) * (HA - HW)])
    O = S + np.array([-sx * 0.8, 3.0, 2.0])
    return O, S, E, W, T
def hl(sx):
    Hp = np.array([sx * 0.100 * S_, Z0 + (-0.100 - YF) * S_, 0.21 * S_])
    Kn = np.array([sx * 0.115 * S_, Z0 + (-0.110 - YF) * S_, 0.14 * S_])
    An = np.array([sx * 0.125 * S_, Z0 + (-0.100 - YF) * S_, 0.05 * S_])
    T = np.array([sx * 0.143 * S_, Z0 + (-0.170 - YF) * S_, 0.005 * S_])
    return Hp, Kn, An, T
NT = 6
TAIL = [np.array([0.0, Z0 + (y - YF) * S_, (0.235 + 0.04 * max(0, y)) * S_]) for y in np.linspace(-0.04, 0.50, NT + 1)]

arm_d = bpy.data.armatures.new("fit"); arm = bpy.data.objects.new("fit", arm_d); bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT"); eb = arm_d.edit_bones
def nb(n, h, t, par=None, con=False):
    b = eb.new(n); b.head = a2b(h); b.tail = a2b(t); b.roll = 0
    if par: b.parent = eb[par]; b.use_connect = con
    return b
nb("Kok", (0, 40, 7.0), (0, 34, 7.0)); nb("Gogus", (0, 27, 9 + 7.0), (0, 21, 9 + 7.0), "Kok")
J = {}
for s, sx in (("L", -1), ("R", 1)):
    O, S, E, W, T = fl(sx); J[s] = (O, S, E, W, T)
    nb(f"Omuz_{s}", O, S, "Gogus"); nb(f"Kol_Ust_{s}", S, E, f"Omuz_{s}", True); nb(f"Kol_Alt_{s}", E, W, f"Kol_Ust_{s}", True); nb(f"Pence_{s}", W, T, f"Kol_Alt_{s}", True)
    Hp, Kn, An, T2 = hl(sx); J["H" + s] = (Hp, Kn, An, T2)
    nb(f"Arka_Ust_{s}", Hp, Kn, "Kok"); nb(f"Arka_Alt_{s}", Kn, An, f"Arka_Ust_{s}", True); nb(f"Arka_Ayak_{s}", An, T2, f"Arka_Alt_{s}", True)
for i in range(NT):
    nb(f"Kuyruk_{i+1:02d}", TAIL[i], TAIL[i + 1], "Kok" if i == 0 else f"Kuyruk_{i:02d}", i > 0)
bpy.ops.object.mode_set(mode="OBJECT")

# --- ağırlıklar (ayakta) ---
P = np.array([b2a(v.co) for v in me.vertices]); X, Z, H = P.T
W_ = {}
def addw(n, w):
    W_[n] = W_.get(n, np.zeros(len(P))) + w
def seg_assign(pts, chain, mem):
    """chain: [(ad, A, B)], mem: üyelik ağırlığı. En yakın parça + eklemde yumuşak geçiş."""
    D = []; T_ = []
    for n, A, B in chain:
        ab = B - A; t = np.clip(((pts - A) @ ab) / (ab @ ab), 0, 1); d = np.linalg.norm(pts - (A + t[:, None] * ab), axis=1)
        D.append(d); T_.append(t)
    D = np.array(D); inv = 1.0 / np.maximum(D, 0.05) ** 4; inv /= inv.sum(0)
    for k, (n, A, B) in enumerate(chain): addw(n, mem * inv[k])
torso = np.ones(len(P))
for s, sx in (("L", -1), ("R", 1)):
    O, S, E, W, T = J[s]
    cx, cz = FCL[s]; r = np.hypot(X - cx, Z - (cz - 1.5))
    mem = np.clip((5.2 - r) / 1.4, 0, 1) * np.clip((S[2] + 0.5 - H) / 2.5, 0, 1) * (np.sign(X) == sx)
    seg_assign(P, [(f"Omuz_{s}", O, S), (f"Kol_Ust_{s}", S, E), (f"Kol_Alt_{s}", E, W), (f"Pence_{s}", W, T)], mem); torso -= mem
    Hp, Kn, An, T2 = J["H" + s]
    hx, hz = sx * 0.12 * S_, Z0 + (-0.115 - YF) * S_; r = np.hypot(X - hx, Z - hz)
    mem = np.clip((5.6 - r) / 1.4, 0, 1) * np.clip((Hp[2] + 0.5 - H) / 3.0, 0, 1) * (np.sign(X) == sx)
    mem = np.minimum(mem, torso)
    seg_assign(P, [(f"Arka_Ust_{s}", Hp, Kn), (f"Arka_Alt_{s}", Kn, An), (f"Arka_Ayak_{s}", An, T2)], mem); torso -= mem
memt = np.clip((Z - (TAIL[0][1] + 1.0)) / 4.0, 0, 1); memt = np.minimum(memt, torso)
seg_assign(P, [(f"Kuyruk_{i+1:02d}", TAIL[i], TAIL[i + 1]) for i in range(NT)], memt); torso -= memt
wg = np.clip((32.0 - Z) / 8.0, 0, 1)
addw("Gogus", torso * wg); addw("Kok", torso * (1 - wg))
tot = sum(W_.values());
for n in W_: W_[n] /= np.maximum(tot, 1e-9)
for n, w in W_.items():
    vg = g.vertex_groups.new(name=n)
    for i in np.where(w > 0.01)[0]: vg.add([int(i)], float(w[i]), "REPLACE")
g.parent = arm; md = g.modifiers.new("Armature", "ARMATURE"); md.object = arm
rap("ağırlık grupları: " + ", ".join(f"{n}:{int((w>0.5).sum())}" for n, w in sorted(W_.items())))

# --- sfenks pozu ---
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
def set_world(name, p0, p1):
    pb = arm.pose.bones[name]; bone = arm_d.bones[name]; rest = bone.matrix_local
    d0 = (bone.tail_local - bone.head_local).normalized(); d1 = (a2b(p1) - a2b(p0)).normalized()
    Rr = d0.rotation_difference(d1).to_matrix().to_4x4()
    pb.matrix = Matrix.Translation(a2b(p0)) @ Rr @ Matrix.Translation(-rest.translation) @ rest
    bpy.context.view_layer.update()
def head(n): return b2a(arm.pose.bones[n].head)
def L(n): return arm_d.bones[n].length
arm.pose.bones["Kok"].location = (0, -DROP, 0)      # kemik uzayı: Kok -Y(blender) ... aşağıda doğrulanır
bpy.context.view_layer.update()
dz = head("Kok")[2] - 7.0
if abs(dz + DROP) > 0.01:                              # yön tutmadıysa dünya uzayında indir
    pb = arm.pose.bones["Kok"]; pb.location = (0, 0, 0); bpy.context.view_layer.update()
    pb.matrix = Matrix.Translation((0, 0, -DROP)) @ pb.matrix; bpy.context.view_layer.update()
rap(f"Kok indirildi: H {head('Kok')[2]:.2f}")
def ik2(Sh, Wt, a_, b_, pole):
    dv = Wt - Sh; dist = np.linalg.norm(dv); u = dv / dist; dc = min(dist, (a_ + b_) * 0.999)
    pl = pole - (pole @ u) * u; pl /= np.linalg.norm(pl)
    x = (a_ * a_ - b_ * b_ + dc * dc) / (2 * dc); h = math.sqrt(max(a_ * a_ - x * x, 0)); return Sh + u * x + pl * h, Sh + u * dc, dist
for s, sx in (("L", -1), ("R", 1)):
    Sh = head(f"Kol_Ust_{s}")
    Wt = np.array([sx * 8.05, 16.2, 1.8])
    El, Wc, dist = ik2(Sh, Wt, L(f"Kol_Ust_{s}"), L(f"Kol_Alt_{s}"), np.array([sx * 0.4, 1.0, -0.35]))
    set_world(f"Kol_Ust_{s}", Sh, El); set_world(f"Kol_Alt_{s}", El, Wc)
    Tt = Wc + L(f"Pence_{s}") * nrm([0, -1.0, -0.25]); set_world(f"Pence_{s}", Wc, Tt)
    rap(f"ön {s}: omuz {np.round(Sh,1)} dirsek {np.round(El,1)} bilek {np.round(Wc,1)} (hedef uzaklık {dist:.1f} / bacak {L(f'Kol_Ust_{s}')+L(f'Kol_Alt_{s}'):.1f}) uç {np.round(Tt,1)}")
    Hp = head(f"Arka_Ust_{s}")
    Kn = Hp + L(f"Arka_Ust_{s}") * nrm([sx * 0.35, -0.75, -0.56]); set_world(f"Arka_Ust_{s}", Hp, Kn)
    An = Kn + L(f"Arka_Alt_{s}") * nrm([sx * 0.1, 0.95, -0.25]); set_world(f"Arka_Alt_{s}", Kn, An)
    Tf = An + L(f"Arka_Ayak_{s}") * nrm([sx * 0.1, -0.97, -0.2]); set_world(f"Arka_Ayak_{s}", An, Tf)
    rap(f"arka {s}: kalça {np.round(Hp,1)} diz {np.round(Kn,1)} bilek {np.round(An,1)} uç {np.round(Tf,1)}")
TD = [(0, 1, -0.55), (-0.25, 1, -0.25), (-0.6, 0.8, -0.08), (-0.9, 0.4, 0), (-1, -0.15, 0), (-0.8, -0.6, 0)]
cur = head("Kuyruk_01")
for i in range(NT):
    n = f"Kuyruk_{i+1:02d}"; nxt = cur + L(n) * nrm(TD[i]); set_world(n, cur, nxt); cur = nxt
rap(f"kuyruk ucu {np.round(cur,1)}")

# kemik sfenks konumları -> JSON
out = {}
for pb in arm.pose.bones:
    out[pb.name] = {"head": list(map(float, b2a(pb.head))), "tail": list(map(float, b2a(pb.tail))),
                    "parent": pb.parent.name if pb.parent else None}
json.dump(out, open(JS, "w"), indent=1)

# sabitle: değiştiriciyi uygula, iskeleti çıkar (gruplar kalır)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.objects.active = g; g.select_set(True)
bpy.ops.object.modifier_apply(modifier="Armature")
g.parent = None
Pz = np.array([b2a(v.co) for v in g.data.vertices])
rap(f"sfenks gövde: X {Pz[:,0].min():.1f}..{Pz[:,0].max():.1f} Z {Pz[:,1].min():.1f}..{Pz[:,1].max():.1f} H {Pz[:,2].min():.1f}..{Pz[:,2].max():.1f}; yer altı (H<-0.3) %{(Pz[:,2]<-0.3).mean()*100:.2f}")
bpy.data.objects.remove(arm)
bpy.ops.wm.save_as_mainfile(filepath=OUT)
