using System;
using Dovus.Game.Diagnostics;

namespace Dovus.Game.DevTools
{
    sealed class DevToolsDebugPanelsChromeSink : IDebugPanelsChromeSink
    {
        public bool Visible => DebugPanelsChrome.Visible;

        public void Register(Action<bool> applyVisibility) => DebugPanelsChrome.Register(applyVisibility);

        public void Unregister(Action<bool> applyVisibility) => DebugPanelsChrome.Unregister(applyVisibility);
    }
}
