using System.Collections.Generic;
using Dovus.Core.Execution;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Çağırma fiili: oyuncunun yanında minion_count kadar minion doğar, minion_duration_sec
    /// boyunca boss'a yürür ve menzildeyken aralıklı vurur. İlk vuruş skill durumlarını taşır.
    /// </summary>
    public sealed class SummonExecutor : SkillExecutor
    {
        static readonly Collider[] Hits = new Collider[24];

        sealed class Minion
        {
            public Transform Body;
            public float NextAttackSec;
        }

        readonly List<Minion> _minions = new();
        float _ageSec;
        bool _statusesApplied;

        public override SkillExecutorKind Kind => SkillExecutorKind.Summon;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _ageSec = 0f;
            _statusesApplied = false;

            Vector3 origin = context.Owner != null ? context.Owner.position : context.Origin;
            HitboxVfxRegistry.Create(
                context.VfxKey,
                context.HitboxShape,
                context.VfxColorHex,
                origin + context.Direction * context.RangeM,
                context.Direction,
                context.RadiusM,
                context.RangeM,
                transform);
            Vector3 side = Vector3.Cross(Vector3.up, context.Direction).normalized;
            float size = context.Tuning.ExecutorMinionSizeM;
            for (int i = 0; i < context.SpawnCount; i++)
            {
                float lateral = (i - (context.SpawnCount - 1) * 0.5f) * size * 1.5f;
                Vector3 at = origin + context.Direction * context.RadiusM + side * lateral;
                at.y = origin.y + size * 0.5f;
                GameObject body = PlaceholderFactory.CreateImpact("strike", context.ColorKey, at, transform);
                if (body == null)
                    continue;
                body.name = $"Minion_{context.Skill.SkillId}_{i}";
                body.transform.localScale = Vector3.one * size;
                _minions.Add(new Minion { Body = body.transform, NextAttackSec = 0f });
            }
        }

        void Update()
        {
            if (!HasContext)
                return;

            float dt = WorldDeltaSec;
            _ageSec += dt;
            for (int i = 0; i < _minions.Count; i++)
                TickMinion(_minions[i], dt);

            if (_ageSec >= Context.DurationSec)
                Finish();
        }

        void TickMinion(Minion m, float dt)
        {
            if (m.Body == null || Context.Target == null)
                return;

            ManifestationTuning t = Context.Tuning;
            if (InReach(m.Body.position, t.ExecutorMinionReachM))
            {
                if (_ageSec < m.NextAttackSec)
                    return;
                m.NextAttackSec = _ageSec + t.ExecutorMinionAttackIntervalSec;
                if (!_statusesApplied)
                {
                    _statusesApplied = true;
                    Apply(1f);
                }
                Context.ApplyFlatDamage?.Invoke(t.ExecutorMinionHitDamage);
                GameObject fx = PlaceholderFactory.CreateImpact("tick", Context.ColorKey, m.Body.position, transform.parent);
                if (fx != null)
                    Destroy(fx, 0.3f);
                return;
            }

            Vector3 to = Context.Target.position - m.Body.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return;
            m.Body.position += to.normalized * (t.ExecutorMinionMoveSpeedMps * dt);
        }

        bool InReach(Vector3 at, float reach)
        {
            int count = Physics.OverlapSphereNonAlloc(at, reach, Hits, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (IsTarget(Hits[i]))
                    return true;
            }
            return false;
        }
    }
}
