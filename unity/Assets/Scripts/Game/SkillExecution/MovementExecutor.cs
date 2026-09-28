using Dovus.Core.Execution;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Hareket fiili (dash_line): oyuncuyu dash planıyla taşır ve geçtiği yolu kare kare
    /// kapsülle tarar. Boss çizgiye değerse etki bir kez uygulanır (fiil_hitbox.3 multi=false).
    /// </summary>
    public sealed class MovementExecutor : SkillExecutor
    {
        static readonly Collider[] Hits = new Collider[24];

        float _ageSec;
        bool _applied;
        Vector3 _start;
        Vector3 _last;
        LineRenderer _trail;
        GameObject _trailGo;
        bool _motionStarted;

        public override SkillExecutorKind Kind => SkillExecutorKind.Movement;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _ageSec = 0f;
            _applied = false;
            _start = OwnerPosition();
            _last = OwnerPosition();
            _motionStarted = false;
            HitboxVfxRegistry.Create(
                context.VfxKey,
                context.HitboxShape,
                context.VfxColorHex,
                _start + Vector3.up * context.RadiusM,
                context.Direction,
                context.RadiusM,
                context.RangeM,
                transform);

            _trailGo = PlaceholderFactory.CreateTrail("blink_line", context.ColorKey, _start, _start + Vector3.up * 0.01f, transform);
            if (_trailGo != null)
            {
                _trailGo.name = $"DashLine_{context.Skill.SkillId}";
                _trail = _trailGo.GetComponent<LineRenderer>();
                if (_trail != null)
                    _trail.widthMultiplier = context.RadiusM * 2f;
            }
        }

        void Update()
        {
            if (!HasContext)
                return;
            if (WaitingForActivation())
                return;
            if (!_motionStarted)
            {
                _motionStarted = true;
                Context.StartMotion?.Invoke();
                _start = OwnerPosition();
                _last = _start;
            }

            _ageSec += WorldDeltaSec;
            Vector3 now = OwnerPosition();
            if (!_applied)
                Sweep(_last, now);
            _last = now;

            if (_trail != null)
            {
                _trail.SetPosition(0, _start + Vector3.up * 0.05f);
                _trail.SetPosition(1, now + Vector3.up * 0.05f);
            }

            if (_ageSec >= Context.DurationSec)
            {
                if (_trailGo != null)
                {
                    _trailGo.transform.SetParent(transform.parent, true);
                    Destroy(_trailGo, 0.35f);
                }
                Finish();
            }
        }

        void Sweep(Vector3 from, Vector3 to)
        {
            float r = Context.RadiusM;
            Vector3 up = Vector3.up * r;
            int count = Physics.OverlapCapsuleNonAlloc(
                from + up,
                to + up,
                r,
                Hits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!IsTarget(Hits[i]))
                    continue;
                _applied = true;
                Apply(1f);
                return;
            }
        }

        Vector3 OwnerPosition() => Context.Owner != null ? Context.Owner.position : Context.Origin;
    }
}
