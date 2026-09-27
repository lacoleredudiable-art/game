using System.IO;
using System.Linq;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SkillMotorV611Tests
{
    static string JsonPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }
        Assert.That(File.Exists(path), Is.True, $"element-sistemi.json bulunamadı: {path}");
        return path;
    }

    static string LoadJson() => File.ReadAllText(JsonPath());
    static SkillMotor Load() => SkillMotor.FromJson(LoadJson());

    [Test]
    public void LoadsLockedV611Catalog()
    {
        SkillMotor motor = Load();

        Assert.That(motor.Version, Is.EqualTo("6.1.1"));
        Assert.That(motor.IsV61, Is.True);
        Assert.That(motor.RuneCount, Is.EqualTo(12));
        Assert.That(motor.SkillCount, Is.EqualTo(144));
        Assert.That(motor.MaxComboLength, Is.EqualTo(2));
        Assert.That(motor.ElementPaints.Count, Is.EqualTo(6));
        Assert.That(motor.MainClasses.Count, Is.EqualTo(80));
    }

    [Test]
    public void DefaultBuildComesFromFirstMainClassAndMapsSixSlots()
    {
        SkillMotor motor = Load();
        RuneLoadout loadout = motor.DefaultLoadout;

        Assert.That(loadout.RuneIds, Is.EqualTo(new[] { 1, 5, 6, 2, 7, 9 }));
        Assert.That(loadout.PassiveCount, Is.Zero);
        Assert.That(loadout.TryResolveSlot(5, out Rune rune), Is.True);
        Assert.That((int)rune, Is.EqualTo(7));

        Assert.That(
            motor.TryCreateMainClassLoadout(3, new[] { 12 }, out RuneLoadout zamanBuild),
            Is.True);
        Assert.That(zamanBuild.ContainsRune(12), Is.True);
        Assert.That(zamanBuild.IsPassive(12), Is.True);
    }

    [Test]
    public void PairIsVerbTimesAdjectiveAndSingleIsPreviewOnly()
    {
        SkillMotor motor = Load();

        SkillResolution preview = motor.Resolve(new[] { 1 });
        Assert.That(preview.IsEmpty, Is.False);
        Assert.That(preview.IsComplete, Is.False);
        Assert.That(preview.VerbName, Is.EqualTo("Saldırı"));

        SkillResolution pair = motor.Resolve(new[] { 1, 2 });
        Assert.That(pair.IsComplete, Is.True);
        Assert.That(pair.SkillId, Is.EqualTo("1-2"));
        Assert.That(pair.DisplayName, Is.EqualTo("Emici Vuruş"));
        Assert.That(pair.VerbId, Is.EqualTo("1"));
        Assert.That(pair.AdjectiveId, Is.EqualTo("2"));
        Assert.That(pair.BaseDamage, Is.EqualTo(40f));
        Assert.That(pair.DamageMult, Is.EqualTo(0.95f));

        Assert.That(motor.Resolve(new[] { 1, 2, 3 }).IsEmpty, Is.True);
    }

    [Test]
    public void EveryLockedPairResolves()
    {
        SkillMotor motor = Load();
        int resolved = 0;
        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            SkillResolution skill = motor.Resolve(new[] { verb, adjective });
            Assert.That(skill.IsEmpty, Is.False, $"{verb}-{adjective}");
            Assert.That(skill.IsComplete, Is.True, $"{verb}-{adjective}");
            resolved++;
        }

        Assert.That(resolved, Is.EqualTo(144));
    }

    [Test]
    public void ZamanResolvesToActorTempoNeverGlobalTime()
    {
        SkillResolution zaman = Load().Resolve(new[] { 12, 1 });

        Assert.That(zaman.Action, Is.EqualTo("tempo"));
        Assert.That(zaman.EngineModifiers["enemy_slow"].AsFloat(), Is.EqualTo(0.7f));
        Assert.That(zaman.EngineModifiers["self_haste"].AsFloat(), Is.EqualTo(0f));
        Assert.That(zaman.EngineModifiers["no_global_timescale"].AsBool(), Is.True);
    }

    [Test]
    public void AnimationDatabaseMapsAllWeaponVerbNamesToExistingCastStates()
    {
        AnimationDatabase database = AnimationDatabase.FromJson(LoadJson());

        Assert.That(database.Count, Is.EqualTo(120));
        Assert.That(database.TryGet("kilic", 1, out AnimationBinding strike), Is.True);
        Assert.That(strike.DisplayName, Is.EqualTo("Geniş Kesme"));
        Assert.That(strike.AnimatorState, Is.EqualTo("CastPierce"));
        Assert.That(database.TryGet("kilic", 5, out AnimationBinding blast), Is.True);
        Assert.That(blast.AnimatorState, Is.EqualTo("CastSlam"));
        Assert.That(database.TryGet("missing", 1, out _), Is.False);
    }

    [Test]
    public void SkillFactoryBuilds144UniqueAnd36PerSelectedBuild()
    {
        string json = LoadJson();
        SkillMotor motor = SkillMotor.FromJson(json);
        EquipmentCatalog equipment = EquipmentCatalog.FromJson(json);
        var factory = new SkillFactory(motor, new EquipmentBonusResolver(equipment));
        EquipmentItem sword = equipment.FindWeapon(4)!;

        var all = Enumerable.Range(1, 12)
            .SelectMany(verb => Enumerable.Range(1, 12)
                .Select(adjective => factory.Create(verb, adjective, sword, 1)))
            .ToArray();
        Assert.That(all.Length, Is.EqualTo(144));
        Assert.That(all.Select(skill => skill.Id).Distinct().Count(), Is.EqualTo(144));
        Assert.That(all[0].DisplayName, Does.StartWith("Ateşli "));

        IReadOnlyList<Skill> buildSkills =
            factory.CreateForBuild(motor.DefaultLoadout, sword, elementPaintId: 1);
        Assert.That(buildSkills.Count, Is.EqualTo(36));
        Assert.That(buildSkills.Select(skill => skill.Id).Distinct().Count(), Is.EqualTo(36));
    }

    [Test]
    public void RuneManagerEnforcesSixUniqueAndZeroToTwoPassiveSlots()
    {
        SkillMotor motor = Load();
        var manager = new RuneManager(motor);

        Assert.That(
            manager.TrySelect(
                new[] { 1, 3, 5, 7, 9, 12 },
                new[] { 3, 12 },
                out string error),
            Is.True,
            error);
        Assert.That(manager.Current.RuneIds, Is.EqualTo(new[] { 1, 3, 5, 7, 9, 12 }));
        Assert.That(manager.Current.PassiveCount, Is.EqualTo(2));

        Assert.That(
            manager.TrySetPassiveSlots(new[] { 1, 3, 5 }, out _),
            Is.False);
        Assert.That(manager.Current.PassiveCount, Is.EqualTo(2));
    }

    [Test]
    public void SentenceEngineMapsScreenSlotsThroughSelectedBuild()
    {
        SkillMotor motor = Load();
        RuneLoadout loadout = motor.CreateLoadout(
            new[] { 12, 11, 10, 9, 8, 7 },
            new[] { 12, 7 });
        var tuning = new SentenceTuning { MaxSentenceDots = 2 };
        var engine = new SentenceEngine(tuning, loadout);

        engine.OnDotTouched(1, 0);
        engine.OnDotTouched(6, 10);

        CompletedSentence completed = engine.History[0];
        Assert.That((int)completed.Words[0].Rune, Is.EqualTo(12));
        Assert.That((int)completed.Words[1].Rune, Is.EqualTo(7));
        Assert.That(completed.Words[0].Dot, Is.EqualTo(1));
        Assert.That(completed.Words[1].Dot, Is.EqualTo(6));
        Assert.That(loadout.PassiveCount, Is.EqualTo(2));
    }
}
