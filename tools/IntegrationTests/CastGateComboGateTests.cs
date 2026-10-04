using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Casting.Input;
using Dovus.Game.Platform;
using NUnit.Framework;
using UnityEngine;

namespace IntegrationTests;

/// <summary>PLAN 2B.21 — CastGate + CombatTuning.Enforce* + oyuncu host'ları (Shim).</summary>
[TestFixture]
public class CastGateComboGateTests
{
    GameObject _go;

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
        _go = null;
    }

    (HexagonInputSession session, CastGate gate, GameClockHost clock) CreateGateHarness(
        float maxMana,
        bool enforceResourceCost,
        bool enforceCooldown)
    {
        _go = new GameObject("gate-hosts");
        var clock = _go.AddComponent<GameClockHost>();
        var resource = _go.AddComponent<PlayerResourceHost>();
        resource.Bind(maxMana: maxMana);
        resource.BindClock(clock);
        var cooldown = _go.AddComponent<PlayerCooldownHost>();
        cooldown.Bind();

        var combat = new CombatTuning
        {
            EnforceResourceCost = enforceResourceCost,
            EnforceCooldown = enforceCooldown,
        };
        var session = new HexagonInputSession
        {
            Clock = clock,
            Combat = combat,
            Resource = resource,
            Cooldown = cooldown,
            Engine = new SentenceEngine(combat.Sentence, RuneLoadout.Sequential),
        };

        var feedback = new CastFeedback(session);
        var gate = new CastGate(session, feedback);
        return (session, gate, clock);
    }

    [Test]
    public void CastGate_EnforceResourceCostTrue_BlocksVerbWhenManaZero()
    {
        var (session, gate, _) = CreateGateHarness(maxMana: 0f, enforceResourceCost: true, enforceCooldown: false);
        session.Skills = SkillMotor.FromJson(System.IO.File.ReadAllText(RepoPaths.Docs("element-sistemi.json")));

        Assert.That(gate.TryAllowSentenceStart(verbDot: 1), Is.False);
        Assert.That(session.Engine.State.Phase, Is.EqualTo(SentencePhase.Idle));
    }

    [Test]
    public void CastGate_EnforceCooldownTrue_BlocksSecondDotWhileComboOnCooldown()
    {
        var (session, gate, clock) = CreateGateHarness(maxMana: 100f, enforceResourceCost: false, enforceCooldown: true);
        string json = System.IO.File.ReadAllText(RepoPaths.Docs("element-sistemi.json"));
        session.Skills = SkillMotor.FromJson(json);

        session.Engine.OnDotTouched(1, 0);
        Assert.That(session.Engine.State.Words.Count, Is.EqualTo(1));

        int verbId = (int)session.Engine.State.Words[0].Rune;
        int adjId = session.Engine.Loadout.RuneIdAtSlot(2);
        var skill = session.Skills.Resolve(new[] { verbId, adjId });
        string comboKey = Dovus.Core.Casting.ComboCooldownKey.For(skill);
        Assert.That(session.Cooldown.TryBeginCast(comboKey, skill.Costs.BaseCooldownSec, 0), Is.True);

        Assert.That(gate.TryAllowComboCooldownForNextDot(2), Is.False);
        clock.Director.Tick(10_000);
        Assert.That(gate.TryAllowComboCooldownForNextDot(2), Is.True);
    }
}
