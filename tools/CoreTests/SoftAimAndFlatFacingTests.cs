using Dovus.App.Casting;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SoftAimAndFlatFacingTests
{
    [Test]
    public void FlatBodyForward_ZeroInput_ReturnsWorldForward()
    {
        FlatFacingMath.FlatBodyForward(0f, 0f, out float x, out float z);
        Assert.That(x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(z, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void FlatAngleDeg_RightAngle_IsNinety()
    {
        float angle = FlatFacingMath.FlatAngleDeg(1f, 0f, 0f, 1f);
        Assert.That(angle, Is.EqualTo(90f).Within(0.05f));
    }

    [Test]
    public void SoftAimResolver_BossInsideCone_SnapsToBoss()
    {
        SoftAimResolver.Resolve(
            playerForwardX: 1f,
            playerForwardZ: 0f,
            velocityX: 0f,
            velocityZ: 0f,
            velocitySqrThreshold: 0.05f,
            hasBoss: true,
            bossX: 5f,
            bossZ: 0f,
            posX: 0f,
            posZ: 0f,
            softAimRangeM: 8f,
            softAimConeDeg: 70f,
            out float facingX,
            out float facingZ);

        Assert.That(facingX, Is.EqualTo(1f).Within(0.001f));
        Assert.That(facingZ, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void SoftAimResolver_BossOutsideCone_KeepsFacing()
    {
        SoftAimResolver.Resolve(
            playerForwardX: 0f,
            playerForwardZ: 1f,
            velocityX: 0f,
            velocityZ: 0f,
            velocitySqrThreshold: 0.05f,
            hasBoss: true,
            bossX: 5f,
            bossZ: 0f,
            posX: 0f,
            posZ: 0f,
            softAimRangeM: 8f,
            softAimConeDeg: 10f,
            out float facingX,
            out float facingZ);

        Assert.That(facingX, Is.EqualTo(0f).Within(0.001f));
        Assert.That(facingZ, Is.EqualTo(1f).Within(0.001f));
    }
}
