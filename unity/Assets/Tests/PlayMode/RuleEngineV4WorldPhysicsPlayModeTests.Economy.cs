using System.Collections;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>v4 mana + bekleme: değerler scale bloğundan; boşa giden skill (hedef yok) manayı harcar, yarım bekleme; ıska tam bekleme.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const int MarkAdj = 9;
        const float ManaTol = 1e-3f;

        RuleEngineV4Scale EconomyScale => _catalog.Catalog.Scale;

        double NowMs => _md.MechanicsClock.Director.WorldTimeMs;

        [UnityTest]
        public IEnumerator Economy_DrawnCombo_SpendsManaStartsCooldown_RepeatDenied()
        {
            ArrangeComboTarget();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            SliceComboDots(input, out int verbDot, out int adjDot);
            int verb = input.Engine.Loadout.RuneIdAtSlot(verbDot);
            int adj = input.Engine.Loadout.RuneIdAtSlot(adjDot);
            string key = RuleEngineV4CastEconomy.ComboKey(verb, adj);

            float before = _cast.Economy.Mana(NowMs);
            yield return DrawThroughDots(input, verbDot, adjDot);
            Assert.That(_cast.Economy.Mana(NowMs), Is.GreaterThanOrEqualTo(before - ManaTol), "parmak kalkmadan mana harcandı");
            // Skill parmak kalkınca çıkar: bekleme o an başlar.
            double castMs = NowMs;
            input.Pointer.EndPointer(cancelled: false);
            Assert.That(_cast.IsRunning, Is.True, "skill çıkmadı");
            Assert.That(_cast.Economy.Mana(castMs), Is.EqualTo(before - EconomyScale.ManaCostPerSkill).Within(ManaTol), "mana harcanmadı");
            Assert.That(_cast.Economy.CooldownRemainingSec(key, castMs), Is.EqualTo(EconomyScale.CooldownSec).Within(ManaTol),
                "bekleme kullanım anında başlamadı");

            yield return WaitUntilIdle();
            Assert.That(_cast.Economy.CooldownRemainingSec(key, NowMs), Is.GreaterThan(0), "test bekleme içinde kalmalı");
            float mana = _cast.Economy.Mana(NowMs);
            Assert.That(_cast.TryLaunchPaid(verb, adj, RuleEngineV4CastContext.VerbAdjectiveComboLen, out _), Is.True);
            Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.Cooldown));
            Assert.That(_cast.IsRunning, Is.False, "beklemedeki skill çıktı");
            Assert.That(_cast.Economy.Mana(NowMs), Is.EqualTo(mana).Within(ManaTol), "reddedilen skill mana harcadı");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Economy_NotEnoughMana_SkillDoesNotFire()
        {
            ArrangeComboTarget();
            yield return null;
            int len = RuleEngineV4CastContext.VerbAdjectiveComboLen;
            int casts = (int)(EconomyScale.ManaPool / EconomyScale.ManaCostPerSkill);
            // İşaretsiz ZUL boş plan: mana harcar, koşucu başlamaz; aynı karede dolum olmaz.
            int[] verbs = RuleEngineV4Slice.Runes.Where(v => v != MarkAdj).Take(casts).ToArray();
            Assert.That(verbs.Length, Is.EqualTo(casts), "yeterli farklı kombo yok");
            foreach (int v in verbs)
            {
                Assert.That(_cast.TryLaunchPaid(v, MarkAdj, len, out _), Is.True);
                Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.None), $"{v}-{MarkAdj}");
            }
            float left = _cast.Economy.Mana(NowMs);
            Assert.That(left, Is.LessThan(EconomyScale.ManaCostPerSkill));

            int adj = RuleEngineV4Slice.Runes.First(a => a != MarkAdj);
            Assert.That(_cast.TryLaunchPaid(AttackVerb, adj, len, out _), Is.True);
            Assert.That(_cast.LastDenial, Is.EqualTo(RuleEngineV4CastDenial.Mana));
            Assert.That(_cast.IsRunning, Is.False, "mana yetmezken skill çıktı");
            Assert.That(_cast.Economy.Mana(NowMs), Is.EqualTo(left).Within(ManaTol));
            string key = RuleEngineV4CastEconomy.ComboKey(AttackVerb, adj);
            Assert.That(_cast.Economy.CooldownRemainingSec(key, NowMs), Is.Zero, "reddedilen skill bekleme başlattı");
        }

        [UnityTest]
        public IEnumerator Economy_NoValidTarget_SpendsManaHalfCooldown()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            yield return null;
            string key = RuleEngineV4CastEconomy.ComboKey(AttackVerb, MarkAdj);
            float before = _cast.Economy.Mana(NowMs);
            double castMs = NowMs;

            Assert.That(_cast.TryLaunchPaid(AttackVerb, MarkAdj, RuleEngineV4CastContext.VerbAdjectiveComboLen, out _), Is.True);
            Assert.That(_cast.IsRunning, Is.False, "işaretsiz ZUL hedefsiz kalmalı");
            Assert.That(_cast.Economy.Mana(castMs), Is.EqualTo(before - EconomyScale.ManaCostPerSkill).Within(ManaTol),
                "boşa giden skill manayı harcar");
            Assert.That(_cast.Economy.CooldownRemainingSec(key, castMs),
                Is.EqualTo(EconomyScale.CooldownSec * EconomyScale.FizzleCooldownFrac).Within(ManaTol), "yarım bekleme");
        }

        /// <summary>Menzil sonuna kadar kimseye değmeyen mermi ıskadır: boşa gitme değil, tam bekleme.</summary>
        [UnityTest]
        public IEnumerator Economy_MissileExpiresWithoutHit_IsMissFullCooldown()
        {
            FreezeBoss();
            Equip(Yay);
            Vector3 away = Flat(Origin - _boss.position).normalized;
            Arrange(away);
            yield return null;

            CommandPlan plan = null;
            foreach (int adj in RuleEngineV4Slice.Runes)
            {
                CommandPlan p = _catalog.Planner.Plan(
                    AttackVerb, adj, Yay, new RuleEngineV4CastContext(hasValidTarget: true, targetInWeaponRange: true));
                if (p.IsValid && p.Commands.Any(c => c is MermiFirlatCommand))
                {
                    plan = p;
                    break;
                }
            }
            Assert.That(plan, Is.Not.Null, "yayda mermi planı yok");

            string key = RuleEngineV4CastEconomy.ComboKey(plan.VerbRune, plan.AdjectiveRune);
            double castMs = NowMs;
            Assert.That(_cast.Economy.TryBegin(key, castMs, out _), Is.True);
            bool done = false;
            RuleEngineV4CastOutcome? outcome = null;
            _cast.Runner.Start(plan, null, _ => done = true, default, o =>
            {
                outcome = o;
                _cast.Economy.Settle(key, castMs, o);
            });
            yield return WaitUntilIdle();
            Assert.That(done, Is.True, $"plan bitmedi; {Diag()}");
            Assert.That(outcome, Is.EqualTo(RuleEngineV4CastOutcome.Missed), "kimseye değmeyen mermi ıska");
            Assert.That(_cast.Economy.CooldownRemainingSec(key, castMs), Is.EqualTo(EconomyScale.CooldownSec).Within(ManaTol),
                "ıska tam bekleme");
        }
    }
}
