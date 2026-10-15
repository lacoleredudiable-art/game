namespace Dovus.Core.Hit
{
    /// <summary>
    /// Vurulabilir bölge. Tek gövdeli aktörde yalnız <see cref="HitDefaults.BodyZoneId"/> vardır (O-1.14).
    /// Çok parçalı boss (kafa, kanat) aynı sözleşmeye ek bölge ekler; çarpan veriden gelir.
    /// </summary>
    public readonly struct HurtZone
    {
        public HurtZone(string id, float damageFactor)
        {
            Id = id ?? HitDefaults.BodyZoneId;
            DamageFactor = damageFactor;
        }

        public string Id { get; }

        /// <summary>Gelen hasar bu çarpanla çarpılır. 1 = fark yok.</summary>
        public float DamageFactor { get; }
    }
}
