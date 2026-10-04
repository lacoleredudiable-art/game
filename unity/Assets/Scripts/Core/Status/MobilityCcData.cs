using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Dovus.Core;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
namespace Dovus.Core.Status
{
    /// <summary>v6.1.1 mobility_cc verisini parse eder ve saf C# çözümlerini sunar.</summary>
    public sealed class MobilityCcData
    {
        readonly Dictionary<int, string> _verbMobility = new();
        readonly Dictionary<int, string> _adjectiveOverride = new();
        readonly Dictionary<int, int> _weaponMobility = new();
        readonly Dictionary<StatusKind, int> _ccRank = new();
        readonly Dictionary<StatusKind, double> _ccDurationMs = new();
        readonly Dictionary<int, float> _ccDurationMult = new();
        readonly Dictionary<string, HashSet<StatusKind>> _visibleByPair = new(StringComparer.Ordinal);

        public string SameCcStacking { get; private set; } = string.Empty;
        public string DifferentCcStacking { get; private set; } = string.Empty;
        /// <summary>Kök bitince kısa bağışıklık. JSON'da yoksa 0.5 sn.</summary>
        public double RootImmunityMs { get; private set; } = StatusDefaults.RootImmunityMs;
        public float LightPoise { get; private set; }
        public float MediumPoise { get; private set; }
        public float HeavyPoise { get; private set; }
        public int PoiseStunMs { get; private set; } = 1000;

        public static MobilityCcData FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static MobilityCcData FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static MobilityCcData FromJsonRoot(JsonValue root)
        {
            var data = new MobilityCcData();
            JsonValue node = root["mobility_cc"];

            ReadStringMap(node["fiil_base_mobility"], data._verbMobility);
            ReadStringMap(node["sifat_mobility_override"], data._adjectiveOverride);
            foreach (KeyValuePair<string, JsonValue> kv in node["weapon_mobility_mod"].AsObject())
                if (TryId(kv.Key, out int id))
                    data._weaponMobility[id] = kv.Value.AsInt(0);

            foreach (JsonValue row in node["cc_priority"].AsArray())
            {
                if (!StatusKindUtil.TryParse(row["cc"].AsString(), out StatusKind kind)
                    || kind == StatusKind.None)
                    continue;
                data._ccRank[kind] = row["rank"].AsInt(int.MaxValue);
                if (row["duration_sec"].Kind == JsonKind.Number)
                    data._ccDurationMs[kind] = row["duration_sec"].AsFloat(0f) * StatusDefaults.SecToMs;
            }

            data.SameCcStacking = node["cc_stacking"]["same_cc"].AsString();
            data.DifferentCcStacking = node["cc_stacking"]["different_cc"].AsString();
            data.RootImmunityMs = ReadRootImmunityMs(node);
            foreach (KeyValuePair<string, JsonValue> kv in node["cc_priority_table"].AsObject())
                data.ParsePriorityPair(kv.Key, kv.Value.AsString());

            var adjectiveIdByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in root["adjective_mods"].AsObject())
            {
                if (TryId(kv.Key, out int id))
                    adjectiveIdByKey[Normalize(kv.Value["label"].AsString())] = id;
            }
            foreach (KeyValuePair<string, JsonValue> kv in node["cc_duration_mult"].AsObject())
            {
                if (string.Equals(kv.Key, "base", StringComparison.Ordinal))
                    continue;
                if (adjectiveIdByKey.TryGetValue(Normalize(kv.Key), out int id))
                    data._ccDurationMult[id] = kv.Value.AsFloat(1f);
            }

            data.LightPoise = node["poise"]["hafif"].AsFloat(0f);
            data.MediumPoise = node["poise"]["orta"].AsFloat(0f);
            data.HeavyPoise = node["poise"]["agir"].AsFloat(0f);
            return data;
        }

        public string ResolveMobility(int verbId, int adjectiveId, int weaponId, int fallbackWeaponMod = 0)
        {
            string baseValue = _verbMobility.TryGetValue(verbId, out string value)
                ? value
                : SkillMobility.FreeMove;
            string adjective = _adjectiveOverride.TryGetValue(adjectiveId, out string over)
                ? over
                : "degistirmez";
            if (string.Equals(adjective, "rooted_zorla", StringComparison.Ordinal))
                return SkillMobility.Rooted;

            int score = baseValue switch
            {
                SkillMobility.FreeMove => 1,
                SkillMobility.SlowedMove => -1,
                _ => -2
            };
            score += adjective switch
            {
                "bir_seviye_kisitla" => -1,
                "bir_seviye_serbestle" => 1,
                _ => 0
            };
            score += _weaponMobility.TryGetValue(weaponId, out int weaponMod)
                ? weaponMod
                : fallbackWeaponMod;

            if (score >= 1)
                return SkillMobility.FreeMove;
            if (score <= -2)
                return SkillMobility.Rooted;
            // Binding metni 0'ı adlandırmıyor; üç kademeli eksenin orta hali.
            return SkillMobility.SlowedMove;
        }

        public double ResolveCcDurationMs(StatusKind kind, int adjectiveId, double fallbackMs)
        {
            double baseMs = _ccDurationMs.TryGetValue(kind, out double value) && value > 0
                ? value
                : fallbackMs;
            float mult = _ccDurationMult.TryGetValue(adjectiveId, out float m) && m > 0f ? m : 1f;
            return baseMs * mult;
        }

        public bool IsCcVisible(StatusKind candidate, IReadOnlyCollection<StatusKind> active)
        {
            if (!_ccRank.ContainsKey(candidate) || active == null)
                return true;
            foreach (StatusKind other in active)
            {
                if (other == candidate || !_ccRank.ContainsKey(other))
                    continue;
                string key = PairKey(candidate, other);
                if (_visibleByPair.TryGetValue(key, out HashSet<StatusKind> visible)
                    && !visible.Contains(candidate))
                    return false;
            }
            return true;
        }

        public bool TryPoiseBreak(float incomingDamage, float threshold, out int stunMs)
        {
            stunMs = PoiseStunMs;
            return threshold > 0f && incomingDamage > threshold;
        }

        public float PoiseThreshold(string tier) => tier switch
        {
            "hafif" => LightPoise,
            "agir" => HeavyPoise,
            _ => MediumPoise
        };

        void ParsePriorityPair(string key, string result)
        {
            string[] ids = key.Split('_');
            if (ids.Length != 2
                || !StatusKindUtil.TryParse(ids[0], out StatusKind a)
                || !StatusKindUtil.TryParse(ids[1], out StatusKind b))
                return;
            var visible = new HashSet<StatusKind>();
            if (ContainsToken(result, ids[0])) visible.Add(a);
            if (ContainsToken(result, ids[1])) visible.Add(b);
            _visibleByPair[PairKey(a, b)] = visible;
        }

        static bool ContainsToken(string text, string token)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
                return false;
            return text.IndexOf(token, StringComparison.Ordinal) >= 0
                   && text.IndexOf(token + " gizli", StringComparison.Ordinal) < 0;
        }

        static string PairKey(StatusKind a, StatusKind b) =>
            (int)a < (int)b ? $"{(int)a}:{(int)b}" : $"{(int)b}:{(int)a}";

        static void ReadStringMap(JsonValue obj, Dictionary<int, string> dst)
        {
            foreach (KeyValuePair<string, JsonValue> kv in obj.AsObject())
                if (TryId(kv.Key, out int id))
                    dst[id] = kv.Value.AsString();
        }

        static double ReadRootImmunityMs(JsonValue node)
        {
            if (TrySeconds(node, "root_immunity_sec", out double sec)
                || TrySeconds(node, "bind_immunity_sec", out sec)
                || TrySeconds(node["cc_stacking"], "root_immunity_sec", out sec)
                || TrySeconds(node["cc_stacking"], "bind_immunity_sec", out sec))
                return sec * StatusDefaults.SecToMs;

            DesignWarnings.Once(
                "mobility_cc.root_immunity_sec",
                "element-sistemi.json kök bağışıklığı yok; yedek 0.5 sn kullanıldı.");
            return StatusDefaults.RootImmunityMs;
        }

        static bool TrySeconds(JsonValue obj, string field, out double seconds)
        {
            seconds = 0;
            if (!obj.Has(field) || obj[field].Kind != JsonKind.Number)
                return false;
            seconds = obj[field].AsFloat(0f);
            return true;
        }

        static bool TryId(string value, out int id) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);

        static string Normalize(string value)
        {
            string lower = (value ?? string.Empty).ToLowerInvariant()
                .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u')
                .Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');
            var sb = new StringBuilder(lower.Length);
            for (int i = 0; i < lower.Length; i++)
                if (char.IsLetterOrDigit(lower[i]))
                    sb.Append(lower[i]);
            return sb.ToString();
        }
    }
}
