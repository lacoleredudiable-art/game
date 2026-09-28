using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Execution;
using Dovus.Core.Mechanic;
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
            public Vector3 OwnerOffset;
        }

        readonly List<Minion> _minions = new();
        float _ageSec;
        bool _statusesApplied;
        MechanicActorKind _actorKind;
        bool _continuousActorFlow;
        Vector3 _spawnOrigin;
        Vector3 _spawnSide;
        float _spawnSize;
        int _spawnedCount;
        bool _ringFormation;
        bool _invisibleActor;

        public override SkillExecutorKind Kind => SkillExecutorKind.Summon;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _ageSec = 0f;
            _statusesApplied = false;
            _actorKind = context.MechanicPlan != null
                ? MechanicWorldProfile.From(context.MechanicPlan).ActorKind
                : MechanicActorKind.Minion;
            _continuousActorFlow = context.MechanicPlan != null
                && MechanicWorldProfile.From(context.MechanicPlan).Continuous;
            MechanicEffect actorEffect = context.MechanicPlan?.Effects
                .FirstOrDefault(e => e.Stat is "aktor_yarat" or "klon");
            _ringFormation = actorEffect?.Has("halka") ?? false;
            _invisibleActor = actorEffect?.Has("gorunmez") ?? false;

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
            _spawnOrigin = origin;
            _spawnSide = Vector3.Cross(Vector3.up, context.Direction).normalized;
            _spawnSize = context.Tuning.ExecutorMinionSizeM;
            _spawnedCount = 0;
            int initialCount = _continuousActorFlow ? 1 : context.SpawnCount;
            for (int i = 0; i < initialCount; i++)
                SpawnActor(i);
        }

        void SpawnActor(int index)
        {
            float lateral = (index - (Context.SpawnCount - 1) * 0.5f) * _spawnSize * 1.5f;
            Vector3 at;
            if (_ringFormation)
            {
                float angle = 360f * index / Mathf.Max(1, Context.SpawnCount);
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Context.Direction;
                at = _spawnOrigin + radial * Mathf.Max(Context.RadiusM, _spawnSize);
            }
            else
            {
                at = _actorKind == MechanicActorKind.Turret
                    ? Context.FieldCenter + _spawnSide * lateral
                    : _spawnOrigin + Context.Direction * Context.RadiusM + _spawnSide * lateral;
            }
            at.y = _spawnOrigin.y + _spawnSize * 0.5f;
            GameObject body = CreateActorBody(at, _spawnSize, index);
            if (body != null)
            {
                _minions.Add(new Minion
                {
                    Body = body.transform,
                    NextAttackSec = 0f,
                    OwnerOffset = body.transform.position - _spawnOrigin
                });
            }
            _spawnedCount = Mathf.Max(_spawnedCount, index + 1);
        }

        GameObject CreateActorBody(Vector3 at, float size, int index)
        {
            PrimitiveType primitive = _actorKind switch
            {
                MechanicActorKind.Turret => PrimitiveType.Sphere,
                MechanicActorKind.Clone or MechanicActorKind.MirrorClone => PrimitiveType.Capsule,
                _ => PrimitiveType.Cylinder
            };
            GameObject body = GameObject.CreatePrimitive(primitive);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, true);
            body.transform.position = at;
            body.transform.localScale = _actorKind is MechanicActorKind.Clone or MechanicActorKind.MirrorClone
                ? new Vector3(size, size * 1.8f, size)
                : Vector3.one * size;
            body.name = $"{_actorKind}_{Context.Skill.SkillId}_{index}";
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color tint = _actorKind switch
                {
                    MechanicActorKind.Turret => new Color(1f, 0.55f, 0.2f, 0.9f),
                    MechanicActorKind.Clone => new Color(0.25f, 0.9f, 1f, 0.7f),
                    MechanicActorKind.MirrorClone => new Color(0.8f, 0.45f, 1f, 0.7f),
                    _ => new Color(0.5f, 0.9f, 0.65f, 0.85f)
                };
                if (_invisibleActor)
                    tint.a = 0.25f;
                renderer.material.color = tint;
            }
            return body;
        }

        void Update()
        {
            if (!HasContext)
                return;
            if (WaitingForActivation())
                return;

            float dt = WorldDeltaSec;
            _ageSec += dt;
            while (_continuousActorFlow && _spawnedCount < Context.SpawnCount
                && _ageSec >= _spawnedCount)
                SpawnActor(_spawnedCount);
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
            if (_actorKind == MechanicActorKind.MirrorClone && Context.Owner != null)
            {
                Vector3 mirrored = Context.Owner.position - m.OwnerOffset;
                mirrored.y = m.Body.position.y;
                m.Body.position = mirrored;
            }
            else if (_actorKind == MechanicActorKind.Guardian && Context.Owner != null)
            {
                Vector3 desired = Context.Owner.position + m.OwnerOffset;
                desired.y = m.Body.position.y;
                m.Body.position = Vector3.MoveTowards(
                    m.Body.position, desired, t.ExecutorMinionMoveSpeedMps * dt);
            }

            float attackReach = _actorKind == MechanicActorKind.Turret
                ? Context.RangeM
                : t.ExecutorMinionReachM;
            if (InReach(m.Body.position, attackReach))
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
                SpawnWeaponAttack(m.Body.position);
                GameObject fx = PlaceholderFactory.CreateImpact("tick", Context.ColorKey, Context.Target.position, transform.parent);
                if (fx != null)
                    Destroy(fx, 0.3f);
                return;
            }

            if (_actorKind is MechanicActorKind.Turret or MechanicActorKind.MirrorClone or MechanicActorKind.Guardian)
                return;

            Vector3 to = Context.Target.position - m.Body.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return;
            m.Body.position += to.normalized * (t.ExecutorMinionMoveSpeedMps * dt);
        }

        void SpawnWeaponAttack(Vector3 from)
        {
            if (Context.Target == null)
                return;
            MechanicEffect actor = Context.MechanicPlan?.Effects
                .FirstOrDefault(e => e.Stat is "aktor_yarat" or "klon");
            string pathClass = actor?.Modes.FirstOrDefault(m => m.StartsWith("silahla:"));
            bool ranged = pathClass is "silahla:ucan" or "silahla:hat" or "silahla:belirme";
            if (!ranged)
                return;
            GameObject shot = PlaceholderFactory.CreateTrail(
                pathClass == "silahla:hat" ? "beam" : "straight",
                Context.ColorKey,
                from,
                Context.Target.position,
                transform.parent);
            if (shot != null)
                Destroy(shot, 0.25f);
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
