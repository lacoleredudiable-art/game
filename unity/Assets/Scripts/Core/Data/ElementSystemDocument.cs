using System;
using Dovus.Core.Grammar;

namespace Dovus.Core.Data
{
    /// <summary>
    /// element-sistemi.json kökü — tek MiniJson.Parse; tüketiciler <see cref="Root"/> üzerinden okur.
    /// </summary>
    public sealed class ElementSystemDocument
    {
        ElementSystemDocument(JsonValue root, string rawJson)
        {
            Root = root;
            RawJson = rawJson ?? string.Empty;
        }

        public JsonValue Root { get; }

        /// <summary>Orijinal metin (Editor/araçlar); parse tekrarı gerekmez.</summary>
        public string RawJson { get; }

        public static ElementSystemDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON boş.", nameof(json));
            JsonValue root = MiniJson.Parse(json);
            if (root.IsNull)
                throw new ArgumentException("JSON kökü okunamadı.", nameof(json));
            return new ElementSystemDocument(root, json);
        }
    }
}
