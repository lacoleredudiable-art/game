using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

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

    static string LoadElementJson() =>
        File.ReadAllText(FindDocsFile("element-sistemi.json"));

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
    public void AllV6VerbBaseHitboxes_CheckedAgainstPresentationCatalog()
    {
        // v6.1.1 verb_base: 12 fiil. Motorun fiil hitbox'ı JSON ile birebir; prezentasyon
        // kataloğunda bulunmayan hitbox id'leri açıkça listelenir (runtime CastPresentation
        // bunları varsayılan animasyona düşürür).
        string elementJson = LoadElementJson();
        var motor = SkillMotor.FromJson(elementJson);
        var validator = new PresentationValidator(LoadPresentation());

        JsonValue verbBase = MiniJson.Parse(elementJson)["verb_base"];
        var missingHitboxIds = new SortedSet<string>(System.StringComparer.Ordinal);
        int checkedCount = 0;

        foreach (var kv in verbBase.AsObject())
        {
            Assert.That(motor.TryGetVerb(kv.Key, out VerbNode verb), Is.True, kv.Key);
            string hitbox = kv.Value["hitbox"].AsString();
            Assert.That(verb.Hitbox, Is.EqualTo(hitbox), kv.Key);

            PresentationValidationResult result = validator.Validate(ResolutionWith(hitbox, ""));
            checkedCount++;
            if (!result.HitboxFound)
                missingHitboxIds.Add(hitbox);
        }

        Assert.That(checkedCount, Is.EqualTo(motor.RuneCount), "Tüm v6 fiilleri tek tek doğrulanmalı.");
        // Bilinen katalog boşlukları (v6 fiil hitbox id'leri presentation kataloğunda yok) — yeni boşluk eklenirse test kırılır.
        Assert.That(missingHitboxIds, Is.EqualTo(new[] { "dash_line", "self_or_ally", "target" }));
    }
}
