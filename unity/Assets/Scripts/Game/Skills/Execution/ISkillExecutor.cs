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
    public interface ISkillExecutor
    {
        SkillExecutorKind Kind { get; }
        void Execute(in SkillExecutionContext context);
    }

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
            GameClock clock,
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
            CastWindowSec = Mathf.Max(0.01f, castWindowSec);
            WindowOpen01 = Mathf.Clamp01(windowOpen01);
            WindowClose01 = Mathf.Clamp(windowClose01, WindowOpen01, 1f);
            RadiusM = Mathf.Max(0.05f, radiusM);
            RangeM = Mathf.Max(RadiusM, rangeM);
            SpeedMps = Mathf.Max(0.01f, speedMps);
            DurationSec = Mathf.Max(0.01f, durationSec);
            TickIntervalSec = Mathf.Max(0.01f, tickIntervalSec);
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
        public GameClock Clock { get; }
        public ManifestationTuning Tuning { get; }
        public Vector3 FieldCenter { get; }
        /// <summary>Summon: minion vuruÅŸu â€” skill base_damage'Ä±ndan baÄŸÄ±msÄ±z ham hasar.</summary>
        public Action<float> ApplyFlatDamage { get; }
        public int SpawnCount { get; }
        public MechanicPlan MechanicPlan { get; }
        public float ActivationDelaySec { get; }
        public float TickEffectFraction { get; }
        /// <summary>Dost fiilde yaydaki herkes. DÃ¼ÅŸman vuruÅŸunda yalnÄ±z kilit hedef.</summary>
        public bool ArcAllies { get; }
    }

    /// <summary>Unity yaÅŸam dÃ¶ngÃ¼sÃ¼ taÅŸÄ±yan fiziksel executor'lar iÃ§in ortak taban.</summary>
    public abstract class SkillExecutor : MonoBehaviour, ISkillExecutor
    {
        protected SkillExecutionContext Context { get; private set; }
        protected bool HasContext { get; private set; }
        float _activationDelayRemainingSec;

        public abstract SkillExecutorKind Kind { get; }

        /// <summary>Sunum katmanÄ± (HitboxVfxRegistry) bu cast'in gramer planÄ±nÄ± ve sahibini okur.</summary>
        public MechanicPlan Plan => HasContext ? Context.MechanicPlan : null;
        public Transform CastOwner => HasContext ? Context.Owner : null;

        /// <summary>DÃ¼nya saati: build menÃ¼sÃ¼ aÃ§Ä±kken 0, TimeDirector Ã¶lÃ§eÄŸini izler.</summary>
        protected float WorldDeltaSec => Context.Clock != null
            ? (float)(Context.Clock.WorldDeltaMs / 1000.0)
            : Time.deltaTime;

        public virtual void Execute(in SkillExecutionContext context)
        {
            Context = context;
            HasContext = true;
            _activationDelayRemainingSec = context.ActivationDelaySec;
        }

        /// <summary>YÃ¼kselen/gecikmeli-an: gÃ¶rsel dÃ¼nyada durur, gameplay bu kapÄ±dan sonra baÅŸlar.</summary>
        protected bool WaitingForActivation()
        {
            if (_activationDelayRemainingSec <= 0f)
                return false;
            _activationDelayRemainingSec -= WorldDeltaSec;
            return _activationDelayRemainingSec > 0f;
        }

        protected bool IsTarget(Collider collider)
        {
            if (collider == null || Context.Target == null)
                return false;
            Transform hit = collider.transform;
            return hit == Context.Target || hit.IsChildOf(Context.Target);
        }

        protected void Apply(float effectFraction)
        {
            if (effectFraction > 0f)
                Context.ApplyEffect?.Invoke(effectFraction);
        }

        protected void Finish()
        {
            HasContext = false;
            Destroy(gameObject);
        }
    }
}
