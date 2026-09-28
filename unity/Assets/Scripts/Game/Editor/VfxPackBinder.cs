using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
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
        static readonly string[] PackRoots = { "Assets/JMO Assets", "Assets/UnityTechnologies" };

        readonly struct Row
        {
            public readonly string Key, Prefab;
            public readonly float Life, RefSize;

            public Row(string key, string prefab, float life, float refSize = 0f)
            {
                Key = key;
                Prefab = prefab;
                Life = life;
                RefSize = refSize;
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
            new("Delivery/Summon", "IceLance", 0f, 3f),
            new("Delivery/Movement", "CFXR Magic Poof", 2f),
            new("Delivery/SelfState", "CFXR3 Magic Aura A (Runic)", 0f, 2f),
            new("Delivery/cone", "CFXR Fire Breath", 3f, 6f),
            new("Delivery/line", "IceLance", 0f, 4f),
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
                };
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

        static GameObject FindPrefab(string name)
        {
            var roots = new List<string>();
            foreach (string r in PackRoots)
                if (AssetDatabase.IsValidFolder(r))
                    roots.Add(r);
            if (roots.Count == 0)
                return null;
            foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab", roots.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }
    }
}
