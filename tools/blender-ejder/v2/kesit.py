"""Gövde yarı genişliği tablosu (arena |X| en büyük), Z x H ızgarası. python kesit.py -- <blend>"""
import sys, bpy, numpy as np
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index("--") + 1])
b = bpy.data.objects["ejder_govde"]
P = np.array([(lambda w: (-w.x, -w.y, w.z))(b.matrix_world @ v.co) for v in b.data.vertices])
Hs = list(range(0, 19, 2)); print("KESIT H   ", " ".join(f"{h:5d}" for h in Hs))
for z in range(16, 60, 3):
    row = []
    for h in Hs:
        m = (abs(P[:, 1] - z) < 1.5) & (abs(P[:, 2] - h) < 1.0)
        row.append(f"{np.abs(P[m, 0]).max():5.1f}" if m.any() else "    -")
    print(f"KESIT Z{z:3d}", " ".join(row))
