using System.IO;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class MdExtractedServicesTests
{
    static string Read(string relative) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", relative)));

    [Test]
    public void ProjectileEraser_lives_in_Projectiles_namespace()
    {
        string src = Read("unity/Assets/Scripts/Game/Skills/Projectiles/ProjectileEraser.cs");
        Assert.That(src, Does.Contain("namespace Dovus.Game.Skills.Projectiles"));
        Assert.That(src, Does.Contain("class ProjectileEraser"));
    }

    [Test]
    public void MotionTemplateDriver_owns_catalog_load()
    {
        string src = Read("unity/Assets/Scripts/Game/Skills/Motion/MotionTemplateDriver.cs");
        Assert.That(src, Does.Contain("motion-templates"));
    }

    [Test]
    public void WeaponPassiveRuntime_is_sealed_not_monoBehaviour()
    {
        string src = Read("unity/Assets/Scripts/Game/Skills/Weapons/WeaponPassiveRuntime.cs");
        Assert.That(src, Does.Contain("public sealed class WeaponPassiveRuntime"));
        Assert.That(src, Does.Not.Contain(": MonoBehaviour"));
    }

    [Test]
    public void WeaponPassiveRuntime_IsSustained_whitelists_channel_combos()
    {
        string src = Read("unity/Assets/Scripts/Game/Skills/Weapons/WeaponPassiveRuntime.cs");
        Assert.That(src, Does.Contain("public static bool IsSustained"));
        Assert.That(src, Does.Contain("\"2-12\""));
    }

    [Test]
    public void OrbController_exposes_swap_hold_sec()
    {
        string src = Read("unity/Assets/Scripts/Game/Skills/Weapons/OrbController.cs");
        Assert.That(src, Does.Contain("SwapButtonHoldSec"));
        Assert.That(src, Does.Contain("TryPlace"));
    }

    [Test]
    public void TemplateDeliveryRuntime_arms_beats_from_core_builder()
    {
        string src = Read("unity/Assets/Scripts/Game/Skills/Motion/TemplateDeliveryRuntime.cs");
        Assert.That(src, Does.Contain("TemplateDelivery.Build"));
    }
}
