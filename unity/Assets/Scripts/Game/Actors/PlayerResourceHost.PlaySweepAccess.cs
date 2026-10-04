#if UNITY_EDITOR
namespace Dovus.Game.Actors
{
    public sealed partial class PlayerResourceHost
    {
        public void SweepRefillMana() => _tracker?.RefillToMax();
    }
}
#endif
