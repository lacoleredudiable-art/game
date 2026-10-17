"""ejder.blend doku yollarını göreli yap: hepsi //doku/<ad>. Eski dosyada baş dokuları başka makinenin geçici
klasörüne bağlıydı (repoda yoktu); yerlerine doku_palet.py ile üretilen _v2 baş dokuları konur. Plakalar
(geçici) v1 gövde dokusunu gösteriyordu; v2 gövde dokusuna bağlanır. Tekrar çalıştırmak güvenlidir.
python doku_yol.py -- <ejder.blend>"""
import sys, os, bpy
B = os.path.abspath(sys.argv[sys.argv.index("--") + 1:][0])
bpy.ops.wm.open_mainfile(filepath=B)
REN = {"ejder_bas_albedo_v1.png": "ejder_bas_albedo_v2.png", "ejder_bas_kor_v1.png": "ejder_bas_kor_v2.png",
       "ejder_govde_albedo_v1.png": "ejder_govde_albedo_v2.png", "ejder_govde_kor_v1.png": "ejder_govde_kor_v2.png"}
D = os.path.join(os.path.dirname(B), "doku")
cache = {}
def get(base, cs):
    if base not in cache:
        im = bpy.data.images.load(os.path.join(D, base)); im.filepath = "//doku/" + base
        im.colorspace_settings.name = cs; cache[base] = im
    return cache[base]
olds = set()
for m in bpy.data.materials:
    if not m.node_tree: continue
    for nd in m.node_tree.nodes:
        if nd.bl_idname == "ShaderNodeTexImage" and nd.image:
            old = nd.image; base = os.path.basename(old.filepath.replace("\\", "/")) or old.name
            base = REN.get(base, base); olds.add(old)
            nd.image = get(base, old.colorspace_settings.name)
for im in olds:
    if im.users == 0 and im not in cache.values(): bpy.data.images.remove(im)
for base, im in cache.items():
    im.name = base; print("YOL", im.name, im.filepath, os.path.exists(bpy.path.abspath(im.filepath)))
bpy.ops.wm.save_as_mainfile(filepath=B)
