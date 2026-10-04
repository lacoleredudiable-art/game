using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class GameJsonValueGateTests
{
    static readonly string[] AllowedFiles =
    {
        // İzinli: yok (Game katmanında JsonValue kullanılmıyor).
    };

    [Test]
    public void GameScripts_DoNotReferenceJsonValue()
    {
        string root = Path.Combine(SkillResolutionSnapshotUtil.RepoRoot(), "unity", "Assets", "Scripts", "Game");
        var hits = new System.Collections.Generic.List<string>();
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = path.Substring(root.Length).TrimStart('\\', '/');
            if (AllowedFiles.Any(a => rel.EndsWith(a, System.StringComparison.OrdinalIgnoreCase)))
                continue;
            string text = File.ReadAllText(path);
            if (text.Contains("JsonValue"))
                hits.Add(rel);
        }
        Assert.That(hits, Is.Empty, "Game JsonValue: " + string.Join(", ", hits));
    }
}
