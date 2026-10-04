using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;

using Dovus.Core.Shared;
namespace IntegrationTests;

[TestFixture]
public class SkillDataIntegrationTests
{
    static string LoadDocsJson() => File.ReadAllText(RepoPaths.Docs("element-sistemi.json"));

    static string LoadMotionJson() => File.ReadAllText(RepoPaths.Docs("motion-templates.json"));

    [Test]
    public void SpecJson_MatchesRuntimeResources_ByteIdentical()
    {
        string docs = RepoPaths.Docs("element-sistemi.json");
        string runtime = Path.Combine(RepoPaths.ResourcesElementSystem, "element-sistemi.json");
        Assert.That(File.Exists(runtime), Is.True, runtime);
        Assert.That(File.ReadAllBytes(docs), Is.EqualTo(File.ReadAllBytes(runtime)));

        string motionDocs = RepoPaths.Docs("motion-templates.json");
        string motionRuntime = Path.Combine(RepoPaths.ResourcesElementSystem, "motion-templates.json");
        Assert.That(File.Exists(motionRuntime), Is.True, motionRuntime);
        Assert.That(File.ReadAllBytes(motionDocs), Is.EqualTo(File.ReadAllBytes(motionRuntime)));
    }

    [Test]
    public void All144Skills_ResolveWithJsonDefinedStringFields()
    {
        // CoreTests zaten Ã§Ã¶zÃ¼m / benzersiz id / motion 144 kontrolÃ¼nÃ¼ yapÄ±yor; burada motor Ã§Ä±ktÄ±sÄ±ndaki
        // string alanlarÄ±n JSON'da tanÄ±mlÄ± kÃ¼melere dÃ¼ÅŸtÃ¼ÄŸÃ¼nÃ¼ doÄŸruluyoruz (spec drift yakalama).
        SkillMotor motor = SkillMotor.FromJson(LoadDocsJson());
        MotionTemplateCatalog motion = MotionTemplateCatalog.FromJson(LoadMotionJson());
        JsonLexicon lex = JsonLexicon.FromFile(RepoPaths.Docs("element-sistemi.json"));

        var skillIds = new HashSet<string>(StringComparer.Ordinal);
        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            SkillResolution skill = motor.Resolve(new[] { verb, adjective });
            string id = $"{verb}-{adjective}";
            Assert.That(skill.IsComplete, Is.True, id);
            Assert.That(skill.SkillId, Is.EqualTo(id), id);
            Assert.That(skillIds.Add(skill.SkillId), Is.True, $"duplicate SkillId {skill.SkillId}");

            Assert.That(skill.VerbId, Is.Not.Empty, $"{id} VerbId");
            Assert.That(skill.VerbName, Is.Not.Empty, $"{id} VerbName");
            Assert.That(skill.Hitbox, Is.Not.Empty, $"{id} Hitbox");
            Assert.That(motion.TryGet(id, out _), Is.True, $"{id} motion");

            if (!string.IsNullOrEmpty(skill.ElementId))
                lex.AssertInSet(skill.ElementId, JsonLexicon.ElementIds, $"{id} ElementId");
            lex.AssertInSet(skill.VerbId, JsonLexicon.VerbIds, $"{id} VerbId");
            lex.AssertInSet(skill.AdjectiveId, JsonLexicon.AdjectiveIds, $"{id} AdjectiveId");
            lex.AssertInSet(skill.Hitbox, JsonLexicon.Hitboxes, $"{id} Hitbox");
            lex.AssertInSet(skill.Action, JsonLexicon.Actions, $"{id} Action");
            lex.AssertInSet(skill.VerbFamily, JsonLexicon.Families, $"{id} VerbFamily");
            lex.AssertInSet(skill.TargetMode, JsonLexicon.TargetModes, $"{id} TargetMode");
            if (!string.IsNullOrEmpty(skill.CastMobility))
                lex.AssertInSet(skill.CastMobility, JsonLexicon.CastMobilities, $"{id} CastMobility");
            if (!string.IsNullOrEmpty(skill.DamageType))
                lex.AssertInSet(skill.DamageType, JsonLexicon.DamageTypes, $"{id} DamageType");
            if (!string.IsNullOrEmpty(skill.SilhouetteAxis))
                lex.AssertInSet(skill.SilhouetteAxis, JsonLexicon.SilhouetteAxes, $"{id} SilhouetteAxis");
            foreach (var kv in skill.TargetBehaviors)
                lex.AssertInSet(kv.Value, JsonLexicon.TargetBehaviorValues, $"{id} target_behavior {kv.Key}");
        }

        Assert.That(skillIds.Count, Is.EqualTo(144));
    }

    /// <summary>JSON'dan tanÄ±mlÄ± string kÃ¼meleleri â€” yalnÄ±zca integration drift testi iÃ§in.</summary>
    sealed class JsonLexicon
    {
        public static readonly HashSet<string> ElementIds = new(StringComparer.Ordinal);
        public static readonly HashSet<string> VerbIds = new(StringComparer.Ordinal);
        public static readonly HashSet<string> AdjectiveIds = new(StringComparer.Ordinal);
        public static readonly HashSet<string> Hitboxes = new(StringComparer.Ordinal);
        public static readonly HashSet<string> Actions = new(StringComparer.Ordinal);
        public static readonly HashSet<string> Families = new(StringComparer.Ordinal);
        public static readonly HashSet<string> TargetModes = new(StringComparer.Ordinal);
        public static readonly HashSet<string> CastMobilities = new(StringComparer.Ordinal);
        public static readonly HashSet<string> DamageTypes = new(StringComparer.Ordinal);
        public static readonly HashSet<string> SilhouetteAxes = new(StringComparer.Ordinal);
        public static readonly HashSet<string> TargetBehaviorValues = new(StringComparer.Ordinal);

        static readonly string[] TrackedKeys =
        {
            "hitbox", "action", "cast_mobility", "family", "target_mode", "damage_type"
        };

        public static JsonLexicon FromFile(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Walk(doc.RootElement);
            for (int i = 1; i <= 12; i++)
            {
                VerbIds.Add(i.ToString());
                AdjectiveIds.Add(i.ToString());
            }

            foreach (var el in doc.RootElement.GetProperty("elements").EnumerateArray())
            {
                if (el.TryGetProperty("id", out var idNum) && idNum.ValueKind == JsonValueKind.Number)
                    ElementIds.Add(idNum.GetInt32().ToString());
            }

            // Motor varsayÄ±lanlarÄ± JSON'da her skill satÄ±rÄ±nda tekrarlanmÄ±yor (SkillMotor verb fallback).
            CastMobilities.Add("free_move");
            foreach (var kv in doc.RootElement.GetProperty("adjective_mods").EnumerateObject())
            {
                if (kv.Value.TryGetProperty("label", out var label) && label.ValueKind == JsonValueKind.String)
                    SilhouetteAxes.Add(label.GetString());
            }

            foreach (var rune in doc.RootElement.GetProperty("runes").EnumerateArray())
            {
                if (rune.TryGetProperty("category", out var cat) && cat.ValueKind == JsonValueKind.String)
                    SilhouetteAxes.Add(cat.GetString());
            }

            // state_machine capability deÄŸerleri (target behavior ile aynÄ± sÃ¶zlÃ¼k ailesi).
            Walk(doc.RootElement.GetProperty("state_machine"));

            return new JsonLexicon();
        }

        static void Walk(JsonElement node)
        {
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in node.EnumerateObject())
                    {
                        if (prop.NameEquals("family") && prop.Value.ValueKind == JsonValueKind.String)
                            Families.Add(prop.Value.GetString());
                        if (prop.NameEquals("target_mode") && prop.Value.ValueKind == JsonValueKind.String)
                            TargetModes.Add(prop.Value.GetString());
                        if (Array.IndexOf(TrackedKeys, prop.Name) >= 0 && prop.Value.ValueKind == JsonValueKind.String)
                        {
                            string v = prop.Value.GetString();
                            if (prop.NameEquals("hitbox")) Hitboxes.Add(v);
                            else if (prop.NameEquals("action")) Actions.Add(v);
                            else if (prop.NameEquals("cast_mobility")) CastMobilities.Add(v);
                            else if (prop.NameEquals("damage_type")) DamageTypes.Add(v);
                        }

                        if (prop.Name is "can_draw" or "can_move" or "can_dodge" or "can_cast"
                            && prop.Value.ValueKind == JsonValueKind.String)
                            TargetBehaviorValues.Add(prop.Value.GetString());

                        if (prop.NameEquals("silhouette_axis") && prop.Value.ValueKind == JsonValueKind.String)
                            SilhouetteAxes.Add(prop.Value.GetString());

                        Walk(prop.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in node.EnumerateArray())
                        Walk(item);
                    break;
            }
        }

        public void AssertInSet(string value, HashSet<string> set, string context)
        {
            Assert.That(set.Contains(value), Is.True, $"{context}: '{value}' JSON kÃ¼mesinde yok");
        }
    }
}
