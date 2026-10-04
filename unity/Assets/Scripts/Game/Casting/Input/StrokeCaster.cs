using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Game.Feel;
using UnityEngine;
using Dovus.Core.Shared;

namespace Dovus.Game.Casting.Input
{
    public sealed class StrokeCaster
    {
        readonly HexagonInputSession _s;
        readonly CastGate _gate;
        readonly CastFeedback _feedback;

        public StrokeCaster(HexagonInputSession session, CastGate gate, CastFeedback feedback)
        {
            _s = session;
            _gate = gate;
            _feedback = feedback;
        }

        public void OnSentenceCompleted(CompletedSentence sentence)
        {
            _s.InkBreakPending = true;
            bool drawn = _s.DrawnSentence;
            _s.DrawnSentence = false;
            if (!drawn || sentence == null || sentence.Phase == SentencePhase.Aborted
                || sentence.Words == null || sentence.Words.Count == 0)
                return;
            _s.InkFlashPending = true;
            _s.RaiseDrawCaption?.Invoke(DrawFeedback.RuneChain(sentence.Words), true);
        }

        public void FlushInkBreak()
        {
            if (!_s.InkBreakPending)
                return;

            _s.Ink?.Break(_s.InkFlashPending);
            _s.InkFlashPending = false;
            _s.LastInkPx = null;
            _s.InkBreakPending = false;
        }

        public void BeginStroke(Vector2 pos)
        {
            _s.StrokeAccepted = 0;
            _s.StrokeDenial = DrawFeedback.DenialKind.None;
            _s.StrokeLengthPx = 0f;
            _s.StrokePrevPx = pos;
            _s.StrokeAcceptedPx.Clear();
            int n = _s.DotXs.Length;
            for (int dot = 1; dot <= n; dot++)
            {
                Vector2 p = HexagonLayoutScreen.DotPx(dot, _s.Tuning, Screen.width, Screen.height);
                _s.DotXs[dot - 1] = p.x;
                _s.DotYs[dot - 1] = p.y;
            }
            _s.Ink?.RawBegin(pos);
            _s.Stroke.Begin(pos.x, pos.y,
                HexagonLayoutScreen.DotHitRadiusPx(_s.Tuning),
                HexagonLayoutScreen.DpToPixels(CastingInputDefaults.StrokeCornerRadiusDp),
                HexagonLayoutScreen.DpToPixels(CastingInputDefaults.StrokeMinSegmentDp),
                _s.DotXs, _s.DotYs, _s.StrokeHits);
            _s.StrokeFedFrame = Time.frameCount;
            ApplyStrokeHits(pos);
        }

        public void FeedStroke(Vector2 pos)
        {
            if (_s.Mode != FingerMode.Drawing)
                return;
            _s.StrokeLengthPx += Vector2.Distance(_s.StrokePrevPx, pos);
            _s.StrokePrevPx = pos;
            _s.Ink?.RawAppend(pos);
            _s.Stroke.Move(pos.x, pos.y, _s.DotXs, _s.DotYs, _s.StrokeHits);
            _s.StrokeFedFrame = Time.frameCount;
            ApplyStrokeHits(pos);
        }

        public void TickStrokeSettle()
        {
            if (_s.Mode != FingerMode.Drawing || _s.StrokeFedFrame == Time.frameCount)
                return;
            _s.Stroke.Move(_s.LastPos.x, _s.LastPos.y, _s.DotXs, _s.DotYs, _s.StrokeHits);
            ApplyStrokeHits(_s.LastPos);
        }

        public void FinishDrawingStroke(bool cancelled)
        {
            if (!cancelled)
            {
                _s.Stroke.End(_s.LastPos.x, _s.LastPos.y, _s.DotXs, _s.DotYs, _s.StrokeHits);
                for (int i = 0; i < _s.StrokeHits.Count; i++)
                    TryRegisterDot(_s.StrokeHits[i]);
            }
            float strokeLengthDp = HexagonPointerHits.PixelsToDp(_s.StrokeLengthPx);
            DrawFeedback.StrokeOutcome outcome = DrawFeedback.OnStrokeEnd(
                true, _s.StrokeAccepted, _s.StrokeDenial, cancelled, strokeLengthDp);
            if (outcome == DrawFeedback.StrokeOutcome.None
                && _s.StrokeAccepted > 0
                && _s.StrokeAcceptedPx.Count >= 1)
                _s.Ink?.RawEnd(false, _s.StrokeAcceptedPx);
            else if (outcome == DrawFeedback.StrokeOutcome.TooShort
                || outcome == DrawFeedback.StrokeOutcome.Unrecognized
                || outcome == DrawFeedback.StrokeOutcome.Cooldown)
                _s.Ink?.RawEnd(true);
            else
                _s.Ink?.RawEnd(false);

            string caption = DrawFeedback.CaptionFor(outcome);
            if (!string.IsNullOrEmpty(caption))
                _s.RaiseDrawCaption?.Invoke(caption, false);

            if (!cancelled
                && _s.StrokeAccepted >= 1
                && outcome == DrawFeedback.StrokeOutcome.None)
            {
                long ms = _s.Tuning != null ? _s.Tuning.Input.DotVibrationMs : CastingInputDefaults.FallbackDotVibrationMs;
                _s.Haptics?.Pulse((int)ms);
            }
        }

        public void TickDwell()
        {
            if (_s.Mode != FingerMode.Drawing || !_s.ActiveDot.HasValue || _s.Engine == null)
                return;

            if (_s.Engine.State.Phase != SentencePhase.Building)
                return;

            if (!_s.Tuning.IsDotOpen(_s.ActiveDot.Value))
                return;

            _s.DwellWorldMs += _s.Clock != null ? _s.Clock.WorldDeltaMs : Time.deltaTime * Units.SecToMs;
            int maxStacks = _s.Combat.Sentence.DwellMaxStacks;
            while (_s.DwellReported < maxStacks &&
                   _s.DwellWorldMs >= _s.Combat.Sentence.DwellMs * (_s.DwellReported + 1))
            {
                double worldMs = _s.Clock != null ? _s.Clock.Director.WorldTimeMs : 0;
                int stacksBefore = _s.Engine.State.Words.Count > 0
                    ? _s.Engine.State.Words[_s.Engine.State.Words.Count - 1].IntensityStacks
                    : 0;
                _s.Engine.OnDwell(worldMs);
                _s.DwellReported++;
                int stacksAfter = _s.Engine.State.Words.Count > 0
                    ? _s.Engine.State.Words[_s.Engine.State.Words.Count - 1].IntensityStacks
                    : 0;
                if (stacksAfter > stacksBefore)
                    _s.Syllable?.PlayForDot(_s.ActiveDot.Value, _s.Engine.State.Words.Count);
            }
        }

        public void TriggerCenter(System.Action syncPlayerState)
        {
            if (_s.Engine == null)
                return;

            double worldMs = _s.Clock != null ? _s.Clock.Director.WorldTimeMs : 0;
            if (_s.Engine.State.Phase == SentencePhase.Building)
            {
                _s.Engine.Commit();
                FlushInkBreak();
                _s.DebugHud?.NoteCommit();
                return;
            }

            syncPlayerState();
            bool engineOk = _s.Engine.State.Phase == SentencePhase.Idle
                || _s.Engine.State.Phase == SentencePhase.Recovering
                || _s.Engine.State.Phase == SentencePhase.Resolved
                || _s.Engine.State.Phase == SentencePhase.Aborted;
            if (!BasicStrikeInput.AllowsCenterStrike(_s.AllowsDrawNow, _s.CenterStrikeArmed, engineOk))
                return;
            _s.CenterStrikeArmed = false;

            int runeId = _s.Tuning.Input.BasicStrikeDot;
            if (!_s.Engine.BeginBasicStrike(runeId, worldMs))
                return;
            _s.Engine.Commit();
            _s.RaiseDotAccepted?.Invoke(0);
            FlushInkBreak();
            _s.Syllable?.PlayForDot(runeId, 1);
            _s.DebugHud?.NoteBasicStrike();
        }

        void ApplyStrokeHits(Vector2 pos)
        {
            for (int i = 0; i < _s.StrokeHits.Count && _s.Mode == FingerMode.Drawing; i++)
                TryRegisterDot(_s.StrokeHits[i]);

            int? under = HexagonPointerHits.HitDot(pos, _s.Tuning);
            if (!under.HasValue || under != _s.ActiveDot)
            {
                if (!under.HasValue)
                    _s.ActiveDot = null;
                _s.DwellWorldMs = 0;
                _s.DwellReported = 0;
            }
        }

        void TryRegisterDot(int dot)
        {
            int? hit = dot;
            if (_s.ActiveDot == hit.Value)
                return;

            if (_s.Engine == null)
                return;

            if (!_s.AllowsDrawNow)
            {
                _s.Readout?.NoteDenied("çizilemez");
                _s.Syllable?.PlayDenied();
                _s.StrokeDenial = DrawFeedback.DenialKind.Other;
                return;
            }

            if (!_s.Tuning.IsDotOpen(hit.Value))
            {
                _s.ActiveDot = hit.Value;
                _s.DwellWorldMs = 0;
                _s.DwellReported = 0;
                if (_s.StrokeDenial == DrawFeedback.DenialKind.None)
                {
                    _s.Readout?.NoteDenied(DrawFeedback.ClosedRune);
                    _s.Syllable?.PlayDenied();
                    _s.StrokeDenial = DrawFeedback.DenialKind.Other;
                }
                return;
            }

            if (!_gate.TryAllowSentenceStart(hit.Value))
            {
                _s.ActiveDot = hit.Value;
                _s.DwellWorldMs = 0;
                _s.DwellReported = 0;
                if (_gate.WouldStartSentence() && !_gate.CanGlobalCooldownGate())
                    _s.StrokeDenial = DrawFeedback.DenialKind.Cooldown;
                else
                    _s.StrokeDenial = DrawFeedback.DenialKind.Other;
                return;
            }

            if (!_gate.TryAllowComboCooldownForNextDot(hit.Value))
            {
                _s.ActiveDot = hit.Value;
                _s.DwellWorldMs = 0;
                _s.DwellReported = 0;
                _feedback.NotifyOnCooldown();
                _s.StrokeDenial = DrawFeedback.DenialKind.Cooldown;
                return;
            }

            if (!_gate.TryAllowProspectiveTarget(hit.Value))
            {
                _s.ActiveDot = hit.Value;
                _s.DwellWorldMs = 0;
                _s.DwellReported = 0;
                _s.Syllable?.PlayDenied();
                _s.StrokeDenial = DrawFeedback.DenialKind.Other;
                return;
            }

            Vector2 dotPx = HexagonLayoutScreen.DotPx(hit.Value, _s.Tuning, Screen.width, Screen.height);
            double worldMs = _s.Clock != null ? _s.Clock.Director.WorldTimeMs : 0;

            Vector2? inkFrom = _s.LastInkPx;
            _s.DrawnSentence = true;
            _s.StrokeAccepted++;
            _s.StrokeAcceptedPx.Add(dotPx);
            _s.Engine.OnDotTouched(hit.Value, worldMs);
            _s.RaiseDotAccepted?.Invoke(hit.Value);

            if (inkFrom.HasValue)
                _s.Ink?.AddSegment(inkFrom.Value, dotPx);
            _s.Syllable?.PlayForDot(hit.Value, _s.Engine.State.Words.Count);

            _s.ActiveDot = hit.Value;
            _s.DwellWorldMs = 0;
            _s.DwellReported = 0;

            if (_s.InkBreakPending)
                FlushInkBreak();
            else
                _s.LastInkPx = _s.Engine.State.Phase == SentencePhase.Building ? dotPx : null;
        }
    }
}
