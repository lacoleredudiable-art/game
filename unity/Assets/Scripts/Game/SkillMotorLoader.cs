using Dovus.Core.Grammar;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Resources/ElementSystem/element-sistemi.json → Core SkillMotor.</summary>
    public static class SkillMotorLoader
    {
        public static SkillMotor LoadOrDefault()
        {
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                SkillMotor motor = design.SkillMotor;
                StatusReactionTable.Rebuild(motor.StatusInteractions);
                Debug.Log(
                    $"[JSONLoader] v{motor.Version}: "
                    + $"{motor.RuneCount} runes, {motor.SkillCount} skills, "
                    + $"default build [{string.Join(",", motor.DefaultLoadout.RuneIds)}]");
                return motor;
            }

            Debug.LogWarning("element-sistemi v6.1.1 yok/geçersiz; gömülü v5 iskeleti kullanılıyor.");
            SkillMotor fallback = SkillMotor.CreateDefault();
            StatusReactionTable.Rebuild(fallback.StatusInteractions);
            return fallback;
        }
    }
}
