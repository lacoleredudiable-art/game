using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CoreTests;

[TestFixture]
public class FindInjectionTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string GameRoot() => Path.Combine(Root(), "unity", "Assets", "Scripts", "Game");

    static IEnumerable<string> RuntimeGameCsFiles()
    {
        foreach (string path in Directory.EnumerateFiles(GameRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains("\\Editor\\") || path.Contains("/Editor/"))
                continue;
            if (path.Contains("\\DevTools\\") || path.Contains("/DevTools/"))
                continue;
            string rel = Path.GetRelativePath(GameRoot(), path).Replace('\\', '/');
            if (rel == "Team/TeamComboHost.cs")
                continue;
            yield return path;
        }
    }

    static readonly Regex FindPattern = new(
        @"FindAnyObjectByType|FindObjectOfType|FindFirstObjectByType|FindObjectsByType|Camera\.main",
        RegexOptions.Compiled);

    static readonly Regex TickMethod = new(
        @"\bvoid\s+(Update|LateUpdate|FixedUpdate)\s*\([^)]*\)\s*\{",
        RegexOptions.Compiled);

    static readonly Regex GetComponentInBody = new(@"GetComponent\s*<", RegexOptions.Compiled);

    [Test]
    public void SourceGate_RuntimeGame_NoFindOrCameraMain()
    {
        foreach (string path in RuntimeGameCsFiles())
        {
            string rel = Path.GetRelativePath(GameRoot(), path);
            string src = File.ReadAllText(path);
            Assert.That(FindPattern.IsMatch(src), Is.False, $"{rel}: Find*/Camera.main");
        }
    }

    [Test]
    public void SourceGate_RuntimeGame_NoUncachedGetComponentInTickMethods()
    {
        foreach (string path in RuntimeGameCsFiles())
        {
            string rel = Path.GetRelativePath(GameRoot(), path);
            string src = File.ReadAllText(path);
            foreach (Match tick in TickMethod.Matches(src))
            {
                int brace = src.IndexOf('{', tick.Index);
                if (brace < 0)
                    continue;
                int depth = 0;
                int end = brace;
                for (int i = brace; i < src.Length; i++)
                {
                    if (src[i] == '{')
                        depth++;
                    else if (src[i] == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            end = i;
                            break;
                        }
                    }
                }

                string body = src.Substring(brace, end - brace + 1);
                Assert.That(GetComponentInBody.IsMatch(body), Is.False,
                    $"{rel}: GetComponent< inside {tick.Groups[1].Value}");
            }
        }
    }
}
