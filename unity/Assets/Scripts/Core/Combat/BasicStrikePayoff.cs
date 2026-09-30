namespace Dovus.Core.Combat
{
    /// <summary>
    /// Düz vuruş pending'i. Etki bang'den önce sönerse vade gelene kadar tutulur,
    /// vade gelince görünüm olmasa da ödenir. İptal, pending listesinden siler;
    /// silinen vuruş bu kapıya düşmez.
    /// </summary>
    public static class BasicStrikePayoff
    {
        public static bool KeepUntilBang(bool isBasic, double worldMs, double bangAtMs) =>
            isBasic && worldMs < bangAtMs;

        public static bool PayWithoutView(bool isBasic, double worldMs, double bangAtMs) =>
            isBasic && worldMs >= bangAtMs;
    }
}
