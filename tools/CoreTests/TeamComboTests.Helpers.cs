using Dovus.Core.Border;
using Dovus.Core.Grammar;
using System.IO;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using NUnit.Framework;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace CoreTests;

public partial class TeamComboTests
{
    static SkillMotor _borderMotor;

    static SkillEngineModifiers BorderEngine(string skillId)
    {
        _borderMotor ??= SkillMechanicTagTests.LoadMotorPublic();
        Assert.That(_borderMotor.TryGetSkill(skillId, out SkillCatalogEntry entry), Is.True, skillId);
        return new SkillEngineModifiers(entry.Engine);
    }

    static PortalSystem JsonPortal() =>
        new PortalSystem(PortalOpTable.FromMotor(SkillMechanicTagTests.LoadMotorPublic()));

    static TeamComboSystem JsonTeam() =>
        new TeamComboSystem(TeamOpTable.FromMotor(SkillMechanicTagTests.LoadMotorPublic()));

    static void AssertTier(string skill, float below, float threshold, float attack, float life, float damage)
    {
        var mode = new BorderMode();
        var engine = BorderEngine(skill);
        Assert.That(mode.OnSkill(1, (SkillId)skill, engine, threshold), Is.False, skill + " eşikte açılmaz");
        Assert.That(mode.OnSkill(1, (SkillId)skill, engine, below), Is.True, skill);
        Assert.That(mode.Threshold(1), Is.EqualTo(threshold).Within(0.001f));
        Assert.That(mode.AttackSpeedMult(1) / mode.ColumnMoveSpeedMult(1), Is.EqualTo(attack).Within(0.001f));
        Assert.That(mode.LifestealAdd(1), Is.EqualTo(life).Within(0.001f));
        Assert.That(mode.DamageMult(1), Is.EqualTo(damage).Within(0.001f));
        Assert.That(mode.AuraLabel(1), Does.Contain("Sınır"));
    }

    static void AssertOutside(float x, float z, float radius, in Disc boss)
    {
        Assert.That(PortalSystem.Overlaps(x, z, radius, boss), Is.False,
            "boss içinde: " + x.ToString("0.00") + "," + z.ToString("0.00"));
    }

    static Placement One(IReadOnlyList<Placement> moves, int id)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].ActorId == id)
                return moves[i];
        }
        Assert.Fail("yerleşim yok: " + id);
        return default;
    }

    static float Dist(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return System.MathF.Sqrt(dx * dx + dz * dz);
    }

    static Disc Boss(float x, float z) => new Disc(true, x, z, 0.85f, PortalSystem.ClearGapM);

    static Body Actor(int id, float x, float z, bool owns = false, float y = 0f) =>
        new Body(id, x, y, z, 0.5f, owns, false);

    static FakeAlly Ally(int id, float x, float z) => new FakeAlly { Id = id, X = x, Z = z };

    sealed class FakeAlly : IAllyPlayer
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Radius { get; set; } = 0.5f;
        public float HpRatio { get; set; } = 1f;
        public string LastSkillId { get; set; } = string.Empty;
        public bool TemplateOwnsPosition { get; set; }
    }
}
