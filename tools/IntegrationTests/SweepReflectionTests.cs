using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IntegrationTests;

[TestFixture]
public sealed class SweepReflectionTests
{
    static readonly Regex ReflectionCall = new(
        @"\b(F|P|S|Call)\s*[\(<]",
        RegexOptions.Compiled);

    static readonly HashSet<string> Allowlist = new();

    [Test]
    public void PlaySweep_uses_no_string_reflection_helpers()
    {
        string dir = Path.Combine(RepoPaths.UnityGame, "Editor", "Sweep");
        int hits = 0;
        foreach (string file in Directory.EnumerateFiles(dir, "PlaySweep*.cs"))
        {
            string text = File.ReadAllText(file);
            foreach (Match m in ReflectionCall.Matches(text))
            {
                string line = LineAt(text, m.Index);
                if (Allowlist.Contains($"{Path.GetFileName(file)}:{line}"))
                    continue;
                hits++;
            }
        }
        Assert.That(hits, Is.EqualTo(0), "PlaySweep reflection helper kullanımı kaldırılmalı");
    }

    static string LineAt(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
                line++;
        }
        return line.ToString();
    }
}
