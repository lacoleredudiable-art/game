using Dovus.Core.Execution;
using Dovus.Core.Combat;
using Dovus.Core.Mechanic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Kısa cast penceresinde gerçek Physics overlap açar. Patlama fiili pencere açılırken
    /// tek geniş sphere; normal saldırı owner→ileri kapsülü kullanır.
    /// </summary>
    public sealed class MeleeHitboxExecutor : SkillExecutor
    {
        static readonly Collider[] Hits = new Collider[24];

        float _ageSec;
        bool _applied;
        bool _homing;

        public override SkillExecutorKind Kind => SkillExecutorKind.MeleeHitbox;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _ageSec = 0f;
            _applied = false;
            _homing = TargetingRules.IsHoming(context.AimMode)
                || (context.MechanicPlan != null
                    && MechanicWorldProfile.From(context.MechanicPlan).Homing);
            Vector3 origin = context.Owner != null ? context.Owner.position : context.Origin;
            Vector3 visualPosition = context.IsBurst
                ? origin + context.Direction * Mathf.Min(
                    context.RadiusM * context.Tuning.ExecutorBurstForwardFrac,
                    context.RangeM)
                : origin + Vector3.up * context.RadiusM;
            HitboxVfxRegistry.Create(
                context.VfxKey,
                context.HitboxShape,
                context.VfxColorHex,
                visualPosition,
                context.Direction,
                context.RadiusM,
                context.RangeM,
                transform);
        }

        void Update()
        {
            if (!HasContext)
                return;
            if (WaitingForActivation())
                return;

            _ageSec += WorldDeltaSec;
            float cast01 = _ageSec / Context.CastWindowSec;
            if (!_applied && cast01 >= Context.WindowOpen01 && cast01 <= Context.WindowClose01)
                Probe();

            if (_ageSec >= Context.CastWindowSec)
                Finish();
        }

        void Probe()
        {
            Vector3 origin = Context.Owner != null ? Context.Owner.position : Context.Origin;
            if (_homing && Context.Target != null)
            {
                Vector3 to = Context.Target.position - origin;
                to.y = 0f;
                if (to.magnitude <= Context.RangeM + Context.RadiusM)
                {
                    _applied = true;
                    Apply(1f);
                }
                return;
            }
            int count;
            if (Context.IsBurst)
            {
                Vector3 center = origin + Context.Direction * Mathf.Min(
                    Context.RadiusM * Context.Tuning.ExecutorBurstForwardFrac,
                    Context.RangeM);
                count = Physics.OverlapSphereNonAlloc(
                    center,
                    Context.RadiusM,
                    Hits,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide);
                // Patlama yakın dövüşte yalnız bir kez örneklenir.
                _applied = true;
            }
            else
            {
                float reach = Mathf.Max(Context.RadiusM, Context.RangeM);
                Vector3 baseAt = origin + Vector3.up * Context.RadiusM;
                Vector3 low = baseAt + Context.Direction * Context.RadiusM;
                Vector3 high = baseAt + Context.Direction * Mathf.Max(
                    Context.RadiusM,
                    reach - Context.RadiusM);
                count = Physics.OverlapCapsuleNonAlloc(
                    low,
                    high,
                    Context.RadiusM,
                    Hits,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide);
            }

            for (int i = 0; i < count; i++)
            {
                if (!IsTarget(Hits[i]))
                    continue;
                _applied = true;
                Apply(1f);
                break;
            }
        }
    }
}
