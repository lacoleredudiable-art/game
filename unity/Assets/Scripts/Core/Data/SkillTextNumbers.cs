using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Shared;
namespace Dovus.Core.Data
{
    public enum TextNumberKind
    {
        Percent,
        Seconds,
        Meters,
        Milliseconds,
        Multiplier
    }

    public readonly struct TextNumber
    {
        public TextNumber(TextNumberKind kind, double value)
        {
            Kind = kind;
            Value = value;
        }

        public TextNumberKind Kind { get; }
        public double Value { get; }
    }

    /// <summary>
    /// Skill kart metnindeki sayıları çıkarır; engine JSON sayılarıyla eşleştirir (metin otorite değildir).
    /// </summary>
    public static class SkillTextNumbers
    {
        const double Tol = 0.06;

        static readonly Regex Percent = new(
            @"%(\d+(?:[.,]\d+)?)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex Seconds = new(
            @"(?<![\d.])(\d+(?:[.,]\d+)?)\s*sn\b",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Meters = new(
            @"(?<![\d.])(\d+(?:[.,]\d+)?)\s*m\b",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Milliseconds = new(
            @"(?<![\d.])(\d+(?:[.,]\d+)?)\s*ms\b",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Times = new(
            @"[×x](\d+(?:[.,]\d+)?)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex Kat = new(
            @"(?<![\d.])(\d+(?:[.,]\d+)?)\s*kat\b",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IEnumerable<TextNumber> Extract(string text)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            foreach (Match m in Percent.Matches(text))
            {
                if (TryParse(m.Groups[1].Value, out double v))
                    yield return new TextNumber(TextNumberKind.Percent, v);
            }

            foreach (Match m in Seconds.Matches(text))
            {
                if (TryParse(m.Groups[1].Value, out double v))
                    yield return new TextNumber(TextNumberKind.Seconds, v);
            }

            foreach (Match m in Meters.Matches(text))
            {
                if (TryParse(m.Groups[1].Value, out double v))
                    yield return new TextNumber(TextNumberKind.Meters, v);
            }

            foreach (Match m in Milliseconds.Matches(text))
            {
                if (TryParse(m.Groups[1].Value, out double v))
                    yield return new TextNumber(TextNumberKind.Milliseconds, v);
            }

            foreach (Match m in Times.Matches(text))
            {
                if (TryParse(m.Groups[1].Value, out double v))
                    yield return new TextNumber(TextNumberKind.Multiplier, v);
            }

            foreach (Match m in Kat.Matches(text))
            {
                if (TryParse(m.Groups[1].Value, out double v))
                    yield return new TextNumber(TextNumberKind.Multiplier, v);
            }
        }

        public static bool Matches(TextNumber number, IReadOnlyList<double> engineValues)
        {
            if (engineValues == null || engineValues.Count == 0)
                return false;

            for (int i = 0; i < engineValues.Count; i++)
            {
                double x = engineValues[i];
                if (Nearly(number.Value, x))
                    return true;
                if (Nearly(number.Value, x * 100.0))
                    return true;
                if (Nearly(number.Value, (1.0 - x) * 100.0))
                    return true;
                if (Nearly(number.Value, (x - 1.0) * 100.0))
                    return true;
                if (Nearly(number.Value, Math.Abs(x) * 100.0))
                    return true;
            }

            return false;
        }

        public static List<double> CollectEngineNumbers(JsonValue engine)
        {
            var values = new List<double>();
            if (engine.Kind != JsonKind.Object)
                return values;
            Collect(engine, values);
            return values;
        }

        static void Collect(JsonValue node, List<double> dst)
        {
            switch (node.Kind)
            {
                case JsonKind.Number:
                    dst.Add(node.AsDouble());
                    break;
                case JsonKind.Object:
                    foreach (KeyValuePair<string, JsonValue> kv in node.AsObject())
                        Collect(kv.Value, dst);
                    break;
                case JsonKind.Array:
                    foreach (JsonValue item in node.AsArray())
                        Collect(item, dst);
                    break;
            }
        }

        static bool Nearly(double a, double b) => Math.Abs(a - b) <= Tol;

        static bool TryParse(string raw, out double value)
        {
            value = 0;
            if (string.IsNullOrEmpty(raw))
                return false;
            string normalized = raw.Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Pasif slot metni: süre sıfat rününün passive_duration_default değerinden gelir.</summary>
        public static string PassiveText(string adjectiveName, double durationSec)
        {
            if (string.IsNullOrEmpty(adjectiveName))
                adjectiveName = string.Empty;
            return FormatDurationSeconds(durationSec) + " sn pasif: " + adjectiveName;
        }

        public static string FormatIncompatibleSkillDetail(string prose, float castTimeMult, float damageMult)
        {
            string line = "Uyumsuz: cast ×" + FormatMultiplier(castTimeMult)
                + ", hasar %" + FormatDamagePercent(damageMult);
            if (string.IsNullOrEmpty(prose))
                return line;
            return line + "  ·  " + prose;
        }

        public static string FormatIncompatibleCompatibilityLabel(float damageMult)
        {
            return "UYUMSUZ  ×" + FormatMultiplier(damageMult);
        }

        public static string FormatDurationSeconds(double durationSec)
        {
            if (Math.Abs(durationSec - Math.Round(durationSec)) < 1e-9)
                return ((int)Math.Round(durationSec)).ToString(CultureInfo.InvariantCulture);
            return durationSec.ToString("0.#", CultureInfo.InvariantCulture);
        }

        static string FormatMultiplier(float mult)
        {
            if (mult <= 0f)
                mult = 1f;
            return mult.ToString("0.#", CultureInfo.InvariantCulture);
        }

        static string FormatDamagePercent(float damageMult)
        {
            if (damageMult <= 0f)
                damageMult = 1f;
            int pct = (int)Math.Round(damageMult * 100.0, MidpointRounding.AwayFromZero);
            return pct.ToString(CultureInfo.InvariantCulture);
        }
    }
}
