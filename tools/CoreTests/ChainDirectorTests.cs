using System.Collections.Generic;
using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json chain_mechanics — 6 zincir pattern/Links/Finisher.
/// Pattern formatı JSON string'inden ("1-X-X-X-X-X"); elle kombo tablosu yok.
/// </summary>
[TestFixture]
public class ChainDirectorTests
{
    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True, $"element-sistemi.json bulunamadı: {path}");
        return path;
    }

    static (SkillMotor motor, ChainRules rules, string json) Load()
    {
        string json = File.ReadAllText(JsonPath());
        var motor = SkillMotor.FromJson(json);
        var rules = ChainRules.FromJsonRoot(MiniJson.Parse(json));
        return (motor, rules, json);
    }

    static int AnchorDot(ChainNode chain)
    {
        string[] tokens = chain.Pattern.Split('-');
        Assert.That(tokens.Length, Is.GreaterThan(0), chain.Element);
        Assert.That(int.TryParse(tokens[0].Trim(), out int dot), Is.True,
            $"pattern ilk slot digit olmalı: {chain.Pattern}");
        return dot;
    }

    [Test]
    public void Motor_Parses_SixChains_WithPatternsAndLinks()
    {
        var (motor, rules, json) = Load();
        var root = MiniJson.Parse(json);
        int expected = root["chain_mechanics"]["chains"].AsArray().Count;

        Assert.That(motor.Chains.Count, Is.EqualTo(expected));
        Assert.That(motor.Chains.Count, Is.EqualTo(6));
        Assert.That(rules.ChainWindowSec, Is.EqualTo(0.8f).Within(0.0001f));
        Assert.That(rules.MaxChainLength, Is.EqualTo(6));

        foreach (ChainNode c in motor.Chains)
        {
            Assert.That(c.Pattern, Does.Contain("-"));
            Assert.That(c.Links.Length, Is.EqualTo(ChainDirector.CountPatternSlots(c.Pattern)));
            Assert.That(c.Finisher, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public void AllSixChains_MatchTheirPattern_ApplyLinksInOrder_FinisherOnlyAtFull()
    {
        var (motor, rules, _) = Load();
        Assert.That(motor.Chains.Count, Is.EqualTo(6));

        foreach (ChainNode chain in motor.Chains)
        {
            var director = new ChainDirector(motor.Chains, rules);
            int dot = AnchorDot(chain);
            int slots = ChainDirector.CountPatternSlots(chain.Pattern);
            Assert.That(slots, Is.EqualTo(chain.Links.Length), chain.Element);

            double t = 0;
            for (int i = 0; i < slots; i++)
            {
                ChainStepResult step = director.RegisterCast(dot, t);
                Assert.That(step.Matched, Is.True, $"{chain.Element} link {i + 1}");
                Assert.That(step.Chain!.Value.Element, Is.EqualTo(chain.Element));
                Assert.That(step.LinkCount, Is.EqualTo(i + 1));
                Assert.That(step.LinkBonus, Is.EqualTo(chain.Links[i]).Within(0.0001f));

                bool expectFinisher = i == slots - 1;
                Assert.That(step.FinisherTriggered, Is.EqualTo(expectFinisher),
                    $"{chain.Element}: finisher yalnızca son linkte");
                if (expectFinisher)
                    Assert.That(step.Finisher, Is.EqualTo(chain.Finisher));
                else
                    Assert.That(step.Finisher, Is.Empty);

                t += rules.ChainWindowSec * 1000.0 * 0.5; // window içinde
            }
        }
    }

    [Test]
    public void Pattern_X_MeansSameAsAnchor_WrongElementBreaks()
    {
        // "1-X-..." → ikinci cast 1 olmalı; 2 gelirse Ateş zinciri kırılır (ceza).
        var (motor, rules, _) = Load();
        var director = new ChainDirector(motor.Chains, rules);

        ChainStepResult first = director.RegisterCast(1, 0);
        Assert.That(first.Matched, Is.True);
        Assert.That(first.Chain!.Value.Element, Is.EqualTo("Ateş"));
        Assert.That(first.LinkCount, Is.EqualTo(1));
        Assert.That(first.FinisherTriggered, Is.False);

        ChainStepResult broken = director.RegisterCast(2, 100);
        Assert.That(broken.Matched, Is.False);
        Assert.That(director.Active, Is.Null);
        Assert.That(director.InBreakPenalty(100), Is.True);

        double after = 100 + rules.BreakPenaltySec * 1000.0 + 1;
        ChainStepResult su = director.RegisterCast(2, after);
        Assert.That(su.Matched, Is.True);
        Assert.That(su.Chain!.Value.Element, Is.EqualTo("Su"));
        Assert.That(su.LinkCount, Is.EqualTo(1));
        Assert.That(su.FinisherTriggered, Is.False);
    }

    [Test]
    public void WindowExpiry_BreaksChain_AppliesPenalty()
    {
        var (motor, rules, _) = Load();
        var director = new ChainDirector(motor.Chains, rules);

        director.RegisterCast(1, 0);
        Assert.That(director.LinkCount, Is.EqualTo(1));

        double late = rules.ChainWindowSec * 1000.0 + 1;
        ChainStepResult step = director.RegisterCast(1, late);
        // Penalty sırasında yeni zincir yok sayılır.
        Assert.That(step.Matched, Is.False);
        Assert.That(director.InBreakPenalty(late), Is.True);

        double afterPenalty = late + rules.BreakPenaltySec * 1000.0 + 1;
        ChainStepResult restart = director.RegisterCast(1, afterPenalty);
        Assert.That(restart.Matched, Is.True);
        Assert.That(restart.LinkCount, Is.EqualTo(1));
    }

    [Test]
    public void MatchPrefixLength_ParsesDigitAndX()
    {
        Assert.That(ChainDirector.MatchPrefixLength("1-X-X", new[] { 1, 1, 1 }), Is.EqualTo(3));
        Assert.That(ChainDirector.MatchPrefixLength("1-X-X", new[] { 1, 1 }), Is.EqualTo(2));
        Assert.That(ChainDirector.MatchPrefixLength("1-X-X", new[] { 1, 2 }), Is.EqualTo(1));
        Assert.That(ChainDirector.MatchPrefixLength("2-X", new[] { 1 }), Is.EqualTo(0));
        Assert.That(ChainDirector.MatchPrefixLength("X-X", new[] { 3, 3 }), Is.EqualTo(2));
        Assert.That(ChainDirector.MatchPrefixLength("X-X", new[] { 3, 4 }), Is.EqualTo(1));
    }
}
