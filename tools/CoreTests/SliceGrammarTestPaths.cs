using System.IO;
using Dovus.Core.RuleEngineV4;
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
}
