using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
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
            (Hit, SfxLibraryDefaults.HitVolume, SfxLibraryDefaults.HitPitchMin, SfxLibraryDefaults.HitPitchMax, SfxLibraryDefaults.HitMinGapSec),
            (Crit, SfxLibraryDefaults.CritVolume, SfxLibraryDefaults.CritPitchMin, SfxLibraryDefaults.CritPitchMax, SfxLibraryDefaults.CritMinGapSec),
            (PlayerHurt, SfxLibraryDefaults.PlayerHurtVolume, SfxLibraryDefaults.PlayerHurtPitchMin, SfxLibraryDefaults.PlayerHurtPitchMax, SfxLibraryDefaults.PlayerHurtMinGapSec),
            (Dodge, SfxLibraryDefaults.DodgeVolume, SfxLibraryDefaults.DodgePitchMin, SfxLibraryDefaults.DodgePitchMax, SfxLibraryDefaults.DodgeMinGapSec),
            (PerfectDodge, SfxLibraryDefaults.PerfectDodgeVolume, 1f, SfxLibraryDefaults.PerfectDodgePitchMax, SfxLibraryDefaults.PerfectDodgeMinGapSec),
            (BossWindup, 0.5f, SfxLibraryDefaults.BossWindupPitchMin, 1f, SfxLibraryDefaults.BossWindupMinGapSec),
            (BossSlam, SfxLibraryDefaults.BossSlamVolume, SfxLibraryDefaults.BossSlamPitchMin, SfxLibraryDefaults.BossSlamPitchMax, SfxLibraryDefaults.BossSlamMinGapSec),
            (BossFire, SfxLibraryDefaults.BossFireVolume, SfxLibraryDefaults.BossFirePitchMin, SfxLibraryDefaults.BossFirePitchMax, SfxLibraryDefaults.BossFireMinGapSec),
            (BossRoar, SfxLibraryDefaults.BossRoarVolume, SfxLibraryDefaults.BossRoarPitchMin, SfxLibraryDefaults.BossRoarPitchMax, 1f),
            (BossStep, SfxLibraryDefaults.BossStepVolume, SfxLibraryDefaults.BossStepPitchMin, SfxLibraryDefaults.BossStepPitchMax, SfxLibraryDefaults.BossStepMinGapSec),
            (UiTap, SfxLibraryDefaults.UiTapVolume, SfxLibraryDefaults.UiTapPitchMin, SfxLibraryDefaults.UiTapPitchMax, SfxLibraryDefaults.UiTapMinGapSec),
            (Footstep, SfxLibraryDefaults.FootstepVolume, SfxLibraryDefaults.FootstepPitchMin, SfxLibraryDefaults.FootstepPitchMax, SfxLibraryDefaults.FootstepMinGapSec),
            (CastPrefix + "strike", SfxLibraryDefaults.CastVerbVolume, SfxLibraryDefaults.CastStrikePitchMin, SfxLibraryDefaults.CastStrikePitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "mend", SfxLibraryDefaults.CastVerbVolume, 1f, SfxLibraryDefaults.CastMendPitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "motion", SfxLibraryDefaults.CastVerbVolume, SfxLibraryDefaults.CastMotionPitchMin, SfxLibraryDefaults.CastMotionPitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "guard", SfxLibraryDefaults.CastVerbVolume, SfxLibraryDefaults.CastGuardPitchMin, 1f, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "control", SfxLibraryDefaults.CastVerbVolume, SfxLibraryDefaults.CastControlPitchMin, SfxLibraryDefaults.CastControlPitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "disrupt", SfxLibraryDefaults.CastVerbVolume, SfxLibraryDefaults.CastDisruptPitchMin, SfxLibraryDefaults.CastDisruptPitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "purge", SfxLibraryDefaults.CastVerbVolume, 1f, SfxLibraryDefaults.CastPurgePitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
            (CastPrefix + "special", 0.5f, SfxLibraryDefaults.CastSpecialPitchMin, SfxLibraryDefaults.CastSpecialPitchMax, SfxLibraryDefaults.CastVerbMinGapSec),
        };

        Dictionary<string, Entry> _map;

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
                    AudioClip[] clips = AssetLoader.LoadAll<AudioClip>("Sfx/" + d.id);
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
