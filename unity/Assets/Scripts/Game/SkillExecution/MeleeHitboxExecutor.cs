using Dovus.Core.Execution;
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

        public override SkillExecutorKind Kind => SkillExecutorKind.MeleeHitbox;

        public override void Execute(in SkillExecutionContext context)
        {
            base.Execute(context);
            _ageSec = 0f;
            _applied = false;
        }

        void Update()
        {
            if (!HasContext)
                return;

            _ageSec += Time.deltaTime;
            float cast01 = _ageSec / Context.CastWindowSec;
            if (!_applied && cast01 >= Context.WindowOpen01 && cast01 <= Context.WindowClose01)
                Probe();

            if (_ageSec >= Context.CastWindowSec)
                Finish();
        }

        void Probe()
        {
            Vector3 origin = Context.Owner != null ? Context.Owner.position : Context.Origin;
            int count;
            if (Context.IsBurst)
            {
                Vector3 center = origin + Context.Direction * Mathf.Min(Context.RadiusM * 0.35f, Context.RangeM);
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
                Vector3 low = origin + Vector3.up * Context.RadiusM;
                Vector3 high = low + Context.Direction * reach;
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
