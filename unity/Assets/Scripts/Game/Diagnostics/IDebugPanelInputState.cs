using UnityEngine;

namespace Dovus.Game.Diagnostics
{
    /// <summary>DevTools tuning/grammar panelleri — runtime klasörler DevTools'a bağlanmaz.</summary>
    public interface IDebugPanelInputState
    {
        bool TuningPanelOpen { get; }
        bool GrammarDebugOpen { get; }
        bool HitTuningToggleButton(Vector2 screenPos);
    }
}
