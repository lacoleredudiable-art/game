using Dovus.Core.Actors;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Kırılabilir koza: can var, çapalı (itilmez / hareket etmez).</summary>
    public sealed class SliceArenaCocoonHost : MonoBehaviour
    {
        int _hp;
        int _maxHp;

        public ActorId ActorId { get; private set; }
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public bool IsBroken => _hp <= 0;

        public void Configure(ActorId actorId, int maxHp)
        {
            ActorId = actorId;
            _maxHp = Mathf.Max(1, maxHp);
            _hp = _maxHp;
        }

        public void ApplyDamage(float amount)
        {
            if (IsBroken || amount <= 0)
                return;
            _hp = Mathf.Max(0, _hp - Mathf.RoundToInt(amount));
            if (IsBroken)
                gameObject.SetActive(false);
        }

        public float BodyRadiusM()
        {
            var col = GetComponent<SphereCollider>();
            return col != null ? col.radius : RuleEngineV4SliceArenaDefaults.CocoonRadiusM;
        }
    }
}
