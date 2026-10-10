using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Dilim oturumu: sabit yapı sınırı, azalma, koruma.</summary>
    public sealed class RuleEngineV4WorldSession
    {
        public readonly RuleEngineV4StructureLimits Structures = new();
        readonly Dictionary<int, (int stack, double lastHitMs)> _diminishByTarget = new();
        double _guardUntilMs;
        Transform _guardCaster;
        float _guardBlockRatio = 1;

        public float GuardBlockRatio => _guardBlockRatio;

        public bool GuardActive(double worldMs, Transform caster) =>
            _guardCaster == caster && worldMs < _guardUntilMs;

        public void ArmGuard(Transform caster, float durationSec, float blockRatio, double worldMs)
        {
            _guardCaster = caster;
            _guardBlockRatio = Mathf.Clamp01(blockRatio);
            _guardUntilMs = worldMs + durationSec * Units.SecToMs;
        }

        public float ApplyDiminishNonDamage(Transform victim, float value, double worldMs)
        {
            int key = victim != null ? victim.GetInstanceID() : 0;
            _diminishByTarget.TryGetValue(key, out var entry);
            double gapMs = RuleEngineV4CatalogDefaults.ZamanLookbackSec * Units.SecToMs;
            if (worldMs - entry.lastHitMs > gapMs)
                entry.stack = 0;
            float scaled = RuleEngineV4DiminishStack.ScaleNonDamage(entry.stack, value);
            entry.stack += 1;
            entry.lastHitMs = worldMs;
            _diminishByTarget[key] = entry;
            return scaled;
        }
    }
}
