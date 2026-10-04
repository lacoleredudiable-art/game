using System;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Kalıp fazının dikey eğrisi. Sayılar fazın height_m alanındadır; burada yalnız şekil vardır.
    /// hop/leap parabol (baş ve son zeminde), slam iniş, hover/channel sabit yükseklik.
    /// </summary>
    public static class VerticalCurve
    {
        public static bool LeavesGround(string motion, float heightM)
        {
            if (heightM <= MotionDefaults.Epsilon01f)
                return false;
            return motion is "hop" or "leap" or "slam" or "hover" or "channel";
        }

        public static float Height(string motion, float heightM, float uLinear)
        {
            if (!LeavesGround(motion, heightM))
                return 0f;
            float u = Math.Clamp(uLinear, 0f, 1f);
            switch (motion)
            {
                case "hop":
                case "leap":
                    return MotionDefaults.Lit4f * heightM * u * (1f - u);
                case "slam":
                    return heightM * (1f - u);
                default:
                    return heightM;
            }
        }
    }
}
