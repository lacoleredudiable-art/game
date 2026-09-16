using System.Collections.Generic;
using System.IO;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Görev 14 — SkillResolution.Hitbox / AnimationType ↔ PresentationCatalog.
/// Trajectory doğrulanmaz.
/// </summary>
[TestFixture]
public class PresentationValidatorTests
{
    static string FindDocsFile(string fileName)
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", fileName));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", fileName));
        }
        Assert.That(File.Exists(path), Is.True, $"{fileName} bulunamadı: {path}");
        return path;
    }

    static PresentationCatalog LoadPresentation() =>
        PresentationCatalog.FromJson(File.ReadAllText(FindDocsFile("prezentasyon-katmani.json")));

    static string LoadElementJson() => File.ReadAllText(FindDocsFile("element-sistemi.json"));

    static SkillResolution ResolutionWith(string hitbox, string animationType) =>
        new SkillResolution(
            elementId: "",
            elementName: "",
            displayName: "test",
            skillId: "",
            skillJob: "",
            verbId: "",
            verbName: "",
            verbFamily: "",
            action: "",
            baseDamage: 0f,
            basePoise: 0f,
            hitbox: hitbox,
            castMobility: "",
            mechanics: System.Array.Empty<string>(),
            adjectiveId: "",
            adjectiveName: "",
            silhouetteAxis: "",
            damageMult: 1f,
            hitboxScaleMult: 1f,
            poiseDamageMult: 1f,
            length: 1,
            lengthRole: "",
            lengthCastMult: 1f,
            lengthMobility: "",
            flavorElement: "",
            animationType: animationType);

    [Test]
    public void Validate_KnownProjectileAndCastProjectile_IsValid()
    {
        var validator = new PresentationValidator(LoadPresentation());
        PresentationValidationResult r = validator.Validate(
            ResolutionWith("projectile", "cast_projectile"));
        Assert.That(r.HitboxFound, Is.True);
        Assert.That(r.AnimationFound, Is.True);
        Assert.That(r.IsValid, Is.True);
    }

    [Test]
    public void Validate_MissingHitbox_TargetAlly_NotFound_DoesNotThrow()
    {
        var validator = new PresentationValidator(LoadPresentation());
        PresentationValidationResult r = validator.Validate(
            ResolutionWith("target_ally", "cast_self"));
        Assert.That(r.HitboxFound, Is.False);
        Assert.That(r.AnimationFound, Is.True);
        Assert.That(r.IsValid, Is.False);
        Assert.That(r.HitboxId, Is.EqualTo("target_ally"));
    }

    [Test]
    public void AllVerbBaseHitboxes_CheckedAgainstPresentationCatalog()
    {
        string elementJson = LoadElementJson();
        var motor = SkillMotor.FromJson(elementJson);
        var catalog = LoadPresentation();
        var validator = new PresentationValidator(catalog);

        JsonValue root = MiniJson.Parse(elementJson);
        IReadOnlyList<JsonValue> verbs = root["verbs"].AsArray();
        Assert.That(verbs.Count, Is.EqualTo(motor.VerbCount));

        var missingHitboxIds = new SortedSet<string>(System.StringComparer.Ordinal);
        var missingHitboxVerbs = new List<string>();
        var missingAnimIds = new SortedSet<string>(System.StringComparer.Ordinal);
        int checkedCount = 0;

        foreach (JsonValue verbRow in verbs)
        {
            string verbId = verbRow["id"].AsString();
            Assert.That(motor.TryGetVerb(verbId, out VerbNode verb), Is.True, verbId);

            string baseHitbox = verbRow["engine_base_stats"]["base_hitbox"].AsString();
            string animationType = verbRow["animation_type"].AsString();
            Assert.That(verb.Hitbox, Is.EqualTo(baseHitbox), verbId);
            Assert.That(verb.AnimationType, Is.EqualTo(animationType), verbId);

            PresentationValidationResult result = validator.Validate(
                ResolutionWith(baseHitbox, animationType));
            checkedCount++;

            if (!result.HitboxFound)
            {
                missingHitboxIds.Add(baseHitbox);
                missingHitboxVerbs.Add($"{verbId}:{baseHitbox}");
            }

            if (!result.AnimationFound)
                missingAnimIds.Add(animationType);
        }

        Assert.That(checkedCount, Is.EqualTo(42), "Tüm fiiller tek tek doğrulanmalı.");
        // docs/durum.md: SkillResolution.Hitbox "target_ally" prezentasyon katmanında yok
        // (cc_arindirma, hiz_buff, kalkan_transferi, arindirma, kutsal_kalkan, dirilis).
        Assert.That(missingHitboxIds, Is.EqualTo(new[] { "target_ally" }));
        Assert.That(missingHitboxVerbs.Count, Is.EqualTo(6));
        Assert.That(missingAnimIds, Is.Empty);
    }
}
