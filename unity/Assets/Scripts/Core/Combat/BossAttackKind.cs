namespace Dovus.Core.Combat
{
    /// <summary>
    /// 16 Eylül: "boss tek atıyor, sadece yaptığı o" bug raporu — Slam'in 3 varyantı hep AYNI
    /// saldırıydı (AoE çember, farklı zamanlama/yarıçap). Bu enum GERÇEKTEN farklı bir ikinci
    /// saldırı ekler (docs/bosses/karadul.json "fire_cone", speclenmiş ama implemented:false).
    /// </summary>
    public enum BossAttackKind
    {
        Slam,
        FireCone,
        /// <summary>Zehir Tükürüğü (karadul.json "volley"): hedefe yelpaze mermi; hacim testi yok.</summary>
        Volley,
        /// <summary>Ağ Örme (aglarin-kralicesi "web_field"): hedef konumunda kalıcı disk; anlık hasar yok.</summary>
        WebField,
        /// <summary>Sıçrayış (aglarin-kralicesi "pounce"): iniş noktasında dairesel vuruş.</summary>
        Pounce
    }
}
