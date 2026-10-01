# Poly Haven (CC0)

Source: https://polyhaven.com (CC0 1.0, public domain; no attribution required, credit given anyway).
Used only by the visual test scene `Assets/Scenes/DenemeSahnesi.unity`.

| Folder | Poly Haven asset | What was changed on the box |
|---|---|---|
| `rock_face_01` | Models / rock_face_01 | 1k glTF, decimated 20,174 → 4,000 tris (UV-preserving quadric), origin at base centre |
| `rock_face_02` | Models / rock_face_02 | 29,566 → 4,000 tris |
| `boulder_01` | Models / boulder_01 | 66,122 → 3,000 tris |
| `mountainside` | Models / mountainside | 153,472 → 5,000 tris (far mountains, seen through fog) |
| `namaqualand_cliff_02` | Models / namaqualand_cliff_02 | 194,080 → 6,000 tris (far cliffs) |
| `dark_rock` | Textures / dark_rock | 1k, desaturated to 25% and brightened (basalt ground) |
| `burned_ground_01` | Textures / burned_ground_01 | 1k normal + grey luminance "detail" map (mean 0.5, URP Lit detail ×2) |

All diffuse maps are desaturated (25–30%) so the scene stays grey before post-processing; normal maps are
the original OpenGL (`nor_gl`) 1k maps. Import settings (max 1024, normal map type, Android ASTC 6x6) are
applied by `Dovus/Visual/Deneme Sahnesi - Build`.
