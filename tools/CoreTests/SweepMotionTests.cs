using Dovus.Core.Motion;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SweepMotionTests
{
    [Test]
    public void JumpLimit_IsTwentyFiveMetersPerSecondTimesFrame_PlusMargin()
    {
        Assert.That(SweepMotion.JumpLimit(1f / 60f), Is.EqualTo(25f / 60f + 0.2f).Within(0.0001f));
        Assert.That(SweepMotion.JumpLimit(0.001f), Is.EqualTo(25f / 60f + 0.2f).Within(0.0001f));
        Assert.That(SweepMotion.JumpLimit(0.038f), Is.EqualTo(25f * 0.038f + 0.2f).Within(0.0001f));
    }

    [Test]
    public void Menus_ListEachWeapon_AndAllWeaponsIsEveryCombo()
    {
        Assert.That(SweepMotion.WeaponMenuNames, Has.Length.EqualTo(10));
        Assert.That(SweepMotion.WeaponMenuNames, Does.Contain("Kılıç"));
        Assert.That(SweepMotion.WeaponMenuNames, Does.Contain("Kalkan"));
        Assert.That(SweepMotion.IsAllWeapons("hepsi"), Is.True);
        Assert.That(SweepMotion.IsAllWeapons("kilic"), Is.False);
        Assert.That(SweepMotion.ComboCount("hepsi"), Is.EqualTo(1440));
        Assert.That(SweepMotion.ComboCount("kilic+asa"), Is.EqualTo(288));
        Assert.That(SweepMotion.ComboCount("yumruk"), Is.EqualTo(144));
    }
}
