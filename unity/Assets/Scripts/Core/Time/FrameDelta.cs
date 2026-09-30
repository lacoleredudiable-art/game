namespace Dovus.Core.Time
{
    /// <summary>
    /// Tek karelik editör takılması dünya saatine yazılmasın.
    /// Yumruk 2-2 taraması: dört kanal vuruşu aynı anda, zaman aşımı notu 11,3 sn
    /// (eşik 6,4). Saat o kareyi yutunca kalıp 2,16 sn yerine 10,9 sn sürer.
    /// </summary>
    public static class FrameDelta
    {
        public const double MaxFrameMs = 100.0;

        public static double ClampMs(double realDtMs)
        {
            if (realDtMs < 0)
                return 0;
            return realDtMs > MaxFrameMs ? MaxFrameMs : realDtMs;
        }
    }
}
