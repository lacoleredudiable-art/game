using Dovus.Core.Grammar;
using Dovus.Game.Diagnostics;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>
    /// Resources/RuleEngineV4/slice-sentence-grammar.json → Core SkillMotor (dilim rünleri).
    /// </summary>
    public static class SkillMotorLoader
    {
        public static SkillMotor Load()
        {
            if (SliceSentenceGrammarLoader.TryLoad(out SkillMotor motor))
            {
                DebugConfig.DevLog(
                    $"[JSONLoader] v{motor.Version}: "
                    + $"{motor.RuneCount} runes, {motor.SkillCount} skills, "
                    + $"default build [{string.Join(",", motor.DefaultLoadout.RuneIds)}]");
                return motor;
            }

            Debug.LogError(
                $"[JSONLoader] Resources/{SliceSentenceGrammarLoader.ResourcePath}.json "
                + "yok veya geçersiz; skill'ler devre dışı (boş motor).");
            return SkillMotor.CreateEmpty();
        }
    }
}
