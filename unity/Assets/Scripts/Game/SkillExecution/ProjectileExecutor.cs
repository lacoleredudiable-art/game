using Dovus.Core.Execution;
using Dovus.Core.Mechanic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Kinematic primitive projectile; hedef temasında etkiyi uygular, menzilde söner.</summary>
    public sealed class ProjectileExecutor : SkillExecutor
    {
        static readonly Collider[] Hits = new Collider[24];
        static readonly RaycastHit[] SweptHits = new RaycastHit[24];

        GameObject _projectile;
        Vector3 _spawn;
        float _travelM;
        bool _homing;

        public override SkillExecutorKind Kind => SkillExecutorKind.Projectile;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _spawn = context.Origin + Vector3.up
                * Mathf.Max(context.Tuning.ExecutorProjectileMinHeightM, context.RadiusM);
            _travelM = 0f;
            _homing = context.MechanicPlan != null
                && MechanicWorldProfile.From(context.MechanicPlan).Homing;

            _projectile = HitboxVfxRegistry.Create(
                context.VfxKey,
                context.HitboxShape,
                context.VfxColorHex,
                _spawn,
                context.Direction,
                context.RadiusM,
                context.RangeM,
                transform);
            if (_projectile == null)
            {
                _projectile = new GameObject("ProjectilePrimitive");
                _projectile.transform.SetParent(transform, false);
                _projectile.transform.position = _spawn;
                _projectile.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Sphere);
                _projectile.AddComponent<MeshRenderer>();
            }
            _projectile.name = $"Projectile_{context.Skill.SkillId}";
            if (context.HitboxShape is not ("capsule" or "line"))
                _projectile.transform.localScale = Vector3.one * context.RadiusM * 2f;

            var collider = _projectile.GetComponent<SphereCollider>();
            if (collider == null)
                collider = _projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;

            var body = _projectile.GetComponent<Rigidbody>();
            if (body == null)
                body = _projectile.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        void Update()
        {
            if (!HasContext || _projectile == null)
                return;
            if (WaitingForActivation())
                return;

            float remaining = Context.RangeM - _travelM;
            if (remaining <= 0f)
            {
                Finish();
                return;
            }

            float step = Mathf.Min(Context.SpeedMps * WorldDeltaSec, remaining);
            Vector3 from = _projectile.transform.position;
            Vector3 travelDirection = Context.Direction;
            if (_homing && Context.Target != null)
            {
                Vector3 toTarget = Context.Target.position - from;
                if (toTarget.sqrMagnitude > 0.0001f)
                    travelDirection = toTarget.normalized;
                _projectile.transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
            }
            int sweptCount = Physics.SphereCastNonAlloc(
                from,
                Context.RadiusM,
                travelDirection,
                SweptHits,
                step,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < sweptCount; i++)
            {
                if (!IsTarget(SweptHits[i].collider))
                    continue;
                _projectile.transform.position = from + travelDirection * SweptHits[i].distance;
                Impact();
                return;
            }

            _projectile.transform.position = from + travelDirection * step;
            _travelM += step;

            int count = Physics.OverlapSphereNonAlloc(
                _projectile.transform.position,
                Context.RadiusM,
                Hits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!IsTarget(Hits[i]))
                    continue;
                Impact();
                return;
            }

            if (_travelM >= Context.RangeM - 0.0001f)
                Finish();
        }

        void Impact()
        {
            Vector3 position = _projectile.transform.position;
            Apply(1f);
            GameObject impact = PlaceholderFactory.CreateImpact(
                Context.IsBurst ? "pulse" : "burst_soft",
                Context.ColorKey,
                position,
                transform.parent);
            if (impact != null)
                Destroy(impact, Context.CastWindowSec);
            Finish();
        }
    }
}
