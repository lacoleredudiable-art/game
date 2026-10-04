using Dovus.Core.Boss;
using Dovus.Core.Shared;

namespace Dovus.Core.Actors
{
    public sealed class BossActor : Actor
    {
        public BossActor(ActorId id, ActorTeam team, BossVitals vitals)
            : base(id, team, ActorKind.Boss)
        {
            Vitals = vitals;
        }

        public BossVitals Vitals { get; }
    }
}
