using System;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    public enum MechanicActorKind
    {
        None,
        Minion,
        Turret,
        Clone,
        MirrorClone,
        Guardian,
        Assassin
    }

    /// <summary>
    /// Gramer planındaki atomları Unity'den bağımsız dünya yeteneklerine indirger.
    /// Etiketler sunumdur; bu profil yalnız gövde/atom/mod verisini okur.
    /// </summary>
    public sealed class MechanicWorldProfile
    {
        MechanicWorldProfile(MechanicPlan plan)
        {
            BlocksMovement = plan.Body.Anchored && plan.Body.Permeability == "kati";
            Homing = plan.Body.Homing;
            Cloud = plan.Body.Cloud;
            Vortex = plan.Body.Pull;
            Link = plan.Body.Link;
            RiseDelay = plan.Body.Ramp;
            Continuous = plan.Body.Continuous;

            Rewind = Has(plan, "geri_sar");
            TempoField = plan.Effects.Any(e => e.Stat == "tempo" && e.Has("zaman_alani"));
            Decoy = Has(plan, "yem_kopya");
            ProjectileBarrier = plan.Effects.Any(e => e.Stat == "mermi_sil");
            CleanseField = plan.Effects.Any(e => e.Stat == "durum_sil"
                && (e.Has("arinma_alani") || e.Has("aura") || e.Has("bag_bagisiklik")));
            GuardTrigger = plan.Effects.Any(e => e.Has("koruyucu_tetik"));
            DelayedMark = plan.Effects.Any(e => e.Has("isaretli_an"));
            Reflector = plan.Effects.Any(e => e.Stat == "yansit" && e.Target is "dost" or "alan");
            DamageShare = plan.Effects.Any(e => e.Stat == "hasar_paylasimi");
            StatusTransfer = plan.Effects.Any(e => e.Stat == "durum_aktar");
            BuffPurge = plan.Effects.Any(e => e.Stat == "iyi_durum_sil");

            var actor = plan.Effects.FirstOrDefault(e => e.Stat is "aktor_yarat" or "klon");
            if (actor == null)
                ActorKind = MechanicActorKind.None;
            else if (actor.Stat == "klon")
                ActorKind = MechanicActorKind.Clone;
            else if (actor.Has("ayna_klon"))
                ActorKind = MechanicActorKind.MirrorClone;
            else if (actor.Has("taret"))
                ActorKind = MechanicActorKind.Turret;
            else if (actor.Has("bagli_muhafiz"))
                ActorKind = MechanicActorKind.Guardian;
            else if (actor.Has("suikastci"))
                ActorKind = MechanicActorKind.Assassin;
            else
                ActorKind = MechanicActorKind.Minion;
        }

        public bool BlocksMovement { get; }
        public bool Homing { get; }
        public bool Cloud { get; }
        public bool Vortex { get; }
        public bool Link { get; }
        public bool RiseDelay { get; }
        public bool Continuous { get; }
        public bool Rewind { get; }
        public bool TempoField { get; }
        public bool Decoy { get; }
        public bool ProjectileBarrier { get; }
        public bool CleanseField { get; }
        public bool GuardTrigger { get; }
        public bool DelayedMark { get; }
        public bool Reflector { get; }
        public bool DamageShare { get; }
        public bool StatusTransfer { get; }
        public bool BuffPurge { get; }
        public MechanicActorKind ActorKind { get; }

        public static MechanicWorldProfile From(MechanicPlan plan) =>
            new MechanicWorldProfile(plan ?? throw new ArgumentNullException(nameof(plan)));

        static bool Has(MechanicPlan plan, string stat) => plan.Effects.Any(e => e.Stat == stat);
    }
}
