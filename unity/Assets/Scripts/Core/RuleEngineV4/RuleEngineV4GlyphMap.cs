using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Rün → Ejder kod adı (kural-motoru-v4.json rune_glyphs) → atlas hücresi (Ejder_Glif_Sirasi.txt).
    /// Atlas sırası kod adına göre; rün kimliğinden hücre türetilmez.
    /// </summary>
    public sealed class RuleEngineV4GlyphMap
    {
        readonly Dictionary<int, string> _verbs;
        readonly Dictionary<int, string> _adjectives;
        readonly IReadOnlyDictionary<string, int> _order;

        RuleEngineV4GlyphMap(
            Dictionary<int, string> verbs,
            Dictionary<int, string> adjectives,
            IReadOnlyDictionary<string, int> order)
        {
            _verbs = verbs;
            _adjectives = adjectives;
            _order = order;
        }

        public static RuleEngineV4GlyphMap Empty { get; } = new(
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<string, int>(StringComparer.Ordinal));

        /// <param name="catalogJson">kural-motoru-v4.json metni.</param>
        /// <param name="orderText">Ejder_Glif_Sirasi.txt metni ("hücre\tönek_AD" satırları).</param>
        public static RuleEngineV4GlyphMap FromJson(string catalogJson, string orderText)
        {
            var verbs = new Dictionary<int, string>();
            var adjectives = new Dictionary<int, string>();
            if (!string.IsNullOrWhiteSpace(catalogJson))
            {
                JsonValue glyphs = MiniJson.Parse(catalogJson)["rune_glyphs"];
                ReadNames(glyphs["verbs"], verbs);
                ReadNames(glyphs["adjectives"], adjectives);
            }
            return new RuleEngineV4GlyphMap(verbs, adjectives, ParseOrder(orderText));
        }

        /// <summary>"0\tfiil_AK" satırları → önekli ad → hücre. Bozuk satır yok sayılır.</summary>
        public static IReadOnlyDictionary<string, int> ParseOrder(string orderText)
        {
            var order = new Dictionary<string, int>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(orderText))
                return order;
            foreach (string raw in orderText.Split('\n'))
            {
                string line = raw.Trim();
                int sep = line.IndexOf(RuleEngineV4GlyphDefaults.OrderSeparator);
                if (sep <= 0)
                    continue;
                if (!int.TryParse(line.Substring(0, sep), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cell))
                    continue;
                string name = line.Substring(sep + 1).Trim();
                if (name.Length > 0)
                    order[name] = cell;
            }
            return order;
        }

        public bool TryVerbName(int runeId, out string name) => _verbs.TryGetValue(runeId, out name!);

        public bool TryAdjectiveName(int runeId, out string name) => _adjectives.TryGetValue(runeId, out name!);

        public bool TryVerbCell(int runeId, out int cell) =>
            TryCell(_verbs, RuleEngineV4GlyphDefaults.VerbPrefix, runeId, out cell);

        public bool TryAdjectiveCell(int runeId, out int cell) =>
            TryCell(_adjectives, RuleEngineV4GlyphDefaults.AdjectivePrefix, runeId, out cell);

        bool TryCell(Dictionary<int, string> names, string prefix, int runeId, out int cell)
        {
            cell = 0;
            return names.TryGetValue(runeId, out string? name)
                && _order.TryGetValue(prefix + name, out cell);
        }

        static void ReadNames(JsonValue node, Dictionary<int, string> into)
        {
            foreach (KeyValuePair<string, JsonValue> kv in node.AsObject())
            {
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    continue;
                string name = kv.Value.AsString();
                if (name.Length > 0)
                    into[id] = name;
            }
        }
    }
}
