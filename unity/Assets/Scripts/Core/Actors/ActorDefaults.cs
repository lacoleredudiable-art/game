using Dovus.Core.Shared;

namespace Dovus.Core.Actors
{
    /// <summary>Kurulum kökünde kayıtlı sabit kimlikler (TeamModifierHub.PlayerActorId = 1 ile uyumlu).</summary>
    public static class ActorDefaults
    {
        public static readonly ActorId PlayerId = new ActorId("1");
        public static readonly ActorId AllyDummyId = new ActorId("2");
        public static readonly ActorId BossId = new ActorId("3");
    }
}
