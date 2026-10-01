using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Karşılaşma tasarım sayıları <c>Resources/Bosses/karadul.json</c>'dan (BossHudData ile aynı dosya).
    /// "targeting" bloğu ve "volley" saldırı sayıları; dalgalar / yardımcılar sonraki PR'larda buraya eklenir.
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

        /// <summary>Zehir Tükürüğü sayıları (karadul.json "volley") BossTuning'e yazılır. Dosya yoksa varsayılan kalır.</summary>
        public static bool ApplyVolley(BossTuning tuning, string resourcePath = "Bosses/karadul")
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (tuning == null || asset == null || string.IsNullOrWhiteSpace(asset.text))
                return false;
            try
            {
                return BossVolleyData.Apply(MiniJson.Parse(asset.text), tuning);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossEncounterData] {resourcePath} volley okunamadı: {e.Message}");
                return false;
            }
        }
    }
}
