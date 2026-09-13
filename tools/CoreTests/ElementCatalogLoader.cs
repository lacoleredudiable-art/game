using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dovus.Core.Elements;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Test helper: kilitli docs/element-sistemi.json → ElementCatalog.</summary>
public static class ElementCatalogLoader
{
    public static string FindSpecPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "element-sistemi.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("docs/element-sistemi.json bulunamadı.");
    }

    public static ElementCatalog LoadFromRepo()
    {
        return LoadFromJson(File.ReadAllText(FindSpecPath()));
    }

    public static ElementCatalog LoadFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var system = root.GetProperty("system");
        string version = system.GetProperty("version").GetString() ?? "";
        bool locked = system.TryGetProperty("locked", out var lk) && lk.GetBoolean();

        var verbs = new List<VerbStats>();
        foreach (var v in root.GetProperty("verbs").EnumerateArray())
        {
            var s = v.GetProperty("engine_base_stats");
            verbs.Add(new VerbStats(
                v.GetProperty("id").GetString()!,
                v.GetProperty("name").GetString()!,
                v.GetProperty("family").GetString()!,
                s.GetProperty("action").GetString()!,
                s.GetProperty("deals_base_damage").GetBoolean(),
                (float)s.GetProperty("base_damage_value").GetDouble(),
                (float)s.GetProperty("base_poise_damage").GetDouble(),
                s.GetProperty("base_hitbox").GetString()!,
                ParseMobility(s.GetProperty("cast_mobility").GetString()!)));
        }

        var adjectives = new List<AdjectiveMods>();
        foreach (var a in root.GetProperty("adjectives").EnumerateArray())
        {
            var m = a.GetProperty("engine_modifiers");
            adjectives.Add(new AdjectiveMods(
                a.GetProperty("id").GetString()!,
                a.GetProperty("name").GetString()!,
                a.GetProperty("category").GetString()!,
                a.TryGetProperty("silhouette_axis", out var ax) ? (ax.GetString() ?? "none") : "none",
                GetFloat(m, "damage_mult", 1f),
                GetFloat(m, "poise_damage_mult", 1f),
                GetFloat(m, "hitbox_scale_mult", 1f)));
        }

        var elements = new List<ElementNode>();
        foreach (var e in root.GetProperty("elements").EnumerateArray())
        {
            bool isCore = e.GetProperty("type").GetString() == "core";
            elements.Add(new ElementNode(
                e.GetProperty("id").GetString()!,
                e.GetProperty("name").GetString()!,
                isCore,
                e.GetProperty("group").GetString()!,
                e.GetProperty("verb_id").GetString()!,
                e.GetProperty("adjective_id").GetString()!,
                GetStringOrNull(e, "skill_id"),
                GetStringOrNull(e, "skill_name"),
                GetStringOrNull(e, "skill_job"),
                GetStringOrNull(e, "verb_unlocks_job")));
        }

        var lengths = new List<LengthEconomy>();
        foreach (var prop in root.GetProperty("scaling_economy").GetProperty("lengths").EnumerateObject())
        {
            var L = prop.Value;
            lengths.Add(new LengthEconomy(
                int.Parse(prop.Name),
                L.GetProperty("role").GetString()!,
                (float)L.GetProperty("cast_time_mult").GetDouble(),
                (float)L.GetProperty("resource_cost_mult").GetDouble(),
                (float)L.GetProperty("damage_mult").GetDouble(),
                (float)L.GetProperty("poise_damage_mult").GetDouble(),
                ParseMobility(L.GetProperty("mobility").GetString()!)));
        }

        return new ElementCatalog(elements, verbs, adjectives, lengths, version, locked);
    }

    static string? GetStringOrNull(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) ? p.GetString() : null;

    static float GetFloat(JsonElement obj, string name, float fallback) =>
        obj.TryGetProperty(name, out var el) ? (float)el.GetDouble() : fallback;

    static CastMobility ParseMobility(string value) => value switch
    {
        "free_move" => CastMobility.FreeMove,
        "slowed_move" => CastMobility.SlowedMove,
        "rooted" => CastMobility.Rooted,
        _ => throw new InvalidOperationException("Bilinmeyen mobility: " + value)
    };
}
