using System;
using Dovus.Core.Shared;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Hasar hattının tek giriş kapısı. Sıra: miktar → ölü mü → aynı takım → bölge → i-frame → tekrar vuruş →
    /// uygula → olaylar. Her ret nedeni adlıdır; sessizce yutulan vuruş yoktur.
    /// HUD, hasar sayıları, ses, hitstop ve ölüm akışı yalnız olaylara abone olur.
    /// </summary>
    public sealed class DamageHub
    {
        public HitRegistry Registry { get; } = new HitRegistry();

        /// <summary>Hasar gerçekten işledi.</summary>
        public event Action<HitEvent, HitResult>? HitLanded;

        /// <summary>Vuruş reddedildi (neden <see cref="HitResult.Reason"/>).</summary>
        public event Action<HitEvent, HitResult>? HitRejected;

        /// <summary>Ölüm anı, ölüm başına bir kez. (kurban, öldüren, zaman ms).</summary>
        public event Action<ActorId, ActorId, double>? ActorDied;

        public HitResult Submit(HitEvent hit, IDamageable target, double nowMs)
        {
            HitResult result = Evaluate(hit, target, nowMs);
            if (!result.Applied)
            {
                HitRejected?.Invoke(hit, result);
                return result;
            }

            HitLanded?.Invoke(hit, result);
            if (result.Killed)
                ActorDied?.Invoke(target.Id, hit.AttackerId, nowMs);
            return result;
        }

        HitResult Evaluate(HitEvent hit, IDamageable target, double nowMs)
        {
            if (hit.Amount <= 0f)
                return HitResult.Rejected(HitRejectReason.ZeroAmount, target.Id, hit.ZoneId, target.Hp);
            if (target.IsDead)
                return HitResult.Rejected(HitRejectReason.TargetDead, target.Id, hit.ZoneId, target.Hp);
            if (!hit.AllowFriendlyFire && hit.AttackerTeam == target.Team)
                return HitResult.Rejected(HitRejectReason.SameTeam, target.Id, hit.ZoneId, target.Hp);
            if (!target.TryGetZone(hit.ZoneId, out HurtZone zone))
                return HitResult.Rejected(HitRejectReason.UnknownZone, target.Id, hit.ZoneId, target.Hp);
            if (target.IsInvulnerable(nowMs, hit.Dodgeable))
                return HitResult.Rejected(HitRejectReason.Invulnerable, target.Id, zone.Id, target.Hp);
            if (!Registry.TryRegister(hit.HitId, target.Id.Value, nowMs, hit.RehitIntervalMs))
                return HitResult.Rejected(HitRejectReason.AlreadyHit, target.Id, zone.Id, target.Hp);

            float requested = hit.Amount * zone.DamageFactor;
            float before = target.Hp;
            float dealt = target.ReceiveDamage(requested, nowMs);
            return new HitResult(true, HitRejectReason.None, target.Id, zone.Id, requested, dealt, before, target.Hp, target.IsDead);
        }
    }
}
