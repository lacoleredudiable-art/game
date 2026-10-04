using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.App.Actors
{
    public sealed class AllyActor : Actor
    {
        public AllyActor(ActorId id, ActorTeam team)
            : base(id, team, ActorKind.Ally)
        {
        }
    }
}
