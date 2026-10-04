using System;

namespace Dovus.Game.Diagnostics
{
    /// <summary>F1 debug panel görünürlüğü — runtime HUD'lar DevTools'a bağlanmaz.</summary>
    public interface IDebugPanelsChromeSink
    {
        bool Visible { get; }
        void Register(Action<bool> applyVisibility);
        void Unregister(Action<bool> applyVisibility);
    }
}
