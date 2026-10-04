using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class ElementSystemParseGateTests
{
    static string GameRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game"));

    static IEnumerable<string> RuntimeGameCs()
    {
        foreach (string path in Directory.EnumerateFiles(GameRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains("\\Editor\\") || path.Contains("/Editor/"))
                continue;
            yield return Path.GetRelativePath(GameRoot(), path).Replace('\\', '/');
        }
    }

    [Test]
    public void SourceGate_Parse_OnlyInElementSystemRuntimeCache()
    {
        var hits = new List<string>();
        foreach (string rel in RuntimeGameCs())
        {
            string text = File.ReadAllText(Path.Combine(GameRoot(), rel));
            if (text.Contains("ElementSystemDocument.Parse("))
                hits.Add(rel);
        }

        Assert.That(hits, Is.EquivalentTo(new[] { "Data/ElementSystemRuntimeCache.cs" }));
    }

    [Test]
    public void SourceGate_ElementJsonResourcePath_OnlyInRuntimeCache()
    {
        var hits = new List<string>();
        var quotedPath = new Regex(
            @"""ElementSystem/element-sistemi""",
            RegexOptions.Compiled);
        foreach (string rel in RuntimeGameCs())
        {
            string text = File.ReadAllText(Path.Combine(GameRoot(), rel));
            if (quotedPath.IsMatch(text))
                hits.Add(rel);
        }

        Assert.That(hits, Is.EquivalentTo(new[] { "Data/ElementSystemRuntimeCache.cs" }));
    }

    [Test]
    public void SourceGate_JsonLoader_UsesRuntimeCacheTryGet()
    {
        string loader = File.ReadAllText(Path.Combine(GameRoot(), "Data/ElementSystemJsonLoader.cs"));
        Assert.That(loader, Does.Contain("ElementSystemRuntimeCache.TryGet"));
        Assert.That(Regex.Matches(loader, @"ElementSystemDocument\.Parse\(").Count, Is.EqualTo(0));
    }
}
