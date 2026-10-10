using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Dilim oturumu: sabit yapı sınırı, azalma, koruma.</summary>
    public sealed class RuleEngineV4WorldSession
    {
        public readonly RuleEngineV4StructureLimits Structures = new();
        readonly Dictionary<int, int> _diminishByTarget = new();
        double _guardUntilMs;
        Transform _guardCaster;
        float _guardBlockRatio = 1f;

        public float GuardBlockRatio => _guardBlockRatio;

        public bool GuardActive(double worldMs, Transform caster) =>
            _guardCaster == caster && worldMs < _guardUntilMs;

        public void ArmGuard(Transform caster, float durationSec, float blockRatio, double worldMs)
        {
            _guardCaster = caster;
            _guardBlockRatio = Mathf.Clamp01(blockRatio);
            _guardUntilMs = worldMs + durationSec * 1000.0;
        }

        public float ApplyDiminishNonDamage(Transform target, float value)
        {
            int key = target != null ? target.GetInstanceID() : 0;
            _diminishByTarget.TryGetValue(key, out int stack);
            float scaled = RuleEngineV4DiminishStack.ScaleNonDamage(stack, value);
            _diminishByTarget[key] = stack + 1;
            return scaled;
        }
    }
}
