using Dovus.Core.Combat;
using Dovus.Core.Status;

namespace Dovus.Core.Equipment
{
    /// <summary>Tılsım değiştirme bonusu: hedefteki bir kötü durumu siler, diğerleri kalır.</summary>
    public static class OneNegativeCleanse
    {
        public static bool TryRemove(StatusBoard board)
        {
            if (board == null)
                return false;
            StatusKind pick = StatusKind.None;
            foreach (StatusKind kind in board.ActiveKinds)
            {
                if (!IsNegative(kind))
                    continue;
                pick = kind;
                break;
            }

            if (pick == StatusKind.None)
                return false;
            board.RemoveKinds(new[] { pick });
            return true;
        }

        public static bool IsNegative(StatusKind kind) =>
            StatusKindUtil.IsHardCc(kind) || StatusKindUtil.IsSoftCc(kind) || StatusKindUtil.IsDebuff(kind);
    }

    /// <summary>Büyü Kitabı değiştirme bonusu: o vuruş mana yemez.</summary>
    public static class WeaponManaWaiver
    {
        public static void Charge(ResourceTracker tracker, float cost, bool freeCast)
        {
            if (freeCast || tracker == null || cost <= 0f)
                return;
            tracker.Consume(cost);
        }
    }

    /// <summary>
    /// Küre yerleştirme girdisi. Uzun basıp bırakmak yerleştirir, kısa çift dokunuş eline çağırır.
    /// Süreler silahın orb satırından gelir.
    /// </summary>
    public sealed class OrbGesture
    {
        readonly float _holdMs;
        readonly float _doubleTapMs;
        bool _down;
        double _downMs;
        double _lastTapMs = double.NegativeInfinity;

        public OrbGesture(float holdSec, float doubleTapSec)
        {
            _holdMs = (holdSec > 0f ? holdSec : 0.4f) * 1000f;
            _doubleTapMs = (doubleTapSec > 0f ? doubleTapSec : 0.3f) * 1000f;
        }

        public void Press(double nowMs)
        {
            _down = true;
            _downMs = nowMs;
        }

        public void Cancel() => _down = false;

        public OrbGestureResult Release(double nowMs)
        {
            if (!_down)
                return OrbGestureResult.None;
            _down = false;
            if (nowMs - _downMs >= _holdMs)
            {
                _lastTapMs = double.NegativeInfinity;
                return OrbGestureResult.Place;
            }

            if (nowMs - _lastTapMs <= _doubleTapMs)
            {
                _lastTapMs = double.NegativeInfinity;
                return OrbGestureResult.Recall;
            }

            _lastTapMs = nowMs;
            return OrbGestureResult.None;
        }
    }

    public enum OrbGestureResult
    {
        None = 0,
        Place = 1,
        Recall = 2
    }
}
