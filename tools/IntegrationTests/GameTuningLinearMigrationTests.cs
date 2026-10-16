using Dovus.Game.Config;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class GameTuningLinearMigrationTests
{
    // v25 (Linear renk uzayı): sahneye serileşmiş Gamma-dönemi ışık şiddeti (1,6) yeni değerle ezilmeli,
    // yoksa telefonda sahne karanlık kalır.
    [TestCase(23)]
    [TestCase(24)]
    public void OldSerializedCopy_GetsLinearKeyLightIntensity(int version)
    {
        var fresh = new GameTuning();
        fresh.EnsureRuntimeDefaults();
        var old = new GameTuning();
        old.TuningVersion = version;
        old.Arena.KeyLightIntensity = 1.6f;

        old.EnsureRuntimeDefaults();

        Assert.That(old.Arena.KeyLightIntensity, Is.EqualTo(fresh.Arena.KeyLightIntensity));
        Assert.That(old.Arena.KeyLightIntensity, Is.GreaterThan(5f));
    }
}
