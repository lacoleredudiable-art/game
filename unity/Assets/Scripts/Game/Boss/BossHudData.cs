using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Boss HUD metinleri <c>Resources/Bosses/*.json</c>: ad, alt başlık, faz adları ve
    /// eşikleri, saldırı adları. Saldırı kind'ı JSON'dan veya karadul uyumu için id'den eşlenir.
    /// </summary>
    public sealed class BossHudData
    {
        public string Name { get; private set; } = "BOSS";
        public string Subtitle { get; private set; } = string.Empty;

        readonly List<(int Phase, string Name, float UpperFrac)> _phases = new();
        readonly Dictionary<string, string> _attackNames = new();
        readonly Dictionary<BossAttackKind, string> _attackNamesByKind = new();

        public IReadOnlyList<(int Phase, string Name, float UpperFrac)> Phases => _phases;

        static readonly CultureInfo Tr = new("tr-TR");

        public static BossHudData Load(string resourcePath = "Bosses/karadul")
        {
            var data = new BossHudData();
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return data;
            try
            {
                JsonValue root = MiniJson.Parse(asset.text);
                data.Name = root["name"].AsString(data.Name);
                data.Subtitle = root["subtitle"].AsString(string.Empty);
                foreach (JsonValue p in root["vitals"]["phases"].AsArray())
                {
                    IReadOnlyList<JsonValue> range = p["range"].AsArray();
                    float upper = range.Count > 0 ? range[0].AsFloat(100f) / 100f : 1f;
                    data._phases.Add((p["phase"].AsInt(), p["name"].AsString(string.Empty), upper));
                }
                foreach (JsonValue a in root["attacks"].AsArray())
                {
                    string id = a["id"].AsString(string.Empty);
                    string display = a["name"].AsString(string.Empty);
                    data._attackNames[id] = display;
                    BossAttackKind? kind = BossEncounterData.ResolveKind(a);
                    if (kind.HasValue && !string.IsNullOrEmpty(display))
                        data._attackNamesByKind[kind.Value] = display;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossHudData] {resourcePath} okunamadı: {e.Message}");
            }
            return data;
        }

        public string Upper(string s) => string.IsNullOrEmpty(s) ? string.Empty : s.ToUpper(Tr);

        public string PhaseName(int phase)
        {
            foreach (var p in _phases)
                if (p.Phase == phase)
                    return p.Name;
            return string.Empty;
        }

        public string AttackName(BossAttackKind kind)
        {
            if (_attackNamesByKind.TryGetValue(kind, out string byKind) && !string.IsNullOrEmpty(byKind))
                return byKind;
            string id = SnakeCase(kind.ToString());
            return _attackNames.TryGetValue(id, out string name) && !string.IsNullOrEmpty(name)
                ? name
                : kind.ToString();
        }

        static string SnakeCase(string pascal)
        {
            var sb = new StringBuilder(pascal.Length + 4);
            for (int i = 0; i < pascal.Length; i++)
            {
                char c = pascal[i];
                if (char.IsUpper(c) && i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
