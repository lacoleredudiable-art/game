using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// PLAN 2B.11: oynanış klasörlerindeki float literal sayısı rapor tavanını aşamaz.
/// Tavan = MagicNumberRatchetTests / docs/ARCHITECTURE.md (origin/master + 2B.11).
/// </summary>
[TestFixture]
public class GameplayLiteralRatchetTests
{
    static readonly Regex FloatLiteral = new(@"\b\d+\.\d+f\b|\b\d+f\b", RegexOptions.Compiled);

    static readonly (string Folder, int MaxLiterals)[] Ceilings =
    {
        ("Skills", 994),
        ("Boss", 387),
        ("Team", 63),
        ("Actors", 287),
        ("Combat", 0),
    };

    static string GameRoot =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game"));

    /// <summary>PhysX mesafe sondası — oynanış ayarı değil; float sayacına dahil edilmez.</summary>
    internal static string StripPhysicsProbeMethods(string src)
    {
        src = StripMethodBody(src, "ClearDistance");
        return StripMethodBody(src, "SweepAndSlide");
    }

    static string StripMethodBody(string src, string methodName)
    {
        int idx = src.IndexOf(methodName + "(", StringComparison.Ordinal);
        if (idx < 0)
            return src;
        int brace = src.IndexOf('{', idx);
        if (brace < 0)
            return src;
        int depth = 0;
        for (int i = brace; i < src.Length; i++)
        {
            if (src[i] == '{')
                depth++;
            else if (src[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return src.Remove(brace, i - brace + 1).Insert(brace, "{}");
            }
        }
        return src;
    }

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
            {
                string text = File.ReadAllText(file);
                if (file.Contains("RuleEngineV4WorldPhysicsUtil.cs", StringComparison.Ordinal))
                    text = StripPhysicsProbeMethods(text);
                count += FloatLiteral.Matches(text).Count;
            }

            Assert.That(count, Is.LessThanOrEqualTo(max),
                () => $"{folder}: {count} float literals (max {max} per MagicNumberRatchetTests)");
        }
    }
}
