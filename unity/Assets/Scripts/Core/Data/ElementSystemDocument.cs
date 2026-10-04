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
            // Fırlatmaz: boş/geçersiz metin Null kök verir (MiniJson.Parse gibi); her tüketici eski string
            // API'sindeki kuralı aynen uygular (fırlat / varsayılan / false) → hata yolu davranışı aynı.
            return new ElementSystemDocument(MiniJson.Parse(json), json);
        }
    }
}
