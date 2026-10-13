using System;
using System.Collections.Generic;
using Dovus.Game.Weapons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Silah başına k3 yürüme seti: <c>k3_&lt;silah&gt;.overrideController</c> temel k3 controller'ındaki
    /// <c>Loko_Kilic_*</c> kliplerini silahın <c>Loko_&lt;Silah&gt;_*</c> setiyle değiştirir ve
    /// <see cref="WeaponVisualRegistry"/>'ye yazılır. Dosyalar repoda (gitignored değil): APK ile editör aynı seti oynar.
    /// Batch: -executeMethod Dovus.Game.Editor.K3WeaponLocomotion.Build
    /// </summary>
    public static class K3WeaponLocomotion
    {
        const string RegistryPath = "Assets/Resources/Animation/WeaponVisualRegistry.asset";
        const string IdleSuffix = "_Idle";
        const string RunSuffix = "_Run";

        /// <summary>weapons[].animations_key → klipler.csv'deki Loko_&lt;Silah&gt;_* seti (dilim silahları).</summary>
        internal static readonly (string WeaponKey, string ClipSet)[] Sets =
        {
            ("kilic", "Kilic"),
            ("yay", "Yay"),
        };

        internal static string OverridePath(string weaponKey) => $"{K3AssetSetup.Root}/k3_{weaponKey}.overrideController";

        [MenuItem("Dovus/k3/Silah yürüme setleri (override + kayıt)")]
        public static void Build()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(K3AssetSetup.ControllerPath)
                ?? throw new InvalidOperationException(K3AssetSetup.ControllerPath + " yok");
            Ensure(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("[k3] silah yürüme setleri yazıldı");
        }

        internal static void Ensure(AnimatorController ctrl)
        {
            Dictionary<string, AnimationClip> clips = K3AssetSetup.LoadClips();
            var registry = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath)
                ?? throw new InvalidOperationException(RegistryPath + " yok");
            registry.WeaponLocomotion.RemoveAll(e => e == null || e.Override == null || e.Override.runtimeAnimatorController == ctrl);
            foreach ((string weaponKey, string set) in Sets)
            {
                var swap = new Dictionary<string, AnimationClip>(StringComparer.Ordinal)
                {
                    [K3AssetSetup.LocoIdle] = RequireClip(clips, K3AssetSetup.LoopPrefix + set + IdleSuffix),
                    [K3AssetSetup.LocoRun] = RequireClip(clips, K3AssetSetup.LoopPrefix + set + RunSuffix),
                };
                AnimatorOverrideController aoc = LoadOrCreate(OverridePath(weaponKey), ctrl);
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                aoc.GetOverrides(pairs);
                for (int i = 0; i < pairs.Count; i++)
                {
                    AnimationClip original = pairs[i].Key;
                    AnimationClip value = original != null && swap.TryGetValue(original.name, out AnimationClip c) ? c : null;
                    pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, value);
                }
                aoc.ApplyOverrides(pairs);
                EditorUtility.SetDirty(aoc);
                registry.WeaponLocomotion.Add(new WeaponVisualRegistry.WeaponLocomotionEntry { WeaponKey = weaponKey, Override = aoc });
            }
            EditorUtility.SetDirty(registry);
        }

        static AnimatorOverrideController LoadOrCreate(string path, AnimatorController ctrl)
        {
            var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(ctrl);
                AssetDatabase.CreateAsset(aoc, path);
            }
            else
            {
                aoc.runtimeAnimatorController = ctrl;
            }
            return aoc;
        }

        static AnimationClip RequireClip(Dictionary<string, AnimationClip> clips, string name) =>
            clips.TryGetValue(name, out AnimationClip c)
                ? c
                : throw new InvalidOperationException($"k3 klibi yok: {name} ({K3AssetSetup.ClipsDir})");
    }
}
