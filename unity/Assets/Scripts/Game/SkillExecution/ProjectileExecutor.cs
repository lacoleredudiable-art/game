using Dovus.Core.Execution;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Kinematic primitive projectile; hedef temasında etkiyi uygular, menzilde söner.</summary>
    public sealed class ProjectileExecutor : SkillExecutor
    {
        static readonly Collider[] Hits = new Collider[24];

        GameObject _projectile;
        Vector3 _spawn;
        float _travelM;

        public override SkillExecutorKind Kind => SkillExecutorKind.Projectile;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _spawn = context.Origin + Vector3.up * Mathf.Max(0.35f, context.RadiusM);
            _travelM = 0f;

            _projectile = PlaceholderFactory.CreateImpact(
                "burst_soft",
                context.ColorKey,
                _spawn,
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

            float step = Context.SpeedMps * Time.deltaTime;
            _projectile.transform.position += Context.Direction * step;
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

            if (_travelM >= Context.RangeM)
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
