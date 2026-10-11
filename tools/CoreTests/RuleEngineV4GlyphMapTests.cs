using System.Collections.Generic;
using System.IO;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4GlyphMapTests
{
    static string RepoPath(params string[] parts)
    {
        var all = new List<string> { TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".." };
        all.AddRange(parts);
        return Path.GetFullPath(Path.Combine(all.ToArray()));
    }

    static string CatalogJson() =>
        File.ReadAllText(RepoPath("unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"));

    static string OrderText() =>
        File.ReadAllText(RepoPath("unity", "Assets", "Resources", "Vfx", "Ejder", "Ejder_Glif_Sirasi.txt"));

    static RuleEngineV4GlyphMap Map() => RuleEngineV4GlyphMap.FromJson(CatalogJson(), OrderText());

    [Test]
    public void SliceRunes_MapToAtlasCellsByCodeName()
    {
        RuleEngineV4GlyphMap map = Map();
        int[] runes = { 1, 2, 3, 4, 6, 9 };
        int[] verbCells = { 0, 10, 8, 11, 6, 9 };
        int[] adjCells = { 22, 21, 18, 15, 13, 23 };
        for (int i = 0; i < runes.Length; i++)
        {
            Assert.That(map.TryVerbCell(runes[i], out int v), Is.True, $"fiil {runes[i]}");
            Assert.That(v, Is.EqualTo(verbCells[i]), $"fiil {runes[i]} hücresi");
            Assert.That(map.TryAdjectiveCell(runes[i], out int a), Is.True, $"sıfat {runes[i]}");
            Assert.That(a, Is.EqualTo(adjCells[i]), $"sıfat {runes[i]} hücresi");
        }
    }

    [Test]
    public void AllTwentyFourRunes_HitDistinctCells()
    {
        RuleEngineV4GlyphMap map = Map();
        var cells = new HashSet<int>();
        for (int rune = 1; rune <= 12; rune++)
        {
            Assert.That(map.TryVerbCell(rune, out int v), Is.True, $"fiil {rune}");
            Assert.That(map.TryAdjectiveCell(rune, out int a), Is.True, $"sıfat {rune}");
            cells.Add(v);
            cells.Add(a);
        }
        Assert.That(cells.Count, Is.EqualTo(24), "24 hücre benzersiz değil");
        Assert.That(cells, Is.EquivalentTo(System.Linq.Enumerable.Range(0, 24)));
    }

    [Test]
    public void MissingName_ReturnsFalse()
    {
        const string json = "{\"rune_glyphs\":{\"verbs\":{\"1\":\"AK\"},\"adjectives\":{\"1\":\"YOK\"},\"_not\":\"x\"}}";
        var map = RuleEngineV4GlyphMap.FromJson(json, OrderText());
        Assert.That(map.TryVerbCell(1, out int v), Is.True);
        Assert.That(v, Is.EqualTo(0));
        Assert.That(map.TryVerbCell(2, out _), Is.False, "adı olmayan fiil");
        Assert.That(map.TryAdjectiveCell(1, out _), Is.False, "sırada olmayan ad");
    }

    [Test]
    public void ParseOrder_ReadsPrefixedNames()
    {
        IReadOnlyDictionary<string, int> order = RuleEngineV4GlyphMap.ParseOrder(OrderText());
        Assert.That(order.Count, Is.EqualTo(24));
        Assert.That(order["fiil_SHEV"], Is.EqualTo(8));
        Assert.That(order["sifat_VORN"], Is.EqualTo(22));
    }
}
