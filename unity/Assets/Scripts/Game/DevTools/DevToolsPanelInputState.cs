using Dovus.Game.Diagnostics;
using UnityEngine;

namespace Dovus.Game.DevTools
{
    public sealed class DevToolsPanelInputState : IDebugPanelInputState
    {
        public bool TuningPanelOpen => TuningPanelHud.IsOpen;
        public bool GrammarDebugOpen => GrammarDebugHud.IsOpen;
        public bool HitTuningToggleButton(Vector2 screenPos) => TuningPanelHud.HitToggleButton(screenPos);
    }
}
