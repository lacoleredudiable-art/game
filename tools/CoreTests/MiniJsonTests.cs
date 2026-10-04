using Dovus.Core.Grammar;
using NUnit.Framework;

using Dovus.Core.Shared;
namespace CoreTests;

[TestFixture]
public class MiniJsonTests
{
    [Test]
    public void Parses_NestedObjectsArraysAndScalars()
    {
        JsonValue v = MiniJson.Parse(@"{
            ""name"": ""Test"",
            ""num"": 12.5,
            ""flag"": true,
            ""nil"": null,
            ""list"": [1, 2, 3],
            ""nested"": { ""inner"": ""value"" }
        }");
        Assert.That(v["name"].AsString(), Is.EqualTo("Test"));
        Assert.That(v["num"].AsFloat(), Is.EqualTo(12.5f).Within(0.001f));
        Assert.That(v["flag"].AsBool(), Is.True);
        Assert.That(v["nil"].IsNull, Is.True);
        Assert.That(v["list"].AsArray().Count, Is.EqualTo(3));
        Assert.That(v["nested"]["inner"].AsString(), Is.EqualTo("value"));
    }

    [Test]
    public void MissingField_ReturnsNullSentinel_NoThrow()
    {
        JsonValue v = MiniJson.Parse(@"{""a"": 1}");
        Assert.That(v["b"].IsNull, Is.True);
        Assert.That(v["b"]["c"].IsNull, Is.True);
        Assert.That(v["b"].AsFloat(7f), Is.EqualTo(7f));
    }

    [Test]
    public void AsTextOrField_HandlesFlatStringAndNestedReadAs()
    {
        JsonValue flat = MiniJson.Parse(@"{""self"": ""kendine_topla""}")["self"];
        Assert.That(flat.AsTextOrField("read_as"), Is.EqualTo("kendine_topla"));

        JsonValue nested = MiniJson.Parse(@"{""self"": {""read_as"": ""Kendine topla""}}")["self"];
        Assert.That(nested.AsTextOrField("read_as"), Is.EqualTo("Kendine topla"));
    }
}
