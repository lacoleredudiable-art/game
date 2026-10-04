using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreTests;

/// <summary>Denetim PR A "Savaş doğruluğu" (1 Ekim 2026): K1, O3, O4, O7, O8, S1, S4, S6, S7, S8, S16, S17.</summary>
[TestFixture]
public class CombatCorrectnessTests
{
    // ── K1: tek "can 0" kancası ──────────────────────────────────────────────

    [Test]
    public void K1_Died_FiresOnce_FromAnyDamageCall()
    {
        var vitals = new BossVitals(100f);
        int died = 0;
        vitals.Died += () => died++;

        Assert.That(vitals.ApplyDamage(60f), Is.False);
        Assert.That(died, Is.EqualTo(0));
        Assert.That(vitals.ApplyDamage(60f), Is.True);
        Assert.That(died, Is.EqualTo(1));
        Assert.That(vitals.IsDown, Is.True);

        // Down iken gelen yanma/yansıma tik'i ikinci ölüm üretmez.
        Assert.That(vitals.ApplyDamage(5f), Is.False);
        Assert.That(died, Is.EqualTo(1));

        vitals.Revive();
        vitals.ApplyDamage(500f);
        Assert.That(died, Is.EqualTo(2));
    }

    [Test]
    public void K1_IsDown_IsAlreadyTrue_WhenDiedFires()
    {
        var vitals = new BossVitals(10f);
        bool downInside = false;
        vitals.Died += () => downInside = vitals.IsDown && vitals.Hp <= 0f;
        vitals.ApplyDamage(10f);
        Assert.That(downInside, Is.True);
    }

    [Test]
    public void K1_HpChanged_ReportsBeforeAfter_AndCrossedBelowServesPhases()
    {
        var vitals = new BossVitals(200f);
        float before = -1f, after = -1f;
        int calls = 0;
        vitals.HpChanged += (b, a) => { before = b; after = a; calls++; };

        vitals.ApplyDamage(30f);
        Assert.That(before, Is.EqualTo(200f));
        Assert.That(after, Is.EqualTo(170f));
        vitals.ApplyDamage(0f);
        Assert.That(calls, Is.EqualTo(1));

        Assert.That(BossVitals.CrossedBelow(110f, 90f, 200f, 0.5f), Is.True);
        Assert.That(BossVitals.CrossedBelow(100f, 90f, 200f, 0.5f), Is.False);
        Assert.That(BossVitals.CrossedBelow(120f, 100f, 200f, 0.5f), Is.True);
        Assert.That(BossVitals.CrossedBelow(150f, 120f, 200f, 0.5f), Is.False);
    }

    [Test]
    public void K1_DeathSchedule_DotKill_SchedulesSingleRevive()
    {
        var vitals = new BossVitals(20f);
        var schedule = new BossDeathSchedule();
        double now = 1000;
        vitals.Died += () => schedule.Begin(now, 0.85);
        vitals.Revived += () => schedule.Cancel();

        // Yanma tik'leri — ApplyClosingDamage değil, düz ApplyDamage.
        for (int i = 0; i < 5; i++)
            vitals.ApplyDamage(6f);
        Assert.That(vitals.IsDown, Is.True);
        Assert.That(schedule.Pending, Is.True);
        Assert.That(schedule.ReviveAtMs, Is.EqualTo(1850).Within(0.001));
        Assert.That(schedule.Begin(1200, 0.85), Is.False, "ikinci Begin dirilişi uzatmaz");

        Assert.That(schedule.TryRevive(1849), Is.False);
        Assert.That(schedule.TryRevive(1850), Is.True);
        Assert.That(schedule.TryRevive(9999), Is.False);
        vitals.Revive();
        Assert.That(vitals.IsDown, Is.False);
        Assert.That(vitals.Hp, Is.EqualTo(20f));
    }

    [Test]
    public void K1_ExternalRevive_CancelsPendingDeath()
    {
        var vitals = new BossVitals(10f);
        var schedule = new BossDeathSchedule();
        vitals.Died += () => schedule.Begin(0, 1);
        vitals.Revived += () => schedule.Cancel();
        vitals.ApplyDamage(10f);
        Assert.That(schedule.Pending, Is.True);
        vitals.Revive();
        Assert.That(schedule.Pending, Is.False);
        Assert.That(schedule.TryRevive(5000), Is.False);
    }

    [Test]
    public void K1_DeathSequence_StartsOnlyFromTheDiedHook()
    {
        string md = Game("Skills/ManifestationDirector.cs");
        string all = string.Concat(Directory.GetFiles(GameDir(), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        int calls = Regex.Matches(all, @"BeginBossDeathSequence\(").Count;
        // 1 tanım + 1 çağrı (OnBossDied).
        Assert.That(calls, Is.EqualTo(2), "BeginBossDeathSequence yalnız OnBossDied'dan çağrılmalı");
        Assert.That(md, Does.Contain("_bossVitals.Died += OnBossDied"));
        Assert.That(Body(md, "void OnBossDied()"), Does.Contain("BeginBossDeathSequence(_clock.Director.WorldTimeMs)"));
    }

    // ── O3: 1-10 sırt vuruşu ikinci kez hasar vermez ────────────────────────

    [Test]
    public void O3_PortalBackStrike_NoSecondDamage()
    {
        string host = Game("Team/PortalBorderTeamHost.cs");
        Assert.That(host, Does.Not.Contain("void ApplyBackStrike"));
        Assert.That(host, Does.Not.Contain("ApplyBackStrike("));
        Assert.That(host, Does.Not.Contain("_strikePending"));
    }

    // ── O4: oyuncu dönüşü dünya saati + durum temizliği ─────────────────────

    [Test]
    public void O4_PlayerRespawn_UsesWorldClock_AndClearsStatus()
    {
        string vitals = Game("Actors/PlayerVitals.cs");
        Assert.That(vitals, Does.Not.Contain("_respawnAtUnscaled"));
        Assert.That(vitals, Does.Contain("public void BindClock(GameClock clock)"));
        Assert.That(Regex.Matches(vitals, @"ClearStatusBoard\(\);").Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(Game("Composition/Builders/ActorsBuilder.cs"), Does.Contain("vitals.BindClock(clock)"));
    }

    // ── O7: kritik tasarımla eşleşir (5% ×2), deterministik zar ─────────────

    [Test]
    public void O7_CritSystem_ReadsDesignJson()
    {
        CritSystem crit = CritSystem.FromJson(Dovus.Core.Shared.MiniJson.Parse(File.ReadAllText(Docs("element-sistemi.json"))));
        Assert.That(crit.BaseChance, Is.EqualTo(0.05f).Within(1e-5f));
        Assert.That(crit.Multiplier, Is.EqualTo(2f).Within(1e-5f));
        Assert.That(crit.MaxChance, Is.EqualTo(0.75f).Within(1e-5f));
        Assert.That(crit.ChanceWith(0f), Is.EqualTo(0.05f).Within(1e-5f));
        Assert.That(crit.ChanceWith(0.20f), Is.EqualTo(0.25f).Within(1e-5f), "Yay koşu atışı 0.25");
        Assert.That(crit.ChanceWith(5f), Is.EqualTo(0.75f).Within(1e-5f));
        Assert.That(crit.ChanceWith(-1f), Is.EqualTo(0.05f).Within(1e-5f));
        Assert.That(DamagePipeline.DefaultCritChance, Is.EqualTo(0.05f).Within(1e-5f));
        Assert.That(DamagePipeline.DefaultCritMultiplier, Is.EqualTo(2f).Within(1e-5f));
    }

    [Test]
    public void O7_CritSystem_MissingJson_FallsBackToDesignDefaults()
    {
        CritSystem crit = CritSystem.FromJson(Dovus.Core.Shared.MiniJson.Parse("{}"));
        Assert.That(crit.BaseChance, Is.EqualTo(0.05f).Within(1e-5f));
        Assert.That(crit.Multiplier, Is.EqualTo(2f).Within(1e-5f));
    }

    [Test]
    public void O7_CombatRng_SameSeedSameSequence()
    {
        var a = new CombatRng(CombatRng.SweepSeed);
        var b = new CombatRng(CombatRng.SweepSeed);
        for (int i = 0; i < 200; i++)
            Assert.That(a.NextRoll01(), Is.EqualTo(b.NextRoll01()));
        a.Reseed(CombatRng.SweepSeed);
        b.Reseed(CombatRng.SweepSeed);
        Assert.That(a.NextRoll01(), Is.EqualTo(b.NextRoll01()));
    }

    [Test]
    public void O7_CritRate_IsAboutFivePercent_AndFirstCritVariesBySeed()
    {
        var rng = new CombatRng(CombatRng.SweepSeed);
        int crits = 0;
        const int n = 20000;
        for (int i = 0; i < n; i++)
        {
            var hit = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = 100f,
                CanCrit = true,
                CritChance = 0.05f,
                CritMultiplier = 2f,
                CritRoll01 = rng.NextRoll01()
            });
            if (hit.WasCrit)
            {
                crits++;
                Assert.That(hit.Amount, Is.EqualTo(200f).Within(0.01f));
            }
        }
        Assert.That(crits / (float)n, Is.InRange(0.04f, 0.06f));

        var firstCrit = new System.Collections.Generic.HashSet<int>();
        for (int seed = 1; seed <= 20; seed++)
        {
            var r = new CombatRng(seed);
            int idx = 0;
            while (r.NextRoll01() >= 0.05f && idx < 10000)
                idx++;
            firstCrit.Add(idx);
        }
        Assert.That(firstCrit.Count, Is.GreaterThan(5), "ilk kritik artık hep aynı vuruş değil");
    }

    [Test]
    public void O7_GameUsesOneSeededRng_NotSequentialSeeds()
    {
        string dmg = Game("Skills/ManifestationDirector.Damage.cs");
        string verb = Game("Skills/ManifestationDirector.VerbExecution.cs");
        Assert.That(dmg + verb, Does.Not.Contain("_damageRoll"));
        Assert.That(dmg + verb, Does.Not.Contain("VarianceSeed"));
        Assert.That(dmg, Does.Contain("CombatRng.SessionSeed()"));
        Assert.That(Regex.Matches(dmg + verb, @"CritRoll01 = _combatRng\.NextRoll01\(\)").Count, Is.EqualTo(2));
        Assert.That(Game(Path.Combine("Editor", "PlaySweep.cs")), Does.Contain("ReseedCombatRng(CombatRng.SweepSeed)"));
    }

    // ── O8: ignore_armor %100, Yay bonusu yalnız kullanılınca tükenir ───────

    [Test]
    public void O8_IgnoreArmor_IsFullPierce_AndWeaponBonusConsumedOnlyWhenUsed()
    {
        Assert.That(SlotPassiveCombat.IgnoreArmorPierce, Is.EqualTo(1f));
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 200f,
            Armor = 100f,
            ArmorPenPercent = SlotPassiveCombat.CombineArmorPen(0f, true, 0f)
        });
        Assert.That(hit.Amount, Is.EqualTo(200f).Within(0.01f));

        string dmg = Game("Skills/ManifestationDirector.Damage.cs");
        Assert.That(dmg, Does.Contain("bool weaponArmorBonus = WeaponIgnoresArmor && !skillIgnoresArmor;"));
        Assert.That(dmg, Does.Contain("if (outcome.Amount > 0f && weaponArmorBonus)"));
    }

    // ── S1: kusursuz kaçış bonusu süreli ────────────────────────────────────

    [Test]
    public void S1_NextHitBuff_ExpiresAfterWindow()
    {
        var buff = new NextHitBuff();
        buff.Arm(1.5f, 1000, 2000);
        Assert.That(buff.IsArmedAt(2999), Is.True);
        Assert.That(buff.IsArmedAt(3000), Is.False);
        Assert.That(buff.Consume(2500), Is.EqualTo(1.5f));
        Assert.That(buff.Consume(2600), Is.EqualTo(1f));

        buff.Arm(1.5f, 1000, 2000);
        Assert.That(buff.Consume(60000), Is.EqualTo(1f), "dakikalar sonra taşınmaz");
        Assert.That(buff.IsArmed, Is.False);

        Assert.That(new DodgeTuning().PerfectNextHitWindowMs, Is.EqualTo(2000));
    }

    // ── S4: DoT zırhı deler, kaçılamaz, boss'ta sayı gösterir ───────────────

    [Test]
    public void S4_DotTick_PiercesArmor_IsUndodgeable_AndShowsNumber()
    {
        string status = Game("Actors/ActorStatus.cs");
        Assert.That(status, Does.Contain("ApplyDamage(payload, dodgeable: false, pierceArmor: true)"));
        Assert.That(status, Does.Contain("DamageOverTimeDealt?.Invoke(LastAppliedDamage)"));
        Assert.That(Game("Skills/ManifestationDirector.cs"), Does.Contain("DamageOverTimeDealt += OnBossDamageOverTime"));
    }

    // ── S6: kalkanın emdiği vuruş cümleyi kesmez ────────────────────────────

    [Test]
    public void S6_ShieldAbsorbedHit_DoesNotAbortSentence()
    {
        string boss = Game("Boss/BossStrikeApplier.cs");
        int apply = boss.IndexOf("_playerStatus.ApplyDamage(raw);");
        int abort = boss.IndexOf("_engine?.Abort();", apply);
        Assert.That(apply, Is.GreaterThan(0));
        Assert.That(abort, Is.GreaterThan(apply), "Abort hasardan sonra");
        Assert.That(boss, Does.Contain("landed = _playerStatus.LastAppliedDamage > 0f;"));
        Assert.That(boss.Substring(apply, abort - apply), Does.Contain("if (landed)"));
    }

    // ── S7: kör büyüklüğü %100 ıska tuzağı değil ────────────────────────────

    [Test]
    public void S7_BlindDefault_IsThirtyPercent()
    {
        var t = new StatusTuning();
        Assert.That(t.BlindMissChance, Is.EqualTo(0.3f).Within(1e-5f));
        var copy = new StatusTuning { BlindMissChance = 0.1f };
        t.CopyFrom(copy);
        Assert.That(t.BlindMissChance, Is.EqualTo(0.1f).Within(1e-5f));
        string applicator = File.ReadAllText(Path.Combine(ScriptsDir(), "Core", "Status", "StatusApplicator.cs"));
        Assert.That(applicator, Does.Not.Contain("board.Apply(kind, Duration(t.BlindMs), 1f)"));
        Assert.That(applicator, Does.Contain("BlindChanceFromAccuracy(t.BlindMissChance)"));
    }

    // ── S8, S16, S17 ────────────────────────────────────────────────────────

    [Test]
    public void S8_MinionCrit_IsShown()
    {
        Assert.That(Game("Skills/ManifestationDirector.VerbExecution.cs"),
            Does.Contain("ShowDamage(damage, dealt.WasCrit, BossHitPoint(), DamageTint(), victimIsBoss: true)"));
    }

    [Test]
    public void S16_EventsUnsubscribedOnDestroy_AndBlockedIsNotALambda()
    {
        string md = Game("Skills/ManifestationDirector.cs");
        Assert.That(md, Does.Not.Contain("DamageBlocked += _ =>"));
        string destroy = Body(md, "void OnDestroy()");
        foreach (string unsub in new[]
                 {
                     "SkillCancelledByDodge -= ", "Died -= OnBossDied", "DamageOverTimeDealt -= ",
                     "DamageTaken -= ", "DamageBlocked -= "
                 })
            Assert.That(destroy, Does.Contain(unsub), unsub);
    }

    [Test]
    public void S17_PortalPulseBurn_UsesTuning()
    {
        string host = Game("Team/PortalBorderTeamHost.cs");
        Assert.That(host, Does.Contain("Tuning.BurnDamagePerSec"));
    }

    // ── yardımcılar ─────────────────────────────────────────────────────────

    static string Body(string src, string signature)
    {
        int start = src.IndexOf(signature);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), signature);
        int brace = src.IndexOf('{', start);
        int depth = 0;
        for (int i = brace; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0)
                return src.Substring(brace, i - brace + 1);
        }
        return src.Substring(brace);
    }

    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Docs(string name) => Path.Combine(RepoRoot(), "docs", name);
    static string ScriptsDir() => Path.Combine(RepoRoot(), "unity", "Assets", "Scripts");
    static string GameDir() => Path.Combine(ScriptsDir(), "Game");
    static string Game(string rel) => File.ReadAllText(Path.Combine(GameDir(), rel));
}
