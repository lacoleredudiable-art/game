using System.IO;
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
}
