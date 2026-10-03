using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Düz vuruş bang süresi (persistent). Tutuş <see cref="WeaponVisualRegistry"/>; spec'te yok.
    /// </summary>
    public static class WeaponFeelStore
    {
        /// <summary>v2: grip alanları artık kullanılmıyor (registry); bang süresi kalır.</summary>
        public const int SchemaVersion = 2;
        const string FileName = "weapon-feel.json";

        public static readonly string[] WeaponKeys =
        {
            "kilic", "kalkan", "cekic", "yumruk", "yay", "asa", "kitap", "kure", "tilsim", "top",
        };

        [Serializable]
        public sealed class GripSave
        {
            public Vector3 LocalPosition;
            public Vector3 LocalEulerAngles;
            public Vector3 LocalScale = Vector3.one;
        }

        [Serializable]
        public sealed class WeaponEntry
        {
            public string key = string.Empty;
            public GripSave right = new GripSave();
            public GripSave left = new GripSave();
            /// <summary>Düz vuruş hasar anı (sn). Manifestation.BasicStrikeBangSec ile aynı birim.</summary>
            public float hitBangSec = 0.22f;
        }

        [Serializable]
        sealed class SaveData
        {
            public int version;
            public WeaponEntry[] weapons = Array.Empty<WeaponEntry>();
        }

        static SaveData _data;
        static bool _loaded;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;
            _data = new SaveData { version = SchemaVersion, weapons = BuildDefaults() };
            TryLoad();
        }

        public static WeaponEntry Get(string weaponKey)
        {
            EnsureLoaded();
            weaponKey ??= string.Empty;
            for (int i = 0; i < _data.weapons.Length; i++)
            {
                if (string.Equals(_data.weapons[i].key, weaponKey, StringComparison.OrdinalIgnoreCase))
                    return _data.weapons[i];
            }

            var fresh = DefaultEntry(weaponKey);
            Array.Resize(ref _data.weapons, _data.weapons.Length + 1);
            _data.weapons[^1] = fresh;
            return fresh;
        }

        public static bool TryLoad()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return false;
                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return false;
                var parsed = JsonUtility.FromJson<SaveData>(json);
                if (parsed?.weapons == null || parsed.weapons.Length == 0)
                    return false;
                if (parsed.version != SchemaVersion)
                {
                    Debug.LogWarning(
                        $"[WeaponFeel] {FileName} sürüm {parsed.version} != {SchemaVersion}; kod varsayılanları korunuyor.");
                    return false;
                }

                Merge(parsed.weapons);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WeaponFeel] yüklenemedi ({FilePath}): {e.Message}");
                return false;
            }
        }

        public static void Save()
        {
            EnsureLoaded();
            _data.version = SchemaVersion;
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WeaponFeel] kaydedilemedi ({FilePath}): {e.Message}");
            }
        }

        public static GripSave GripFor(string weaponKey, bool rightHand) =>
            rightHand ? Get(weaponKey).right : Get(weaponKey).left;

        static void Merge(WeaponEntry[] saved)
        {
            for (int s = 0; s < saved.Length; s++)
            {
                WeaponEntry src = saved[s];
                if (string.IsNullOrEmpty(src.key))
                    continue;
                WeaponEntry dst = Get(src.key);
                dst.right = src.right ?? dst.right;
                dst.left = src.left ?? dst.left;
                dst.hitBangSec = src.hitBangSec > 0.01f ? src.hitBangSec : dst.hitBangSec;
            }
        }

        static WeaponEntry[] BuildDefaults()
        {
            var list = new List<WeaponEntry>(WeaponKeys.Length);
            foreach (string key in WeaponKeys)
                list.Add(DefaultEntry(key));
            return list.ToArray();
        }

        static WeaponEntry DefaultEntry(string key)
        {
            var entry = new WeaponEntry { key = key, hitBangSec = 0.22f };
            SeedGrip(key, isRight: true, entry.right);
            SeedGrip(key, isRight: false, entry.left);
            return entry;
        }

        static void SeedGrip(string key, bool isRight, GripSave save)
        {
            save.LocalPosition = Vector3.zero;
            save.LocalEulerAngles = Vector3.zero;
            save.LocalScale = Vector3.one;
        }

        public static void ResetGripTuning(string weaponKey)
        {
            EnsureLoaded();
            WeaponEntry entry = Get(weaponKey);
            SeedGrip(weaponKey, isRight: true, entry.right);
            SeedGrip(weaponKey, isRight: false, entry.left);
        }

        public static WeaponGripProfile.GripOffset ToGripOffset(GripSave save) => new WeaponGripProfile.GripOffset
        {
            LocalPosition = save.LocalPosition,
            LocalEulerAngles = save.LocalEulerAngles,
            LocalScale = save.LocalScale.sqrMagnitude < 0.0001f ? Vector3.one : save.LocalScale,
        };
    }
}
