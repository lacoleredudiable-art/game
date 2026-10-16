using Dovus.Core.Grammar;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>Dilim cümle grameri: kural-motoru-v4 dilim rünleri, element-sistemi.json yok.</summary>
    public static class SliceSentenceGrammarLoader
    {
        public const string ResourcePath = "RuleEngineV4/slice-sentence-grammar";

        public static bool TryLoad(out SkillMotor motor)
        {
            motor = null;
            TextAsset asset = AssetLoader.Load<TextAsset>(ResourcePath, null);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return false;
            motor = SkillMotor.FromJson(asset.text);
            return motor.IsV61;
        }
    }
}
