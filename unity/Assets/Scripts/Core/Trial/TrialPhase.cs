namespace Dovus.Core.Trial
{
    /// <summary>Deneme döngüsü evreleri (O-17.1 – O-17.3).</summary>
    public enum TrialPhase
    {
        /// <summary>Açılış: iki silah seçilir.</summary>
        WeaponSelect,

        /// <summary>Arenada, boss uyuyor.</summary>
        BossAsleep,

        /// <summary>Boss uyanıyor (2 sn); oyuncu bu sürede hasar almaz (SI-10.4).</summary>
        BossWaking,

        /// <summary>Dövüş.</summary>
        Fight,

        /// <summary>Oyuncu öldü, ölüm animasyonu (2 sn).</summary>
        PlayerDying,

        /// <summary>"Tekrar" / "Silah değiştir" düğmeleri bekleniyor.</summary>
        DeathMenu,

        /// <summary>"Tekrar" basıldı; arenaya dönüş (3 sn).</summary>
        Retrying,

        /// <summary>Boss öldü, sonuç ekranına bekleniyor (2 sn).</summary>
        BossDying,

        /// <summary>Sonuç kartı gösteriliyor.</summary>
        Result,
    }
}
