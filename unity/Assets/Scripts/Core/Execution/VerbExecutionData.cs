using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Execution
{
    /// <summary>docs/element-sistemi.json hitbox_vfx.fiil_hitbox satırı (metin boyutlar sayıya çevrilir).</summary>
    public readonly struct VerbHitboxSpec
    {
        public VerbHitboxSpec(
            string shape,
            float sizeA,
            float sizeB,
            bool isRadius,
            float durationSec,
            bool isTimed,
            string multi,
            bool sizeBIsWidth = false)
        {
            Shape = shape ?? string.Empty;
            SizeA = sizeA;
            SizeB = sizeB;
            IsRadius = isRadius;
            DurationSec = durationSec;
            IsTimed = isTimed;
            Multi = multi ?? string.Empty;
            SizeBIsWidth = sizeBIsWidth;
        }

        public string Shape { get; }
        /// <summary>Uzunluk (line/capsule/cone) veya yarıçap (sphere/point/cylinder), metre.</summary>
        public float SizeA { get; }
        /// <summary>Yarıçap (m) ya da koni açısı (°); yarıçaplı şekillerde 0.</summary>
        public float SizeB { get; }
        public bool IsRadius { get; }
        /// <summary>"0.3 sn" → 0.3; "anlık" → 0; "süreli" → 0 ve <see cref="IsTimed"/>.</summary>
        public float DurationSec { get; }
        /// <summary>Süre skill engine'inden gelir (reflect_duration_sec, minion_duration_sec…).</summary>
        public bool IsTimed { get; }
        public string Multi { get; }
        /// <summary>
        /// İkinci sayı yarıçap değil genişlik (çap). Oyun yarıçapı bunun yarısıdır;
        /// kapsülün görünür genişliği yazılan metreyle aynı kalır.
        /// </summary>
        public bool SizeBIsWidth { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Shape);
    }

    /// <summary>mobility_cc.i_frame satırı — kaynak skill id'si ("3-7") ya da "dodge".</summary>
    public readonly struct IFrameRule
    {
        public IFrameRule(string source, int durationMs, string condition)
        {
            Source = source ?? string.Empty;
            DurationMs = Math.Max(0, durationMs);
            Condition = condition ?? string.Empty;
        }

        public string Source { get; }
        public int DurationMs { get; }
        public string Condition { get; }
    }

    public readonly struct ElementVfxColor
    {
        public ElementVfxColor(string primary, string secondary, float brightness, float saturation)
        {
            Primary = primary ?? string.Empty;
            Secondary = secondary ?? string.Empty;
            Brightness = brightness;
            Saturation = saturation;
        }

        public string Primary { get; }
        public string Secondary { get; }
        public float Brightness { get; }
        public float Saturation { get; }
    }

    /// <summary>
    /// Fiil executor'larının JSON otoritesi: fiil başına hitbox boyutu ve skill başına
    /// dokunulmazlık (i-frame). Sayılar yalnız JSON'dan; Unity içermez.
    /// </summary>
    public sealed class VerbExecutionData
    {
        static readonly Regex Number = new(@"\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant);

        readonly Dictionary<int, VerbHitboxSpec> _hitboxes = new();
        readonly Dictionary<int, float> _adjectiveSize = new();
        readonly Dictionary<int, float> _weaponSize = new();
        readonly Dictionary<int, ElementVfxColor> _elementColors = new();
        readonly List<IFrameRule> _iFrames = new();

        public IReadOnlyList<IFrameRule> IFrames => _iFrames;

        public bool TryGetHitbox(int verbId, out VerbHitboxSpec spec) => _hitboxes.TryGetValue(verbId, out spec);
        public float AdjectiveSizeMult(int adjectiveId) =>
            _adjectiveSize.TryGetValue(adjectiveId, out float value) && value > 0f ? value : 1f;
        public float WeaponSizeMult(int weaponId, float fallback = 1f) =>
            _weaponSize.TryGetValue(weaponId, out float value) && value > 0f ? value : fallback;
        public bool TryGetElementColor(int elementId, out ElementVfxColor color) =>
            _elementColors.TryGetValue(elementId, out color);
        public string VfxKey(string element, int verbId, int adjectiveId) =>
            $"VFX_{element}_{verbId}_{adjectiveId}";

        public bool TryGetHitbox(in SkillResolution skill, out VerbHitboxSpec spec)
        {
            spec = default;
            return int.TryParse(skill.VerbId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int verbId)
                && _hitboxes.TryGetValue(verbId, out spec);
        }

        /// <summary>Skill id'si ("3-7") için i-frame; condition'a bakılmaz, çağıran anı seçer.</summary>
        public int IFrameMsFor(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;
            for (int i = 0; i < _iFrames.Count; i++)
            {
                if (string.Equals(_iFrames[i].Source, skillId, StringComparison.Ordinal))
                    return _iFrames[i].DurationMs;
            }
            return 0;
        }

        public static VerbExecutionData FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static VerbExecutionData FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static VerbExecutionData FromJsonRoot(JsonValue root)
        {
            var data = new VerbExecutionData();
            foreach (KeyValuePair<string, JsonValue> kv in root["hitbox_vfx"]["fiil_hitbox"].AsObject())
            {
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int verbId))
                    continue;
                data._hitboxes[verbId] = ParseHitbox(kv.Value);
            }

            foreach (KeyValuePair<string, JsonValue> kv in root["hitbox_vfx"]["sifat_override"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int adjectiveId))
                    data._adjectiveSize[adjectiveId] = kv.Value["size_mult"].AsFloat(1f);

            foreach (KeyValuePair<string, JsonValue> kv in root["hitbox_vfx"]["weapon_size_mult"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weaponId))
                    data._weaponSize[weaponId] = kv.Value.AsFloat(1f);

            foreach (KeyValuePair<string, JsonValue> kv in root["hitbox_vfx"]["element_color"].AsObject())
            {
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int elementId))
                    continue;
                data._elementColors[elementId] = new ElementVfxColor(
                    kv.Value["primary"].AsString(),
                    kv.Value["secondary"].AsString(),
                    kv.Value["brightness"].AsFloat(1f),
                    kv.Value["saturation"].AsFloat(1f));
            }

            foreach (JsonValue row in root["mobility_cc"]["i_frame"].AsArray())
            {
                data._iFrames.Add(new IFrameRule(
                    row["source"].AsString(),
                    row["duration_ms"].AsInt(0),
                    row["condition"].AsString()));
            }

            return data;
        }

        static VerbHitboxSpec ParseHitbox(JsonValue row)
        {
            string shape = row["shape"].AsString();
            // "A × B" okuma kuralı (sahip, 29 Eylül):
            //   line ve box: ikinci sayı tam genişliktir (çap). Oyun yarıçapı B/2.
            //   capsule ve sphere: ikinci sayı yarıçaptır, yarılmaz.
            //   cross_section "width" genişlik kuralını açıkça ister. Fiil 3 (line) ve
            //   fiil 7 (kapsül) bu işareti taşır; fiil 1 kapsülü işaretsiz, 0.5 m yarıçaptır.
            //   Koni: menzil × açı.
            string size = row["base_size"].AsString();
            MatchCollection numbers = Number.Matches(size);
            float a = numbers.Count > 0 ? ParseFloat(numbers[0].Value) : 0f;
            float b = numbers.Count > 1 ? ParseFloat(numbers[1].Value) : 0f;
            bool isRadius = numbers.Count == 1;

            string duration = row["duration"].AsString();
            MatchCollection durationNumbers = Number.Matches(duration);
            float durationSec = durationNumbers.Count > 0 ? ParseFloat(durationNumbers[0].Value) : 0f;
            bool timed = durationNumbers.Count == 0
                && duration.StartsWith("s", StringComparison.OrdinalIgnoreCase);

            JsonValue multi = row["multi"];
            string multiText = multi.Kind == JsonKind.Bool
                ? (multi.AsBool() ? "true" : "false")
                : multi.AsString();

            bool width = string.Equals(row["cross_section"].AsString(), "width", StringComparison.Ordinal);
            return new VerbHitboxSpec(shape, a, b, isRadius, durationSec, timed, multiText, width);
        }

        static float ParseFloat(string s) =>
            float.Parse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
