using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Tuning;
using Dovus.Game.Composition;
using System;
using UnityEngine;
namespace Dovus.Game.Skills.Execution
{
public readonly struct SkillExecutionContext
    {
        public SkillExecutionContext(
            SkillResolution skill,
            Transform owner,
            Transform target,
            SkillAimMode aimMode,
            Vector3 origin,
            Vector3 direction,
            float castWindowSec,
            float windowOpen01,
            float windowClose01,
            float radiusM,
            float rangeM,
            float speedMps,
            float durationSec,
            float tickIntervalSec,
            bool isBurst,
            bool isFriendly,
            string colorKey,
            string hitboxShape,
            float hitboxAngleDeg,
            string vfxKey,
            string vfxColorHex,
            Action<float> applyEffect,
            GameClockHost clock,
            ManifestationTuning tuning,
            Vector3 fieldCenter,
            Action<float> applyFlatDamage = null,
            int spawnCount = 1,
            MechanicPlan mechanicPlan = null,
            float activationDelaySec = 0f,
            float tickEffectFraction = 0f,
            bool arcAllies = false)
        {
            FieldCenter = fieldCenter;
            ApplyFlatDamage = applyFlatDamage;
            SpawnCount = Mathf.Max(1, spawnCount);
            MechanicPlan = mechanicPlan;
            ActivationDelaySec = Mathf.Max(0f, activationDelaySec);
            TickEffectFraction = Mathf.Max(0f, tickEffectFraction);
            ArcAllies = arcAllies;
            Skill = skill;
            Owner = owner;
            Target = target;
            AimMode = aimMode;
            Origin = origin;
            Direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
            CastWindowSec = Mathf.Max(ISkillExecutorDefaults.MinPositiveSec, castWindowSec);
            WindowOpen01 = Mathf.Clamp01(windowOpen01);
            WindowClose01 = Mathf.Clamp(windowClose01, WindowOpen01, 1f);
            RadiusM = Mathf.Max(ISkillExecutorDefaults.MinRadiusM, radiusM);
            RangeM = Mathf.Max(RadiusM, rangeM);
            SpeedMps = Mathf.Max(ISkillExecutorDefaults.MinPositiveSec, speedMps);
            DurationSec = Mathf.Max(ISkillExecutorDefaults.MinPositiveSec, durationSec);
            TickIntervalSec = Mathf.Max(ISkillExecutorDefaults.MinPositiveSec, tickIntervalSec);
            IsBurst = isBurst;
            IsFriendly = isFriendly;
            ColorKey = colorKey ?? string.Empty;
            HitboxShape = hitboxShape ?? string.Empty;
            HitboxAngleDeg = Mathf.Max(0f, hitboxAngleDeg);
            VfxKey = vfxKey ?? string.Empty;
            VfxColorHex = vfxColorHex ?? string.Empty;
            ApplyEffect = applyEffect;
            Clock = clock;
            Tuning = tuning ?? new ManifestationTuning();
        }

        public SkillResolution Skill { get; }
        public Transform Owner { get; }
        public Transform Target { get; }
        public SkillAimMode AimMode { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public float CastWindowSec { get; }
        public float WindowOpen01 { get; }
        public float WindowClose01 { get; }
        public float RadiusM { get; }
        public float RangeM { get; }
        public float SpeedMps { get; }
        public float DurationSec { get; }
        public float TickIntervalSec { get; }
        public bool IsBurst { get; }
        public bool IsFriendly { get; }
        public string ColorKey { get; }
        public string HitboxShape { get; }
        public float HitboxAngleDeg { get; }
        public string VfxKey { get; }
        public string VfxColorHex { get; }
        public Action<float> ApplyEffect { get; }
        public GameClockHost Clock { get; }
        public ManifestationTuning Tuning { get; }
        public Vector3 FieldCenter { get; }
        /// <summary>Summon: minion vuruşu — skill base_damage'ından bağımsız ham hasar.</summary>
        public Action<float> ApplyFlatDamage { get; }
        public int SpawnCount { get; }
        public MechanicPlan MechanicPlan { get; }
        public float ActivationDelaySec { get; }
        public float TickEffectFraction { get; }
        /// <summary>Dost fiilde yaydaki herkes. Düşman vuruşunda yalnız kilit hedef.</summary>
        public bool ArcAllies { get; }
    }

    /// <summary>Unity yaşam döngüsü taşıyan fiziksel executor'lar için ortak taban.</summary>
}
