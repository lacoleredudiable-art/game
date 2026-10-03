using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Audio
{
    /// <summary>
    /// Olay → klip listesi + ses/pitch rastgeleliği. <c>Resources/SfxLibrary.asset</c> varsa o;
    /// yoksa her olay <c>Resources/Sfx/{id}/</c> klasöründeki kliplerle aşağıdaki varsayılan
    /// ses/pitch değerlerini kullanır (hepsi önerilen — docs/durum.md his turu Faz 4).
    /// Cast sesi fiil ailesine göre <c>cast_{family}</c> (element-sistemi.json verbs[].family).
    /// </summary>
    [CreateAssetMenu(menuName = "Dovus/Feel/Sfx Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        public const string Hit = "hit";
        public const string Crit = "crit";
        public const string PlayerHurt = "player_hurt";
        public const string Dodge = "dodge";
        public const string PerfectDodge = "perfect_dodge";
        public const string BossWindup = "boss_windup";
        public const string BossSlam = "boss_slam";
        public const string BossFire = "boss_fire";
        public const string BossRoar = "boss_roar";
        public const string BossStep = "boss_step";
        public const string UiTap = "ui_tap";
        public const string Footstep = "footstep";
        public const string CastPrefix = "cast_";

        [Serializable]
        public struct Entry
        {
            public string Id;
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume;
            public float PitchMin;
            public float PitchMax;
            /// <summary>Aynı olay bu aralıktan sık çalmaz (ayak sesi / çoklu isabet yığılması).</summary>
            public float MinGapSec;
        }

        public List<Entry> Entries = new();

        [Range(0f, 1f)] public float MasterVolume = 0.8f;

        static readonly (string id, float vol, float pMin, float pMax, float gap)[] Defaults =
        {
            (Hit, 0.55f, 0.92f, 1.08f, 0.04f),
            (Crit, 0.75f, 0.95f, 1.05f, 0.05f),
            (PlayerHurt, 0.7f, 0.9f, 1.05f, 0.1f),
            (Dodge, 0.45f, 0.95f, 1.1f, 0.08f),
            (PerfectDodge, 0.7f, 1f, 1.05f, 0.1f),
            (BossWindup, 0.5f, 0.9f, 1f, 0.3f),
            (BossSlam, 0.9f, 0.85f, 0.95f, 0.2f),
            (BossFire, 0.65f, 0.9f, 1.05f, 0.2f),
            (BossRoar, 0.85f, 0.7f, 0.75f, 1f),
            (BossStep, 0.45f, 0.55f, 0.65f, 0.25f),
            (UiTap, 0.25f, 0.95f, 1.08f, 0.03f),
            (Footstep, 0.22f, 0.9f, 1.1f, 0.12f),
            (CastPrefix + "strike", 0.45f, 0.95f, 1.08f, 0.06f),
            (CastPrefix + "mend", 0.45f, 1f, 1.1f, 0.06f),
            (CastPrefix + "motion", 0.45f, 0.95f, 1.1f, 0.06f),
            (CastPrefix + "guard", 0.45f, 0.9f, 1f, 0.06f),
            (CastPrefix + "control", 0.45f, 0.9f, 1.05f, 0.06f),
            (CastPrefix + "disrupt", 0.45f, 0.95f, 1.1f, 0.06f),
            (CastPrefix + "purge", 0.45f, 1f, 1.1f, 0.06f),
            (CastPrefix + "special", 0.5f, 0.9f, 1.05f, 0.06f),
        };

        static SfxLibrary _current;
        Dictionary<string, Entry> _map;

        public static SfxLibrary Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<SfxLibrary>("SfxLibrary");
                    if (_current == null)
                    {
                        _current = CreateInstance<SfxLibrary>();
                        _current.hideFlags = HideFlags.DontSave;
                    }
                }
                return _current;
            }
        }

        public bool TryGet(string id, out Entry entry)
        {
            entry = default;
            return !string.IsNullOrEmpty(id) && Map.TryGetValue(id, out entry)
                   && entry.Clips != null && entry.Clips.Length > 0;
        }

        Dictionary<string, Entry> Map
        {
            get
            {
                if (_map != null)
                    return _map;
                _map = new Dictionary<string, Entry>(StringComparer.Ordinal);
                foreach (var d in Defaults)
                {
                    AudioClip[] clips = Resources.LoadAll<AudioClip>("Sfx/" + d.id);
                    _map[d.id] = new Entry
                    {
                        Id = d.id, Clips = clips, Volume = d.vol, PitchMin = d.pMin, PitchMax = d.pMax, MinGapSec = d.gap
                    };
                }
                foreach (Entry e in Entries)
                {
                    if (!string.IsNullOrEmpty(e.Id))
                        _map[e.Id] = e;
                }
                return _map;
            }
        }

        void OnValidate() => _map = null;
    }
}
