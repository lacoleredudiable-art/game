using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dovus.Core.Grammar;

namespace Dovus.Core.Mechanic
{
    public sealed class MechanicWeapon
    {
        public MechanicWeapon(int id, string name, string type, double damageMult, double rangeMult, int[] compatibleVerbs, string path)
        {
            Id = id;
            Name = name ?? string.Empty;
            Type = type ?? string.Empty;
            DamageMult = damageMult;
            RangeMult = rangeMult;
            CompatibleVerbs = compatibleVerbs ?? Array.Empty<int>();
            Path = path ?? string.Empty;
        }

        public int Id { get; }
        public string Name { get; }
        public string Type { get; }
        public double DamageMult { get; }
        public double RangeMult { get; }
        public int[] CompatibleVerbs { get; }
        /// <summary>mechanic_grammar.weapon_delivery.path — silahın teslim yolu.</summary>
        public string Path { get; }
    }

    public readonly struct MechanicHitbox
    {
        public MechanicHitbox(string shape, double sizeA, double sizeB, bool timed)
        {
            Shape = shape ?? string.Empty;
            SizeA = sizeA;
            SizeB = sizeB;
            Timed = timed;
        }

        public string Shape { get; }
        public double SizeA { get; }
        public double SizeB { get; }
        public bool Timed { get; }
    }

    /// <summary>
    /// element-sistemi.json: sayılar (verb_base, adjective_mods, weapons, hitbox_vfx, uyumsuz_cizim)
    /// ve kurallar (mechanic_grammar). Motor yalnız buradan okur; sayı gömülmez.
    /// </summary>
    public sealed class MechanicRules
    {
        static readonly Regex Number = new Regex(@"\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant);

        readonly JsonValue _root;
        readonly JsonValue _grammar;
        readonly Dictionary<int, string> _verbNames = new Dictionary<int, string>();
        readonly Dictionary<int, string> _adjectiveNames = new Dictionary<int, string>();
        readonly Dictionary<int, string> _adjectiveOps = new Dictionary<int, string>();
        readonly List<MechanicWeapon> _weapons = new List<MechanicWeapon>();

        MechanicRules(JsonValue root)
        {
            _root = root;
            _grammar = root["mechanic_grammar"];
            foreach (JsonValue r in root["runes"].AsArray())
            {
                int id = r["id"].AsInt();
                _verbNames[id] = r["verb_face"].AsString();
                _adjectiveNames[id] = r["adjective_prefix"].AsString();
            }
            foreach (KeyValuePair<string, JsonValue> kv in _grammar["adjective_rules"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    _adjectiveOps[id] = kv.Value["op"].AsString();

            JsonValue delivery = _grammar["weapon_delivery"];
            foreach (JsonValue w in root["weapons"].AsArray())
            {
                int id = w["id"].AsInt();
                _weapons.Add(new MechanicWeapon(
                    id,
                    w["name"].AsString(),
                    w["type"].AsString(),
                    w["damage_mult"].AsDouble(1),
                    w["range_mult"].AsDouble(1),
                    w["compatible_verbs"].AsArray().Select(x => x.AsInt()).ToArray(),
                    delivery[Key(id)]["path"].AsString()));
            }
        }

        public static MechanicRules FromJson(string json) => new MechanicRules(MiniJson.Parse(json));
        public static MechanicRules FromJsonRoot(JsonValue root) => new MechanicRules(root);

        public bool IsValid => !_grammar.IsNull && _adjectiveOps.Count == 12 && _weapons.Count > 0
            && _weapons.All(w => w.Path.Length > 0);

        public IReadOnlyList<MechanicWeapon> Weapons => _weapons;

        public MechanicWeapon Weapon(int id) => _weapons.FirstOrDefault(w => w.Id == id);

        public string VerbName(int id) => _verbNames.TryGetValue(id, out string n) ? n : id.ToString(CultureInfo.InvariantCulture);
        public string AdjectiveName(int id) => _adjectiveNames.TryGetValue(id, out string n) ? n : string.Empty;
        public string AdjectiveOp(int id) => _adjectiveOps.TryGetValue(id, out string op) ? op : string.Empty;

        public double VerbNum(int verb, string key, double fallback = 0) =>
            Num(_root["verb_base"][Key(verb)][key], fallback);

        public double AdjNum(int adjective, string key, double fallback = 0) =>
            Num(_root["adjective_mods"][Key(adjective)][key], fallback);

        public bool AdjHas(int adjective, string key) => _root["adjective_mods"][Key(adjective)].Has(key);

        public double Param(string key) => _grammar["params"][key].AsDouble();

        public JsonValue VerbAtoms(int verb) => _grammar["verb_atoms"][Key(verb)];

        /// <summary>Düşmanda karşılığı olmayan stat (ör. boss'ta kalkan yok) yerine kullanılan stat.</summary>
        public string EnemyStat(string stat)
        {
            string s = _grammar["enemy_stat_fallback"][stat].AsString();
            return string.IsNullOrEmpty(s) ? stat : s;
        }

        /// <summary>uyumsuz_cizim.effects.uyumsuz — uyumsuz silahta etki ve cast süresi çarpanı.</summary>
        public double IncompatibleEffectMult => _root["uyumsuz_cizim"]["effects"]["uyumsuz"]["damage_mult"].AsDouble(1);
        public double IncompatibleCastTimeMult => _root["uyumsuz_cizim"]["effects"]["uyumsuz"]["cast_time_mult"].AsDouble(1);

        public MechanicHitbox Hitbox(int verb)
        {
            JsonValue h = _root["hitbox_vfx"]["fiil_hitbox"][Key(verb)];
            MatchCollection m = Number.Matches(h["base_size"].AsString());
            double a = m.Count > 0 ? ParseD(m[0].Value) : 1;
            double b = m.Count > 1 ? ParseD(m[1].Value) : 0;
            return new MechanicHitbox(h["shape"].AsString(), a, b, h["duration"].AsString().Contains("süreli"));
        }

        static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);

        static double Num(JsonValue v, double fallback)
        {
            switch (v.Kind)
            {
                case JsonKind.Number: return v.AsDouble();
                case JsonKind.Bool: return v.AsBool() ? 1 : 0;
                default: return fallback;
            }
        }

        static double ParseD(string s) => double.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture);
    }
}
