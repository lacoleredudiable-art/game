using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// Player_Synty.controller tabanından silah arketipi başına AnimatorOverrideController üretir
    /// (<c>Assets/Art/Mixamo/Animators/Archetypes/&lt;Arketip&gt;.overrideController</c>, gitignored)
    /// ve <see cref="WeaponVisualRegistry"/> asset'ine (<c>Resources/Animation/</c>, repoda) yazar.
    /// <see cref="MixamoAnimatorBind.Bind"/> çağırır. Klipler: <c>Mixamo/Archetypes/&lt;Klasör&gt;/*.fbx</c>.
    /// Override yalnız AnimationClip kimliğiyle çalışır: base controller'daki her state farklı bir
    /// klibe bağlı olmalı (Player_Strike_A/B/C, Player_Block_Hold, Player_Ground_Slam vb. — aynı
    /// klibi paylaşan iki state'i ayrı arketip kliplerine override etmek mümkün değildir).
    /// </summary>
    public static class MixamoArchetypeBind
    {
        const string ArchetypesDir = "Assets/Art/Mixamo/Archetypes";
        const string OutDir = "Assets/Art/Mixamo/Animators/Archetypes";
        const string RegistryDir = "Assets/Resources/Animation";
        const string RegistryPath = RegistryDir + "/WeaponVisualRegistry.asset";
        const string QuatWeaponsDir = "Assets/Art/Quaternius/Characters/RPG Characters - Nov 2020/FBX/Only Weapons";
        const string ShieldPrefabPath = QuaterniusShieldBind.PrefabPath;
        const string StaffPrefabPath = QuaterniusCc0PropsBind.StaffPrefab;
        const string OrbPrefabPath = QuaterniusCc0PropsBind.OrbPrefab;
        const string TalismanPrefabPath = QuaterniusCc0PropsBind.TalismanPrefab;
        const string CannonPrefabPath = QuaterniusCc0PropsBind.CannonPrefab;

        /// <summary>Çalıştırır; log metnini döner (boş = temel controller yok, atlandı).</summary>
        public static string Build()
        {
            var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(MixamoAnimatorBind.PlayerCtrl);
            if (baseController == null)
            {
                Debug.LogWarning("[ArchetypeBind] " + MixamoAnimatorBind.PlayerCtrl + " yok, arketip override atlandı.");
                return string.Empty;
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder(RegistryDir);
            EnsureFolder(OutDir);

            Dictionary<string, AnimationClip> baseClip = CollectBaseStateClips(baseController);
            Dictionary<string, AnimationClip> archetypeClips = CollectAllArchetypeClips();
            WeaponVisualRegistry registry = LoadOrCreateRegistry();

            var log = new StringBuilder();
            foreach (string archetype in WeaponArchetypeMap.All)
            {
                Dictionary<AnimationClip, AnimationClip> overrides =
                    BuildOverrides(archetype, baseClip, archetypeClips);
                string ctrlPath = OutDir + "/" + archetype + ".overrideController";
                AnimatorOverrideController aoc = LoadOrCreateOverride(ctrlPath, baseController);
                ApplyOverrides(aoc, overrides);
                EditorUtility.SetDirty(aoc);
                SetRegistryEntry(registry, archetype, aoc);

                log.Append(archetype).Append(" →");
                foreach (var kv in overrides)
                    log.Append(' ').Append(StateNameOf(baseClip, kv.Key)).Append(':').Append(kv.Value.name);
                log.Append('\n');
            }

            BindProps(registry);

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        /// <summary>
        /// Elde silah prop referansları (Animasyon b). WeaponKey = weapons[].animations_key
        /// (<see cref="WeaponArchetypeMap"/> girdileriyle birebir). Quaternius "Only Weapons" FBX
        /// kılıç/hançer/yay/asa için; kalkan/çekiç/kitap/küre/tılsım/top'ta referans boş bırakılır —
        /// <see cref="WeaponHandProps"/> boş referansta primitive placeholder kurar. Ölçekler mesh
        /// local bounds'una göre hesaplandı (görev hedefi: kılıç ~0,9 m, asa ~1,6 m).
        /// "yumruk" bilinçli olarak kayıtsız: spec'te prop yok (çıplak el).
        /// </summary>
        static void BindProps(WeaponVisualRegistry registry)
        {
            SetProp(registry, "kilic",
                LoadWeaponMesh("Warrior_Sword"), Vector3.zero, Vector3.zero, Vector3.one * 28.3f,
                LoadShieldPrefab(), Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "kalkan",
                LoadWeaponMesh("Rogue_Dagger"), Vector3.zero, Vector3.zero, Vector3.one * 34.7f,
                LoadShieldPrefab(), Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "cekic",
                null, new Vector3(0f, 0f, 0.07f), Vector3.zero, Vector3.one,
                null, Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "yay",
                null, Vector3.zero, Vector3.zero, Vector3.one,
                LoadWeaponMesh("Ranger_Bow"), Vector3.zero, new Vector3(0f, 90f, 0f), Vector3.one * 32.6f);

            SetProp(registry, "asa",
                LoadPropPrefab(StaffPrefabPath), Vector3.zero, Vector3.zero, Vector3.one,
                null, Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "kitap",
                null, Vector3.zero, Vector3.zero, Vector3.one,
                null, Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "kure",
                null, Vector3.zero, Vector3.zero, Vector3.one,
                LoadPropPrefab(OrbPrefabPath), Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "tilsim",
                LoadPropPrefab(TalismanPrefabPath), Vector3.zero, Vector3.zero, Vector3.one,
                null, Vector3.zero, Vector3.zero, Vector3.one);

            SetProp(registry, "top",
                LoadPropPrefab(CannonPrefabPath), Vector3.zero, Vector3.zero, Vector3.one,
                null, Vector3.zero, Vector3.zero, Vector3.one);
        }

        static GameObject LoadWeaponMesh(string fileName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(QuatWeaponsDir + "/" + fileName + ".fbx");

        static GameObject LoadShieldPrefab() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ShieldPrefabPath);

        static GameObject LoadPropPrefab(string path) =>
            string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);

        static void SetProp(
            WeaponVisualRegistry registry, string weaponKey,
            GameObject rightPrefab, Vector3 rightPos, Vector3 rightRot, Vector3 rightScale,
            GameObject leftPrefab, Vector3 leftPos, Vector3 leftRot, Vector3 leftScale)
        {
            List<WeaponVisualRegistry.PropEntry> list = registry.Props;
            WeaponVisualRegistry.PropEntry entry = list.Find(p => p != null && p.WeaponKey == weaponKey);
            if (entry == null)
            {
                entry = new WeaponVisualRegistry.PropEntry { WeaponKey = weaponKey };
                list.Add(entry);
            }
            entry.RightHandPrefab = rightPrefab;
            entry.RightLocalPosition = rightPos;
            entry.RightLocalEulerAngles = rightRot;
            entry.RightLocalScale = rightScale;
            entry.LeftHandPrefab = leftPrefab;
            entry.LeftLocalPosition = leftPos;
            entry.LeftLocalEulerAngles = leftRot;
            entry.LeftLocalScale = leftScale;
        }

        /// <summary>Base state adı → o state'in o anki klibi (Locomotion idle/walk/run blend tree yaprakları dahil).</summary>
        static Dictionary<string, AnimationClip> CollectBaseStateClips(AnimatorController ac)
        {
            var map = new Dictionary<string, AnimationClip>();
            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            foreach (ChildAnimatorState cs in sm.states)
            {
                if (cs.state.name == "Locomotion")
                {
                    if (cs.state.motion is BlendTree bt && bt.children.Length >= 3)
                    {
                        map["LocomotionIdle"] = bt.children[0].motion as AnimationClip;
                        map["LocomotionWalk"] = bt.children[1].motion as AnimationClip;
                        map["LocomotionRun"] = bt.children[2].motion as AnimationClip;
                    }
                    continue;
                }
                if (cs.state.motion is AnimationClip clip)
                    map[cs.state.name] = clip;
            }
            return map;
        }

        /// <summary>
        /// Mixamo/Archetypes altındaki (Shared + her arketip klasörü) tüm klipler, dosya adıyla.
        /// internal: MixamoAnimatorBind temel controller'ın Backstep/Sidestep/JumpAttack/Spin/Throw
        /// state'lerini aynı havuzdan besler (bkz. görev notu O-anim c).
        /// </summary>
        internal static Dictionary<string, AnimationClip> CollectAllArchetypeClips()
        {
            var map = new Dictionary<string, AnimationClip>();
            if (!AssetDatabase.IsValidFolder(ArchetypesDir))
                return map;
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ArchetypesDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (o is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                        continue;
                    string key = Path.GetFileNameWithoutExtension(path);
                    if (!map.ContainsKey(key))
                        map[key] = clip;
                }
            }
            return map;
        }

        /// <summary>
        /// Arketip başına slot → klip adı. "Slot önerileri" (görev notu): her arketip kendi
        /// Mixamo/Archetypes klasöründen ya da (Top/koşu, Çekiç&Top/Hit) paylaşılan bir klipten besler.
        /// Eşlenmeyen slot temel (Player_*) klipte kalır.
        /// </summary>
        static Dictionary<string, string> SlotClipNames(string archetype)
        {
            var m = new Dictionary<string, string>();
            switch (archetype)
            {
                case WeaponArchetypeMap.SwordShield:
                    m["CastGuard"] = "SS_Block";
                    break;
                case WeaponArchetypeMap.Hammer:
                    m["LocomotionIdle"] = "Hammer_Idle";
                    m["LocomotionRun"] = "Hammer_Run";
                    m["BasicStrike"] = "Hammer_Overhead";
                    m["BasicStrikeB"] = "Hammer_Slash";
                    m["BasicStrikeC"] = "Hammer_Horizontal";
                    m["CastSlam"] = "Hammer_JumpAttack";
                    m["CastPierce"] = "Hammer_Overhead";
                    m["CastSweep"] = "Hammer_Horizontal";
                    m["Hit"] = "Shared_React_Large_Front";
                    // O-anim(c): temel controller'ın leap/land/spin anahtarı zaten bu klipler —
                    // açık override, klip kaynağı ileride değişirse Çekiç kendi klibinde kalsın.
                    m["JumpAttack"] = "Hammer_JumpAttack";
                    m["Spin"] = "Hammer_Spin";
                    break;
                case WeaponArchetypeMap.Fist:
                    m["LocomotionIdle"] = "Fist_Idle";
                    m["BasicStrike"] = "Fist_Jab";
                    m["BasicStrikeB"] = "Fist_Cross";
                    m["BasicStrikeC"] = "Fist_Hook";
                    m["CastGuard"] = "Fist_Block";
                    m["CastPierce"] = "Fist_Uppercut";
                    m["CastSweep"] = "Fist_Hook";
                    break;
                case WeaponArchetypeMap.Bow:
                    m["LocomotionIdle"] = "Bow_Idle";
                    m["LocomotionRun"] = "Bow_Run";
                    m["CastShoot"] = "Bow_Recoil";
                    m["CastPierce"] = "Bow_Overdraw";
                    m["CastSweep"] = "Bow_Dive";
                    break;
                case WeaponArchetypeMap.Caster:
                    m["LocomotionIdle"] = "Caster_Idle";
                    m["LocomotionRun"] = "Caster_Run";
                    m["CastShoot"] = "Caster_1H_Attack01";
                    m["CastChannel"] = "Caster_2H_Cast";
                    m["CastSlam"] = "Caster_2H_Area";
                    m["CastPierce"] = "Caster_1H_Attack02";
                    m["CastSweep"] = "Caster_WideArm";
                    break;
                case WeaponArchetypeMap.Gun:
                    m["LocomotionIdle"] = "Gun_Idle";
                    m["LocomotionRun"] = "Hammer_Run"; // Top: kendi koşu klibi yok (görev notu).
                    m["CastShoot"] = "Gun_Fire";
                    m["CastPierce"] = "Gun_Fire";
                    m["CastSweep"] = "Gun_Reload";
                    m["Hit"] = "Shared_React_Large_Front";
                    break;
            }
            return m;
        }

        static Dictionary<AnimationClip, AnimationClip> BuildOverrides(
            string archetype, Dictionary<string, AnimationClip> baseClip, Dictionary<string, AnimationClip> archetypeClips)
        {
            var result = new Dictionary<AnimationClip, AnimationClip>();
            foreach (var kv in SlotClipNames(archetype))
            {
                if (!baseClip.TryGetValue(kv.Key, out AnimationClip original) || original == null)
                    continue;
                if (!archetypeClips.TryGetValue(kv.Value, out AnimationClip replacement) || replacement == null)
                {
                    Debug.LogWarning($"[ArchetypeBind] {archetype}.{kv.Key}: klip bulunamadı '{kv.Value}', temel klip korunuyor.");
                    continue;
                }
                result[original] = replacement;
            }
            return result;
        }

        static string StateNameOf(Dictionary<string, AnimationClip> baseClip, AnimationClip original)
        {
            foreach (var kv in baseClip)
            {
                if (kv.Value == original)
                    return kv.Key;
            }
            return "?";
        }

        static AnimatorOverrideController LoadOrCreateOverride(string path, AnimatorController baseController)
        {
            var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(aoc, path);
            }
            else if (aoc.runtimeAnimatorController != baseController)
            {
                aoc.runtimeAnimatorController = baseController;
            }
            return aoc;
        }

        /// <summary>Her çalıştırmada tüm slotları ya override'a ya da temel klibe sıfırlar (idempotent).</summary>
        static void ApplyOverrides(AnimatorOverrideController aoc, Dictionary<AnimationClip, AnimationClip> overrides)
        {
            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(list);
            for (int i = 0; i < list.Count; i++)
            {
                AnimationClip original = list[i].Key;
                if (original == null)
                    continue;
                AnimationClip value = overrides.TryGetValue(original, out AnimationClip repl) ? repl : original;
                list[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, value);
            }
            aoc.ApplyOverrides(list);
        }

        static WeaponVisualRegistry LoadOrCreateRegistry()
        {
            var reg = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<WeaponVisualRegistry>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }
            return reg;
        }

        static void SetRegistryEntry(WeaponVisualRegistry reg, string archetypeKey, AnimatorOverrideController aoc)
        {
            List<WeaponVisualRegistry.ArchetypeEntry> list = reg.Archetypes;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].ArchetypeKey == archetypeKey)
                {
                    list[i].Override = aoc;
                    return;
                }
            }
            list.Add(new WeaponVisualRegistry.ArchetypeEntry { ArchetypeKey = archetypeKey, Override = aoc });
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
