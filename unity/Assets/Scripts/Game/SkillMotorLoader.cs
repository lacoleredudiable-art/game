using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Resources/ElementSystem/element-sistemi.json → Core SkillMotor.</summary>
    public static class SkillMotorLoader
    {
        const string ResourcePath = "ElementSystem/element-sistemi";

        public static SkillMotor LoadOrDefault()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try
                {
                    return SkillMotor.FromJson(asset.text);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"element-sistemi.json okunamadı, gömülü yedek: {e.Message}");
                }
            }

            return SkillMotor.CreateDefault();
        }
    }
}
