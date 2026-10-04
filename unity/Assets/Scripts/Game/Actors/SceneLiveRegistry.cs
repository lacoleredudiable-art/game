using System.Collections.Generic;

namespace Dovus.Game.Actors
{
    /// <summary>MonoBehaviour etkin kayıtları — sahne başına Composition örneği.</summary>
    public sealed class SceneLiveRegistry<T> where T : class
    {
        readonly List<T> _live = new();

        public IReadOnlyList<T> Live => _live;

        public void Register(T item)
        {
            if (item != null && !_live.Contains(item))
                _live.Add(item);
        }

        public void Unregister(T item)
        {
            if (item != null)
                _live.Remove(item);
        }
    }
}
