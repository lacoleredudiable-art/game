namespace Dovus.Core.Motion
{
    /// <summary>
    /// Play taramasının kare sıçrama sınırı ve silah menüsü.
    /// Editör aracı bunu okur; sayı burada sabitlenir.
    /// </summary>
    public static class SweepMotion
    {
        public const float JumpMaxSpeedMps = 25f;
        public const float JumpMarginM = 0.2f;
        public const float JumpMinDtSec = 1f / 60f;

        public static readonly string[] WeaponMenuNames =
        {
            "Yumruk", "Kılıç", "Çekiç", "Kalkan", "Yay",
            "Top", "Asa", "Tılsım", "Büyü Kitabı", "Küre"
        };

        /// <summary>25 m/s × dt (en az 1/60) + 0,2 m. 60 FPS'te ~0,62 m.</summary>
        public static float JumpLimit(float dt)
        {
            float frame = dt > JumpMinDtSec ? dt : JumpMinDtSec;
            return JumpMaxSpeedMps * frame + JumpMarginM;
        }

        public static bool IsAllWeapons(string preset) => preset == "hepsi";

        public static int ComboCount(string preset)
        {
            if (IsAllWeapons(preset))
                return WeaponMenuNames.Length * MotionDefaults.Lit144;
            if (preset == "kilic+asa")
                return MotionDefaults.Lit288;
            return MotionDefaults.Lit144;
        }
    }
}
