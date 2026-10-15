"""Her mesh için yüz başına UV üçgenleri, normal (dünya) ve merkez (arena) -> npz. Doku işleme için."""
import sys, bpy, bmesh
import numpy as np
args = sys.argv[sys.argv.index("--") + 1:]
OUT = args[0]; pairs = args[1:]          # blend:obj ...
for pr in pairs:
    blend, name = pr.split(":")
    bpy.ops.wm.open_mainfile(filepath=blend)
    o = bpy.data.objects[name]
    bm = bmesh.new(); bm.from_mesh(o.data); bmesh.ops.triangulate(bm, faces=bm.faces)
    uv = bm.loops.layers.uv.active
    M = o.matrix_world; R = M.to_3x3()
    UV, N, C, P = [], [], [], []
    for f in bm.faces:
        UV.append([[l[uv].uv.x, l[uv].uv.y] for l in f.loops])
        n = (R @ f.normal).normalized(); N.append([-n.x, -n.y, n.z])          # arena (doğu=-X Blender)
        c = M @ f.calc_center_median(); C.append([-c.x, -c.y, c.z])
        P.append([[-(M @ l.vert.co).x, -(M @ l.vert.co).y, (M @ l.vert.co).z] for l in f.loops])
    np.savez(f"{OUT}/{name}_yuz.npz", uv=np.array(UV, np.float32), n=np.array(N, np.float32), c=np.array(C, np.float32), p=np.array(P, np.float32))
    print("YUZ", name, len(UV))
