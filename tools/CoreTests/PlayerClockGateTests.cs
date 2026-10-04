using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class PlayerClockGateTests
{
    static string ActorsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts", "Game", "Actors"));

    static readonly Regex TimeUse = new(@"\bTime\.", RegexOptions.Compiled);

    static readonly string[] HostPrefixes =
    {
        "PlayerResourceHost",
        "PlayerVitalsHost",
        "PlayerCooldownHost",
    };

    [Test]
    public void SourceGate_PlayerHosts_DoNotUseUnityTime()
    {
        var hits = new List<string>();
        foreach (string path in Directory.EnumerateFiles(ActorsRoot(), "*.cs"))
        {
            string name = Path.GetFileName(path);
            bool match = false;
            foreach (string prefix in HostPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    match = true;
                    break;
                }
            }

            if (!match)
                continue;

            string text = File.ReadAllText(path);
            if (TimeUse.IsMatch(text))
                hits.Add(name);
        }

        Assert.That(hits, Is.Empty, string.Join(", ", hits));
    }
}
