import trimesh, pymeshlab, numpy as np, os, shutil, sys
SRC='/workspace/karakterler/visual-testscene/src/ph'
OUT='/workspace/karakterler/visual-testscene/out/PolyHaven'
targets={'rock_face_01':4000,'rock_face_02':4000,'rock_07':2500,'rock_09':2500,'boulder_01':3000,'mountainside':5000,'namaqualand_cliff_02':6000}
os.makedirs(OUT,exist_ok=True)
for name,tgt in targets.items():
    sc=trimesh.load(f'{SRC}/{name}/{name}.gltf')
    geoms=list(sc.dump()) if isinstance(sc,trimesh.Scene) else [sc]
    m=trimesh.util.concatenate(geoms) if len(geoms)>1 else geoms[0]
    uv=m.visual.uv
    tmp=f'/tmp/{name}_hi.obj'
    # write OBJ with wedge uvs (per-vertex here since gltf splits seams)
    with open(tmp,'w') as f:
        for v in m.vertices: f.write(f'v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n')
        for t in uv: f.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
        for a,b,c in m.faces+1: f.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
    ms=pymeshlab.MeshSet(); ms.load_new_mesh(tmp)
    hi=ms.current_mesh().face_number()
    ms.meshing_merge_close_vertices(threshold=pymeshlab.PercentageValue(0.01))
    ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum=tgt, qualitythr=0.5, preserveboundary=False, optimalplacement=True, preservenormal=True)
    lo=ms.current_mesh()
    # center on base: x,z centered, min y = 0
    V=lo.vertex_matrix().copy(); F=lo.face_matrix(); W=lo.wedge_tex_coord_matrix()  # (3F,2)
    c=np.array([(V[:,0].min()+V[:,0].max())/2, V[:,1].min(), (V[:,2].min()+V[:,2].max())/2]); V-=c
    od=f'{OUT}/{name}'; os.makedirs(od,exist_ok=True)
    with open(f'{od}/{name}_lod.obj','w') as f:
        f.write(f'# {name} (Poly Haven, CC0) decimated {hi}->{len(F)} tris for mobile; origin = base centre\n')
        for v in V: f.write(f'v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n')
        for t in W: f.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
        for i,(a,b,cc) in enumerate(F):
            f.write(f'f {a+1}/{3*i+1} {b+1}/{3*i+2} {cc+1}/{3*i+3}\n')
    ext=V.max(0)-V.min(0)
    print(name, hi,'->',len(F),'size m',np.round(ext,2))
