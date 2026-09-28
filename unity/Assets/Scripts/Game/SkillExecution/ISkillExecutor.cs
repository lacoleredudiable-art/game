using System;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
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
            Action startMotion = null,
            Action<float> applyFlatDamage = null,
            int spawnCount = 1)
        {
            FieldCenter = fieldCenter;
            StartMotion = startMotion;
            ApplyFlatDamage = applyFlatDamage;
            SpawnCount = Mathf.Max(1, spawnCount);
            Skill = skill;
            Owner = owner;
            Target = target;
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
        /// <summary>Movement: oyuncuyu dash planıyla taşır (executor başlarken bir kez).</summary>
        public Action StartMotion { get; }
        /// <summary>Summon: minion vuruşu — skill base_damage'ından bağımsız ham hasar.</summary>
        public Action<float> ApplyFlatDamage { get; }
        public int SpawnCount { get; }
    }

    /// <summary>Unity yaşam döngüsü taşıyan fiziksel executor'lar için ortak taban.</summary>
    public abstract class SkillExecutor : MonoBehaviour, ISkillExecutor
    {
        protected SkillExecutionContext Context { get; private set; }
        protected bool HasContext { get; private set; }

        public abstract SkillExecutorKind Kind { get; }

        /// <summary>Dünya saati: build menüsü açıkken 0, TimeDirector ölçeğini izler.</summary>
        protected float WorldDeltaSec => Context.Clock != null
            ? (float)(Context.Clock.WorldDeltaMs / 1000.0)
            : Time.deltaTime;

        public virtual void Execute(in SkillExecutionContext context)
        {
            Context = context;
            HasContext = true;
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
