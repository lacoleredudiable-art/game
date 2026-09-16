using System.Collections.Generic;

namespace Dovus.Core.Status
{
    /// <summary>Bir reaksiyon kuralının hangi tarafı(nı) değiştirdiği.</summary>
    public enum ReactionTarget
    {
        A,
        B,
        Both
    }

    /// <summary>
    /// İki status aynı hedefte aynı anda varken ne olur (docs/element-sistemi.json
    /// "status_interaction_table", 16 Eylül sohbetinden). Sayılar sahibinin verdiği spec'ten —
    /// uydurma yok. "Fiziksel kimya" değil (ıslak/donmuş yok); sadece mantıklı, isimli çiftler.
    /// Üç kombinasyon burada YOK çünkü genellenemiyor, StatusBoard/StatusApplicator'da özel
    /// işleniyor: burn+poison (ekstra tick), shield+burn (kalkan aşınması), stun+knockback
    /// (aynı vuruşta süre uzaması — Knockback kalıcı status değil, anlık bayrak).
    /// </summary>
    public readonly struct StatusReactionRule
    {
        public StatusReactionRule(
            StatusKind a, StatusKind b, string name, string readAs, ReactionTarget target,
            float magnitudeMult = 1f, float? magnitudeSet = null,
            float durationMult = 1f, double durationAddMs = 0)
        {
            A = a;
            B = b;
            Name = name;
            ReadAs = readAs;
            Target = target;
            MagnitudeMult = magnitudeMult;
            MagnitudeSet = magnitudeSet;
            DurationMult = durationMult;
            DurationAddMs = durationAddMs;
        }

        public StatusKind A { get; }
        public StatusKind B { get; }
        public string Name { get; }
        public string ReadAs { get; }
        public ReactionTarget Target { get; }
        public float MagnitudeMult { get; }
        public float? MagnitudeSet { get; }
        public float DurationMult { get; }
        public double DurationAddMs { get; }
    }

    public static class StatusReactionTable
    {
        static readonly StatusReactionRule[] Rules =
        {
            new(StatusKind.Burn, StatusKind.ArmorBreak, "Erimiş Zırh",
                "Yanan zırh daha hızlı erir", ReactionTarget.B,
                magnitudeMult: 1.5f, durationAddMs: 2000),

            new(StatusKind.Burn, StatusKind.Weaken, "Zayıf Yanma",
                "Zayıflamış yanma daha uzun sürer", ReactionTarget.A,
                magnitudeMult: 0.8f, durationMult: 1.5f),

            new(StatusKind.Poison, StatusKind.Slow, "Ağır Zehir",
                "Zehir yavaşlar ama daha uzun kemirir", ReactionTarget.A,
                magnitudeMult: 0.7f, durationMult: 1.5f),

            new(StatusKind.ArmorBreak, StatusKind.Weaken, "Kırılganlık",
                "Zırh da hasar da çöker", ReactionTarget.Both,
                magnitudeMult: 1.3f),

            new(StatusKind.GrievousWounds, StatusKind.Burn, "Kavurucu Yara",
                "Yanık yaralar iyileşmez", ReactionTarget.A,
                magnitudeSet: 0.7f),

            new(StatusKind.Stun, StatusKind.Burn, "Alevli Sersemletme",
                "Sersemlemiş hedef yanarken acı çeker", ReactionTarget.B,
                magnitudeMult: 1.5f),

            new(StatusKind.Root, StatusKind.Burn, "Hapscul Yanma",
                "Kaçamayan hedef daha uzun yanar", ReactionTarget.B,
                durationMult: 1.5f),

            new(StatusKind.Root, StatusKind.ArmorBreak, "Çaresiz Zırh",
                "Kilitli hedef zırhını koruyamaz", ReactionTarget.B,
                magnitudeMult: 1.5f),

            new(StatusKind.Slow, StatusKind.Burn, "Sürünen Alev",
                "Yavaş hedef alevden kaçamaz", ReactionTarget.B,
                magnitudeMult: 1.3f),

            new(StatusKind.Slow, StatusKind.Root, "Tam Hapsetme",
                "Önce yavaşlar, sonra tamamen kilitlenir", ReactionTarget.B,
                durationMult: 1.5f),

            new(StatusKind.Silence, StatusKind.Blind, "Tam Karanlık",
                "Göremez ve konuşamaz", ReactionTarget.Both,
                durationMult: 1.3f),

            new(StatusKind.Root, StatusKind.Blind, "Kapana Kısılmış",
                "Kilitli ve kör — çaresiz", ReactionTarget.B,
                durationMult: 1.5f),

            new(StatusKind.Regen, StatusKind.Poison, "Zehirli Yenilenme",
                "Zehir iyileşmeyi yavaşlatır", ReactionTarget.A,
                magnitudeMult: 0.7f),

            new(StatusKind.Haste, StatusKind.Slow, "Nötrleşme",
                "Hız ve yavaşlık birbirini götürür", ReactionTarget.Both,
                magnitudeMult: 0.5f),
        };

        static readonly Dictionary<(StatusKind, StatusKind), int> Index = BuildIndex();

        static Dictionary<(StatusKind, StatusKind), int> BuildIndex()
        {
            var map = new Dictionary<(StatusKind, StatusKind), int>();
            for (int i = 0; i < Rules.Length; i++)
                map[(Rules[i].A, Rules[i].B)] = i;
            return map;
        }

        /// <summary>
        /// incoming/other eşleşirse kuralı döner. incomingIsA=true ise incoming==rule.A.
        /// </summary>
        public static bool TryGetRule(
            StatusKind incoming, StatusKind other, out StatusReactionRule rule, out bool incomingIsA)
        {
            if (Index.TryGetValue((incoming, other), out int i))
            {
                rule = Rules[i];
                incomingIsA = true;
                return true;
            }

            if (Index.TryGetValue((other, incoming), out int j))
            {
                rule = Rules[j];
                incomingIsA = false;
                return true;
            }

            rule = default;
            incomingIsA = false;
            return false;
        }

        public static IReadOnlyList<StatusReactionRule> All => Rules;
    }
}
