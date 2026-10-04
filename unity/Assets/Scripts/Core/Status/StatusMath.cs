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
    }
}
