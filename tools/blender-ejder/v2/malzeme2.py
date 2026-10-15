import sys, bpy
B, D = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.open_mainfile(filepath=B)
base = bpy.data.materials["ejder_bas_Mat"]
def make(name, alb, kor):
    m = bpy.data.materials.get(name)
    if m: bpy.data.materials.remove(m)
    m = base.copy(); m.name = name
    for nd in m.node_tree.nodes:
        if nd.bl_idname == "ShaderNodeTexImage":
            k = "kor" if "kor" in nd.image.name else "albedo"; cs = nd.image.colorspace_settings.name
            img = bpy.data.images.load(f"{D}/{kor if k == 'kor' else alb}"); img.colorspace_settings.name = cs; nd.image = img
    return m
mg = make("ejder_govde_Mat", "ejder_govde_albedo_v2.png", "ejder_govde_kor_v2.png")
mk = make("ejder_kanat_Mat", "ejder_kanat_albedo_v2.png", "ejder_kanat_kor_v2.png")
g = bpy.data.objects["ejder_govde"]
for i, m in enumerate(g.data.materials):
    if m is None or m.name != "ejder_ic_Mat": g.data.materials[i] = mg
w = bpy.data.objects["ejder_kanatlar"]
for i in range(len(w.data.materials)): w.data.materials[i] = mk
print("MAT", [m.name for m in g.data.materials], [m.name for m in w.data.materials])
bpy.ops.wm.save_as_mainfile(filepath=B)
