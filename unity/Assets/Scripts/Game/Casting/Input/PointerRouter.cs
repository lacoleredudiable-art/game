using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Dovus.Core.Shared;

namespace Dovus.Game.Casting.Input
{
    public sealed class PointerRouter
    {
        readonly HexagonInputSession _s;
        readonly StrokeCaster _stroke;
        readonly DodgeTrigger _dodge;
        readonly System.Action _syncPlayerState;

        public PointerRouter(
            HexagonInputSession session,
            StrokeCaster stroke,
            DodgeTrigger dodge,
            System.Action syncPlayerState)
        {
            _s = session;
            _stroke = stroke;
            _dodge = dodge;
            _syncPlayerState = syncPlayerState;
        }

        public bool PanelBlocking =>
            (_s.DebugPanel?.TuningPanelOpen ?? false)
            || (_s.DebugPanel?.GrammarDebugOpen ?? false)
            || BuildSelectHud.IsOpen;

        public void HandleKeyboardDodge()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return;
            if (kb.qKey.wasPressedThisFrame)
                _s.RaiseWeaponSwapRequested?.Invoke();
            if (kb.rKey.wasPressedThisFrame)
                _s.RaiseOrbCommandRequested?.Invoke();
            if (kb.spaceKey.wasPressedThisFrame)
                _dodge.TriggerDodge();
        }

        public void HandleMouse()
        {
#if !UNITY_EDITOR
            if (Touch.activeTouches.Count > 0)
                return;
#endif

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!HexagonPointerHits.IsDrawHalf(pos, _s.Tuning)
                    || (_s.DebugPanel?.HitTuningToggleButton(pos) ?? false))
                    return;
                if (!HexagonPointerHits.HitDodgeButton(pos, _s.Tuning)
                    && !HexagonPointerHits.HitSwapButton(pos, _s.Tuning)
                    && !HexagonPointerHits.HitCenter(pos, _s.Tuning)
                    && HexagonPointerHits.HitDot(pos, _s.Tuning) == null)
                    return;
                _s.MouseHeld = true;
                BeginPointer(pos);
            }
            else if (_s.MouseHeld && mouse.leftButton.isPressed)
            {
                MovePointer(pos);
            }
            else if (_s.MouseHeld && mouse.leftButton.wasReleasedThisFrame)
            {
                _s.MouseHeld = false;
                EndPointer(cancelled: false);
            }
        }

        public void OnFingerDown(Finger finger)
        {
            if (_s.InputLocked || PanelBlocking)
                return;

            Vector2 pos = finger.screenPosition;
            if (!HexagonPointerHits.IsDrawHalf(pos, _s.Tuning)
                || (_s.DebugPanel?.HitTuningToggleButton(pos) ?? false))
                return;

            if (_s.FingerId.HasValue)
            {
                if (_s.DodgeFingerId.HasValue || !HexagonPointerHits.HitDodgeButton(pos, _s.Tuning))
                    return;

                _s.DodgeFingerId = finger.index;
                _dodge.TriggerDodge();
                return;
            }

            if (!HexagonPointerHits.HitDodgeButton(pos, _s.Tuning)
                && !HexagonPointerHits.HitSwapButton(pos, _s.Tuning)
                && !HexagonPointerHits.HitCenter(pos, _s.Tuning)
                && HexagonPointerHits.HitDot(pos, _s.Tuning) == null)
                return;

            _s.FingerId = finger.index;
            BeginPointer(pos);
        }

        public void OnFingerMove(Finger finger)
        {
            if (_s.DodgeFingerId.HasValue && finger.index == _s.DodgeFingerId.Value)
                return;

            if (!_s.FingerId.HasValue || finger.index != _s.FingerId.Value)
                return;

            MovePointer(finger.screenPosition);
        }

        public void OnFingerUp(Finger finger)
        {
            if (_s.DodgeFingerId.HasValue && finger.index == _s.DodgeFingerId.Value)
            {
                _s.DodgeFingerId = null;
                return;
            }

            if (!_s.FingerId.HasValue || finger.index != _s.FingerId.Value)
                return;

            bool cancelled = finger.currentTouch.phase == TouchPhase.Canceled;
            _s.FingerId = null;
            EndPointer(cancelled);
        }

        /// <summary>OnDisable: eski davranış — fare basılı bayrağı korunur, yalnız parmaklar ve çizim kesilir.</summary>
        public void CancelOnDisable()
        {
            _s.FingerId = null;
            _s.DodgeFingerId = null;
            EndPointer(cancelled: true);
        }

        public void CancelAllPointers()
        {
            _s.FingerId = null;
            _s.MouseHeld = false;
            _s.DodgeFingerId = null;
            EndPointer(cancelled: true);
        }

        public void TickSwapHold()
        {
            if (_s.Mode != FingerMode.SwapPending || _s.SwapHoldFired)
                return;
            double heldSec = (HexagonPointerHits.NowRealMs() - _s.PressRealMs) / Units.SecToMs;
            if (!TouchButtonGesture.HoldCommandDue(heldSec, SwapHoldSec()))
                return;
            _s.SwapHoldFired = true;
            _s.RaiseOrbCommandRequested?.Invoke();
        }

        public bool IsCombatControlAt(Vector2 pos) =>
            HexagonPointerHits.IsDrawHalf(pos, _s.Tuning)
            && (HexagonPointerHits.HitDodgeButton(pos, _s.Tuning)
                || HexagonPointerHits.HitSwapButton(pos, _s.Tuning)
                || HexagonPointerHits.HitCenter(pos, _s.Tuning)
                || HexagonPointerHits.HitDot(pos, _s.Tuning).HasValue);

        public bool IsStickHalf(Vector2 pos) => !HexagonPointerHits.IsDrawHalf(pos, _s.Tuning);

        void BeginPointer(Vector2 pos)
        {
            _s.PressOrigin = pos;
            _s.LastPos = pos;
            _s.PressRealMs = HexagonPointerHits.NowRealMs();
            _s.ActiveDot = null;
            _s.DwellWorldMs = 0;
            _s.DwellReported = 0;
            _s.LastInkPx = null;
            _s.SwapHoldFired = false;

            if (HexagonPointerHits.HitDodgeButton(pos, _s.Tuning))
            {
                _s.Mode = FingerMode.None;
                _dodge.TriggerDodge();
                return;
            }

            if (HexagonPointerHits.HitSwapButton(pos, _s.Tuning))
            {
                if (TouchButtonGesture.SwapFiresOnPress(SwapHoldSec()))
                {
                    _s.Mode = FingerMode.None;
                    _s.RaiseWeaponSwapRequested?.Invoke();
                    return;
                }
                _s.Mode = FingerMode.SwapPending;
                return;
            }

            if (HexagonPointerHits.HitCenter(pos, _s.Tuning))
            {
                _s.Mode = FingerMode.CenterPending;
                return;
            }

            _s.Mode = FingerMode.Drawing;
            _stroke.BeginStroke(pos);
        }

        void MovePointer(Vector2 pos)
        {
            if (_s.Mode == FingerMode.None)
                return;

            _s.LastPos = pos;
            if (_s.Mode == FingerMode.SwapPending)
                return;

            if (_s.Mode == FingerMode.CenterPending)
            {
                float moveDp = HexagonPointerHits.PixelsToDp(Vector2.Distance(pos, _s.PressOrigin));
                if (moveDp > _s.Combat.Dodge.TapMaxMoveDp)
                {
                    _s.Mode = FingerMode.Drawing;
                    _s.LastInkPx = _s.PressOrigin;
                    _stroke.BeginStroke(_s.PressOrigin);
                    _stroke.FeedStroke(pos);
                }

                return;
            }

            _stroke.FeedStroke(pos);
        }

        void EndPointer(bool cancelled)
        {
            double heldSec = (HexagonPointerHits.NowRealMs() - _s.PressRealMs) / Units.SecToMs;
            if (_s.Mode == FingerMode.Drawing)
                _stroke.FinishDrawingStroke(cancelled);

            if (_s.Mode == FingerMode.CenterPending)
            {
                float moveDp = HexagonPointerHits.PixelsToDp(Vector2.Distance(_s.LastPos, _s.PressOrigin));
                if (TouchButtonGesture.CenterFiresOnRelease(moveDp, _s.Combat.Dodge.TapMaxMoveDp, cancelled))
                    _stroke.TriggerCenter(_syncPlayerState);
            }
            else if (_s.Mode == FingerMode.SwapPending && !_s.SwapHoldFired
                && TouchButtonGesture.SwapOnRelease(heldSec, SwapHoldSec(), cancelled))
            {
                _s.RaiseWeaponSwapRequested?.Invoke();
            }

            _s.Mode = FingerMode.None;
            _s.ActiveDot = null;
            _s.DwellWorldMs = 0;
            _s.DwellReported = 0;
            _s.LastInkPx = null;
            _s.MouseHeld = false;
            _s.SwapHoldFired = false;
        }

        float SwapHoldSec() => _s.SwapHoldCommandSec != null ? _s.SwapHoldCommandSec() : 0f;
    }
}
