using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// PLAN 2B.11: oynanış klasörlerindeki float literal sayısı rapor tavanını aşamaz.
/// Tavan = <see cref="docs/constants-report.md"/> "sonra" sütunu (origin/master + 2B.11).
/// </summary>
[TestFixture]
public class GameplayLiteralRatchetTests
{
    static readonly Regex FloatLiteral = new(@"\b\d+\.\d+f\b|\b\d+f\b", RegexOptions.Compiled);

    static readonly (string Folder, int MaxLiterals)[] Ceilings =
    {
        ("Skills", 1013),
        ("Boss", 387),
        ("Team", 63),
        ("Actors", 287),
        ("Combat", 0),
    };

    static string GameRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game"));

    [Test]
    public void GameplayFolders_FloatLiteralCount_DoesNotExceedReportCeiling()
    {
        foreach ((string folder, int max) in Ceilings)
        {
            string dir = Path.Combine(GameRoot, folder);
            if (!Directory.Exists(dir))
            {
                Assert.That(max, Is.EqualTo(0), $"missing folder {folder}");
                continue;
            }

            int count = 0;
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
                count += FloatLiteral.Matches(File.ReadAllText(file)).Count;

            Assert.That(count, Is.LessThanOrEqualTo(max),
                () => $"{folder}: {count} float literals (max {max} per constants-report.md ratchet)");
        }
    }
}
