using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Mermi / alan / yakın teslimat sonucu (komut sırasına göre güncellenir).</summary>
    public sealed class RuleEngineV4DeliveryState
    {
        bool _ok;
        Transform _missileHit;
        List<Transform> _areaHits;

        public Transform MissileHit
        {
            get => _missileHit;
            set => _missileHit = value;
        }

        public void SetAreaHits(List<Transform> hits) => _areaHits = hits;

        public void Refresh(ManifestationDirector director, CommandPlan plan, Transform target) =>
            _ok = RuleEngineV4CommandRunner.EvaluateDelivery(director, plan, target);

        public bool HitAllowed(Transform victim, Transform primaryTarget)
        {
            if (victim == null)
                return false;
            if (_areaHits != null && _areaHits.Count > 0)
                return _areaHits.Contains(victim);
            if (_missileHit != null)
                return victim == _missileHit || victim.IsChildOf(_missileHit);
            return _ok;
        }

        public IEnumerable<Transform> ResolveDamageTargets(Transform primary)
        {
            if (_areaHits != null && _areaHits.Count > 0)
                return _areaHits;
            if (_missileHit != null)
                return new[] { _missileHit };
            if (primary != null)
                return new[] { primary };
            return System.Array.Empty<Transform>();
        }
    }
}
