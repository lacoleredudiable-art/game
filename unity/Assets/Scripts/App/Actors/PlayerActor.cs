using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.App.Actors
{
    public sealed class PlayerActor : Actor
    {
        public PlayerActor(ActorId id, ActorTeam team, PlayerHealth health)
            : base(id, team, ActorKind.Player)
        {
            Health = health;
        }

        public PlayerHealth Health { get; }
    }
}
