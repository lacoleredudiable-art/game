using Dovus.App.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class PlayerHealthTests
{
    [Test]
    public void Bind_RoundsStartHp_BankersRounding()
    {
        var h = new PlayerHealth();
        h.Bind(25, 0.5f);
        Assert.That(h.Hp, Is.EqualTo(12));
        Assert.That(h.MaxHp, Is.EqualTo(25));

        h.Bind(27, 0.5f);
        Assert.That(h.Hp, Is.EqualTo(14));
    }

    [Test]
    public void Bind_ClampsRatioAndMinimumHp()
    {
        var h = new PlayerHealth();
        h.Bind(10, -1f);
        Assert.That(h.Hp, Is.EqualTo(1));

        h.Bind(10, 2f);
        Assert.That(h.Hp, Is.EqualTo(10));

        h.Bind(1, 0f);
        Assert.That(h.Hp, Is.EqualTo(1));
    }

    [Test]
    public void SetDevHp_PoolWhenEnabled_ClampWhenDisabled()
    {
        var h = new PlayerHealth();
        h.Bind(50, 1f);
        h.SetDevHp(true);
        Assert.That(h.MaxHp, Is.EqualTo(DevPlayerHp.Pool));
        Assert.That(h.Hp, Is.EqualTo(DevPlayerHp.Pool));
        Assert.That(h.IsDown, Is.False);

        h.SetDevHp(false);
        Assert.That(h.MaxHp, Is.EqualTo(50));
        Assert.That(h.Hp, Is.EqualTo(50));
    }

    [Test]
    public void SetMaxHp_ReduceDoesNotKill()
    {
        var h = new PlayerHealth();
        h.Bind(100, 1f);
        h.SetMaxHp(30);
        Assert.That(h.Hp, Is.EqualTo(30));
        Assert.That(h.IsDown, Is.False);
    }

    [Test]
    public void SuppressDown_KeepsOneHp()
    {
        var h = new PlayerHealth();
        h.Bind(10, 1f);
        h.SuppressDown = true;
        var loss = h.ApplyHpLoss(50, 0f, 2f);
        Assert.That(loss.Kind, Is.EqualTo(HpLossKind.Hit));
        Assert.That(h.Hp, Is.EqualTo(1));
        Assert.That(h.IsDown, Is.False);
    }

    [Test]
    public void ApplyHpLoss_Down_SetsRespawnTimer()
    {
        var h = new PlayerHealth();
        h.Bind(10, 1f);
        var loss = h.ApplyHpLoss(10, 100f, 5f);
        Assert.That(loss.Kind, Is.EqualTo(HpLossKind.Down));
        Assert.That(h.IsDown, Is.True);
        Assert.That(h.RespawnInSec(102f), Is.EqualTo(3f).Within(0.0001f));
        Assert.That(h.RespawnInSec(105f), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void ReviveDue_AtBoundary()
    {
        var h = new PlayerHealth();
        h.Bind(10, 1f);
        h.ApplyHpLoss(10, 0f, 5f);
        Assert.That(h.ReviveDue(4.999f), Is.False);
        Assert.That(h.ReviveDue(5f), Is.True);
        h.Revive();
        Assert.That(h.Hp, Is.EqualTo(10));
        Assert.That(h.IsDown, Is.False);
    }

    [Test]
    public void ApplyHeal_CapsAtMax_ZeroWhenDown()
    {
        var h = new PlayerHealth();
        h.Bind(10, 0.5f);
        Assert.That(h.ApplyHeal(100), Is.EqualTo(5));
        Assert.That(h.Hp, Is.EqualTo(10));

        h.ApplyHpLoss(10, 0f, 2f);
        Assert.That(h.ApplyHeal(5), Is.EqualTo(0));
    }
}
