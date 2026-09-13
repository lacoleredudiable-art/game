using System.Collections.Generic;
using Dovus.Core.Elements;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// 6¹+6²+6³+6⁴ = 1554 rün dizisinin gramer kapsamı.
/// 1'li skill değil (red); 2/3/4 tamamı çözülür — kombo tablosu yok, gramerden doğar.
/// </summary>
[TestFixture]
public class ElementComboExhaustiveTests
{
    ElementCatalog _catalog = null!;
    ElementResolver _resolver = null!;

    [OneTimeSetUp]
    public void LoadLockedSpec()
    {
        _catalog = ElementCatalogLoader.LoadFromRepo();
        _resolver = new ElementResolver(_catalog);
    }

    [Test]
    public void DesignSpace_Is1554Sequences()
    {
        Assert.That(Pow(6, 1) + Pow(6, 2) + Pow(6, 3) + Pow(6, 4), Is.EqualTo(1554));
    }

    [Test]
    public void Length1_AllSix_Reject()
    {
        int failed = 0;
        for (int a = 1; a <= 6; a++)
        {
            Assert.That(_resolver.TryResolve(new[] { a }, out _), Is.False, $"1-rune {a}");
            failed++;
        }
        Assert.That(failed, Is.EqualTo(6));
    }

    [Test]
    public void Length2_All36_IdentitySkills()
    {
        var skillIds = new HashSet<string>();
        int ok = 0;
        for (int a = 1; a <= 6; a++)
        for (int b = 1; b <= 6; b++)
        {
            int[] runes = { a, b };
            Assert.That(_resolver.TryResolve(runes, out var r), Is.True, Id(runes));
            Assert.That(r.Kind, Is.EqualTo(ElementResolveKind.IdentitySkill), Id(runes));
            Assert.That(r.SkillId, Is.Not.Null.And.Not.Empty, Id(runes));
            Assert.That(r.Adjective, Is.Null, Id(runes));
            Assert.That(r.FinalDamage, Is.EqualTo(r.Verb.BaseDamage), Id(runes));
            Assert.That(skillIds.Add(r.SkillId!), Is.True, $"duplicate skill {r.SkillId}");
            ok++;
        }
        Assert.That(ok, Is.EqualTo(36));
        Assert.That(skillIds.Count, Is.EqualTo(36));
    }

    [Test]
    public void Length3_All216_VerbPlusCoreAdjective()
    {
        int ok = 0;
        for (int a = 1; a <= 6; a++)
        for (int b = 1; b <= 6; b++)
        for (int c = 1; c <= 6; c++)
        {
            int[] runes = { a, b, c };
            Assert.That(_resolver.TryResolve(runes, out var r), Is.True, Id(runes));
            Assert.That(r.Kind, Is.EqualTo(ElementResolveKind.VerbPlusCoreAdjective), Id(runes));
            Assert.That(r.SkillId, Is.Null, Id(runes));
            Assert.That(r.Adjective, Is.Not.Null, Id(runes));
            Assert.That(r.Adjective!.Value.DamageMult, Is.LessThanOrEqualTo(0.9f), Id(runes));
            Assert.That(r.CastTimeMult, Is.EqualTo(1.4f), Id(runes));
            Assert.That(
                r.FinalDamage,
                Is.EqualTo(r.Verb.BaseDamage * r.Adjective.Value.DamageMult).Within(0.001f),
                Id(runes));
            ok++;
        }
        Assert.That(ok, Is.EqualTo(216));
    }

    [Test]
    public void Length4_All1296_VerbPlusCompoundAdjective()
    {
        int ok = 0;
        for (int a = 1; a <= 6; a++)
        for (int b = 1; b <= 6; b++)
        for (int c = 1; c <= 6; c++)
        for (int d = 1; d <= 6; d++)
        {
            int[] runes = { a, b, c, d };
            Assert.That(_resolver.TryResolve(runes, out var r), Is.True, Id(runes));
            Assert.That(r.Kind, Is.EqualTo(ElementResolveKind.VerbPlusCompoundAdjective), Id(runes));
            Assert.That(r.SkillId, Is.Null, Id(runes));
            Assert.That(r.Adjective, Is.Not.Null, Id(runes));
            Assert.That(r.Adjective!.Value.DamageMult, Is.LessThanOrEqualTo(0.9f), Id(runes));
            Assert.That(r.CastTimeMult, Is.EqualTo(2f), Id(runes));
            Assert.That(
                r.FinalDamage,
                Is.EqualTo(r.Verb.BaseDamage * r.Adjective.Value.DamageMult).Within(0.001f),
                Id(runes));
            ok++;
        }
        Assert.That(ok, Is.EqualTo(1296));
    }

    [Test]
    public void All1554_ResolveOrRejectByLength()
    {
        int resolveOk = 0;
        int rejectOk = 0;

        foreach (var runes in EnumerateLengths(1, 4))
        {
            bool ok = _resolver.TryResolve(runes, out var r);
            if (runes.Length == 1)
            {
                Assert.That(ok, Is.False, Id(runes));
                rejectOk++;
                continue;
            }

            Assert.That(ok, Is.True, Id(runes));
            Assert.That(r.CompoundId, Is.EqualTo($"{runes[0]}-{runes[1]}"), Id(runes));
            resolveOk++;
        }

        Assert.That(rejectOk, Is.EqualTo(6));
        Assert.That(resolveOk, Is.EqualTo(1548));
        Assert.That(rejectOk + resolveOk, Is.EqualTo(1554));
    }

    static IEnumerable<int[]> EnumerateLengths(int minLen, int maxLen)
    {
        for (int len = minLen; len <= maxLen; len++)
        {
            int[] cur = new int[len];
            foreach (var _ in Fill(cur, 0))
                yield return (int[])cur.Clone();
        }
    }

    static IEnumerable<int> Fill(int[] cur, int i)
    {
        if (i == cur.Length)
        {
            yield return 0;
            yield break;
        }

        for (int r = 1; r <= 6; r++)
        {
            cur[i] = r;
            foreach (var x in Fill(cur, i + 1))
                yield return x;
        }
    }

    static string Id(int[] runes) => string.Join('-', runes);

    static int Pow(int b, int e)
    {
        int n = 1;
        for (int i = 0; i < e; i++) n *= b;
        return n;
    }
}
