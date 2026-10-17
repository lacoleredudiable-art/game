namespace Dovus.Core.Boss
{
    /// <summary>
    /// KE-2.8 - KE-2.10 vurulabilir bölgeler. Modeldeki collider'lar bu kimliklerle eşlenir. Kalp (KE-2.11) bölge
    /// değildir: yalnız görünür, vurulmaz ([K] 17 Ekim).
    /// </summary>
    public enum KulEjderiHitZone
    {
        Head,
        Claw,
        Chest
    }
}
