using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Mermi / alan / yakın teslimat sonucu (komut sırasına göre güncellenir).</summary>
    public sealed class RuleEngineV4DeliveryState
    {
        bool _ok;
        bool _missileResolved;
        Transform _missileHit;
        List<Transform> _areaHits;

        public Transform MissileHit
        {
            get => _missileHit;
            set
            {
                _missileResolved = true;
                _missileHit = value;
            }
        }

        public void SetAreaHits(List<Transform> hits) => _areaHits = hits;

        public void Refresh(
            ManifestationDirector director,
            CommandPlan plan,
            Transform focus,
            RuleEngineV4DeliveryEvaluator evaluator) =>
            _ok = evaluator.EvaluateDelivery(director, plan, focus);

        public bool HitAllowed(Transform victim, Transform primaryFocus)
        {
            if (victim == null)
                return false;
            if (_areaHits != null && _areaHits.Count > 0)
                return _areaHits.Contains(victim);
            if (_missileResolved)
                return _missileHit != null
                    && (victim == _missileHit || victim.IsChildOf(_missileHit));
            return _ok;
        }

        public IEnumerable<Transform> ResolveDamageTargets(Transform primaryFocus)
        {
            if (_areaHits != null && _areaHits.Count > 0)
                return _areaHits;
            if (_missileResolved)
                return _missileHit != null ? new[] { _missileHit } : System.Array.Empty<Transform>();
            if (primaryFocus != null)
                return new[] { primaryFocus };
            return System.Array.Empty<Transform>();
        }
    }
}
