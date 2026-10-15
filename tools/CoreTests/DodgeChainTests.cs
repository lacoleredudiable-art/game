using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class DodgeChainTests
{
    static DodgeTuning Tuning() => new DodgeTuning
    {
        MaxCharges = 2,
        ChargeRechargeMs = 6000,
        DoubleTapWindowMs = 200,
        CombinedIframeMs = 520,
        IframeMs = 260,
        IframeStartMs = 0,
        DurationMs = 250,
        StartupMs = 10,
        GlideTailMs = 120
    };

    // 15-dodge-tasarimi §3.2-§3.4: test değerleri (T_tek 0,5 s, T_zincir 1,3 s), Model A.
    // Ayar varsayılanlarıyla çalışır; sayılar dengelemede (Aşama 8) kesinleşecek.
    [Test]
    public void O_4_5_Zincir_PencereBirlesir()
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        Assert.That(DodgeChain.TryConsumePress(0, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        // ikinci basış birinci pencere (T_tek) açıkken, hareket sürerken gelir
        Assert.That(DodgeChain.TryConsumePress(300, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.UpgradedCombined));

        // Model A: pencere birinci basıştan itibaren sabit T_zincir, arada boşluk yok
        for (int t = 0; t < 1300; t++)
            Assert.That(dodge.IsInvulnerable(t), Is.True, $"t={t} ms hasar alınabilir boşluk");
        Assert.That(dodge.IsInvulnerable(1300), Is.False);
    }

    // 15-dodge-tasarimi §3.2: birleşme penceresi (T_tek) kapandıktan sonra gelen basış zincir değildir;
    // takla yok, basış anında yeni T_tek penceresi açılır, 1 hak harcanır. Arada korunma olmaması
    // bilinçli bir "boşluk mekaniği" değil, sıradan dodge davranışıdır.
    [Test]
    public void O_4_5_GecIkinciBasis_SiradanDodge()
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);
        int latePress = tuning.IframeMs + 100;

        DodgeChain.TryConsumePress(0, dodge, bank, tuning);
        Assert.That(bank.Ready, Is.EqualTo(1));

        Assert.That(DodgeChain.TryConsumePress(latePress, dodge, bank, tuning),
            Is.EqualTo(DodgeChain.PressOutcome.StartedNew), "zincir değil, sıradan yeni dodge");
        Assert.That(dodge.IsCombined, Is.False, "takla yok");
        Assert.That(bank.Ready, Is.EqualTo(0), "tam 1 hak harcanır");

        Assert.That(dodge.IsInvulnerable(tuning.IframeMs + 50), Is.False, "eski pencere bitti, yeni basış henüz yok");
        Assert.That(dodge.IsInvulnerable(latePress), Is.True, "yeni T_tek penceresi basış anında açılır");
        Assert.That(dodge.IsInvulnerable(latePress + tuning.IframeMs - 1), Is.True);
        Assert.That(dodge.IsInvulnerable(latePress + tuning.IframeMs), Is.False, "pencere T_tek, T_zincir değil");
    }

    [Test]
    public void O_4_7_Zincir_IkiHakYer()
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        DodgeChain.TryConsumePress(0, dodge, bank, tuning);
        DodgeChain.TryConsumePress(300, dodge, bank, tuning);
        Assert.That(bank.Ready, Is.EqualTo(0));
    }

    [Test]
    public void O_4_5_BirlesmePenceresi_TekPencereyeEsit()
    {
        var tuning = new DodgeTuning();
        Assert.That(tuning.DoubleTapWindowMs, Is.EqualTo(tuning.IframeMs), "15 §3.4 / D-4: birleşme penceresi = T_tek");
    }

    // O-4.4a: süzülme 0,40 s. Hareket (10 + 250 + 400 = 660 ms) birleşme penceresini (500 ms) örter;
    // yoksa 380-500 ms arası ikinci basış takla olmaz, yeni tek dodge başlar.
    [Test]
    public void O_4_4a_HareketBirlesmePenceresiniOrter()
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        dodge.Begin(0);
        for (int t = 0; t < tuning.DoubleTapWindowMs; t++)
            Assert.That(dodge.IsActive(t), Is.True, $"t={t} ms hareket bitti, pencere hâlâ açık");
    }

    [Test]
    public void O_4_5_Takla_SuzulmedeBasis_ZincirOlur()
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        DodgeChain.TryConsumePress(0, dodge, bank, tuning);
        // 450 ms: patlama bitti, süzülme sürüyor, pencere (500 ms) açık
        Assert.That(DodgeChain.TryConsumePress(450, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.UpgradedCombined));
        Assert.That(bank.Ready, Is.EqualTo(0));
    }

    [TestCase(0)]
    [TestCase(100)]
    [TestCase(260)]
    [TestCase(450)]
    [TestCase(499)]
    public void O_4_5_Takla_YolSureklidirVeToplamMesafeCarpandir(int promoteMs)
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        dodge.Begin(0);
        float before = dodge.GetDisplacementRatio(promoteMs) * dodge.DistanceMultiplier;
        dodge.PromoteToCombined(promoteMs);
        float after = dodge.GetDisplacementRatio(promoteMs) * dodge.DistanceMultiplier;
        Assert.That(after, Is.EqualTo(before).Within(1e-4f), "takla anında konum sıçradı");

        // yol bitene kadar geri gitmez, adım başına sıçrama yok, sonunda ×CombinedDistanceMult
        float prev = after;
        int end = tuning.StartupMs + dodge.MoveDurationMs;
        Assert.That(end, Is.GreaterThanOrEqualTo(promoteMs), "yol, taklanın başladığı andan önce bitmiş");
        for (int t = promoteMs; t <= end; t++)
        {
            float r = dodge.GetDisplacementRatio(t) * dodge.DistanceMultiplier;
            Assert.That(r, Is.GreaterThanOrEqualTo(prev - 1e-4f), $"t={t} geri gitti");
            Assert.That(r - prev, Is.LessThan(0.35f), $"t={t} 1 ms'de sıçradı");
            prev = r;
        }
        Assert.That(prev, Is.EqualTo(tuning.CombinedDistanceMult).Within(1e-3f));
        Assert.That(dodge.IsActive(end), Is.True, "takla yolu bitmeden dodge kapandı");
    }

    [Test]
    public void Recharge_IsSixSecondsPerCharge()
    {
        var bank = new DodgeChargeBank(Tuning());
        bank.TrySpend(0);
        bank.TrySpend(0);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Tick(5999);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Tick(6000);
        Assert.That(bank.Ready, Is.EqualTo(1));
        bank.Tick(12000);
        Assert.That(bank.Ready, Is.EqualTo(2));
    }

    [Test]
    public void DoubleTapInsideWindow_SpendsTwoCharges_CombinedIframe520()
    {
        var tuning = Tuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        Assert.That(DodgeChain.TryConsumePress(1000, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(bank.Ready, Is.EqualTo(1));

        Assert.That(DodgeChain.TryConsumePress(1150, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.UpgradedCombined));
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(dodge.IsCombined, Is.True);
        Assert.That(dodge.IsInvulnerable(1519), Is.True);
        Assert.That(dodge.IsInvulnerable(1520), Is.False);
    }

    [Test]
    public void Promote_KeepsDisplacementContinuous()
    {
        var tuning = Tuning();
        tuning.CombinedDistanceMult = 1.6f;
        tuning.CombinedDurationMult = 1.4f;
        var dodge = new DodgeState(tuning);
        dodge.Begin(1000);
        float before = dodge.GetDisplacementRatio(1100) * dodge.DistanceMultiplier;
        dodge.PromoteToCombined(1100);
        float after = dodge.GetDisplacementRatio(1100) * dodge.DistanceMultiplier;
        Assert.That(after, Is.EqualTo(before).Within(1e-4f));
        float end = dodge.GetDisplacementRatio(1000 + tuning.StartupMs + dodge.MoveDurationMs) * dodge.DistanceMultiplier;
        Assert.That(end, Is.EqualTo(1.6f).Within(1e-4f));
    }

    [Test]
    public void DoubleTapOutsideWindow_TwoSeparateDodges()
    {
        var tuning = Tuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        Assert.That(DodgeChain.TryConsumePress(0, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(DodgeChain.TryConsumePress(250, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(dodge.IsCombined, Is.False);
        Assert.That(dodge.PressTimeMs, Is.EqualTo(250));
        Assert.That(dodge.IsInvulnerable(509), Is.True);
        Assert.That(dodge.IsInvulnerable(510), Is.False);
    }

    [Test]
    public void SingleCharge_DoubleTap_SecondDenied_NormalDodgeOnly()
    {
        var tuning = Tuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning, startReady: 1);

        Assert.That(DodgeChain.TryConsumePress(0, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(DodgeChain.TryConsumePress(50, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.DeniedNoCharge));
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(dodge.IsCombined, Is.False);
        Assert.That(dodge.IsInvulnerable(259), Is.True);
    }

    [Test]
    public void PerfectRefund_OncePerCombinedDodgePress()
    {
        var tuning = Tuning();
        var bank = new DodgeChargeBank(tuning);
        bank.TrySpend(0);
        bank.TrySpend(0);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Refund(tuning.PerfectChargeRefund);
        Assert.That(bank.Ready, Is.EqualTo(1));
        bank.Refund(tuning.PerfectChargeRefund);
        Assert.That(bank.Ready, Is.EqualTo(2));
    }
}
