using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Resources/ElementSystem/element-sistemi.json → Core SkillMotor. Tek kaynak bu JSON'dur;
    /// gömülü v5 yedeği yoktur. Dosya yok/geçersizse net bir hata loglanır ve boş motor döner:
    /// hiçbir skill çözülmez ama oyun çökmez (CoreTests gerçek JSON'un yüklendiğini ayrıca doğrular).
    /// </summary>
    public static class SkillMotorLoader
    {
        public static SkillMotor Load()
        {
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                SkillMotor motor = design.SkillMotor;
                Debug.Log(
                    $"[JSONLoader] v{motor.Version}: "
                    + $"{motor.RuneCount} runes, {motor.SkillCount} skills, "
                    + $"default build [{string.Join(",", motor.DefaultLoadout.RuneIds)}]");
                return motor;
            }

            Debug.LogError(
                $"[JSONLoader] Resources/{ElementSystemJsonLoader.ResourcePath}.json "
                + $"(v{ElementSystemJsonLoader.RequiredVersion}) yok veya geçersiz. Gömülü yedek yok; "
                + "skill'ler devre dışı (boş motor).");
            return SkillMotor.CreateEmpty();
        }
    }
}
