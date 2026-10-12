namespace Dovus.Core.Boss
{
    /// <summary>
    /// Saldırı türü başına boss hasarı ve Zehir Nefesi zehri. Verilirse <see cref="BossAttack"/> hasarı BossTuning yerine
    /// buradan okur (dilim: kural-motoru-v4.json <c>slice_boss_attacks</c>).
    /// </summary>
    public readonly struct BossAttackDamage
    {
        public BossAttackDamage(int slam, int volleyPerProjectile, int fireCone, int pounce, float poisonSec, float poisonPerSec)
        {
            Slam = slam;
            VolleyPerProjectile = volleyPerProjectile;
            FireCone = fireCone;
            Pounce = pounce;
            PoisonSec = poisonSec;
            PoisonPerSec = poisonPerSec;
        }

        public int Slam { get; }
        public int VolleyPerProjectile { get; }
        public int FireCone { get; }
        public int Pounce { get; }
        /// <summary>Zehir Nefesi isabetinde uygulanan zehrin süresi (s).</summary>
        public float PoisonSec { get; }
        /// <summary>Zehir Nefesi zehrinin saniyelik hasarı.</summary>
        public float PoisonPerSec { get; }

        public int For(BossAttackKind kind) => kind switch
        {
            BossAttackKind.FireCone => FireCone,
            BossAttackKind.Volley => VolleyPerProjectile,
            BossAttackKind.WebField => 0,
            BossAttackKind.Pounce => Pounce,
            _ => Slam
        };
    }
}
