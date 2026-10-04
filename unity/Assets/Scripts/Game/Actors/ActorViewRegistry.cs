using System.Collections.Generic;
using Dovus.Core.Actors;
using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>ActorId → sahne Transform (yalnız Game; Core/App Unity içermez).</summary>
    public sealed class ActorViewRegistry
    {
        readonly Dictionary<ActorId, Transform> _transforms = new();

        public void Register(ActorId id, Transform transform)
        {
            if (id.IsEmpty || transform == null)
                return;
            _transforms[id] = transform;
        }

        public bool TryGetTransform(ActorId id, out Transform transform) =>
            _transforms.TryGetValue(id, out transform!);

        public Transform? GetTransform(ActorId id) =>
            _transforms.TryGetValue(id, out Transform? t) ? t : null;
    }
}
