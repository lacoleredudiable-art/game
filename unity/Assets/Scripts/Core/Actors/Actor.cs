using Dovus.Core.Shared;

namespace Dovus.Core.Actors
{
    public class Actor
    {
        protected Actor(ActorId id, ActorTeam team, ActorKind kind)
        {
            Id = id;
            Team = team;
            Kind = kind;
        }

        public ActorId Id { get; }
        public ActorTeam Team { get; }
        public ActorKind Kind { get; }
    }
}
