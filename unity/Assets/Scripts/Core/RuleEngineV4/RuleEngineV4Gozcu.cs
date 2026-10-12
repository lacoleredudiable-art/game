using System;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// kural-motoru-v4.json "gozcu": bariz hata dedektörünün eşikleri ve silah-klip uyumu.
    /// Blok ya da alan yoksa <see cref="RuleEngineV4GozcuDefaults"/>.
    /// </summary>
    public sealed class RuleEngineV4Gozcu
    {
        public float FootBelowGroundMaxM { get; private set; } = RuleEngineV4GozcuDefaults.FootBelowGroundMaxM;
        public float BodyPenetrationMaxM { get; private set; } = RuleEngineV4GozcuDefaults.BodyPenetrationMaxM;
        public float CameraAngularSpeedMaxDps { get; private set; } = RuleEngineV4GozcuDefaults.CameraAngularSpeedMaxDps;
        public float EffectScreenCoverMaxFrac { get; private set; } = RuleEngineV4GozcuDefaults.EffectScreenCoverMaxFrac;
        public string FallbackLogTag { get; private set; } = RuleEngineV4GozcuDefaults.FallbackLogTag;
        public int FallbackLogMax { get; private set; } = RuleEngineV4GozcuDefaults.FallbackLogMax;
        public float LogIntervalSec { get; private set; } = RuleEngineV4GozcuDefaults.LogIntervalSec;
        public float LineShowSec { get; private set; } = RuleEngineV4GozcuDefaults.LineShowSec;
        public float ClipWeightMin { get; private set; } = RuleEngineV4GozcuDefaults.ClipWeightMin;
        public float CameraProbeRadiusM { get; private set; } = RuleEngineV4GozcuDefaults.CameraProbeRadiusM;
        public float CameraFovMinDeg { get; private set; } = RuleEngineV4GozcuDefaults.CameraFovMinDeg;

        readonly Dictionary<int, string> _weaponClipNames = new();
        readonly List<string> _sharedClipPrefixes = new();

        public IReadOnlyDictionary<int, string> WeaponClipNames => _weaponClipNames;
        public IReadOnlyList<string> SharedClipPrefixes => _sharedClipPrefixes;

        public static RuleEngineV4Gozcu FromJson(JsonValue root)
        {
            var g = new RuleEngineV4Gozcu();
            JsonValue n = root["gozcu"];
            if (n.Kind != JsonKind.Object)
                return g;
            g.FootBelowGroundMaxM = n["foot_below_ground_max_m"].AsFloat(g.FootBelowGroundMaxM);
            g.BodyPenetrationMaxM = n["body_penetration_max_m"].AsFloat(g.BodyPenetrationMaxM);
            g.CameraAngularSpeedMaxDps = n["camera_angular_speed_max_dps"].AsFloat(g.CameraAngularSpeedMaxDps);
            g.EffectScreenCoverMaxFrac = n["effect_screen_cover_max_frac"].AsFloat(g.EffectScreenCoverMaxFrac);
            g.FallbackLogTag = n["fallback_log_tag"].AsString(g.FallbackLogTag);
            g.FallbackLogMax = n["fallback_log_max"].AsInt(g.FallbackLogMax);
            g.LogIntervalSec = n["log_interval_sec"].AsFloat(g.LogIntervalSec);
            g.LineShowSec = n["line_show_sec"].AsFloat(g.LineShowSec);
            g.ClipWeightMin = n["clip_weight_min"].AsFloat(g.ClipWeightMin);
            g.CameraProbeRadiusM = n["camera_probe_radius_m"].AsFloat(g.CameraProbeRadiusM);
            g.CameraFovMinDeg = n["camera_fov_min_deg"].AsFloat(g.CameraFovMinDeg);
            foreach (KeyValuePair<string, JsonValue> kv in n["weapon_clip_names"].AsObject())
            {
                if (int.TryParse(kv.Key, out int weaponId) && kv.Value.Kind == JsonKind.String)
                    g._weaponClipNames[weaponId] = kv.Value.AsString();
            }
            foreach (JsonValue p in n["shared_clip_prefixes"].AsArray())
            {
                if (p.Kind == JsonKind.String)
                    g._sharedClipPrefixes.Add(p.AsString());
            }
            return g;
        }

        public static RuleEngineV4Gozcu FromJson(string json) => FromJson(MiniJson.Parse(json));

        /// <summary>
        /// Yalnız başka bir silahın adını '_' ile ayrılmış parça olarak taşıyan klip uyumsuzdur
        /// (Yay eldeyken <c>Loko_Kilic_Idle</c>). Ortak önekler ve silah adı taşımayan klipler
        /// (dodge, takla, darbe alma) serbest. Tabloda olmayan silah denetlenmez.
        /// </summary>
        public bool IsClipAllowed(string clip, int weaponId)
        {
            if (string.IsNullOrEmpty(clip) || !_weaponClipNames.TryGetValue(weaponId, out string own))
                return true;
            for (int i = 0; i < _sharedClipPrefixes.Count; i++)
            {
                if (clip.StartsWith(_sharedClipPrefixes[i], StringComparison.Ordinal))
                    return true;
            }
            foreach (KeyValuePair<int, string> kv in _weaponClipNames)
            {
                if (kv.Key != weaponId && !string.Equals(kv.Value, own, StringComparison.OrdinalIgnoreCase)
                    && HasNameSegment(clip, kv.Value))
                    return false;
            }
            return true;
        }

        static bool HasNameSegment(string clip, string name)
        {
            int start = 0;
            while (start <= clip.Length)
            {
                int end = clip.IndexOf('_', start);
                if (end < 0)
                    end = clip.Length;
                if (end - start == name.Length
                    && string.Compare(clip, start, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
                start = end + 1;
            }
            return false;
        }
    }
}
