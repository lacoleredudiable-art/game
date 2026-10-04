#if UNITY_EDITOR
namespace Dovus.Game.Actors
{
    public sealed partial class AllyDummyController
    {
        /// <summary>Eski reflection yazımıyla aynı: yalnız _hp alanı (clamp/etiket yenileme yok).</summary>
        public void SweepSetHp(int hp) => _hp = hp;
    }
}
#endif
