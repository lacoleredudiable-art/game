using System;
using System.IO;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Input;
using Dovus.Core.Tuning;
using Dovus.Game.Casting;
using Dovus.Game.Casting.Input;
using Dovus.Game.Config;
using Dovus.Game.Platform;
using NUnit.Framework;
using UnityEngine;

namespace IntegrationTests;

/// <summary>
/// PLAN 2B.21 — HexagonInputSession + StrokeCaster + CastGate (Shim); ölçülen MaxSentenceDots ve cast zamanı.
/// </summary>
[TestFixture]
public class HexagonInputComboTests
{
    GameObject _clockGo;

    [TearDown]
    public void TearDown()
    {
        if (_clockGo != null)
            UnityEngine.Object.DestroyImmediate(_clockGo);
        _clockGo = null;
    }

    static string LoadElementJson()
    {
        string path = RepoPaths.Docs("element-sistemi.json");
        Assert.That(File.Exists(path), Is.True);
        return File.ReadAllText(path);
    }

    (HexagonInputSession session, StrokeCaster stroke, GameClockHost clock) CreateHarness(
        SkillMotor skills,
        bool applyBindMaxDots)
    {
        _clockGo = new GameObject("test-clock");
        var clock = _clockGo.AddComponent<GameClockHost>();

        var session = new HexagonInputSession
        {
            Tuning = new GameTuning(),
            Combat = new CombatTuning(),
            Clock = clock,
            Skills = skills,
        };

        if (applyBindMaxDots && skills != null && skills.MaxComboLength > 0)
            session.Combat.Sentence.MaxSentenceDots = skills.MaxComboLength;

        session.Engine = new SentenceEngine(
            session.Combat.Sentence,
            skills?.DefaultLoadout ?? RuneLoadout.Sequential);

        var feedback = new CastFeedback(session);
        var gate = new CastGate(session, feedback);
        var stroke = new StrokeCaster(session, gate, feedback);
        return (session, stroke, clock);
    }

    static void Advance(GameClockHost clock, HexagonInputSession session, double realMs)
    {
        double worldDelta = clock.Director.Tick(realMs);
        session.Engine?.Tick(worldDelta);
    }

    static Vector2 Dot(int dot, GameTuning tuning) =>
        HexagonLayoutScreen.DotPx(dot, tuning, Screen.width, Screen.height);

    [Test]
    public void BindPath_MaxSentenceDots_MeasuredAs2_WhenSkillMotorLoaded()
    {
        SkillMotor skills = SkillMotor.FromJson(LoadElementJson());
        var (session, _, _) = CreateHarness(skills, applyBindMaxDots: true);

        Assert.That(skills.MaxComboLength, Is.EqualTo(2));
        Assert.That(session.Combat.Sentence.MaxSentenceDots, Is.EqualTo(2),
            "HexagonInputController.Bind sırası: Skills.MaxComboLength → Sentence.MaxSentenceDots");
    }

    [Test]
    public void TwoDotStroke_MaxSentenceDots2_CompletesAtSecondDotWorldTime_Measured()
    {
        SkillMotor skills = SkillMotor.FromJson(LoadElementJson());
        var (session, stroke, clock) = CreateHarness(skills, applyBindMaxDots: true);

        double? completedMs = null;
        session.Engine.SentenceCompleted += _ => completedMs = clock.Director.WorldTimeMs;

        session.Mode = FingerMode.Drawing;
        Vector2 d1 = Dot(1, session.Tuning);
        Vector2 d2 = Dot(2, session.Tuning);

        stroke.BeginStroke(d1);
        Advance(clock, session, 50);
        stroke.FeedStroke(d2);

        Assert.That(session.StrokeAccepted, Is.EqualTo(2));
        Assert.That(completedMs, Is.Not.Null, "2. nokta StrokeCaster → OnDotTouched sonrası kapanış");
        Assert.That(completedMs!.Value, Is.EqualTo(50).Within(0.01));
        Assert.That(session.Engine.State.Phase, Is.EqualTo(SentencePhase.Recovering));
    }

    [Test]
    public void TwoDotStroke_DefaultMaxSentenceDots4_FingerLiftThen360msWindow_Measured()
    {
        var (session, stroke, clock) = CreateHarness(skills: null, applyBindMaxDots: false);
        Assert.That(session.Combat.Sentence.MaxSentenceDots, Is.EqualTo(4),
            "SkillMotor yazılmadığında SentenceTuning varsayılanı 4 — 0,36 sn bekleme kaynağı");

        double? completedMs = null;
        session.Engine.SentenceCompleted += _ => completedMs = clock.Director.WorldTimeMs;

        session.Mode = FingerMode.Drawing;
        stroke.BeginStroke(Dot(1, session.Tuning));
        Advance(clock, session, 10);
        stroke.FeedStroke(Dot(2, session.Tuning));
        double afterSecondDot = clock.Director.WorldTimeMs;

        stroke.FinishDrawingStroke(cancelled: false);
        Assert.That(completedMs, Is.Null, "parmak kaldırma pencereyi kapatmaz");

        Advance(clock, session, 360);
        Assert.That(completedMs, Is.Not.Null);
        Assert.That(completedMs!.Value - afterSecondDot, Is.EqualTo(360).Within(0.01));
    }
}
