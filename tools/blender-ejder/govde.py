"""ejder_kaba.glb -> ejder_govde (sabit gövde + kanatlar), arena koordinatlarında.

Arena: X doğu, Z kuzey, Y yukarı (Unity). Blender: X, Y, Z yukarı; Unity +Z = Blender -Y.
Gövde, pivotu arena merkezinde (0,0,0) olacak şekilde konumlanır; Unity'de konum ve dönüş sıfır.
"""
import sys, math
import bpy, bmesh
from mathutils import Vector

SRC, OUT_DIR = sys.argv[sys.argv.index("--") + 1:][:2]
SCALE = 45.0          # Tripo kutusu 1 birim -> 45 m (KE-2.1)
CUT_Z = 12.5          # bu Z'nin (arena) önündeki baş ve boyun kesilir
LEG_CUT = dict(abs_x=4.5, z_max=22.0, y_max=9.0)  # göğsün altındaki ön bacaklar
SHIFT_NORTH = 6.0     # kesik ön yüz Z=18,5'e (duvar çizgisi) gelsin
TARGET_TRIS = 10000   # AL-1.19
TEX_SIZE = 1024       # AL-1.20

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
obj = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
for o in list(bpy.context.scene.objects):
    if o is not obj and o.type != "MESH":
        pass
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

me = obj.data
# glTF +Z (baş) -> Blender -Y. Arena'da baş güneye (-Z arena = +Y Blender) baksın: 180° Z etrafında döndür.
zs = [v.co.y for v in me.vertices]
ymin = min(zs)  # baş ucu (Blender -Y en küçük)
for v in me.vertices:
    x, y, z = v.co
    # ölçek, sonra 180° döndür (x -> -x, y -> -y), burun arena Z=7,5'e
    ax = -x * SCALE
    az = 7.5 + (y - ymin) * SCALE   # arena Z (kuzey)
    ah = z * SCALE                  # yükseklik
    v.co = Vector((ax, -az, ah))    # Blender: Y = -arenaZ
me.update()

def arena(co):
    return co.x, -co.y, co.z  # X, Z, Y(yükseklik)

bm = bmesh.new(); bm.from_mesh(me)
# glTF içe aktarımı doku dikişlerinde köşeleri ayırır; birleştir (UV köşe başına korunur)
n0 = len(bm.verts)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.002)
print(f"RAPOR dikiş birleştirme: {n0} -> {len(bm.verts)} köşe")
kill = []
for f in bm.faces:
    c = f.calc_center_median()
    x, z, h = arena(c)
    if z < CUT_Z:
        kill.append(f)
    elif abs(x) > LEG_CUT["abs_x"] and z < LEG_CUT["z_max"] and h < LEG_CUT["y_max"]:
        kill.append(f)
bmesh.ops.delete(bm, geom=kill, context="FACES")
# yalnız kalan küçük parçaları temizle
bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
for v in bm.verts:
    v.co.y -= SHIFT_NORTH      # kuzeye kaydır (Blender -Y)
bm.to_mesh(me); bm.free()

# küçük kopuk adacıkları sil (< 60 yüz)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="DESELECT")
bpy.ops.object.mode_set(mode="OBJECT")
bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
seen, islands = set(), []
for f in bm.faces:
    if f.index in seen: continue
    stack, isl = [f], []
    seen.add(f.index)
    while stack:
        g = stack.pop(); isl.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h.index not in seen:
                    seen.add(h.index); stack.append(h)
    islands.append(isl)
small = [f for isl in islands if len(isl) < 60 for f in isl]
bmesh.ops.delete(bm, geom=small, context="FACES")
bm.to_mesh(me); bm.free()

tris_before = sum(len(p.vertices) - 2 for p in me.polygons)
mod = obj.modifiers.new("dec", "DECIMATE")
mod.ratio = min(1.0, TARGET_TRIS / tris_before)
mod.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier="dec")
tris_after = sum(len(p.vertices) - 2 for p in me.polygons)

# doku 1024'e
for mat in obj.data.materials:
    for n in mat.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            n.image.scale(TEX_SIZE, TEX_SIZE)
            n.image.filepath_raw = f"{OUT_DIR}/ejder_govde_albedo.png"
            n.image.file_format = "PNG"
            n.image.save()
    mat.name = "ejder_govde_Mat"
obj.name = obj.data.name = "ejder_govde"

# pivot arena merkezinde (0,0,0)
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")

xs = [v.co.x for v in me.vertices]; zs_ = [-v.co.y for v in me.vertices]; hs = [v.co.z for v in me.vertices]
print(f"RAPOR üçgen {tris_before} -> {tris_after}")
print(f"RAPOR X {min(xs):.1f}..{max(xs):.1f}  Z {min(zs_):.1f}..{max(zs_):.1f}  Y {min(hs):.1f}..{max(hs):.1f}")
r_in = min(math.hypot(x, z) for x, z in zip(xs, zs_))
print(f"RAPOR arena merkezine en yakın nokta r={r_in:.1f} m")

bpy.ops.export_scene.fbx(filepath=f"{OUT_DIR}/ejder_govde.fbx", use_selection=True,
                         apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                         bake_space_transform=True, mesh_smooth_type="FACE", path_mode="COPY",
                         embed_textures=False, add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=f"{OUT_DIR}/ejder_govde_onizleme.glb", use_selection=True)
bpy.ops.wm.save_as_mainfile(filepath=f"{OUT_DIR}/ejder_govde.blend")
print("RAPOR tamam")
