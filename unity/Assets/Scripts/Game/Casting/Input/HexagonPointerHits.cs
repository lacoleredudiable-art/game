using Dovus.Core.Input;
using Dovus.Core.Grammar;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using UnityEngine;

namespace Dovus.Game.Casting.Input
{
    public static class HexagonPointerHits
    {
        public static bool IsDrawHalf(Vector2 pos, PrototypeTuning tuning) =>
            HexagonLayoutScreen.IsRightHalf(pos, tuning.Input.MirrorForLeftHand, Screen.width);

        public static bool HitDodgeButton(Vector2 pos, PrototypeTuning tuning)
        {
            Vector2 c = HexagonLayoutScreen.DodgeButtonPx(tuning, Screen.width, Screen.height);
            return Vector2.Distance(pos, c) <= HexagonLayoutScreen.DodgeButtonRadiusPx(tuning);
        }

        public static bool HitSwapButton(Vector2 pos, PrototypeTuning tuning)
        {
            Vector2 c = HexagonLayoutScreen.WeaponSwapButtonPx(tuning, Screen.width, Screen.height);
            return Vector2.Distance(pos, c) <= HexagonLayoutScreen.WeaponSwapButtonRadiusPx(tuning);
        }

        public static bool HitCenter(Vector2 pos, PrototypeTuning tuning)
        {
            Vector2 c = HexagonLayoutScreen.CenterPx(tuning, Screen.width, Screen.height);
            float centerR = HexagonLayoutScreen.CenterHitRadiusPx(tuning);
            return Vector2.Distance(pos, c) <= centerR;
        }

        public static int? HitDot(Vector2 pos, PrototypeTuning tuning)
        {
            if (HitDodgeButton(pos, tuning) || HitSwapButton(pos, tuning) || HitCenter(pos, tuning))
                return null;

            float hitR = HexagonLayoutScreen.DotHitRadiusPx(tuning);
            int? best = null;
            float bestDist = float.MaxValue;
            for (int dot = 1; dot <= HexagonLayout.DotCount; dot++)
            {
                Vector2 p = HexagonLayoutScreen.DotPx(dot, tuning, Screen.width, Screen.height);
                float d = Vector2.Distance(pos, p);
                if (d <= hitR && d < bestDist)
                {
                    bestDist = d;
                    best = dot;
                }
            }

            return best;
        }

        public static float PixelsToDp(float px)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return px * (160f / dpi);
        }

        public static double NowRealMs() => Time.realtimeSinceStartupAsDouble * 1000.0;
    }
}
