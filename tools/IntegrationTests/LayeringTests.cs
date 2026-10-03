using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class LayeringTests
{
    static readonly string[] BannedTokens =
    {
        "JsonValue",
        "MiniJson",
        ".AsArray()",
        ".AsObject()"
    };

    [Test]
    public void GameScripts_ExcludeEditor_DoNotReferenceRawJsonTypes()
    {
        string gameRoot = Path.Combine(RepoPaths.UnityAssets, "Scripts", "Game");
        var violations = new List<string>();

        foreach (string path in Directory.EnumerateFiles(gameRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.IndexOf("Editor", StringComparison.Ordinal) >= 0
                && path.Replace('\\', '/').Contains("/Editor/"))
                continue;

            string rel = Path.GetRelativePath(RepoPaths.Root, path).Replace('\\', '/');
            string text = File.ReadAllText(path);
            foreach (string token in BannedTokens)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                    violations.Add($"{rel}: {token}");
            }
        }

        if (violations.Count > 0)
            Assert.Fail("Game katmanında ham JSON API kullanımı:\n" + string.Join("\n", violations.OrderBy(v => v)));
    }
}
