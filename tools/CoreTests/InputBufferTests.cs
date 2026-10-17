using Dovus.Core.Cancel;
using NUnit.Framework;

namespace CoreTests;

/// <summary>O-4.12 girdi tamponu.</summary>
[TestFixture]
public sealed class InputBufferTests
{
    [Test]
    public void O_4_12_Entry_lives_exactly_ttl()
    {
        var b = new InputBuffer();
        b.Push(ActionKind.Dodge, 0, nowMs: 1000, ttlMs: 150);
        b.Expire(1149);
        Assert.That(b.Count, Is.EqualTo(1));
        b.Expire(1150);
        Assert.That(b.Count, Is.EqualTo(0));
    }

    [Test]
    public void Same_kind_replaces_older_and_refreshes_expiry()
    {
        var b = new InputBuffer();
        b.Push(ActionKind.Skill, 7, 0, 150);
        b.Push(ActionKind.Skill, 9, 100, 150);
        Assert.That(b.Count, Is.EqualTo(1));
        Assert.That(b.Entries[0].Token, Is.EqualTo(9));
        b.Expire(200);
        Assert.That(b.Count, Is.EqualTo(1), "yenisi süreyi tazeledi");
    }

    [Test]
    public void Zero_ttl_is_not_stored()
    {
        var b = new InputBuffer();
        b.Push(ActionKind.Dodge, 0, 0, 0);
        Assert.That(b.Count, Is.EqualTo(0));
    }
}
