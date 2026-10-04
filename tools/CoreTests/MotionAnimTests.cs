using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.IO;

using Dovus.Core.Shared;
namespace CoreTests;

[TestFixture]
public class MotionAnimTests
{
    [SetUp]
    public void Reset() => DesignWarnings.ResetForTests();

    [Test]
    public void EveryPhase_DeclaresAKnownAnimKey()
    {
        var catalog = Load();
        Assert.That(catalog.Anims.Resolve("spin", 0, 0).State, Is.EqualTo("Spin"));
        Assert.That(catalog.Anims.Resolve("lunge", 0, 0).State, Is.EqualTo("BasicStrike"));
        Assert.That(catalog.Anims.Resolve("dash", 0, 0).Fallback, Is.False);

        for (int i = 0; i < catalog.Templates.Count; i++)
        {
            MotionTemplate template = catalog.Templates[i];
            for (int p = 0; p < template.Phases.Count; p++)
            {
                string key = template.Phases[p].Anim;
                Assert.That(MotionAnimTable.IsKnown(key), Is.True, template.Id + " " + key);
                Assert.That(template.Phases[p].AnimSpeed, Is.GreaterThan(0.2f));
            }
        }

        Assert.That(catalog.TryGetTemplate("yukle_birak", out MotionTemplate charge), Is.True);
        Assert.That(charge.Phases[0].Anim, Is.EqualTo("windup"));
        Assert.That(charge.Phases[1].Anim, Is.EqualTo("lunge"));
        Assert.That(catalog.TryGetTemplate("donen_kesik", out MotionTemplate spin), Is.True);
        Assert.That(spin.Phases[0].Anim, Is.EqualTo("spin"));
    }

    [Test]
    public void Bridge_PrefersWeaponRow_AndFallsBackOnce()
    {
        const string json = """
        {"anim_bridge":{"fallback_state":"Locomotion","rows":[
          {"key":"lunge","weapon":0,"verb":0,"state":"BasicStrike","trigger":""},
          {"key":"lunge","weapon":4,"verb":1,"state":"SwordThrust","trigger":"DoThrust"}
        ]}}
        """;
        var table = MotionAnimTable.Parse(MiniJson.Parse(json));
        MotionAnimClip sword = table.Resolve("lunge", 4, 1);
        Assert.That(sword.State, Is.EqualTo("SwordThrust"));
        Assert.That(sword.Trigger, Is.EqualTo("DoThrust"));
        Assert.That(sword.Fallback, Is.False);

        MotionAnimClip other = table.Resolve("lunge", 2, 1);
        Assert.That(other.State, Is.EqualTo("BasicStrike"));
        Assert.That(other.Fallback, Is.False);

        int warnings = 0;
        DesignWarnings.Warned += message =>
        {
            if (message != null && message.Contains("anahtarı yok", System.StringComparison.Ordinal))
                warnings++;
        };
        MotionAnimClip missing = table.Resolve("yokboyle", 0, 0);
        MotionAnimClip again = table.Resolve("yokboyle", 4, 1);
        Assert.That(missing.Fallback, Is.True);
        Assert.That(missing.State, Is.EqualTo("Locomotion"));
        Assert.That(again.State, Is.EqualTo("Locomotion"));
        Assert.That(warnings, Is.EqualTo(1));
    }

    [Test]
    public void Velocity_FeedsForwardStrafeAndSpeed()
    {
        const float walk = 6.4f;
        LocoBlend forward = LocoBlend.FromVelocity(0f, 4f, 0f, 1f, walk);
        Assert.That(forward.SpeedMps, Is.EqualTo(4f).Within(0.01f));
        Assert.That(forward.Speed01, Is.GreaterThan(0.5f));
        Assert.That(forward.Forward, Is.GreaterThan(0.9f));
        Assert.That(forward.Strafe, Is.EqualTo(0f).Within(0.05f));

        LocoBlend back = LocoBlend.FromVelocity(0f, -3f, 0f, 1f, walk);
        Assert.That(back.Forward, Is.LessThan(-0.9f));
        Assert.That(back.Speed01, Is.GreaterThan(0.3f));

        LocoBlend strafe = LocoBlend.FromVelocity(3f, 0f, 0f, 1f, walk);
        Assert.That(strafe.Strafe, Is.GreaterThan(0.9f));
        Assert.That(strafe.Forward, Is.EqualTo(0f).Within(0.05f));

        LocoBlend still = LocoBlend.FromVelocity(0f, 0f, 0f, 1f, walk);
        Assert.That(still.Speed01, Is.EqualTo(0f));
        Assert.That(still.Forward, Is.EqualTo(0f));
    }

    [Test]
    public void Runner_ReportsWindupThenLungeVelocity_AndSpinTurnsTheBody()
    {
        var catalog = Load();
        Assert.That(catalog.TryGetTemplate("yukle_birak", out MotionTemplate charge), Is.True);
        var runner = new MotionTemplateRunner();
        runner.Begin(charge, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        MotionTick wind = runner.Tick(0.1f, new MotionTarget(true, 0f, 3f, 0.85f), new MotionStick(true, 0f, 0f));
        Assert.That(wind.AnimKey, Is.EqualTo("windup"));
        Assert.That(wind.VelZ, Is.EqualTo(0f).Within(0.05f));

        MotionTick lunged = default;
        bool sawLunge = false;
        for (int i = 0; i < 40 && !runner.Finished; i++)
        {
            lunged = runner.Tick(0.02f, new MotionTarget(true, 0f, 3f, 0.85f), default);
            if (lunged.AnimKey == "lunge" && lunged.VelZ > 0.5f)
            {
                sawLunge = true;
                break;
            }
        }
        Assert.That(sawLunge, Is.True);
        LocoBlend legs = LocoBlend.FromVelocity(lunged.VelX, lunged.VelZ, lunged.FaceX, lunged.FaceZ, 6.4f);
        Assert.That(legs.Speed01, Is.GreaterThan(0.2f));
        Assert.That(legs.Forward, Is.GreaterThan(0.5f));

        Assert.That(catalog.TryGetTemplate("donen_kesik", out MotionTemplate spin), Is.True);
        var turn = new MotionTemplateRunner();
        turn.Begin(spin, 0f, 0f, 0f, 0f, 1f, 0.5f, 0.15f);
        bool spun = false;
        for (int i = 0; i < 40 && !turn.Finished; i++)
        {
            MotionTick tick = turn.Tick(0.02f, new MotionTarget(true, 0f, 2f, 0.85f), default);
            if (tick.Spin && tick.AnimKey == "spin" && tick.FaceZ < -0.2f)
                spun = true;
        }
        Assert.That(spun, Is.True, "dönüş klibi ve gövde yaw'ı birlikte");
    }

    static MotionTemplateCatalog Load()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "ElementSystem", "motion-templates.json"));
        return MotionTemplateCatalog.FromJson(File.ReadAllText(path));
    }
}
