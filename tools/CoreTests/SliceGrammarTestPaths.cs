using System.IO;
using NUnit.Framework;

namespace CoreTests;

/// <summary>Dilim cümle grameri — element-sistemi.json yerine Resources/RuleEngineV4.</summary>
static class SliceGrammarTestPaths
{
    public static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    public static string SliceGrammarJsonPath()
    {
        string path = Path.Combine(
            RepoRoot(),
            "unity",
            "Assets",
            "Resources",
            "RuleEngineV4",
            "slice-sentence-grammar.json");
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    public static string ReadSliceGrammarJson() => File.ReadAllText(SliceGrammarJsonPath());

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
}
