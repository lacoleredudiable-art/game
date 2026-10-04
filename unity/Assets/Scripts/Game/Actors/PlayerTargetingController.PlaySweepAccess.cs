#if UNITY_EDITOR
namespace Dovus.Game.Actors
{
    public sealed partial class PlayerTargetingController
    {
        public void SweepSelect(TargetableHost target) => Select(target);
    }
}
#endif
