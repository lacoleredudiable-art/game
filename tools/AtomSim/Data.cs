using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AtomSim;

sealed record Weapon(int Id, string Name, string Type, double DamageMult, double RangeMult, int[] CompatibleVerbs, string Path);
sealed record VerbHitbox(string Shape, double SizeA, double SizeB, bool Timed);

/// <summary>element-sistemi.json (bağlayıcı sayılar) + atom-grammar-taslak.json (kurallar ve önerilen parametreler).</summary>
sealed class Data
{
    public readonly JsonElement Es;
    public readonly JsonElement Rules;
    public readonly Dictionary<int, string> VerbNames = new();
    public readonly Dictionary<int, string> AdjectiveNames = new();
    public readonly Dictionary<int, string> AdjectiveOps = new();
    public readonly List<Weapon> Weapons = new();

    Data(JsonElement es, JsonElement rules)
    {
        Es = es;
        Rules = rules;
        foreach (JsonElement r in es.GetProperty("runes").EnumerateArray())
        {
            int id = r.GetProperty("id").GetInt32();
            VerbNames[id] = r.GetProperty("verb_face").GetString() ?? "";
            AdjectiveNames[id] = r.GetProperty("adjective_prefix").GetString() ?? "";
        }
        foreach (JsonProperty p in rules.GetProperty("adjective_rules").EnumerateObject())
            AdjectiveOps[int.Parse(p.Name)] = p.Value.GetProperty("op").GetString() ?? "";

        JsonElement delivery = rules.GetProperty("weapon_delivery");
        foreach (JsonElement w in es.GetProperty("weapons").EnumerateArray())
        {
            int id = w.GetProperty("id").GetInt32();
            Weapons.Add(new Weapon(
                id,
                w.GetProperty("name").GetString() ?? "",
                w.GetProperty("type").GetString() ?? "",
                w.GetProperty("damage_mult").GetDouble(),
                w.GetProperty("range_mult").GetDouble(),
                w.GetProperty("compatible_verbs").EnumerateArray().Select(x => x.GetInt32()).ToArray(),
                delivery.GetProperty(id.ToString(CultureInfo.InvariantCulture)).GetProperty("path").GetString() ?? ""));
        }
    }

    public static Data Load(string esPath, string rulesPath)
    {
        var es = JsonDocument.Parse(File.ReadAllText(esPath)).RootElement;
        var rules = JsonDocument.Parse(File.ReadAllText(rulesPath)).RootElement;
        return new Data(es, rules);
    }

    public double VerbNum(int verb, string key, double fallback = 0) =>
        Num(Es.GetProperty("verb_base"), verb, key, fallback);

    public double AdjNum(int adj, string key, double fallback = 0) =>
        Num(Es.GetProperty("adjective_mods"), adj, key, fallback);

    public bool AdjHas(int adj, string key) =>
        Es.GetProperty("adjective_mods").TryGetProperty(adj.ToString(CultureInfo.InvariantCulture), out JsonElement o)
        && o.TryGetProperty(key, out _);

    public double Param(string key) => Rules.GetProperty("params").GetProperty(key).GetDouble();

    /// <summary>Düşmanda karşılığı olmayan stat (ör. boss'ta kalkan yok) yerine kullanılan stat.</summary>
    public string EnemyStat(string stat) =>
        Rules.GetProperty("enemy_stat_fallback").TryGetProperty(stat, out JsonElement s) ? s.GetString() ?? stat : stat;

    public JsonElement VerbAtoms(int verb) =>
        Rules.GetProperty("verb_atoms").GetProperty(verb.ToString(CultureInfo.InvariantCulture));

    public VerbHitbox Hitbox(int verb)
    {
        JsonElement h = Es.GetProperty("hitbox_vfx").GetProperty("fiil_hitbox")
            .GetProperty(verb.ToString(CultureInfo.InvariantCulture));
        string size = h.GetProperty("base_size").GetString() ?? "";
        MatchCollection m = Regex.Matches(size, @"\d+(?:[.,]\d+)?");
        double a = m.Count > 0 ? ParseD(m[0].Value) : 1;
        double b = m.Count > 1 ? ParseD(m[1].Value) : 0;
        string duration = h.GetProperty("duration").GetString() ?? "";
        return new VerbHitbox(h.GetProperty("shape").GetString() ?? "", a, b, duration.Contains("süreli"));
    }

    static double Num(JsonElement table, int id, string key, double fallback)
    {
        if (!table.TryGetProperty(id.ToString(CultureInfo.InvariantCulture), out JsonElement o)
            || !o.TryGetProperty(key, out JsonElement v))
            return fallback;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDouble(),
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => fallback
        };
    }

    static double ParseD(string s) => double.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture);
}
