namespace Dovus.Core.Targeting
{
    public enum LockReleaseReason
    {
        None = 0,
        /// <summary>Oyuncu bıraktı.</summary>
        Manual = 1,
        /// <summary>Hedef öldü, kayboldu ya da seçilemez oldu (K 22:15).</summary>
        TargetGone = 2,
        /// <summary>Hedef bırakma mesafesinin dışına çıktı.</summary>
        OutOfRange = 3,
    }
}
