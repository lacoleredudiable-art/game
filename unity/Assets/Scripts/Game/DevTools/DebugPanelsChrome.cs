using System;
using System.Collections.Generic;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// Editör / DOVUS_DEBUG: tüm debug panelleri ve köşe kısayolları tek yerden gizlenir (F1).
    /// Release derlemelerinde derlenmez.
    /// </summary>
    public static class DebugPanelsChrome
    {
#if UNITY_EDITOR || DOVUS_DEBUG
        static readonly List<Action<bool>> _apply = new();

        public static bool Visible { get; private set; }

        public static void Register(Action<bool> applyVisibility)
        {
            if (applyVisibility == null)
                return;
            if (!_apply.Contains(applyVisibility))
                _apply.Add(applyVisibility);
            applyVisibility(Visible);
        }

        public static void Unregister(Action<bool> applyVisibility) => _apply.Remove(applyVisibility);

        public static void SetVisible(bool visible)
        {
            if (Visible == visible)
                return;
            Visible = visible;
            for (int i = 0; i < _apply.Count; i++)
                _apply[i]?.Invoke(visible);
        }
#endif
    }
}
