using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace IntegrationTests;

/// <summary>
/// Unity (netstandard2.1) BCL'inde IsExternalInit yok. Polyfill yalnız Dovus.Core içinde (internal) —
/// diğer assembly'lerde (App, Game, Editor) `init` erişimcisi Unity'de CS0518 ile derlenmez. tools/UnityCompile
/// her şeyi tek assembly derlediği için bunu yakalayamaz; bu kapı yakalar.
/// </summary>
[TestFixture]
public class UnityLanguageGuardTests
{
    static readonly Regex InitAccessor = new(@"\binit\s*;", RegexOptions.Compiled);
    static readonly Regex RecordDecl = new(@"^\s*(public|internal|private|protected)?\s*(sealed\s+|abstract\s+|readonly\s+)*record\s+", RegexOptions.Compiled | RegexOptions.Multiline);

    [Test]
    public void NonCoreScripts_DoNotUseInitAccessorsOrRecords()
    {
        string scripts = Path.Combine(RepoPaths.UnityAssets, "Scripts");
        string core = Path.Combine(scripts, "Core") + Path.DirectorySeparatorChar;
        var problems = new List<string>();

        foreach (string file in Directory.EnumerateFiles(scripts, "*.cs", SearchOption.AllDirectories))
        {
            if (file.StartsWith(core, StringComparison.OrdinalIgnoreCase))
                continue;
            string rel = Path.GetRelativePath(RepoPaths.UnityAssets, file).Replace('\\', '/');
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int comment = line.IndexOf("//", StringComparison.Ordinal);
                string code = comment >= 0 ? line.Substring(0, comment) : line;
                if (InitAccessor.IsMatch(code) || RecordDecl.IsMatch(code))
                    problems.Add($"{rel}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.That(problems, Is.Empty,
            "Core dışı assembly'de `init`/record Unity'de derlenmez (IsExternalInit yok):" + Environment.NewLine
            + string.Join(Environment.NewLine, problems));
    }
}
