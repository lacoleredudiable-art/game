using System;
using System.IO;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>10 silah tutuş export/import (APK pano ↔ Editor registry). spec'te yok.</summary>
    public static class WeaponGripClipboardJson
    {
        public const int Version = 1;
        public const string ExportFileName = "weapon-feel-export.json";

        [Serializable]
        public sealed class HandGrip
        {
            public Vector3 pos;
            public Vector3 euler;
            public Vector3 scale = Vector3.one;
        }

        [Serializable]
        public sealed class WeaponEntry
        {
            public string key = string.Empty;
            public HandGrip right = new HandGrip();
            public HandGrip left = new HandGrip();
        }

        [Serializable]
        sealed class ExportRoot
        {
            public int version = Version;
            public WeaponEntry[] weapons = Array.Empty<WeaponEntry>();
        }

        public static string SerializeRegistry(WeaponVisualRegistry registry)
        {
            var root = new ExportRoot { version = Version };
            if (registry != null)
            {
                var list = new System.Collections.Generic.List<WeaponEntry>(WeaponFeelStore.WeaponKeys.Length);
                foreach (string key in WeaponFeelStore.WeaponKeys)
                {
                    WeaponVisualRegistry.PropEntry prop = registry.FindProps(key);
                    if (prop == null)
                        continue;
                    list.Add(new WeaponEntry
                    {
                        key = key,
                        right = FromProp(prop, right: true),
                        left = FromProp(prop, right: false),
                    });
                }

                root.weapons = list.ToArray();
            }

            return JsonUtility.ToJson(root, prettyPrint: false);
        }

        public static bool TryApplyToRegistry(string json, WeaponVisualRegistry registry, out int appliedCount)
        {
            appliedCount = 0;
            if (!TryParse(json, out ExportRoot root))
                return false;
            appliedCount = ApplyToRegistry(registry, root);
            return appliedCount > 0;
        }

        static bool TryParse(string json, out ExportRoot root)
        {
            root = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;
            try
            {
                var parsed = JsonUtility.FromJson<ExportRoot>(json);
                if (parsed?.weapons == null || parsed.weapons.Length == 0)
                    return false;
                root = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static int ApplyToRegistry(WeaponVisualRegistry registry, ExportRoot root)
        {
            if (registry == null || root?.weapons == null)
                return 0;
            int applied = 0;
            for (int i = 0; i < root.weapons.Length; i++)
            {
                WeaponEntry src = root.weapons[i];
                if (src == null || string.IsNullOrEmpty(src.key))
                    continue;
                WeaponVisualRegistry.PropEntry dst = registry.FindProps(src.key);
                if (dst == null)
                    continue;
                ApplyHand(src.right, dst, right: true);
                ApplyHand(src.left, dst, right: false);
                applied++;
            }

            return applied;
        }

        public static string ExportFilePath => Path.Combine(Application.persistentDataPath, ExportFileName);

        static HandGrip FromProp(WeaponVisualRegistry.PropEntry prop, bool right) => new HandGrip
        {
            pos = right ? prop.RightLocalPosition : prop.LeftLocalPosition,
            euler = right ? prop.RightLocalEulerAngles : prop.LeftLocalEulerAngles,
            scale = right ? prop.RightLocalScale : prop.LeftLocalScale,
        };

        static void ApplyHand(HandGrip src, WeaponVisualRegistry.PropEntry dst, bool right)
        {
            if (src == null)
                return;
            if (right)
            {
                dst.RightLocalPosition = src.pos;
                dst.RightLocalEulerAngles = src.euler;
                dst.RightLocalScale = src.scale.sqrMagnitude < 0.0001f ? Vector3.one : src.scale;
            }
            else
            {
                dst.LeftLocalPosition = src.pos;
                dst.LeftLocalEulerAngles = src.euler;
                dst.LeftLocalScale = src.scale.sqrMagnitude < 0.0001f ? Vector3.one : src.scale;
            }
        }
    }
}
