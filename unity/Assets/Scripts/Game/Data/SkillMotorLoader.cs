using Dovus.Core.Grammar;
using Dovus.Game.Diagnostics;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>kural-motoru-v4.json + SliceGrammarDefaults → Core SkillMotor.</summary>
    public static class SkillMotorLoader
    {
        public static SkillMotor Load()
        {
            if (SliceGrammarLoader.TryLoad(out SkillMotor motor))
            {
                DebugConfig.DevLog(
                    $"[JSONLoader] v{motor.Version}: "
                    + $"{motor.RuneCount} runes, {motor.SkillCount} skills, "
                    + $"default build [{string.Join(",", motor.DefaultLoadout.RuneIds)}]");
                return motor;
            }

            Debug.LogError(
                "[JSONLoader] kural-motoru-v4.json veya dilim grameri yok/geçersiz; "
                + "skill'ler devre dışı (boş motor).");
            return SkillMotor.CreateEmpty();
        }
    }
}
