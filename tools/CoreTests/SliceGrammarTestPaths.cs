using System.IO;
using Dovus.Core.Casting;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Status;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Dilim grameri: kural-motoru-v4.json + SliceGrammarDefaults (slice-sentence-grammar.json yok).</summary>
static class SliceGrammarTestPaths
{
    public static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    public static string RuleEngineV4JsonPath()
    {
        string path = Path.Combine(
            RepoRoot(),
            "unity",
            "Assets",
            "Resources",
            "RuleEngineV4",
            "kural-motoru-v4.json");
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    public static string ReadSliceGrammarJson() =>
        SliceGrammarDefaults.BuildGrammarJson(File.ReadAllText(RuleEngineV4JsonPath()));

    public static RuleEngineV4Catalog LoadV4Catalog() =>
        RuleEngineV4Catalog.FromJson(File.ReadAllText(RuleEngineV4JsonPath()));

    public static VerbExecutionData SliceVerbExecution() =>
        SliceCastingFromV4.VerbExecution(LoadV4Catalog());

    public static MobilityCcData SliceMobilityCc() =>
        SliceCastingFromV4.MobilityCc(LoadV4Catalog());

    public static SkillNumberCatalog SliceSkillNumbers()
    {
        var catalog = LoadV4Catalog();
        var root = Dovus.Core.Data.GrammarJsonDocument.Parse(ReadSliceGrammarJson()).Root;
        return SliceCastingFromV4.SkillNumbers(catalog, root);
    }
}
