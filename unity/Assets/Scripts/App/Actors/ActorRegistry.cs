using System.Collections.Generic;
using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.App.Actors
{
    public sealed class ActorRegistry
    {
        readonly Dictionary<ActorId, Actor> _byId = new();

        public void Register(Actor actor)
        {
            if (actor == null)
                return;
            _byId[actor.Id] = actor;
        }

        public bool TryGet(ActorId id, out Actor actor) => _byId.TryGetValue(id, out actor!);

        public Actor? Get(ActorId id) => _byId.TryGetValue(id, out Actor? actor) ? actor : null;
    }
}
