using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dovus.Core.Element;
using NUnit.Framework;

namespace CoreTests;

/// <summary>PLAN 2B.14a — A26 rün dili + A27 skill modeli kapıları.</summary>
[TestFixture]
public sealed class RuneLanguageSkillModelTests
{
    static readonly string[] RuneElementAliasMembers =
    {
        "Ates", "Su", "Hava", "Toprak", "Aydinlik", "Karanlik",
    };

    static readonly string[] LegacyV6MemberNames =
    {
        "Saldiri", "Iyilestirme", "Hareket", "Savunma", "Patlama", "Kontrol",
        "Zayiflatma", "Guclendirme", "Arindirma", "Yansima", "Cagirma", "Zaman",
    };

    static readonly (Rune Rune, string Turkish, string Legacy)[] DisplayTable =
    {
        (Rune.Attack, "Saldırı", "Saldiri"),
        (Rune.Heal, "İyileştirme", "Iyilestirme"),
        (Rune.Move, "Hareket", "Hareket"),
        (Rune.Defense, "Savunma", "Savunma"),
        (Rune.Burst, "Patlama", "Patlama"),
        (Rune.Control, "Kontrol", "Kontrol"),
        (Rune.Weaken, "Zayıflatma", "Zayiflatma"),
        (Rune.Empower, "Güçlendirme", "Guclendirme"),
        (Rune.Cleanse, "Arındırma", "Arindirma"),
        (Rune.Reflect, "Yansıma", "Yansima"),
        (Rune.Summon, "Çağırma", "Cagirma"),
        (Rune.Time, "Zaman", "Zaman"),
    };

    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    [Test]
    public void Rune_enum_has_no_element_alias_members()
    {
        string runeCs = File.ReadAllText(Path.Combine(ScriptsRoot(), "Core", "Element", "Rune.cs"));
        foreach (string alias in RuneElementAliasMembers)
            Assert.That(runeCs, Does.Not.Contain($"{alias} ="), $"alias üyesi kaldırılmalı: {alias}");
    }

    [Test]
    public void Scripts_do_not_reference_rune_element_alias_identifiers()
    {
        var hits = new List<string>();
        var pattern = new Regex(@"Rune\.(" + string.Join("|", RuneElementAliasMembers) + @")\b", RegexOptions.Compiled);
        foreach (string file in Directory.EnumerateFiles(ScriptsRoot(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(ScriptsRoot(), file).Replace('\\', '/');
            foreach (Match m in pattern.Matches(File.ReadAllText(file)))
                hits.Add($"{rel}: {m.Value}");
        }

        Assert.That(hits, Is.Empty, string.Join("\n", hits));
    }

    [Test]
    public void RuneInfo_DisplayName_and_LegacySerializationName_tables_unchanged()
    {
        Assert.Multiple(() =>
        {
            foreach ((Rune rune, string turkish, string legacy) in DisplayTable)
            {
                Assert.That(RuneInfo.DisplayName(rune), Is.EqualTo(turkish), rune.ToString());
                Assert.That(RuneInfo.LegacySerializationName(rune), Is.EqualTo(legacy), rune.ToString());
            }
        });
    }

    [Test]
    public void Scripts_do_not_reference_removed_V61SkillNode_type()
    {
        string all = string.Concat(Directory.EnumerateFiles(ScriptsRoot(), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.That(all, Does.Not.Contain("V61SkillNode"));
    }

    [Test]
    public void Rune_enum_uses_English_member_names_not_legacy_v6()
    {
        string runeCs = File.ReadAllText(Path.Combine(ScriptsRoot(), "Core", "Element", "Rune.cs"));
        foreach (string legacy in LegacyV6MemberNames)
            Assert.That(runeCs, Does.Not.Contain($"{legacy} ="), $"eski v6 üye adı enum'da olmamalı: {legacy}");
    }
}
