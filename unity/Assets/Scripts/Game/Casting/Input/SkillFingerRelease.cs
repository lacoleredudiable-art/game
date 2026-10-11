using UnityEngine;

namespace Dovus.Game.Casting.Input
{
    /// <summary>Çizim parmağı kalktı: nerede ve iptal mi (basılı tutarak hedef seçme bunu okur).</summary>
    public readonly struct SkillFingerRelease
    {
        public readonly Vector2 ScreenPos;
        public readonly bool Cancelled;
        public readonly bool OnCenter;
        public readonly bool OnHexagon;

        public SkillFingerRelease(Vector2 screenPos, bool cancelled, bool onCenter, bool onHexagon)
        {
            ScreenPos = screenPos;
            Cancelled = cancelled;
            OnCenter = onCenter;
            OnHexagon = onHexagon;
        }
    }
}
