namespace Dovus.Core.Hit
{
    public enum HitRejectReason
    {
        None = 0,
        ZeroAmount,
        TargetDead,
        SameTeam,
        UnknownZone,
        Invulnerable,
        AlreadyHit,
    }
}
