using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Karşılaşma tasarım sayıları <c>Resources/Bosses/karadul.json</c>'dan (BossHudData ile aynı dosya).
    /// Şimdilik yalnız "targeting" bloğu; dalgalar / yardımcılar sonraki PR'larda buraya eklenir.
    /// </summary>
    public static class BossEncounterData
    {
        public static TargetingConfig LoadTargeting(string resourcePath = "Bosses/karadul")
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return new TargetingConfig();
            try
            {
                return TargetingConfig.FromJson(MiniJson.Parse(asset.text));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossEncounterData] {resourcePath} okunamadı: {e.Message}");
                return new TargetingConfig();
            }
        }
    }
}
