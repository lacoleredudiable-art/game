using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Casting.Input
{
    /// <summary>HexagonInput paylaşılan durum ve servis referansları (MonoBehaviour değil).</summary>
    public sealed class HexagonInputSession
    {
        public PrototypeTuning Tuning = new();
        public CombatTuning Combat = new();
        public SentenceEngine Engine;
        public DodgeState Dodge;
        public DodgeChargeBank Charges;
        public GameClock Clock;
        public InkTrail Ink;
        public SyllableFeedback Syllable;
        public SentenceDebugHud DebugHud;

        public int? FingerId;
        public bool MouseHeld;
        public FingerMode Mode;
        public Vector2 PressOrigin;
        public Vector2 LastPos;
        public double PressRealMs;
        public int? ActiveDot;
        public double DwellWorldMs;
        public int DwellReported;
        public Vector2? LastInkPx;
        public bool InkBreakPending;
        public bool SentenceHooked;

        public readonly StrokeDotTracker Stroke = new StrokeDotTracker();
        public readonly System.Collections.Generic.List<int> StrokeHits = new System.Collections.Generic.List<int>(6);
        public readonly float[] DotXs = new float[HexagonLayout.DotCount];
        public readonly float[] DotYs = new float[HexagonLayout.DotCount];
        public int StrokeFedFrame = -1;
        public int StrokeAccepted;
        public DrawFeedback.DenialKind StrokeDenial;
        public float StrokeLengthPx;
        public Vector2 StrokePrevPx;
        public readonly System.Collections.Generic.List<Vector2> StrokeAcceptedPx =
            new System.Collections.Generic.List<Vector2>(6);
        public bool DrawnSentence;
        public bool InkFlashPending;
        public bool CenterStrikeArmed;

        public int? DodgeFingerId;
        public bool SwapHoldFired;

        public PlayerVitals Vitals;
        public ActorStatus Status;
        public PlayerResource Resource;
        public PlayerCooldown Cooldown;
        public ReactionReadout Readout;
        public SkillMotor Skills;
        public PlayerStateMachine PlayerStates;
        public System.Func<bool> IsCasting;
        public System.Func<SkillResolution, bool> SkillTargetGate;

        public System.Func<float> SwapHoldCommandSec;

        public System.Action<int> RaiseDotAccepted;
        public System.Action<string, bool> RaiseDrawCaption;
        public System.Action RaiseWeaponSwapRequested;
        public System.Action RaiseOrbCommandRequested;
        public System.Action RaiseSkillCancelledByDodge;

        public bool InputLocked =>
            (Vitals != null && Vitals.IsDown)
            || (Status != null && Status.Board.BlocksCast);

        public bool AllowsDrawNow => PlayerStates == null || PlayerStates.AllowsDraw;
    }

    public enum FingerMode
    {
        None,
        CenterPending,
        SwapPending,
        Drawing
    }
}
