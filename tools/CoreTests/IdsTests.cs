using Dovus.Core.Shared;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class IdsTests
{
    [Test]
    public void SkillId_NullBecomesEmpty_OrdinalEqualityAndConversion()
    {
        var a = new SkillId(null);
        var b = new SkillId("");
        Assert.That(a.IsEmpty, Is.True);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(a.ToString(), Is.EqualTo(""));
        Assert.That((string)a, Is.EqualTo(""));

        var c = new SkillId("3-5");
        var d = (SkillId)"3-5";
        Assert.That(c, Is.EqualTo(d));
        Assert.That(c != new SkillId("3-6"), Is.True);
    }

    [Test]
    public void WeaponId_RuneId_ElementId_ActorId_ShareSameSemantics()
    {
        Assert.That(new WeaponId("kilic"), Is.EqualTo((WeaponId)"kilic"));
        Assert.That(new RuneId("7"), Is.EqualTo((RuneId)"7"));
        Assert.That(new ElementId("ates"), Is.EqualTo((ElementId)"ates"));
        Assert.That(new ActorId("boss"), Is.EqualTo((ActorId)"boss"));
        Assert.That(new ElementId(null).IsEmpty, Is.True);
    }
}
