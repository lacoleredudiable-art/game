using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Assets;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>kural-motoru-v4.json + <see cref="SliceGrammarDefaults"/> → SkillMotor.</summary>
    public static class SliceGrammarLoader
    {
        public static bool TryLoad(out SkillMotor motor)
        {
            motor = null;
            TextAsset v4 = AssetLoader.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource, null);
            if (v4 == null || string.IsNullOrWhiteSpace(v4.text))
                return false;
            string grammarJson = SliceGrammarDefaults.BuildGrammarJson(v4.text);
            motor = SkillMotor.FromJson(grammarJson);
            return motor.IsV61;
        }
    }
}
