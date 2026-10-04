using Dovus.Core.Casting;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Shared;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class CatalogRepositoryTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string ElementJson() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", "element-sistemi.json"));

    static string MotionJson() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", "motion-templates.json"));

    [Test]
    public void SkillNumberCatalog_MatchesParserOnlyPath()
    {
        JsonValue root = ElementSystemDocument.Parse(ElementJson()).Root;
        var catalog = SkillNumberCatalog.FromJsonRoot(root);
        var parserOnly = SkillNumberParser.Parse(root);

        Assert.That(parserOnly.MaxMana, Is.EqualTo(catalog.MaxMana));
        Assert.That(parserOnly.GlobalCooldownSec, Is.EqualTo(catalog.GlobalCooldownSec));
        for (int verb = 1; verb <= 12; verb++)
        {
            catalog.TryGetVerb(verb, out float d1, out float c1, out float m1, out float dur1, out float r1, out float rad1);
            parserOnly.TryGetVerb(verb, out float d2, out float c2, out float m2, out float dur2, out float r2, out float rad2);
            Assert.That(d2, Is.EqualTo(d1), "verb " + verb);
            Assert.That(c2, Is.EqualTo(c1));
            Assert.That(m2, Is.EqualTo(m1));
            Assert.That(dur2, Is.EqualTo(dur1));
            Assert.That(r2, Is.EqualTo(r1));
            Assert.That(rad2, Is.EqualTo(rad1));
        }
    }

    [Test]
    public void MotionTemplateCatalog_MatchesParserOnlyPath()
    {
        JsonValue root = MiniJson.Parse(MotionJson());
        var catalog = MotionTemplateCatalog.FromJsonRoot(root);
        var parserOnly = MotionTemplateParser.Parse(root);

        Assert.That(parserOnly.SkillCount, Is.EqualTo(catalog.SkillCount));
        Assert.That(parserOnly.TemplateCount, Is.EqualTo(catalog.TemplateCount));
        Assert.That(parserOnly.FamilyCount, Is.EqualTo(catalog.FamilyCount));
        for (int v = 1; v <= 12; v++)
        {
            for (int a = 1; a <= 12; a++)
            {
                string id = v + "-" + a;
                bool aOk = catalog.TryGet((SkillId)id, out MotionBinding b1);
                bool bOk = parserOnly.TryGet((SkillId)id, out MotionBinding b2);
                Assert.That(bOk, Is.EqualTo(aOk), id);
                if (!aOk)
                    continue;
                Assert.That(b2.Template.Id, Is.EqualTo(b1.Template.Id));
                Assert.That(b2.Implemented, Is.EqualTo(b1.Implemented));
            }
        }
    }

    [Test]
    public void SourceGate_CatalogTypes_DoNotEmbedJsonParsing()
    {
        string skillCatalog = File.ReadAllText(Path.Combine(
            RepoRoot(), "unity", "Assets", "Scripts", "Core", "Casting", "SkillNumberCatalog.cs"));
        string motionCatalog = File.ReadAllText(Path.Combine(
            RepoRoot(), "unity", "Assets", "Scripts", "Core", "Motion", "MotionTemplateCatalog.cs"));

        Assert.That(skillCatalog, Does.Not.Contain("NeedFloat("));
        Assert.That(skillCatalog, Does.Not.Contain("FirstDuration("));
        Assert.That(motionCatalog, Does.Not.Contain("ParsePhase("));
        Assert.That(motionCatalog, Does.Not.Contain("static float Need("));
    }

    [Test]
    public void SkillNumberCatalog_ImplementsSkillRepository()
    {
        ISkillRepository repo = SkillNumberCatalog.FromJson(ElementJson());
        Assert.That(repo.VerbDamageReference, Is.GreaterThan(0f));
        Assert.That(repo.TryGetVerb(1, out _, out _, out _, out _, out _, out _), Is.True);
    }

    [Test]
    public void MotionTemplateCatalog_ImplementsMotionTemplateRepository()
    {
        IMotionTemplateRepository repo = MotionTemplateCatalog.FromJson(MotionJson());
        Assert.That(repo.SkillCount, Is.GreaterThan(100));
        Assert.That(repo.TryPlay((SkillId)"1-1", out _), Is.True);
    }
}
