namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Komut planındaki tek adım; yürütme katmanı (Unity) PR2+.</summary>
    public abstract class PhysicsCommand
    {
        public abstract PhysicsCommandKind Kind { get; }
    }

    public sealed class OnSureCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.OnSure;
        public float PrefireSec { get; init; }
        public float ChargeSec { get; init; }
        public float TotalCapSec { get; init; }
        public float RecoverySec { get; init; }
        public float DamageScale { get; init; }
        public bool PrefireMoves { get; init; }
    }

    public sealed class BedelOdeCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.BedelOde;
        public float HpRatio { get; init; }
    }

    public sealed class MenzileYuruCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.MenzileYuru;
        public float RangeM { get; init; }
    }

    public sealed class YakinVurusCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YakinVurus;
        public float RangeM { get; init; }
        public int HitParts { get; init; }
    }

    public sealed class MermiFirlatCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.MermiFirlat;
        public float RangeM { get; init; }
        public float SpeedMps { get; init; }
    }

    public sealed class AlanAcCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.AlanAc;
        public string Shape { get; init; } = "daire";
        public float RadiusM { get; init; }
        public int MaxTargets { get; init; }
        public float PowerMult { get; init; }
    }

    public sealed class SekCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Sek;
        public int BounceCount { get; init; }
        public float BounceMult { get; init; }
        public float SearchRadiusM { get; init; }
    }

    public sealed class YapiKurCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YapiKur;
        public float LifeSec { get; init; }
    }

    public sealed class KilitlenCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Kilitlen;
        public float LockCapSec { get; init; }
        public float BlockInputSec { get; init; }
    }

    public sealed class HasarVerCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.HasarVer;
        public float Amount { get; init; }
        public float Poise { get; init; }
        public float PowerMult { get; init; }
    }

    public sealed class SifaVerCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.SifaVer;
        public float Amount { get; init; }
        public float PowerMult { get; init; }
    }

    public sealed class DurusAcCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.DurusAc;
        public float DurationSec { get; init; }
        public float BlockRatio { get; init; }
    }

    public sealed class KontrolUygulaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KontrolUygula;
        public float DurationSec { get; init; }
    }

    public sealed class TasmaBaglaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.TasmaBagla;
        public float DurationSec { get; init; }
        public float MaxLengthM { get; init; }
    }

    public sealed class EtkiSokCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.EtkiSok;
        public int Count { get; init; }
    }

    public sealed class KendiniTasiCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KendiniTasi;
        public float MaxDistanceM { get; init; }
        public float SpeedMps { get; init; }
        public bool SkillWhileMoving { get; init; }
    }

    public sealed class DengeVerCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.DengeVer;
        public float Amount { get; init; }
    }

    public sealed class YorungeKurCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YorungeKur;
        public float RadiusM { get; init; }
        public int PartCount { get; init; }
        public float LifeSec { get; init; }
    }

    public sealed class IsaretKoyCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.IsaretKoy;
        public float PlaceRangeM { get; init; }
        public float LifeSec { get; init; }
    }

    public sealed class ZincirKullanCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.ZincirKullan;
        public float LinkWidthM { get; init; }
        public int MaxTargets { get; init; }
    }

    public sealed class KapanKurCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KapanKur;
        public float WaitSec { get; init; }
    }

    public sealed class YukBindirCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YukBindir;
        public float WaitSec { get; init; }
    }

    public sealed class YardimciCagirCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YardimciCagir;
        public int Count { get; init; }
        public float LifeSec { get; init; }
    }

    public sealed class ItCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.It;
        public float DistanceM { get; init; }
        public bool ClipToWeaponRange { get; init; }
    }

    public sealed class DurumUygulaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.DurumUygula;
        public float Magnitude { get; init; }
        public float DurationSec { get; init; }
    }

    public sealed class YansitCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Yansit;
        public float Ratio { get; init; }
        public float DurationSec { get; init; }
    }

    public sealed class IsinlaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Isinla;
        public float MaxDistanceM { get; init; }
    }

    public sealed class KayitAlCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KayitAl;
        public float LookbackSec { get; init; }
    }

    public sealed class KaydaDonCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KaydaDon;
        public float PowerMult { get; init; }
        public bool Revive { get; init; }
    }
}
