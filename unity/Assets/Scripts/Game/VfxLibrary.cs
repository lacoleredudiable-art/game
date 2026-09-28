using System;
using System.Collections.Generic;
using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// VFX eşleme tablosu: anahtar → prefab. Skill anahtarları <see cref="VfxKeyChain"/> ile
    /// genelleşir (<c>VFX_{Element}_{Fiil}_{Sifat}</c> → <c>VFX_{Element}_{Fiil}</c> →
    /// <c>VFX_{Fiil}</c>); his efektleri sabit <c>FX_*</c> anahtarlarıdır. Tabloda yoksa
    /// <c>Resources/Vfx/Hitbox/{anahtar}</c> ve <c>Resources/Vfx/{anahtar}</c> denenir, en son
    /// prosedürel şekle düşülür. CFX / Particle Pack prefab'ları buraya sürüklenir; paketler
    /// gitignore'da olduğundan tablo boşken oyun prosedürel yolda çalışır.
    /// <c>Resources/VfxLibrary.asset</c> yoksa varsayılan örnek.
    /// </summary>
    [CreateAssetMenu(menuName = "Dovus/Feel/Vfx Library", fileName = "VfxLibrary")]
    public sealed class VfxLibrary : ScriptableObject
    {
        public const string HitSpark = "FX_HitSpark";
        public const string CritSpark = "FX_CritSpark";
        public const string DodgeDust = "FX_DodgeDust";
        public const string FootDust = "FX_FootDust";
        public const string BossStepDust = "FX_BossStepDust";
        public const string SlamShockwave = "FX_SlamShockwave";
        public const string GroundCrack = "FX_GroundCrack";
        public const string FireCone = "FX_FireCone";

        [Serializable]
        public struct Entry
        {
            public string Key;
            public GameObject Prefab;
            /// <summary>Prefab örneği bu kadar sonra yok edilir (0 = prefab kendi yönetir).</summary>
            public float LifetimeSec;
            /// <summary>Prefab'ın 1x ölçekte kapladığı boyut (m); çağıran boyut verirse ona ölçeklenir (0 = ölçeklenmez).</summary>
            public float ReferenceSizeM;
        }

        public List<Entry> Entries = new();

        [Header("Paket prefab'ı renklendirme — önerilen")]
        [Range(0f, 1f)] public float ImpactTintStrength = 0.75f;
        [Range(0f, 1f)] public float HitSparkTintStrength = 0.5f;

        [Header("Prosedürel yedek — hepsi önerilen (docs/durum.md his turu Faz 4)")]
        public int HitSparkCount = 14;
        public float HitSparkSpeed = 7f;
        public float HitSparkLifeSec = 0.22f;
        public float HitSparkSize = 0.09f;
        public float CritSparkMult = 1.8f;

        public int DodgeDustCount = 16;
        public float DodgeDustLifeSec = 0.45f;
        public float DodgeDustSize = 0.45f;
        public Color DustColor = new(0.62f, 0.56f, 0.5f, 0.55f);

        public int FootDustCount = 4;
        public float FootDustLifeSec = 0.35f;
        public float FootDustSize = 0.22f;
        public float BossStepDustMult = 3f;

        public float ShockwaveSec = 0.45f;
        public float ShockwaveWidthM = 0.35f;
        public Color ShockwaveColor = new(1f, 0.78f, 0.5f, 0.85f);
        public float CrackHoldSec = 1.4f;
        public float CrackFadeSec = 0.6f;
        public Color CrackColor = new(0.08f, 0.05f, 0.04f, 0.85f);

        public float FlameSec = 0.5f;
        public int FlameRate = 220;
        public float FlameSpeed = 11f;
        public float FlameSize = 0.7f;
        public Color FlameColorA = new(1f, 0.85f, 0.35f, 1f);
        public Color FlameColorB = new(0.95f, 0.25f, 0.05f, 0f);

        static VfxLibrary _current;
        Dictionary<string, Entry> _map;

        public static VfxLibrary Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<VfxLibrary>("VfxLibrary");
                    if (_current == null)
                    {
                        _current = CreateInstance<VfxLibrary>();
                        _current.hideFlags = HideFlags.DontSave;
                    }
                }
                return _current;
            }
        }

        /// <summary>Zincirdeki ilk bulunan prefab; hiçbiri yoksa false (çağıran prosedürel çizer).</summary>
        public bool TryResolve(string key, out GameObject prefab, out float lifetimeSec) =>
            TryResolve(key, out prefab, out lifetimeSec, out _);

        bool TryResolve(string key, out GameObject prefab, out float lifetimeSec, out float referenceSizeM)
        {
            prefab = null;
            lifetimeSec = 0f;
            referenceSizeM = 0f;
            foreach (string candidate in VfxKeyChain.Expand(key))
            {
                if (Map.TryGetValue(candidate, out Entry e) && e.Prefab != null)
                {
                    prefab = e.Prefab;
                    lifetimeSec = e.LifetimeSec;
                    referenceSizeM = e.ReferenceSizeM;
                    return true;
                }
                if (Missing.Contains(candidate))
                    continue;
                prefab = Resources.Load<GameObject>("Vfx/Hitbox/" + candidate);
                if (prefab == null)
                    prefab = Resources.Load<GameObject>("Vfx/" + candidate);
                if (prefab != null)
                    return true;
                Missing.Add(candidate);
            }
            return false;
        }

        static readonly HashSet<string> Missing = new(StringComparer.Ordinal);

        /// <summary>
        /// Çözülen prefab'ı örnekler; yoksa null. <paramref name="sizeM"/> &gt; 0 ve kayıtta
        /// <see cref="Entry.ReferenceSizeM"/> varsa örnek o boyuta ölçeklenir.
        /// </summary>
        public GameObject TrySpawn(string key, Vector3 position, Quaternion rotation, Transform parent = null,
            float sizeM = 0f)
        {
            if (!TryResolve(key, out GameObject prefab, out float life, out float refSize))
                return null;
            GameObject go = Instantiate(prefab, position, rotation, parent);
            if (sizeM > 0f && refSize > 0f)
                ScaleTo(go, sizeM / refSize);
            if (life > 0f)
                Destroy(go, life);
            return go;
        }

        static void ScaleTo(GameObject go, float factor)
        {
            go.transform.localScale *= factor;
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        /// <summary>Paket prefab'ının başlangıç rengini elemente doğru çeker (alfa korunur).</summary>
        public static void Tint(GameObject go, Color tint, float strength)
        {
            if (go == null || strength <= 0f)
                return;
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                ParticleSystem.MinMaxGradient c = main.startColor;
                switch (c.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        main.startColor = Mix(c.color, tint, strength);
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        main.startColor = new ParticleSystem.MinMaxGradient(
                            Mix(c.colorMin, tint, strength), Mix(c.colorMax, tint, strength));
                        break;
                }
            }
        }

        static Color Mix(Color from, Color tint, float t)
        {
            Color c = Color.Lerp(from, tint, t);
            c.a = from.a;
            return c;
        }

        Dictionary<string, Entry> Map
        {
            get
            {
                if (_map != null)
                    return _map;
                _map = new Dictionary<string, Entry>(StringComparer.Ordinal);
                foreach (Entry e in Entries)
                {
                    if (!string.IsNullOrEmpty(e.Key))
                        _map[e.Key] = e;
                }
                return _map;
            }
        }

        void OnValidate() => _map = null;
    }
}
