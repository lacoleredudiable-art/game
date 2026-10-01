using Dovus.Core.Grammar;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>karadul.json "attacks" içindeki "volley" satırı → BossTuning (saf; eksik alan varsayılanı korur).</summary>
    public static class BossVolleyData
    {
        public static bool Apply(JsonValue root, BossTuning tuning)
        {
            if (root == null || tuning == null)
                return false;
            foreach (JsonValue a in root["attacks"].AsArray())
            {
                if (a["id"].AsString() != "volley")
                    continue;
                tuning.VolleyWindupMs = a["windup_ms"].AsInt(tuning.VolleyWindupMs);
                tuning.VolleyCount = a["count"].AsInt(tuning.VolleyCount);
                tuning.VolleyCountEnraged = a["count_enraged"].AsInt(tuning.VolleyCountEnraged);
                tuning.VolleySpreadDeg = a["spread_deg"].AsFloat(tuning.VolleySpreadDeg);
                tuning.VolleySpeedMps = a["speed_mps"].AsFloat(tuning.VolleySpeedMps);
                tuning.VolleyDamage = a["damage"].AsInt(tuning.VolleyDamage);
                tuning.VolleyRadiusM = a["radius_m"].AsFloat(tuning.VolleyRadiusM);
                tuning.VolleyLifeSec = a["life_sec"].AsFloat(tuning.VolleyLifeSec);
                return true;
            }
            return false;
        }
    }
}
