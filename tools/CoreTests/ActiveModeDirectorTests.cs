using System.Collections.Generic;
using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json "active_modes" (ulti) — 6 mod, aynı elementin 4'lüsü.
/// 16 Eylül: "v5.2.1 hiç tam aktif olmadı" güven kaygısına karşılık — bu testler hem
/// motorun parse ettiğini hem ActiveModeDirector'ın tetik/soğuma/süre mantığını kanıtlar.
/// </summary>
[TestFixture]
public class ActiveModeDirectorTests
{
    static SkillMotor LoadFull()
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
        return SkillMotor.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void Motor_Parses_SixActiveModes_OneUsePerElement()
    {
        var motor = LoadFull();
        Assert.That(motor.ActiveModes.Count, Is.EqualTo(6));

        var dots = new HashSet<int>();
        foreach (var m in motor.ActiveModes)
            dots.Add(m.TriggerDot);
        Assert.That(dots, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6 }));
    }

    static ActiveModeNode Find(SkillMotor motor, string id)
    {
        foreach (var m in motor.ActiveModes)
            if (m.Id == id) return m;
        Assert.Fail($"active mode bulunamadı: {id}");
        return default;
    }

    [Test]
    public void OfkePatlamasi_ParsesEffectsAndHpCost()
    {
        var m = Find(LoadFull(), "ofke_patlamasi");
        Assert.That(m.TriggerDot, Is.EqualTo(1));
        Assert.That(m.HasDuration, Is.True);
        Assert.That(m.DurationSec, Is.EqualTo(8f));
        Assert.That(m.CooldownSec, Is.EqualTo(45f));
        Assert.That(m.ActivationType, Is.EqualTo("dealt_damage_recently"));
        Assert.That(m.ActivationWithinSec, Is.EqualTo(5f));
        Assert.That(m.GetEffect("damage_mult"), Is.EqualTo(1.8f).Within(0.001f));
        Assert.That(m.HpPerSecPercentCost, Is.EqualTo(3f));
        Assert.That(m.BlocksMovement, Is.False);
    }

    [Test]
    public void FirtinaAkisi_DefenseDropTextParsedAsMultiplier()
    {
        // "savunma %50 düşer" -> 1.5 (sayı uydurma değil, metnin içindeki yüzdeden türetilir)
        var m = Find(LoadFull(), "firtina_akisi");
        Assert.That(m.DefenseDropMult, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(m.GetEffect("move_speed_mult"), Is.EqualTo(2.0f).Within(0.001f));
    }

    [Test]
    public void KutsalKaynak_And_AsilmazDuvar_BlockMovement()
    {
        var motor = LoadFull();
        Assert.That(Find(motor, "kutsal_kaynak").BlocksMovement, Is.True);
        Assert.That(Find(motor, "asilmaz_duvar").BlocksMovement, Is.True);
    }

    [Test]
    public void KanCilginligi_HasNoDuration_AndHealBreaksIt()
    {
        var m = Find(LoadFull(), "kan_cilginligi");
        Assert.That(m.HasDuration, Is.False, "duration_sec: null -> süresiz");
        Assert.That(m.HealBreaksMode, Is.True);
        Assert.That(m.DeactivationType, Is.EqualTo("hp_above_percent"));
        Assert.That(m.DeactivationThreshold, Is.EqualTo(0.3f));
        Assert.That(m.GetEffect("lifesteal"), Is.EqualTo(0.15f).Within(0.001f));
    }

    [Test]
    public void TryTrigger_RespectsActivationCondition()
    {
        var director = new ActiveModeDirector(LoadFull().ActiveModes);

        // Son 5 sn içinde hasar yok -> ofke_patlamasi (dot 1) açılmamalı.
        var ctxNoDamage = new ActiveModeContext { SecondsSinceLastDamageDealt = 99 };
        Assert.That(director.TryTrigger(1, ctxNoDamage, worldMs: 0), Is.Null);
        Assert.That(director.Active, Is.Null);

        // Yakın zamanda hasar verildi -> açılmalı.
        var ctxDamage = new ActiveModeContext { SecondsSinceLastDamageDealt = 1.5 };
        var activated = director.TryTrigger(1, ctxDamage, worldMs: 0);
        Assert.That(activated, Is.Not.Null);
        Assert.That(activated.Value.Id, Is.EqualTo("ofke_patlamasi"));
        Assert.That(director.Active.Value.Id, Is.EqualTo("ofke_patlamasi"));
        Assert.That(director.DamageMult, Is.EqualTo(1.8f).Within(0.001f));
    }

    [Test]
    public void TryTrigger_BlockedWhileOnCooldown()
    {
        var director = new ActiveModeDirector(LoadFull().ActiveModes);
        var ctx = new ActiveModeContext { SecondsSinceLastDamageDealt = 0 };

        var first = director.TryTrigger(1, ctx, worldMs: 0);
        Assert.That(first, Is.Not.Null);

        // Süre dolunca kapat (8 sn), soğuma 45 sn -> hemen tekrar tetiklenmemeli.
        director.Tick(8001, ctx);
        Assert.That(director.Active, Is.Null);

        var second = director.TryTrigger(1, ctx, worldMs: 8100);
        Assert.That(second, Is.Null, "45 sn soğuma dolmadan tekrar açılmamalı");
        Assert.That(director.CooldownRemainingSec("ofke_patlamasi", 8100), Is.GreaterThan(0f));

        // Soğuma dolunca tekrar açılabilmeli.
        var third = director.TryTrigger(1, ctx, worldMs: 0 + 45_000 + 1);
        Assert.That(third, Is.Not.Null);
    }

    [Test]
    public void Tick_ExpiresAfterDuration()
    {
        var director = new ActiveModeDirector(LoadFull().ActiveModes);
        var ctx = new ActiveModeContext { SecondsSinceLastDamageDealt = 0 };
        director.TryTrigger(1, ctx, worldMs: 1000);

        Assert.That(director.Tick(1000 + 7000, ctx), Is.False, "8 sn dolmadı");
        Assert.That(director.Active, Is.Not.Null);

        Assert.That(director.Tick(1000 + 8001, ctx), Is.True, "8 sn doldu, kapanmalı");
        Assert.That(director.Active, Is.Null);
    }

    [Test]
    public void Tick_KanCilginligi_DeactivatesWhenHpRecovers()
    {
        var director = new ActiveModeDirector(LoadFull().ActiveModes);
        var lowHp = new ActiveModeContext { HpRatio = 0.2f };
        var activated = director.TryTrigger(6, lowHp, worldMs: 0);
        Assert.That(activated, Is.Not.Null);

        // Süresiz oldugu için sadece HP eşiği kapatabilir.
        Assert.That(director.Tick(worldMs: 999_999, lowHp), Is.False);

        var healedUp = new ActiveModeContext { HpRatio = 0.5f };
        Assert.That(director.Tick(worldMs: 1_000_000, healedUp), Is.True);
        Assert.That(director.Active, Is.Null);
    }

    [Test]
    public void NotifyHealed_EndsKanCilginligiOnly()
    {
        var director = new ActiveModeDirector(LoadFull().ActiveModes);
        director.TryTrigger(6, new ActiveModeContext { HpRatio = 0.1f }, worldMs: 0);
        Assert.That(director.NotifyHealed(), Is.True);
        Assert.That(director.Active, Is.Null);

        // Heal ile bitmeyen bir modda (ofke) hiçbir şey yapmamalı.
        director.TryTrigger(1, new ActiveModeContext { SecondsSinceLastDamageDealt = 0 }, worldMs: 0);
        Assert.That(director.NotifyHealed(), Is.False);
        Assert.That(director.Active, Is.Not.Null);
    }
}
