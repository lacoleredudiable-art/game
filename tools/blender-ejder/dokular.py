"""ejder_on.blend: paketli dokuları PNG olarak kaydet, FBX'i dokularla yeniden çıkar."""
import sys, bpy
BLEND, OUT = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.open_mainfile(filepath=BLEND)
for o in bpy.data.objects:
    if o.type != "MESH": continue
    base = "ejder_bacak" if o.name.startswith("ejder_bacak") else o.name
    for m in o.data.materials:
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image:
                im = n.image; p = f"{OUT}/{base}_albedo.png"
                im.filepath_raw = p; im.file_format = "PNG"; im.save()
                if im.packed_file: im.unpack(method="REMOVE")
                im.filepath = p; print("DOKU", o.name, "->", p, im.size[:])
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
arm = bpy.data.objects["ejder_on"]
bpy.ops.object.select_all(action="DESELECT")
for o in bpy.data.objects:
    if o.type in ("MESH", "ARMATURE"): o.select_set(True)
bpy.ops.export_scene.fbx(filepath=f"{OUT}/ejder_on.fbx", use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                         bake_anim_force_startend_keying=True, path_mode="RELATIVE")
print("ACTIONS", sorted(a.name for a in bpy.data.actions))
