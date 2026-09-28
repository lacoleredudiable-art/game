using Dovus.Core.Execution;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Yerde yaşayan ince alan. Süresi boyunca presentation tick aralığında heal/damage/status
    /// callback'i üretir ve sonra hem mantık hem disk görseli birlikte düşer.
    /// </summary>
    public sealed class FieldAuraExecutor : SkillExecutor
    {
        static readonly Collider[] Hits = new Collider[24];

        Vector3 _center;
        float _ageSec;
        float _nextTickSec;
        int _remainingTicks;
        float _effectFraction;

        public override SkillExecutorKind Kind => SkillExecutorKind.FieldAura;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            Transform anchor = context.IsFriendly ? context.Owner : context.Target;
            _center = anchor != null ? anchor.position : context.Origin;
            _ageSec = 0f;
            _nextTickSec = 0f;
            _remainingTicks = Mathf.Max(1, Mathf.CeilToInt(context.DurationSec / context.TickIntervalSec));
            _effectFraction = 1f / _remainingTicks;

            GameObject disk = PlaceholderFactory.CreateZoneDisk(
                context.ColorKey,
                _center,
                context.RadiusM,
                transform,
                alpha: context.Tuning.ExecutorFieldDiskAlpha);
            if (disk != null)
                disk.name = $"FieldAura_{context.Skill.SkillId}";
        }

        void Update()
        {
            if (!HasContext)
                return;

            _ageSec += WorldDeltaSec;
            while (_remainingTicks > 0 && _ageSec >= _nextTickSec)
            {
                TickField();
                _remainingTicks--;
                _nextTickSec += Context.TickIntervalSec;
            }

            if (_ageSec >= Context.DurationSec)
                Finish();
        }

        void TickField()
        {
            if (Context.IsFriendly)
            {
                Apply(_effectFraction);
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(
                _center,
                Context.RadiusM,
                Hits,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!IsTarget(Hits[i]))
                    continue;
                Apply(_effectFraction);
                return;
            }
        }
    }
}
