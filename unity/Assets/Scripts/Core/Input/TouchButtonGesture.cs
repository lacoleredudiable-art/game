namespace Dovus.Core.Input
{
    /// <summary>
    /// Denetim B (O1/K3): dokunmatik düğme kuralları. Saf; HexagonInputController uygular.
    /// - Kaçış basınca tetiklenir (süre sınırı yok, bırakmayı beklemez).
    /// - Silah düğmesi: uzun basma komutu yoksa basınca değiştirir. Küre gibi uzun basma komutu
    ///   olan silahta (JSON weapons[].orb.hold_sec) bırakınca değiştirir; eşiğe ulaşan basış
    ///   küreyi yollar/çağırır. İkisi de hiçbir basış süresinde sessizce düşmez.
    /// - Merkez düz vuruş bırakınca tetiklenir (merkezden sürükleme çizimdir); süre sınırı yok,
    ///   yalnız hareket eşiği.
    /// </summary>
    public static class TouchButtonGesture
    {
        public static bool SwapFiresOnPress(float holdCommandSec) => holdCommandSec <= 0f;

        public static bool HoldCommandDue(double heldSec, float holdCommandSec) =>
            holdCommandSec > 0f && heldSec >= holdCommandSec;

        /// <summary>Uzun basma eşiğine ulaşmadan bırakılan silah düğmesi değiştirir.</summary>
        public static bool SwapOnRelease(double heldSec, float holdCommandSec, bool cancelled) =>
            !cancelled && holdCommandSec > 0f && heldSec < holdCommandSec;

        /// <summary>Merkez bırakıldığında düz vuruş/erken kapanış; süre sınırı yok.</summary>
        public static bool CenterFiresOnRelease(float moveDp, int maxMoveDp, bool cancelled) =>
            !cancelled && moveDp <= maxMoveDp;
    }
}
