using Dovus.Game.Config;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class GameTuningHexagonXMigrationTests
{
    // v22 serileşmiş kopya yeni altıgen X'ini almazsa telefonda kayma görünmez.
    [Test]
    public void OldSerializedCopy_GetsNewHexagonCenterX()
    {
        var fresh = new GameTuning();
        fresh.EnsureRuntimeDefaults();
        var old = new GameTuning();
        old.TuningVersion = 22;
        old.Input.HexagonCenterXNorm = 0.78f;

        old.EnsureRuntimeDefaults();

        Assert.That(old.Input.HexagonCenterXNorm, Is.EqualTo(fresh.Input.HexagonCenterXNorm));
        Assert.That(old.Input.HexagonCenterXNorm, Is.GreaterThan(0.78f));
    }
}
