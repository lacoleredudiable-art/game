using System.Collections.Generic;
using Dovus.Core.Combat;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// manipulation_layers.time_layer — 4 tipin worldMs zamanlama hesabı (uygulama yok).
/// JSON: max_active_fields=2; karabasan 2s; alev 1s×0.6; lav ×1.7; cehennem 2s.
/// </summary>
[TestFixture]
public class TimeEffectDirectorTests
{
    [Test]
    public void Defaults_MatchJson_MaxActiveFields()
    {
        var dir = new TimeEffectDirector();
        Assert.That(dir.MaxActiveFields, Is.EqualTo(2));
        Assert.That(TimeEffectDirector.DefaultMaxActiveFields, Is.EqualTo(2));
    }

    [Test]
    public void DelayedDetonation_TriggersAfterDelay_WorldMs()
    {
        // karabasan_gecikme: delay_sec 2.0
        var dir = new TimeEffectDirector();
        Assert.That(
            dir.TryScheduleDelayedDetonation("karabasan_gecikme", "Karabasan", 2f, worldMs: 1000,
                out var field, pendingDamage: 42f),
            Is.True);

        Assert.That(field.Type, Is.EqualTo(TimeEffectTypes.DelayedDetonation));
        Assert.That(field.TriggerAtMs, Is.EqualTo(3000));
        Assert.That(dir.RemainingSec(field.Id, 1000), Is.EqualTo(2f).Within(0.0001f));
        Assert.That(dir.RemainingSec(field.Id, 2500), Is.EqualTo(0.5f).Within(0.0001f));

        var due = new List<TimeEffectField>();
        Assert.That(dir.CollectDue(2999, due), Is.EqualTo(0));
        Assert.That(dir.ActiveFields.Count, Is.EqualTo(1));

        Assert.That(dir.CollectDue(3000, due), Is.EqualTo(1));
        Assert.That(due[0].Id, Is.EqualTo(field.Id));
        Assert.That(due[0].ComputedDetonationDamage, Is.EqualTo(42f).Within(0.0001f));
        Assert.That(dir.ActiveFields.Count, Is.EqualTo(0));
    }

    [Test]
    public void Echo_SchedulesDamageRatio_AfterDelay()
    {
        // alev_yanki: delay_sec 1.0, damage_ratio 0.6
        var dir = new TimeEffectDirector();
        Assert.That(
            dir.TryScheduleEcho("alev_yanki", "Alev", 1f, 0.6f, sourceDamage: 50f, worldMs: 0,
                out var field),
            Is.True);

        Assert.That(field.Type, Is.EqualTo(TimeEffectTypes.Echo));
        Assert.That(field.TriggerAtMs, Is.EqualTo(1000));
        Assert.That(field.DamageRatio, Is.EqualTo(0.6f).Within(0.0001f));
        Assert.That(field.ComputedEchoDamage, Is.EqualTo(30f).Within(0.0001f));
        Assert.That(TimeEffectDirector.EchoDamage(50f, 0.6f), Is.EqualTo(30f).Within(0.0001f));

        var due = new List<TimeEffectField>();
        Assert.That(dir.CollectDue(999, due), Is.EqualTo(0));
        Assert.That(dir.CollectDue(1000, due), Is.EqualTo(1));
        Assert.That(due[0].ComputedEchoDamage, Is.EqualTo(30f).Within(0.0001f));
    }

    [Test]
    public void ExtendLifetime_MultipliesRemainingSec()
    {
        // lav_kalicilik: multiplier 1.7 — anlık, alan yuvası tutmaz
        float extended = TimeEffectDirector.ExtendRemainingSec(10f, 1.7f);
        Assert.That(extended, Is.EqualTo(17f).Within(0.0001f));

        Assert.That(TimeEffectDirector.ExtendRemainingSec(8f, 1.7f), Is.EqualTo(13.6f).Within(0.0001f));
        Assert.That(TimeEffectDirector.ExtendRemainingSec(-1f, 1.7f), Is.EqualTo(0f));
        Assert.That(TimeEffectDirector.ExtendRemainingSec(5f, 0f), Is.EqualTo(0f));

        var dir = new TimeEffectDirector();
        Assert.That(dir.ActiveFields.Count, Is.EqualTo(0), "extend alan yuvası tüketmez");
    }

    [Test]
    public void DeathDelay_DefersTrigger_FromDeclaredDeathMs()
    {
        // cehennem_yavas_olme: delay_sec 2.0
        var dir = new TimeEffectDirector();
        const double deathDeclaredAtMs = 5000;
        Assert.That(
            dir.TryScheduleDeathDelay("cehennem_yavas_olme", "Cehennem", 2f, deathDeclaredAtMs,
                out var field),
            Is.True);

        Assert.That(field.Type, Is.EqualTo(TimeEffectTypes.DeathDelay));
        Assert.That(field.TriggerAtMs, Is.EqualTo(7000));
        Assert.That(dir.RemainingSec(field.Id, 5000), Is.EqualTo(2f).Within(0.0001f));
        Assert.That(dir.RemainingSec(field.Id, 6500), Is.EqualTo(0.5f).Within(0.0001f));

        var due = new List<TimeEffectField>();
        Assert.That(dir.CollectDue(6999, due), Is.EqualTo(0));
        Assert.That(dir.CollectDue(7000, due), Is.EqualTo(1));
        Assert.That(due[0].EffectId, Is.EqualTo("cehennem_yavas_olme"));
    }

    [Test]
    public void SoftCap_DropsOldest_WhenMaxActiveFieldsFull()
    {
        var dir = new TimeEffectDirector(maxActiveFields: 2);
        Assert.That(dir.TryScheduleDelayedDetonation("a", "Karabasan", 2f, 0, out var first), Is.True);
        Assert.That(dir.TryScheduleEcho("b", "Alev", 1f, 0.6f, 10f, 0, out _), Is.True);
        Assert.That(dir.ActiveFields.Count, Is.EqualTo(2));

        Assert.That(dir.TryScheduleDeathDelay("c", "Cehennem", 2f, 0, out var newest), Is.True);
        Assert.That(dir.ActiveFields.Count, Is.EqualTo(2));
        Assert.That(dir.ActiveFields[0].Id, Is.Not.EqualTo(first.Id), "en eski düşmeli");
        Assert.That(dir.ActiveFields[1].Id, Is.EqualTo(newest.Id));
    }

    [Test]
    public void TrySchedule_RejectsNonPositiveDelay()
    {
        var dir = new TimeEffectDirector();
        Assert.That(dir.TryScheduleDelayedDetonation("x", "Karabasan", 0f, 0, out _), Is.False);
        Assert.That(dir.TryScheduleEcho("x", "Alev", -1f, 0.6f, 10f, 0, out _), Is.False);
        Assert.That(dir.TryScheduleDeathDelay("x", "Cehennem", 0f, 0, out _), Is.False);
        Assert.That(dir.ActiveFields.Count, Is.EqualTo(0));
    }
}
