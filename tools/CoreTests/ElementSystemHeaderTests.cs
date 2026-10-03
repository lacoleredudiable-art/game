using Dovus.Core.Data;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class ElementSystemHeaderTests
{
    static string ElementSystemJson()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(root, "docs")))
            root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        string path = Path.Combine(root, "docs", "element-sistemi.json");
        Assert.That(File.Exists(path), Is.True);
        return File.ReadAllText(path);
    }

    [Test]
    public void CanonicalSpec_VersionBindingAndTransition()
    {
        string json = ElementSystemJson();
        Assert.That(ElementSystemHeader.TryParse(json, 300, out ElementSystemHeader header), Is.True);
        Assert.That(header.Version, Is.EqualTo("6.1.1"));
        Assert.That(header.Binding, Is.True);
        Assert.That(header.SelectionTransitionMs, Is.EqualTo(300));
    }

    [Test]
    public void MissingTransition_UsesCallerFallback()
    {
        const string minimal = "{\"system\":{\"version\":\"1\",\"binding\":true}}";
        Assert.That(ElementSystemHeader.TryParse(minimal, 300, out ElementSystemHeader header), Is.True);
        Assert.That(header.SelectionTransitionMs, Is.EqualTo(300));
        Assert.That(ElementSystemHeader.TryParse(minimal, 999, out header), Is.True);
        Assert.That(header.SelectionTransitionMs, Is.EqualTo(999));
    }
}
