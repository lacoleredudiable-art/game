using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CoreTests;

[TestFixture]
public sealed class CaptureAnglesJsonTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string AnglesPath() => Path.Combine(RepoRoot(), "tools", "capture", "angles.json");

    [Test]
    public void AnglesJson_NamesUnique_AndResolutionPositive()
    {
        string path = AnglesPath();
        Assert.That(File.Exists(path), Is.True, path);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = doc.RootElement;
        Assert.That(root.TryGetProperty("angles", out JsonElement angles), Is.True);
        Assert.That(angles.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(angles.GetArrayLength(), Is.GreaterThanOrEqualTo(4));

        var seen = new HashSet<string>();
        foreach (JsonElement entry in angles.EnumerateArray())
        {
            string name = entry.GetProperty("name").GetString();
            Assert.That(string.IsNullOrWhiteSpace(name), Is.False, "name boş olamaz");
            Assert.That(seen.Add(name), Is.True, "benzersiz name: " + name);

            AssertVec3(entry, "position");
            AssertVec3(entry, "euler");

            Assert.That(entry.GetProperty("fov").GetDouble(), Is.GreaterThan(0));
            int width = entry.GetProperty("width").GetInt32();
            int height = entry.GetProperty("height").GetInt32();
            Assert.That(width, Is.GreaterThan(0));
            Assert.That(height, Is.GreaterThan(0));
        }
    }

    static void AssertVec3(JsonElement entry, string field)
    {
        JsonElement vec = entry.GetProperty(field);
        Assert.That(vec.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(vec.GetArrayLength(), Is.EqualTo(3));
    }
}
