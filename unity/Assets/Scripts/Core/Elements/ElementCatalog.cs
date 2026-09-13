using System;
using System.Collections.Generic;

namespace Dovus.Core.Elements
{
    /// <summary>Kilitli element kataloğu — saf veri, JSON bağımlılığı yok.</summary>
    public sealed class ElementCatalog
    {
        readonly Dictionary<string, ElementNode> _elements =
            new Dictionary<string, ElementNode>(StringComparer.Ordinal);
        readonly Dictionary<string, VerbStats> _verbs =
            new Dictionary<string, VerbStats>(StringComparer.Ordinal);
        readonly Dictionary<string, AdjectiveMods> _adjectives =
            new Dictionary<string, AdjectiveMods>(StringComparer.Ordinal);
        readonly Dictionary<int, LengthEconomy> _lengths = new Dictionary<int, LengthEconomy>();

        public ElementCatalog(
            IEnumerable<ElementNode> elements,
            IEnumerable<VerbStats> verbs,
            IEnumerable<AdjectiveMods> adjectives,
            IEnumerable<LengthEconomy> lengths,
            string version,
            bool locked)
        {
            foreach (var e in elements)
                _elements[e.Id] = e;
            foreach (var v in verbs)
                _verbs[v.Id] = v;
            foreach (var a in adjectives)
                _adjectives[a.Id] = a;
            foreach (var L in lengths)
                _lengths[L.Length] = L;

            Version = version ?? "";
            Locked = locked;
        }

        public string Version { get; }
        public bool Locked { get; }
        public int ElementCount => _elements.Count;
        public int VerbCount => _verbs.Count;
        public int AdjectiveCount => _adjectives.Count;

        public static string CompoundId(int a, int b) => a.ToString() + "-" + b.ToString();

        public bool TryGetElement(string id, out ElementNode element) =>
            _elements.TryGetValue(id, out element);

        public bool TryGetVerb(string id, out VerbStats verb) =>
            _verbs.TryGetValue(id, out verb);

        public bool TryGetAdjective(string id, out AdjectiveMods adjective) =>
            _adjectives.TryGetValue(id, out adjective);

        public bool TryGetLength(int length, out LengthEconomy economy) =>
            _lengths.TryGetValue(length, out economy);

        public bool TryGetCore(int coreId, out ElementNode core)
        {
            core = default;
            if (coreId < 1 || coreId > 6)
                return false;
            return TryGetElement(coreId.ToString(), out core) && core.IsCore;
        }
    }
}
