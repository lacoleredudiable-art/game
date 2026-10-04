#if UNITY_EDITOR
using UnityEngine;

namespace Dovus.Game.Actors
{
    public sealed partial class AllyDummyController
    {
        public void SweepSetHp(int hp)
        {
            _hp = Mathf.Clamp(hp, 0, _maxHp);
            RefreshLabel();
        }
    }
}
#endif
