using System.Collections;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// "Atılmadı" geri bildirimi: kombo tanındı ama atılmadı — yalnız mana yetmedi / bekleme sürüyor.
    /// Kül grisi sönük harf, 0,35 s'de iki titreme + dağılma; mana → kül tozu, bekleme → çatlak.
    /// Iska ve boşa gitmede KULLANILMAZ.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float NotCastFrameSlackSec = 0.05f;

        RuleEngineV4NotCastFx NotCastFx => RuleVfxSettings.Slice.NotCast;

        /// <summary>Mana havuzunu işaretsiz ZUL boş planlarıyla bitirir; bunlar boşa gitmedir (geri bildirim yok).</summary>
        void DrainManaWithFizzles(DragonRuneFlashView runes)
        {
            int len = RuleEngineV4CastContext.VerbAdjectiveComboLen;
            int casts = (int)(EconomyScale.ManaPool / EconomyScale.ManaCostPerSkill);
            int before = runes.NotCastCount;
            foreach (int v in RuleEngineV4Slice.Runes.Where(v => v != MarkAdj).Take(casts))
            {
                Assert.That(_cast.TryLaunchPaid(v, MarkAdj, len, out _), Is.True);
                Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.None), $"{v}-{MarkAdj}");
            }
            Assert.That(_cast.Economy.Mana(NowMs), Is.LessThan(EconomyScale.ManaCostPerSkill), "mana bitmedi");
            Assert.That(runes.NotCastCount, Is.EqualTo(before), "boşa giden skill 'atılmadı' gösterdi");
        }

        [UnityTest]
        public IEnumerator NotCast_NoMana_AshLetters_DustFalls_ScattersIn035()
        {
            ArrangeComboTarget();
            yield return null;
            DragonRuneFlashView runes = RuneFlash();
            DrainManaWithFizzles(runes);
            int before = runes.NotCastCount;

            int adj = RuleEngineV4Slice.Runes.First(a => a != MarkAdj);
            Assert.That(_cast.TryLaunchPaid(AttackVerb, adj, RuleEngineV4CastContext.VerbAdjectiveComboLen, out _), Is.True);
            Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.Mana));
            Assert.That(runes.NotCastCount, Is.EqualTo(before + 1), "mana yetmedi ama harf belirmedi");
            Assert.That(runes.NotCastKind, Is.EqualTo(RuleEngineV4CastDenial.Mana));
            Assert.That(runes.NotCastActive, Is.True);
            float startAt = Time.unscaledTime;

            yield return null;
            VfxBatch().Rebuild();
            AssertAshDim(runes);
            Assert.That(runes.NotCastDustQuads, Is.EqualTo(2 * NotCastFx.DustCount), "kül tozu her harfin altından düşmeli");
            Assert.That(runes.NotCastHalfQuads, Is.Zero, "mana yokken çatlak olmaz");
            yield return CaptureScreen("atilmadi_mana.png");

            yield return WatchNotCastEnd(runes, startAt);
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator NotCast_Cooldown_AshLetters_CrackSplits_ScattersIn035()
        {
            ArrangeComboTarget();
            yield return null;
            DragonRuneFlashView runes = RuneFlash();
            int adj = RuleEngineV4Slice.Runes.First(a => a != MarkAdj);
            string key = RuleEngineV4CastEconomy.ComboKey(AttackVerb, adj);
            Assert.That(_cast.Economy.TryBegin(key, NowMs, out _), Is.True, "bekleme kurulamadı");
            Assert.That(_cast.Economy.CooldownRemainingSec(key, NowMs), Is.GreaterThan(0));
            int before = runes.NotCastCount;

            Assert.That(_cast.TryLaunchPaid(AttackVerb, adj, RuleEngineV4CastContext.VerbAdjectiveComboLen, out _), Is.True);
            Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.Cooldown));
            Assert.That(_cast.IsRunning, Is.False, "beklemedeki skill çıktı");
            Assert.That(runes.NotCastCount, Is.EqualTo(before + 1), "bekleme sürerken harf belirmedi");
            Assert.That(runes.NotCastKind, Is.EqualTo(RuleEngineV4CastDenial.Cooldown));
            float startAt = Time.unscaledTime;

            yield return null;
            VfxBatch().Rebuild();
            AssertAshDim(runes);
            Assert.That(runes.NotCastHalfQuads, Is.EqualTo(4), "iki harf çatlakla ikiye bölünmeli");
            Assert.That(runes.NotCastDustQuads, Is.Zero, "beklemede kül tozu olmaz");
            yield return new WaitForSecondsRealtime(NotCastFx.DurationSec * NotCastFx.ShakeUntilFrac * 0.5f);
            yield return CaptureScreen("atilmadi_bekleme.png");

            yield return WatchNotCastEnd(runes, startAt);
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator NotCast_NotShown_OnFizzleOrMiss()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            yield return null;
            DragonRuneFlashView runes = RuneFlash();
            int before = runes.NotCastCount;

            // Boşa gitme: işaretsiz ZUL hedefsiz — mana harcanır, skill çıkmaz, geri bildirim yok.
            Assert.That(_cast.TryLaunchPaid(AttackVerb, MarkAdj, RuleEngineV4CastContext.VerbAdjectiveComboLen, out _), Is.True);
            Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.None));
            Assert.That(_cast.IsRunning, Is.False, "işaretsiz ZUL hedefsiz kalmalı");
            yield return null;
            Assert.That(runes.NotCastCount, Is.EqualTo(before), "boşa gitmede 'atılmadı' gösterildi");
            Assert.That(runes.NotCastActive, Is.False);

            // Iska: kimseye değmeyen yay mermisi.
            Equip(Yay);
            Vector3 away = Flat(Origin - _boss.position).normalized;
            Arrange(away);
            yield return null;
            CommandPlan plan = null;
            foreach (int a in RuleEngineV4Slice.Runes)
            {
                CommandPlan p = _catalog.Planner.Plan(
                    AttackVerb, a, Yay, new RuleEngineV4CastContext(hasValidTarget: true, targetInWeaponRange: true));
                if (p.IsValid && p.Commands.Any(c => c is MermiFirlatCommand))
                {
                    plan = p;
                    break;
                }
            }
            Assert.That(plan, Is.Not.Null, "yayda mermi planı yok");
            RuleEngineV4CastOutcome? outcome = null;
            _cast.Runner.Start(plan, null, _ => { }, default, o => outcome = o);
            yield return WaitUntilIdle();
            Assert.That(outcome, Is.EqualTo(RuleEngineV4CastOutcome.Missed), "ıska kurulamadı");
            Assert.That(runes.NotCastCount, Is.EqualTo(before), "ıskada 'atılmadı' gösterildi");
            Assert.That(runes.NotCastActive, Is.False);
        }

        void AssertAshDim(DragonRuneFlashView runes)
        {
            RuleEngineV4NotCastFx fx = NotCastFx;
            Color c = runes.NotCastColor;
            Assert.That(c.r, Is.EqualTo(fx.AshR).Within(ColorTol), "kül grisi (r)");
            Assert.That(c.g, Is.EqualTo(fx.AshG).Within(ColorTol), "kül grisi (g)");
            Assert.That(c.b, Is.EqualTo(fx.AshB).Within(ColorTol), "kül grisi (b)");
            Assert.That(c.a, Is.EqualTo(fx.Alpha).Within(ColorTol), "sönük alfa");
            Assert.That(c.a, Is.LessThan(1f), "harf sönük değil");
            Assert.That(runes.VerbVisible || runes.AdjectiveVisible, Is.False, "normal harf flaşı da yanıyor");
            Assert.That(VfxBatch().QuadCount, Is.GreaterThan(0), "atılmadı harfi çizilmiyor");
        }

        IEnumerator WatchNotCastEnd(DragonRuneFlashView runes, float startAt)
        {
            float maxScatter = 0f;
            float maxDt = 0f;
            while (runes.NotCastActive && Time.unscaledTime - startAt < NotCastFx.DurationSec * 2f)
            {
                yield return null;
                VfxBatch().Rebuild();
                maxDt = Mathf.Max(maxDt, Time.unscaledDeltaTime);
                if (runes.NotCastActive)
                    maxScatter = Mathf.Max(maxScatter, runes.NotCastScatter);
            }
            float took = Time.unscaledTime - startAt;
            Assert.That(runes.NotCastActive, Is.False, "atılmadı harfi sönmedi");
            Assert.That(took, Is.EqualTo(NotCastFx.DurationSec).Within(maxDt + NotCastFrameSlackSec), "0,35 s içinde dağılmalı");
            Assert.That(maxScatter, Is.GreaterThan(0f), "harf dağılmadan kayboldu");
        }
    }
}
