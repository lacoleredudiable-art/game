import sys, bpy
B, OUT = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.open_mainfile(filepath=B)
for on in ("ejder_govde", "ejder_kanatlar"):
    o = bpy.data.objects[on]
    for m in o.data.materials:
        for nd in (m.node_tree.nodes if m and m.use_nodes else []):
            if nd.bl_idname == "ShaderNodeTexImage" and nd.image:
                nd.image.filepath_raw = f"{OUT}/{on}_kaynak.png"; nd.image.file_format = "PNG"; nd.image.save(); print("KAYNAK", on, nd.image.name, nd.image.size[:])
                break
