using System;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;

namespace Dovus.Core.Casting
{
    /// <summary>
    /// İki rünlük skill'i fiil + canonical weapon.type üzerinden fiziksel teslimata yollar.
    /// Unity fiziği içermez; gerçek spawn/overlap Game katmanındaki executor'lardadır.
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
                case 7: // Zayıflatma — hedefe capsule (hitbox_vfx.fiil_hitbox.7)
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

                case 3: // Hareket — dash_line
                    return new SkillExecutorRoute(SkillExecutorKind.Movement, false, string.Empty);

                case 10: // Yansıma — kendine süreli yansıtma
                    return new SkillExecutorRoute(SkillExecutorKind.SelfState, false, string.Empty);

                case 11: // Çağırma — minion
                    return new SkillExecutorRoute(SkillExecutorKind.Summon, false, string.Empty);

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
