using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class FileLayoutTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string ScriptsRoot => Path.Combine(RepoRoot(), "unity", "Assets", "Scripts");

    static readonly Regex TopLevelType = new(
        @"^\s*(?:(?:public|internal|private|protected)\s+)?(?:(?:static|readonly|sealed|partial|unsafe|new)\s+)*"
        + @"(class|struct|enum|interface|record|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    static IEnumerable<string> LayerCsFiles(params string[] layers)
    {
        foreach (string layer in layers)
        {
            string root = Path.Combine(ScriptsRoot, layer);
            if (!Directory.Exists(root))
                continue;
            foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                yield return path;
        }
    }

    static IEnumerable<string> CoreAndAppCsFiles() => LayerCsFiles("Core", "App");

    static IEnumerable<string> GameCsFiles() => LayerCsFiles("Game");

    static bool IsCodeLine(string line)
    {
        string t = line.Trim();
        if (t.Length == 0)
            return false;
        if (t.StartsWith("using ", StringComparison.Ordinal))
            return false;
        if (t.StartsWith("namespace ", StringComparison.Ordinal))
            return false;
        if (t is "{" or "}")
            return false;
        return true;
    }

    static string StripCommentsAndStrings(string line)
    {
        // Naive: good enough for layout scan (tests avoid tricky string literals in declarations).
        int i = line.IndexOf("//", StringComparison.Ordinal);
        if (i >= 0)
            line = line.Substring(0, i);
        return line;
    }

    static List<(string Name, bool Partial)> TopLevelTypesInFile(string path)
    {
        var types = new List<(string, bool)>();
        int braceDepth = 0;
        foreach (string raw in File.ReadLines(path))
        {
            string line = StripCommentsAndStrings(raw);
            foreach (char c in line)
            {
                if (c == '{')
                    braceDepth++;
                else if (c == '}')
                    braceDepth--;
            }

            if (braceDepth != 1)
                continue;

            Match m = TopLevelType.Match(line);
            if (!m.Success)
                continue;
            bool partial = line.Contains("partial ", StringComparison.Ordinal);
            types.Add((m.Groups[2].Value, partial));
        }

        return types;
    }

    [Test]
    public void Core_and_App_each_file_has_at_most_one_top_level_type_and_name_matches_file()
    {
        var problems = new List<string>();
        foreach (string path in CoreAndAppCsFiles())
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string expectedTypeName = fileName.Split('.')[0];

            List<(string Name, bool Partial)> types = TopLevelTypesInFile(path);
            var nonPartial = types.Where(t => !t.Partial).ToList();
            if (nonPartial.Count > 1)
            {
                problems.Add($"{path}: {nonPartial.Count} top-level types ({string.Join(", ", nonPartial.Select(t => t.Name))})");
                continue;
            }

            if (nonPartial.Count == 1 && nonPartial[0].Name != expectedTypeName)
                problems.Add($"{path}: file {expectedTypeName} vs type {nonPartial[0].Name}");
            if (nonPartial.Count == 0 && types.Count > 0)
            {
                // partial-only file (e.g. ManifestationDirector.Topic.cs) — name must match stem
                if (types[0].Name != expectedTypeName)
                    problems.Add($"{path}: partial type {types[0].Name} vs file {expectedTypeName}");
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    [Test]
    public void Game_each_file_has_at_most_one_top_level_type_and_name_matches_file()
    {
        var problems = new List<string>();
        foreach (string path in GameCsFiles())
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string expectedTypeName = fileName.Split('.')[0];

            List<(string Name, bool Partial)> types = TopLevelTypesInFile(path);
            var nonPartial = types.Where(t => !t.Partial).ToList();
            if (nonPartial.Count > 1)
            {
                problems.Add($"{path}: {nonPartial.Count} top-level types ({string.Join(", ", nonPartial.Select(t => t.Name))})");
                continue;
            }

            if (nonPartial.Count == 1 && nonPartial[0].Name != expectedTypeName)
                problems.Add($"{path}: file {expectedTypeName} vs type {nonPartial[0].Name}");
            if (nonPartial.Count == 0 && types.Count > 0)
            {
                if (types[0].Name != expectedTypeName)
                    problems.Add($"{path}: partial type {types[0].Name} vs file {expectedTypeName}");
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }
}
