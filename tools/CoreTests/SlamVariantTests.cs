using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SlamVariantTests
{
    [Test]
    public void Defaults_MatchSection11Table()
    {
        var b = new BossTuning();
        Assert.Multiple(() =>
        {
            Assert.That(b.WindupMs, Is.EqualTo(640));
            Assert.That(b.RadiusM, Is.EqualTo(5.4f));
            Assert.That(b.GecWindupMs, Is.EqualTo(900));
            Assert.That(b.GecRadiusM, Is.EqualTo(5.4f));
            Assert.That(b.GenisWindupMs, Is.EqualTo(640));
            Assert.That(b.GenisRadiusM, Is.EqualTo(8.0f));
            Assert.That(b.MaxSameVariantStreak, Is.EqualTo(2));
        });
    }

    [Test]
    public void ApplyVariant_SetsWindupAndRadius_SharedActiveRecovery()
    {
        var tuning = new BossTuning();
        var attack = new BossAttack(tuning);

        attack.ApplyVariant(SlamVariant.Yakin);
        Assert.That(attack.WindupMs, Is.EqualTo(640));
        Assert.That(attack.RadiusM, Is.EqualTo(5.4f));
        Assert.That(attack.ActiveMs, Is.EqualTo(90));
        Assert.That(attack.RecoveryMs, Is.EqualTo(720));

        attack.ApplyVariant(SlamVariant.Gec);
        Assert.That(attack.WindupMs, Is.EqualTo(900));
        Assert.That(attack.RadiusM, Is.EqualTo(5.4f));
        Assert.That(attack.StrikeTimeMs(1000), Is.EqualTo(1900));

        attack.ApplyVariant(SlamVariant.Genis);
        Assert.That(attack.WindupMs, Is.EqualTo(640));
        Assert.That(attack.RadiusM, Is.EqualTo(8.0f));
        Assert.That(attack.ActiveMs, Is.EqualTo(90));
    }

    /// <summary>
    /// §11 yan fayda: 5.4 m'de TEMİZ/SIYIRDI kaçan dodge hacim dışı (Safe); 8.0 m'de içeride.
    /// T8 tablosu: gap ~197 ms → mesafe 5.40 m sınır; gap 220/240 → ~5.47+ m.
    /// </summary>
    [Test]
    public void GenisRadius_KeepsTemizAndSiyirdiDistancesInVolume()
    {
        var attack = new BossAttack();
        const float temizDistM = 5.47f;
        const float siyirdiDistM = 5.9f;

        attack.ApplyVariant(SlamVariant.Yakin);
        Assert.That(attack.IsInEffectVolume(temizDistM, 0f), Is.False, "YAKIN: TEMİZ mesafesi menzil dışı");
        Assert.That(attack.IsInEffectVolume(siyirdiDistM, 0f), Is.False, "YAKIN: SIYIRDI mesafesi menzil dışı");

        attack.ApplyVariant(SlamVariant.Genis);
        Assert.That(attack.IsInEffectVolume(temizDistM, 0f), Is.True, "GENİŞ: TEMİZ mesafesi hacimde");
        Assert.That(attack.IsInEffectVolume(siyirdiDistM, 0f), Is.True, "GENİŞ: SIYIRDI mesafesi hacimde");
        Assert.That(attack.IsInEffectVolume(8.0f, 0f), Is.True);
        Assert.That(attack.IsInEffectVolume(8.01f, 0f), Is.False);
    }

    [Test]
    public void GenisInVolume_ResolveProducesTemizOrSiyirdi_NotSafe()
    {
        var resolver = new ExchangeResolver();
        var attack = new BossAttack();
        attack.ApplyVariant(SlamVariant.Genis);

        const int telegraph = 1000;
        int strike = attack.StrikeTimeMs(telegraph);
        // gap 240 → SIYIRDI (iframe 260 içinde)
        int press = strike - 240;
        Assert.That(attack.IsInEffectVolume(5.9f, 0f), Is.True);

        var result = resolver.Resolve(new ExchangeInput
        {
            TelegraphStartMs = telegraph,
            StrikeTimeMs = strike,
            DodgePressMs = press,
            InEffectVolume = attack.IsInEffectVolume(5.9f, 0f)
        });

        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Dodged));
        Assert.That(result.Grade, Is.EqualTo(DodgeGrade.Siyirdi));
    }

    [Test]
    public void Gec_EarlyDodgeRelativeToYakinTiming_IsErkenBastin()
    {
        var resolver = new ExchangeResolver();
        var attack = new BossAttack();
        attack.ApplyVariant(SlamVariant.Gec);

        const int telegraph = 1000;
        int gecStrike = attack.StrikeTimeMs(telegraph); // 1900
        // YAKIN vuruş anına (1640) göre "iyi" basış: gap 100 → ama GEÇ vuruşu 260 ms sonra;
        // i-frame 260 ms, yani basış+260 = 1800 < 1900 → erken bastın.
        int press = telegraph + 640 - 100; // 1540
        Assert.That(gecStrike - press, Is.EqualTo(360));
        Assert.That(press + 260, Is.LessThan(gecStrike));

        var result = resolver.Resolve(new ExchangeInput
        {
            TelegraphStartMs = telegraph,
            StrikeTimeMs = gecStrike,
            DodgePressMs = press,
            InEffectVolume = true
        });

        Assert.That(result.Outcome, Is.EqualTo(ExchangeOutcome.Hit));
        Assert.That(result.Reason, Is.EqualTo(HitReason.ErkenBastin));
        Assert.That(result.HitReasonText, Is.EqualTo("erken bastın"));
    }

    [Test]
    public void Picker_RespectsMaxSameVariantStreak()
    {
        var rng = new Random(42);
        const int maxStreak = 2;

        SlamVariant? last = null;
        int streak = 0;
        var counts = new Dictionary<SlamVariant, int>
        {
            [SlamVariant.Yakin] = 0,
            [SlamVariant.Gec] = 0,
            [SlamVariant.Genis] = 0
        };

        int tripleRepeats = 0;
        for (int i = 0; i < 300; i++)
        {
            SlamVariant picked = SlamVariantPicker.Pick(last, streak, maxStreak, rng);
            if (last.HasValue && picked == last.Value && streak >= maxStreak)
                Assert.Fail($"streak {streak} iken {picked} tekrar seçildi");

            if (last.HasValue && picked == last.Value && streak + 1 >= 3)
                tripleRepeats++;

            streak = SlamVariantPicker.NextStreak(last, streak, picked);
            last = picked;
            counts[picked]++;
        }

        Assert.That(tripleRepeats, Is.EqualTo(0));
        Assert.That(counts[SlamVariant.Yakin], Is.GreaterThan(0));
        Assert.That(counts[SlamVariant.Gec], Is.GreaterThan(0));
        Assert.That(counts[SlamVariant.Genis], Is.GreaterThan(0));
    }

    [Test]
    public void Picker_WhenStreakAtCap_ExcludesLastVariant()
    {
        // Deterministik: tek adaylı aralıkta hep aynı seçim.
        var forced = new ForcedRandom(0);
        SlamVariant picked = SlamVariantPicker.Pick(SlamVariant.Yakin, currentStreak: 2, maxSameStreak: 2, forced);
        Assert.That(picked, Is.Not.EqualTo(SlamVariant.Yakin));
    }

    /// <summary>Next(n) her zaman 0 döner — aday listesinin ilk elemanını zorlar.</summary>
    sealed class ForcedRandom : Random
    {
        readonly int _value;
        public ForcedRandom(int value) => _value = value;
        public override int Next(int maxValue) => _value;
    }
}
