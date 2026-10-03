using Dovus.Core.Grammar;

namespace Dovus.Core.Data
{
    public readonly struct ElementSystemHeader
    {
        public ElementSystemHeader(string version, bool binding, int selectionTransitionMs)
        {
            Version = version ?? string.Empty;
            Binding = binding;
            SelectionTransitionMs = selectionTransitionMs;
        }

        public string Version { get; }
        public bool Binding { get; }
        public int SelectionTransitionMs { get; }

        public static bool TryParse(string json, int selectionTransitionMsFallback, out ElementSystemHeader header)
        {
            header = default;
            if (string.IsNullOrWhiteSpace(json))
                return false;
            JsonValue root = MiniJson.Parse(json);
            if (root.IsNull)
                return false;
            string version = root["system"]["version"].AsString();
            bool binding = root["system"]["binding"].AsBool(false);
            int transitionMs = root["element_system"]["selection"]["transition_time_ms"]
                .AsInt(selectionTransitionMsFallback);
            header = new ElementSystemHeader(version, binding, transitionMs);
            return true;
        }
    }
}
