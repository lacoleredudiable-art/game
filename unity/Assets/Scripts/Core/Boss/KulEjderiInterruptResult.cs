namespace Dovus.Core.Boss
{
    public enum KulEjderiInterruptResult
    {
        Interrupted,
        /// <summary>Boss ön işarette değil.</summary>
        NotTelegraphing,
        /// <summary>Saldırı kesilemez (altın kenar, KE-5.7, KE-6.6, ...).</summary>
        NotInterruptible,
        /// <summary>Saldırıya yalnız menzilli Kontrol ulaşır (KE-6.5).</summary>
        OutOfReach,
        /// <summary>45 s Dirençli; girişim denge hasarına döner (çağıran poise düşürür).</summary>
        Resistant
    }
}
