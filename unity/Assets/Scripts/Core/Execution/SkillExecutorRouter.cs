using System;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;

namespace Dovus.Core.Execution
{
    public enum SkillExecutorKind
    {
        Fallback,
        MeleeHitbox,
        Projectile,
        FieldAura
    }

    public readonly struct SkillExecutorRoute
    {
        public SkillExecutorRoute(SkillExecutorKind kind, bool isStub, string reason)
        {
            Kind = kind;
            IsStub = isStub;
            Reason = reason ?? string.Empty;
        }

        public SkillExecutorKind Kind { get; }
        public bool IsStub { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// İki rünlük skill'i fiil + canonical weapon.type üzerinden fiziksel teslimata yollar.
    /// Unity fiziği içermez; gerçek spawn/overlap Game katmanındaki üç executor'dadır.
    /// </summary>
    public sealed class SkillExecutorRouter
    {
        public SkillExecutorRoute Route(in SkillResolution skill, EquipmentItem? weapon)
        {
            if (skill.IsEmpty || !skill.IsComplete || !TryVerbId(skill, out int verbId))
                return Stub("tamamlanmış 2-rün skill yok");

            switch (verbId)
            {
                case 1: // Saldırı
                case 5: // Patlama
                    return IsRangedWeapon(weapon)
                        ? new SkillExecutorRoute(SkillExecutorKind.Projectile, false, string.Empty)
                        : new SkillExecutorRoute(SkillExecutorKind.MeleeHitbox, false, string.Empty);

                case 2:  // İyileştirme
                case 4:  // Savunma
                case 6:  // Kontrol
                case 8:  // Güçlendirme
                case 9:  // Arındırma
                case 12: // Zaman
                    return new SkillExecutorRoute(SkillExecutorKind.FieldAura, false, string.Empty);

                case 3:  // Hareket
                case 7:  // Zayıflatma
                case 10: // Yansıma / SelfState
                case 11: // Çağırma
                    return Stub($"fiil {verbId} executor kapsamı dışında");

                default:
                    return Stub($"fiil {verbId} için executor yok");
            }
        }

        /// <summary>
        /// JSON üç sınıf taşır: melee / medium / ranged. Yalnız explicit ranged mermi üretir;
        /// medium (Mızrak/Kılıç) yakın temas olarak kalır, isim listesiyle tahmin yapılmaz.
        /// </summary>
        public static bool IsRangedWeapon(EquipmentItem? weapon) =>
            weapon != null
            && string.Equals(weapon.Type, "ranged", StringComparison.OrdinalIgnoreCase);

        static bool TryVerbId(in SkillResolution skill, out int verbId) =>
            int.TryParse(skill.VerbId, out verbId) && verbId > 0;

        static SkillExecutorRoute Stub(string reason) =>
            new(SkillExecutorKind.Fallback, true, reason);
    }
}
