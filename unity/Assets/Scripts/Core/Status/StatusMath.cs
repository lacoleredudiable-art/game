namespace Dovus.Core.Status
{
    /// <summary>Status tahtasından bağımsız saf dönüşümler (Combat döngüsünü kırmak için burada).</summary>
    public static class StatusMath
    {
        /// <summary>accuracy_debuff büyüklüğünü kör ıskalama şansına kırpar.</summary>
        public static float BlindChanceFromAccuracy(float accuracy)
        {
            if (accuracy <= 0f)
                return 0f;
            if (accuracy >= 1f)
                return 1f;
            return accuracy;
        }

        /// <summary>Y9: kare içinde etkinin gerçekten sürdüğü saniye; son kare kalan süreyi aşmaz (3 s × 20/s tam 60).</summary>
        public static float ActiveTickSec(double worldDtMs, double remainingMs) =>
            (float)(System.Math.Max(0, System.Math.Min(worldDtMs, remainingMs)) / Dovus.Core.Shared.Units.SecToMs);
    }
}
