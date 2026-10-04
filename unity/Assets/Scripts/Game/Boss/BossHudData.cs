using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Boss HUD metinleri <c>Resources/Bosses/*.json</c>: ad, alt başlık, faz adları ve
    /// eşikleri, saldırı adları.
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
                if (!BossEncounterMapper.TryParseHud(asset.text, out BossHudSnapshot snap))
                    return data;
                data.Name = snap.Name;
                data.Subtitle = snap.Subtitle;
                data._phases.AddRange(snap.Phases);
                foreach (KeyValuePair<string, string> kv in snap.AttackNamesById)
                    data._attackNames[kv.Key] = kv.Value;
                foreach (KeyValuePair<BossAttackKind, string> kv in snap.AttackNamesByKind)
                    data._attackNamesByKind[kv.Key] = kv.Value;
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
