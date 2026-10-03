using Dovus.Core.Data;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace CoreTests;

[TestFixture]
public class VfxBindingMapperTests
{
    [Test]
    public void PrezentasyonKatmani_HasElementColorsAndTrailStyles()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(root, "unity")))
            root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        string path = Path.Combine(root, "unity", "Assets", "Resources", "Presentation", "prezentasyon-katmani.json");
        Assert.That(File.Exists(path), Is.True);
        string json = File.ReadAllText(path);
        Assert.That(VfxBindingMapper.TryParse(json, out VfxBindingData data), Is.True);
        Assert.That(data.ElementPrimaryHex.ContainsKey("Ateş"), Is.True);
        Assert.That(data.ElementPrimaryHex["Ateş"], Does.StartWith("#"));
        Assert.That(data.TrailEntries.Count, Is.GreaterThan(5));
        Assert.That(data.ImpactStyleIds.Count, Is.GreaterThan(3));
        Assert.That(data.TrailEntries.Any(e => e.Id == "straight"), Is.True);
    }
}
