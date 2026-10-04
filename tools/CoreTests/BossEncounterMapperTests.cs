using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace CoreTests;

[TestFixture]
public class BossEncounterMapperTests
{
    static string BossesDir()
    {
        string root = RepoRoot();
        return Path.Combine(root, "unity", "Assets", "Resources", "Bosses");
    }

    static string RepoRoot()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(root, "unity")))
            root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        return root;
    }

    [Test]
    public void Karadul_Phase1KindsAndVolleyFields()
    {
        string json = File.ReadAllText(Path.Combine(BossesDir(), "karadul.json"));
        Assert.That(BossEncounterMapper.TryParseHud(json, out BossHudSnapshot hud), Is.True);
        Assert.That(hud.Name, Is.EqualTo("Karadul"));
        Assert.That(hud.Subtitle, Is.EqualTo("Yuvanın Efendisi"));
        Assert.That(hud.Phases.Count, Is.EqualTo(2));
        Assert.That(hud.Phases[0].Phase, Is.EqualTo(1));
        Assert.That(hud.Phases[0].Name, Is.EqualTo("Uyanış"));
        Assert.That(hud.Phases[0].UpperFrac, Is.EqualTo(1f).Within(0.001f));

        BossAttackKind[] phase1 = BossEncounterMapper.LoadPhaseAttackKinds(json, 1);
        Assert.That(phase1, Is.EqualTo(new[] { BossAttackKind.Slam, BossAttackKind.Volley }));

        IReadOnlyList<BossAttackEntry> attacks = BossEncounterMapper.LoadAttacks(json);
        var ids = attacks.Select(a => a.Id).ToList();
        Assert.That(ids, Does.Contain("slam"));
        Assert.That(ids, Does.Contain("volley"));
        Assert.That(ids, Does.Contain("fire_cone"));

        BossAttackEntry volley = attacks.First(a => a.Id == "volley");
        Assert.That(volley.Kind, Is.EqualTo(BossAttackKind.Volley));
        Assert.That(BossEncounterMapper.TryLoadAttack(json, "volley", out BossAttackEntry volley2), Is.True);
        Assert.That(volley2.Mechanics, Is.Empty);

        TargetingConfig targeting = BossEncounterMapper.LoadTargeting(json);
        Assert.That(targeting.AllyWeight, Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(targeting.DecoyPriority, Is.True);
    }

    [Test]
    public void AglarinKralicesi_PhaseAttackIdsAndWebField()
    {
        string json = File.ReadAllText(Path.Combine(BossesDir(), "aglarin-kralicesi.json"));
        Assert.That(BossEncounterMapper.TryParseHud(json, out BossHudSnapshot hud), Is.True);
        Assert.That(hud.Name, Is.EqualTo("Ağların Kraliçesi"));
        Assert.That(hud.Phases.Count, Is.EqualTo(2));

        BossAttackKind[] phase1 = BossEncounterMapper.LoadPhaseAttackKinds(json, 1);
        Assert.That(phase1, Is.EqualTo(new[]
        {
            BossAttackKind.Slam,
            BossAttackKind.Volley,
            BossAttackKind.WebField
        }));

        Assert.That(BossEncounterMapper.TryLoadAttack(json, "web_field", out BossAttackEntry web), Is.True);
        Assert.That(web.Kind, Is.EqualTo(BossAttackKind.WebField));
        Assert.That(web.Field, Is.Not.Null);
        Assert.That(web.Field.Value.RadiusM, Is.GreaterThan(0f));
    }

    [Test]
    public void EveryBossResourceFile_ParsesDocument()
    {
        foreach (string path in Directory.EnumerateFiles(BossesDir(), "*.json"))
        {
            string json = File.ReadAllText(path);
            Assert.That(BossEncounterMapper.TryParseDocument(json, out _), Is.True, path);
            Assert.That(BossEncounterMapper.TryParseHud(json, out BossHudSnapshot hud), Is.True, path);
            Assert.That(hud.Phases.Count, Is.GreaterThan(0), path);
        }
    }
}
