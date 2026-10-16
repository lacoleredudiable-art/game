using Dovus.Core.Shared;

namespace Dovus.Core.Data
{
    /// <summary>SkillMotor / EquipmentCatalog gramer JSON kökü — tek MiniJson.Parse.</summary>
    public sealed class GrammarJsonDocument
    {
        GrammarJsonDocument(JsonValue root, string rawJson)
        {
            Root = root;
            RawJson = rawJson ?? string.Empty;
        }

        public JsonValue Root { get; }

        /// <summary>Orijinal metin (Editor/araçlar); parse tekrarı gerekmez.</summary>
        public string RawJson { get; }

        public static GrammarJsonDocument Parse(string json)
        {
            return new GrammarJsonDocument(MiniJson.Parse(json), json);
        }

        public static GrammarJsonDocument FromRoot(JsonValue root) =>
            new GrammarJsonDocument(root, string.Empty);
    }
}
