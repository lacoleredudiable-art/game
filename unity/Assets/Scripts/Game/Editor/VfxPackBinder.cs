using Dovus.Game.Skills.Execution;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Cartoon FX Remaster Free + Particle Pack prefab'larını <c>Resources/VfxLibrary.asset</c>
    /// anahtarlarına yazar. Paketler gitignore'da; başka makinede paketleri import edip bu menüyü
    /// çalıştırmak yeter. Bulunamayan prefab atlanır (o anahtar prosedürel yolda kalır).
    /// Ömür / referans boyutları gözle ayarlandı — önerilen (docs/durum.md).
    /// </summary>
    public static class VfxPackBinder
    {
        const string LibraryPath = "Assets/Resources/VfxLibrary.asset";
        const string ShapePrefix = "shape:";

        static bool IsShape(string chunk) => chunk.StartsWith(ShapePrefix, System.StringComparison.Ordinal);
        static readonly string[] PackRoots = { "Assets/JMO Assets", "Assets/UnityTechnologies" };

        readonly struct Row
        {
            public readonly string Key, Prefab, ChunkMesh, ChunkMaterial;
            public readonly float Life, RefSize;

            public Row(string key, string prefab, float life, float refSize = 0f, string chunkMesh = null,
                string chunkMaterial = null)
            {
                Key = key;
                Prefab = prefab;
                Life = life;
                RefSize = refSize;
                ChunkMesh = chunkMesh;
                ChunkMaterial = chunkMaterial;
            }
        }

        static readonly Row[] Rows =
        {
            new(VfxLibrary.HitSpark, "CFXR3 Hit Misc A", 2f),
            new(VfxLibrary.CritSpark, "CFXR Hit D 3D (Yellow)", 2f),
            new(VfxLibrary.DodgeDust, "CFXR3 Hit Misc F Smoke", 2.5f),
            new(VfxLibrary.SlamShockwave, "CFXR2 Ground Hit", 3f, 3f),
            new(VfxLibrary.GroundCrack, "EarthShatter", 3f, 4f),
            new(VfxLibrary.FireCone, "CFXR Fire Breath", 3f, 6f),
            new("Impact/strike", "CFXR3 Hit Misc A", 2f),
            new("Impact/cleave", "CFXR3 Hit Misc A", 2f),
            new("Impact/impact", "CFXR3 Hit Misc A", 2f),
            new("Impact/pierce_hit", "CFXR3 Hit Light B (Air)", 2f),
            new("Impact/pulse", "CFXR Impact Glowing HDR (Blue)", 2f),
            new("Impact/burst_soft", "CFXR Impact Glowing HDR (Blue)", 2f),
            new("Impact/tick", "CFXR Flash", 1f),
            new("Impact/drain", "CFXR Magic Poof", 2f),
            new("Impact/reflect", "CFXR Flash", 1f),

            // Skill teslim yolu giydirmesi (HitboxVfxRegistry). Ömür 0 = executor bitince onunla gider.
            new("Delivery/Projectile", "FireBall", 0f),
            new("Delivery/MeleeHitbox", "CFXR4 Sword Hit PLAIN (Cross)", 2f),
            new("Delivery/MeleeHitbox/sphere", "CFXR Explosion 1", 3f, 3f),
            new("Delivery/FieldAura", "EarthShatter", 0f, 4f),
            new("Delivery/Summon", "CFXR2 Souls Escape", 2.5f),
            new("Delivery/Movement", "CFXR Magic Poof", 2f),
            new("Delivery/SelfState", "CFXR3 Magic Aura A (Runic)", 0f, 2f),
            new("Delivery/cone", "CFXR Fire Breath", 3f, 6f),
            new("Delivery/line", "CFXR3 Hit Light B (Air)", 2f),

            // Fiil maddesi (Substance/{verb_id}): skill görselinin neyden yapıldığı. Dizilim
            // gramerden gelir (MechanicVisualComposer); burada yalnız 12 fiilin malzemesi var.
            // Her fiilin katı parçası var: paket mesh'i (kaya/buz) ya da "shape:" ile koddan üretilen
            // şekil (ProceduralChunkMesh) — her parçada gerçek cisim çıkar.
            new("Substance/1", "CFXR2 Ground Hit", 2.5f, 1.5f, "RockDebris_Low", "RockSpike"),
            new("Substance/2", "CFXR3 Hit Leaves A (Lit)", 2f, 1f, "shape:leaf"),
            new("Substance/3", "DustExplosion", 2.5f, 1.5f, "shape:arrow"),
            new("Substance/4", "CFXR Impact Glowing HDR (Blue)", 2.5f, 1.5f, "RockSpike_Low", "RockSpike"),
            new("Substance/5", "SmallExplosion", 3f, 1.5f, "shape:burst"),
            new("Substance/6", "CFXR Electrified 3", 2f, 1f, "shape:ring"),
            new("Substance/7", "CFXR2 Poison Cloud", 3f, 2f, "shape:blob"),
            new("Substance/8", "RisingSteam", 2.5f, 1f, "shape:crystal"),
            new("Substance/9", "CFXR4 Falling Stars", 2.5f, 2f, "shape:star"),
            new("Substance/10", "CFXR4 Sword Hit ICE (Cross)", 2.5f, 1f, "IceLance_Low"),
            new("Substance/11", "CFXR2 Souls Escape", 3f, 1.5f, "shape:obelisk"),
            new("Substance/12", "CFXR4 Bouncing Glows Bubble (Blue Purple)", 3f, 1.5f, "shape:hourglass"),
        };

        [MenuItem("Dovus/Feel/Bind VFX Packs")]
        public static void Bind()
        {
            var lib = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VfxLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var byKey = new Dictionary<string, int>();
            for (int i = 0; i < lib.Entries.Count; i++)
                byKey[lib.Entries[i].Key] = i;

            int bound = 0;
            var missing = new List<string>();
            foreach (Row row in Rows)
            {
                GameObject prefab = FindPrefab(row.Prefab);
                if (prefab == null)
                {
                    missing.Add(row.Prefab);
                    continue;
                }
                var e = new VfxLibrary.Entry
                {
                    Key = row.Key,
                    Prefab = prefab,
                    LifetimeSec = row.Life,
                    ReferenceSizeM = row.RefSize,
                    ChunkMesh = row.ChunkMesh != null && !IsShape(row.ChunkMesh) ? FindMesh(row.ChunkMesh) : null,
                    ChunkMaterial = row.ChunkMaterial != null ? FindAsset<Material>(row.ChunkMaterial, "Material") : null,
                    ChunkShape = row.ChunkMesh != null && IsShape(row.ChunkMesh) ? row.ChunkMesh.Substring(ShapePrefix.Length) : null,
                };
                if (row.ChunkMesh != null && e.ChunkMesh == null && e.ChunkShape == null)
                    missing.Add(row.ChunkMesh);
                if (byKey.TryGetValue(row.Key, out int idx))
                    lib.Entries[idx] = e;
                else
                {
                    byKey[row.Key] = lib.Entries.Count;
                    lib.Entries.Add(e);
                }
                bound++;
            }

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VfxPackBinder] {bound}/{Rows.Length} anahtar bağlandı" +
                      (missing.Count > 0 ? "; bulunamayan: " + string.Join(", ", missing) : "."));
        }

        static GameObject FindPrefab(string name) => FindAsset<GameObject>(name, "Prefab");

        static T FindAsset<T>(string name, string type) where T : Object
        {
            string[] roots = Roots();
            if (roots.Length == 0)
                return null;
            foreach (string guid in AssetDatabase.FindAssets(name + " t:" + type, roots))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }

        /// <summary>Mesh'ler FBX alt varlığıdır: adıyla eşleşen ilk mesh.</summary>
        static Mesh FindMesh(string name)
        {
            string[] roots = Roots();
            if (roots.Length == 0)
                return null;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", roots))
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (sub is Mesh mesh && mesh.name == name)
                        return mesh;
            return null;
        }

        static string[] Roots()
        {
            var roots = new List<string>();
            foreach (string r in PackRoots)
                if (AssetDatabase.IsValidFolder(r))
                    roots.Add(r);
            return roots.ToArray();
        }
    }
}
