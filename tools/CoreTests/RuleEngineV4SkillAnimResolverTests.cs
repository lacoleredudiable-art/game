using System.IO;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4SkillAnimResolverTests
{
    static string JsonPath => Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json");

    [Test]
    public void Kilic_Hareket_Yogun_UsesAtilClipAndAllEvents()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        var plan = new CommandPlan(3, 1, 4, System.Array.Empty<PhysicsCommand>());
        RuleEngineV4SkillAnimBinding binding = RuleEngineV4SkillAnimResolver.Resolve(plan, catalog.SkillAnim);
        Assert.That(binding.Clip, Is.EqualTo("Kilic_ATIL"));
        Assert.That(binding.Events.HasFlag(RuleEngineV4SkillAnimEvent.TrailOn), Is.True);
        Assert.That(binding.Events.HasFlag(RuleEngineV4SkillAnimEvent.Ejder), Is.True);
    }

    [Test]
    public void Cekic_Zarar_WildcardAdj_UsesVurWithEjder()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        var plan = new CommandPlan(1, 2, 6, System.Array.Empty<PhysicsCommand>());
        RuleEngineV4SkillAnimBinding binding = RuleEngineV4SkillAnimResolver.Resolve(plan, catalog.SkillAnim);
        Assert.That(binding.Clip, Is.EqualTo("Cekic_VUR"));
        Assert.That(binding.Events.HasFlag(RuleEngineV4SkillAnimEvent.Impact), Is.True);
        Assert.That(binding.Events.HasFlag(RuleEngineV4SkillAnimEvent.Ejder), Is.True);
    }

    [Test]
    public void Yay_Hareket_MarkedTaklaElseBackstep_WingEntrySilhouette_OnlyOnThoseRows()
    {
        RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(File.ReadAllText(Path.GetFullPath(JsonPath)));
        RuleEngineV4SkillAnimBinding marked = catalog.SkillAnim.Resolve(3, 9, 2);
        Assert.That(marked.Clip, Is.EqualTo("Ortak_Takla"));
        Assert.That(marked.FallbackAnimatorState, Is.EqualTo("Dodge"));
        RuleEngineV4SkillAnimBinding yay = catalog.SkillAnim.Resolve(3, 1, 2);
        Assert.That(yay.Clip, Is.EqualTo("Ortak_GeriSicrama"));
        Assert.That(yay.Events.HasFlag(RuleEngineV4SkillAnimEvent.TrailOn), Is.False, "geri sıçramada kor izi");
        foreach (RuleEngineV4SkillAnimBinding b in new[] { marked, yay })
        {
            Assert.That(b.EntrySilhouetteCell, Is.EqualTo(VfxPlanDefaults.EjderAtlasCellD));
            Assert.That(b.EntrySilhouetteLifeSec, Is.EqualTo(0.7f).Within(1e-6f));
            Assert.That(b.EntrySilhouetteFadeInSec, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(b.EntrySilhouetteFadeOutSec, Is.EqualTo(0.5f).Within(1e-6f));
        }
        foreach (int weapon in new[] { 2, 4, 6 })
        foreach (int verb in RuleEngineV4Slice.Runes)
        foreach (int adj in RuleEngineV4Slice.Runes)
        {
            int expected = weapon == 2 && verb == 3
                ? VfxPlanDefaults.EjderAtlasCellD
                : RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell;
            Assert.That(catalog.SkillAnim.Resolve(verb, adj, weapon).EntrySilhouetteCell, Is.EqualTo(expected),
                $"w{weapon} {verb}-{adj}");
        }
    }

    [Test]
    public void EntrySilhouette_AtlasLetters_MapToCells()
    {
        static RuleEngineV4SkillAnimBinding Parse(string cell) =>
            RuleEngineV4SkillAnimCatalog.FromJson(Dovus.Core.Shared.MiniJson.Parse(
                "{\"skill_anim\":{\"rules\":[{\"weapon_id\":2,\"verb_rune\":3,\"clip\":\"X\",\"entry_silhouette\":{\"atlas_cell\":\""
                + cell + "\",\"life_sec\":0.3}}]}}")).Resolve(3, 1, 2);
        Assert.That(Parse("A").EntrySilhouetteCell, Is.EqualTo(0));
        Assert.That(Parse("D").EntrySilhouetteCell, Is.EqualTo(3));
        Assert.That(Parse("H").EntrySilhouetteCell, Is.EqualTo(7));
        Assert.That(Parse("I").EntrySilhouetteCell, Is.EqualTo(-1));
        Assert.That(Parse("").EntrySilhouetteCell, Is.EqualTo(-1));
    }
}
