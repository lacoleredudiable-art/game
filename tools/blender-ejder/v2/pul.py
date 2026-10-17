"""Can katmanı pulları (18 KE-3.4, SI-9.7): sol pençe (%80), alın (%60), sağ pençe (%40), boyun (%20).
Her pul, kaynak mesh'in bir yamasının kopyasıdır (UV, malzeme ve kemik ağırlıkları korunur), 4 cm dışa itilir ve
12 cm kalınlık alır. Kaynak yerinde kalır: pul koptuğunda altındaki yüzey görünür (kor yarası Unity'de emission ile).
Konumlar dinlenme mesh'inde yukarıdan / önden ışın atılarak bulunur. Tekrar çalıştırmak güvenlidir.
python pul.py -- <ejder.blend>"""
import sys, os
import bpy, bmesh
from mathutils import Vector
B = os.path.abspath(sys.argv[sys.argv.index("--") + 1:][0])
bpy.ops.wm.open_mainfile(filepath=B)
def a2b(x, z, h): return Vector((-x, -z, h))
def b2a(v): return (-v.x, -v.y, v.z)
def rap(s): print("RAPOR " + s)
arm = bpy.data.objects["ejder_on"]
ad0 = arm.animation_data.action if arm.animation_data else None
if arm.animation_data: arm.animation_data.action = None          # ışın dinlenme mesh'ine atılsın
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.view_layer.update()
for o in [o for o in bpy.data.objects if o.name.startswith("ejder_pul_")]: bpy.data.objects.remove(o)
# ad, kaynak, ışın başlangıcı (arena x, z, h), yön (arena), yarıçap (m)
PUL = [("sol_pence", "ejder_govde", (-8.0, 13.8, 20.0), (0, 0, -1), 0.9),
       ("alin", "ejder_bas", (0.0, 11.0, 20.0), (0, 0, -1), 0.8),
       ("sag_pence", "ejder_govde", (8.0, 13.8, 20.0), (0, 0, -1), 0.9),
       ("boyun", "ejder_bas", (0.0, 17.5, 25.0), (0, 0, -1), 1.0)]
for ad, kaynak, o_, d_, r in PUL:
    src = bpy.data.objects[kaynak]
    mw = src.matrix_world; mi = mw.inverted()
    org = mi @ a2b(*o_); dirv = (mi.to_3x3() @ a2b(*d_)).normalized()
    ok, loc, nor, fi = src.ray_cast(org, dirv)
    if not ok: raise SystemExit(f"[pul] {ad}: ışın {kaynak} yüzeyine değmedi")
    for o in bpy.context.view_layer.objects: o.select_set(False)
    bpy.context.view_layer.objects.active = src; src.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT"); bm = bmesh.from_edit_mesh(src.data)
    for f in bm.faces: f.select_set(False)
    n = 0; rr = r
    while n < 20 and rr < 3 * r:                 # seyrek bölgede yarıçap büyür
        n = 0
        for f in bm.faces:
            s_ = (f.calc_center_median() - loc).length < rr and f.normal.dot(nor) > 0.3; f.select_set(s_); n += s_
        rr *= 1.25
    bm.select_mode = {"FACE"}; bm.select_flush_mode(); bpy.context.tool_settings.mesh_select_mode = (False, False, True)
    bmesh.update_edit_mesh(src.data)
    if n == 0:
        dmin = min((f.calc_center_median() - loc).length for f in bm.faces)
        raise SystemExit(f"[pul] {ad}: yüz seçilemedi (isabet {tuple(round(x,1) for x in b2a(mw @ loc))}, en yakın yüz {dmin:.2f} m, normal {tuple(round(x,2) for x in nor)})")
    bpy.ops.mesh.duplicate(); bpy.ops.mesh.separate(type="SELECTED"); bpy.ops.object.mode_set(mode="OBJECT")
    p = [o for o in bpy.context.selected_objects if o is not src][0]
    p.name = p.data.name = f"ejder_pul_{ad}"
    bm = bmesh.new(); bm.from_mesh(p.data); bm.normal_update()
    for v in bm.verts: v.co += v.normal * 0.04
    bm.to_mesh(p.data); bm.free()
    sm = p.modifiers.new("kalinlik", "SOLIDIFY"); sm.thickness = 0.12; sm.offset = 1.0
    bpy.context.view_layer.objects.active = p
    for m in list(p.modifiers):
        if m.type != "ARMATURE": bpy.ops.object.modifier_apply(modifier=m.name)
    if not any(m.type == "ARMATURE" for m in p.modifiers): md = p.modifiers.new("Armature", "ARMATURE"); md.object = arm
    c = mw @ loc
    rap(f"{p.name}: {n} yüz -> {sum(len(q.vertices) - 2 for q in p.data.polygons)} üçgen, merkez (dinlenme) "
        f"{tuple(round(x, 1) for x in b2a(c))}, gruplar {[g.name for g in p.vertex_groups][:4]}")
if ad0:
    arm.animation_data.action = ad0
    try: arm.animation_data.action_slot = ad0.slots[0]
    except Exception: pass
bpy.ops.wm.save_as_mainfile(filepath=B)
