using NUnit.Framework;
using SweepV2;
using System.Collections.Generic;

namespace IntegrationTests;

[TestFixture]
public sealed class ReportGateTests
{
    [Test]
    public void Min_pass_per_weapon_is_144()
    {
        Assert.That(Report.MinPassPerWeapon, Is.EqualTo(Report.WeaponCases));
    }

    [Test]
    public void Gate_compare_allows_known_play_diff_only()
    {
        var agreement = new Report.Agreement();
        agreement.Mismatches.Add("2-9 [Kılıç] Play=KALDI başsız=geçti");
        agreement.Mismatches.Add("9-9 [Kılıç] Play=KALDI başsız=geçti");
        var known = new HashSet<(string, string)> { ("2-9", "Kılıç") };
        var fails = Report.GateCompareFailures(agreement, known);
        Assert.That(fails.Count, Is.EqualTo(1));
        Assert.That(fails[0], Does.Contain("9-9"));
    }
}
