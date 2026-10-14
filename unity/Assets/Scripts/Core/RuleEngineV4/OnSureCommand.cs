namespace Dovus.Core.RuleEngineV4
{
    public sealed class OnSureCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.OnSure;
        /// <summary>K01: dokunuştan çıkışa bekleme; Yoğun şarjını da kapsar.</summary>
        public float PrefireSec { get; init; }
        /// <summary><see cref="PrefireSec"/>'in şarj payı; ayrıca beklenmez.</summary>
        public float ChargeSec { get; init; }
        public float TotalCapSec { get; init; }
        public float RecoverySec { get; init; }
        /// <summary>K15, O-13.5: silahın ritim çarpanı; HasarVer/SifaVer miktarına planlayıcıda bir kez girmiştir.</summary>
        public float DamageScale { get; init; }
        public bool PrefireMoves { get; init; }
    }
}
