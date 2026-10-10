using Dovus.Core.Actors;
using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Dilim hafif düşman (0,5 m gövde); PhysX trigger collider ile v4 sorgularına katılır.</summary>
    public sealed class SliceLightMinionHost : MonoBehaviour
    {
        int _hp;
        int _maxHp;

        public ActorId ActorId { get; private set; }
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsDown => _hp <= 0;

        public void Configure(ActorId actorId, int maxHp)
        {
            ActorId = actorId;
            _maxHp = Mathf.Max(1, maxHp);
            _hp = _maxHp;
        }

        public void ApplyDamage(float amount)
        {
            if (IsDown || amount <= 0f)
                return;
            _hp = Mathf.Max(0, _hp - Mathf.RoundToInt(amount));
        }

        public float BodyRadiusM()
        {
            var col = GetComponent<CapsuleCollider>();
            return col != null ? col.radius : RuleEngineV4PhysicsDefaults.SliceMinionFallbackRadiusM;
        }
    }
}
