using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class NamespaceMatchesFolderTests
{
    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static readonly Regex NamespaceLine = new(
        @"^\s*namespace\s+([\w.]+)\s*(?:[;{]|$)",
        RegexOptions.Compiled);

    static readonly (string layer, string prefix)[] Layers =
    {
        ("Core", "Dovus.Core"),
        ("App", "Dovus.App"),
        ("Game", "Dovus.Game"),
    };

    static bool IsNamespaceException(string relUnix) =>
        relUnix == "Core/Compat/IsExternalInit.cs"
        || relUnix.EndsWith("/AssemblyInfo.cs", StringComparison.Ordinal);

    static string ExpectedNamespace(string relUnix, string nsPrefix)
    {
        string afterLayer = relUnix.Substring(relUnix.IndexOf('/') + 1);
        int lastSlash = afterLayer.LastIndexOf('/');
        if (lastSlash < 0)
            return nsPrefix;
        string folderPath = afterLayer.Substring(0, lastSlash);
        return nsPrefix + "." + folderPath.Replace('/', '.');
    }

    [Test]
    public void Every_script_namespace_matches_folder_path()
    {
        var problems = new List<string>();
        foreach ((string layer, string prefix) in Layers)
        {
            string layerRoot = Path.Combine(ScriptsRoot(), layer);
            if (!Directory.Exists(layerRoot))
                continue;

            foreach (string path in Directory.EnumerateFiles(layerRoot, "*.cs", SearchOption.AllDirectories))
            {
                string relUnix = Path.GetRelativePath(ScriptsRoot(), path).Replace('\\', '/');
                if (IsNamespaceException(relUnix))
                    continue;

                string? found = null;
                foreach (string line in File.ReadLines(path))
                {
                    Match m = NamespaceLine.Match(line);
                    if (m.Success)
                    {
                        found = m.Groups[1].Value;
                        break;
                    }
                }

                if (found == null)
                {
                    problems.Add($"namespace missing: {relUnix}");
                    continue;
                }

                string expected = ExpectedNamespace(relUnix, prefix);
                if (found != expected)
                    problems.Add($"{relUnix}: expected {expected}, got {found}");
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }
}
