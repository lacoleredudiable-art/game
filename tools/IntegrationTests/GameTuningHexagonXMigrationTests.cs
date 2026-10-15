using Dovus.Game.Config;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class GameTuningHexagonXMigrationTests
{
    // v22 serileşmiş kopya yeni altıgen X'ini almazsa telefonda kayma görünmez.
    // 23: ışık ve arayüz taslakları ayrı ayrı 23'e çıkmıştı; birleşik sürüm 24 ikisini de yeniden uygular.
    [TestCase(22)]
    [TestCase(23)]
    public void OldSerializedCopy_GetsNewHexagonCenterX(int version)
    {
        var fresh = new GameTuning();
        fresh.EnsureRuntimeDefaults();
        var old = new GameTuning();
        old.TuningVersion = version;
        old.Input.HexagonCenterXNorm = 0.78f;

        old.EnsureRuntimeDefaults();

        Assert.That(old.Input.HexagonCenterXNorm, Is.EqualTo(fresh.Input.HexagonCenterXNorm));
        Assert.That(old.Input.HexagonCenterXNorm, Is.GreaterThan(0.78f));
    }
}
