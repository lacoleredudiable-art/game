using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Status;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class WeaponPayoffTests
{
    [Test]
    public void OrbHud_TapSendsWhenAtHand_AndRecallsWhenAway()
    {
        Assert.That(OrbHudCommand.Tap(atHand: true), Is.EqualTo(OrbGestureResult.Place));
        Assert.That(OrbHudCommand.Tap(atHand: false), Is.EqualTo(OrbGestureResult.Recall));
    }

    [Test]
    public void Cannon_PushesAway_ButNotIntoTheBoss_AndWaitsOutTheTemplate()
    {
        float x = 1.2f;
        float z = 0f;
        CannonBlast.Move(ref x, ref z, -1f, 0f, 0.5f, 0f, 0f, 1.5f, 50f, 0.5f);
        Assert.That(x, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(z, Is.EqualTo(0f).Within(0.001f));

        float far = 48f;
        float fz = 0f;
        CannonBlast.Move(ref far, ref fz, 1f, 0f, 5f, 0f, 0f, 0f, 50f, 0.5f);
        Assert.That(far, Is.LessThanOrEqualTo(49.5f));

        var recoil = new CannonRecoil();
        float px = 3f;
        float pz = 0f;
        recoil.Queue(1f, 0f, 0.5f, 0f, 0f, 1.5f, 50f, 0.5f);
        Assert.That(recoil.TryApply(templateOwnsPosition: true, ref px, ref pz), Is.False);
        Assert.That(px, Is.EqualTo(3f));
        Assert.That(recoil.Pending, Is.True);
        Assert.That(recoil.TryApply(templateOwnsPosition: false, ref px, ref pz), Is.True);
        Assert.That(px, Is.EqualTo(3.5f).Within(0.001f));
        Assert.That(recoil.Pending, Is.False);
    }

    [Test]
    public void ShortShield_AbsorbsAfterDodge_AndExpires()
    {
        var shield = new WeaponShortShield();
        shield.Grant(15f, 0, 3f);
        Assert.That(shield.Points, Is.EqualTo(15f));

        Assert.That(shield.Absorb(10f, 100), Is.EqualTo(0f));
        Assert.That(shield.Points, Is.EqualTo(5f));
        Assert.That(shield.Absorb(8f, 2000), Is.EqualTo(3f));
        Assert.That(shield.Points, Is.EqualTo(0f));

        shield.Grant(15f, 0, 3f);
        Assert.That(shield.Absorb(4f, 3000), Is.EqualTo(4f));
        Assert.That(shield.Points, Is.EqualTo(15f));
    }

    [Test]
    public void Talisman_CleansesOneNegativeStatus()
    {
        var board = new StatusBoard();
        board.Apply(StatusKind.Burn, 4000, 1f);
        board.Apply(StatusKind.Poison, 4000, 1f);
        board.Apply(StatusKind.Haste, 4000, 1f);
        Assert.That(OneNegativeCleanse.TryRemove(board), Is.True);
        int negatives = 0;
        if (board.Has(StatusKind.Burn)) negatives++;
        if (board.Has(StatusKind.Poison)) negatives++;
        Assert.That(negatives, Is.EqualTo(1));
        Assert.That(board.Has(StatusKind.Haste), Is.True);
        Assert.That(OneNegativeCleanse.TryRemove(board), Is.True);
        Assert.That(board.Has(StatusKind.Burn) || board.Has(StatusKind.Poison), Is.False);
        Assert.That(OneNegativeCleanse.TryRemove(board), Is.False);
    }

    [Test]
    public void Book_FreeMana_DoesNotSpend()
    {
        var tracker = new ResourceTracker(100f, 8f, 1.5f);
        WeaponManaWaiver.Charge(tracker, 20f, freeCast: true);
        Assert.That(tracker.Mana, Is.EqualTo(100f));
        WeaponManaWaiver.Charge(tracker, 20f, freeCast: false);
        Assert.That(tracker.Mana, Is.EqualTo(80f));
    }

    [Test]
    public void CannonAndOrb_ReadTheDocNumbers()
    {
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!System.IO.File.Exists(path))
            path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        string json = System.IO.File.ReadAllText(path);
        var catalog = EquipmentCatalog.FromJson(json);
        EquipmentItem cannon = catalog.FindWeapon(7);
        EquipmentItem orb = catalog.FindWeapon(5);
        Assert.That(cannon.Profile.BasicRadiusM, Is.EqualTo(3f));
        Assert.That(cannon.Profile.BossPushM, Is.EqualTo(0.5f));
        Assert.That(cannon.Profile.RecoilM, Is.EqualTo(0.5f));
        Assert.That(orb.Profile.OrbHoldSec, Is.EqualTo(0.4f));
        Assert.That(orb.Profile.OrbDoubleTapSec, Is.EqualTo(0.3f));
    }
}
