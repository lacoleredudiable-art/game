namespace Dovus.Core.Boss
{
    /// <summary>Ön işaret ve vuruş alanının geometrisi (görünümü Unity tarafı çizer).</summary>
    public enum KulEjderiShape
    {
        Circle,
        /// <summary>Yay: yarıçap + toplam açı.</summary>
        Arc,
        Cone,
        /// <summary>Şerit: genişlik x uzunluk.</summary>
        Strip,
        /// <summary>Genişleyen halka.</summary>
        Ring,
        /// <summary>Arenanın bir parçası ya da tamamı.</summary>
        Arena,
        /// <summary>Çoklu daire (yanan kayalar).</summary>
        Barrage
    }
}
