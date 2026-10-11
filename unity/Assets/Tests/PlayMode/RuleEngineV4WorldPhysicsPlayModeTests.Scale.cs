using System.Collections;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>Dilim düzeltme E: bayrak açıkken can/hasar kural-motoru-v4.json scale bloğundan.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float TenPercent = 0.1f;

        [UnityTest]
        public IEnumerator Scale_FlagOn_HpFromJson()
        {
            FreezeBoss();
            yield return null;
            RuleEngineV4Scale s = _catalog.Catalog.Scale;
            PlayerVitalsHost vitals = AuthoredPlayerVitals();
            Assert.That(vitals.MaxHp, Is.EqualTo(1000).And.EqualTo(Mathf.RoundToInt(s.PlayerMaxHp)), "oyuncu can");
            foreach (Transform ally in _allies)
            {
                var dummy = ally.GetComponent<AllyDummyController>();
                Assert.That(dummy.MaxHp, Is.EqualTo(1000).And.EqualTo(Mathf.RoundToInt(s.AllyDummyMaxHp)), ally.name + " can");
            }
            foreach (SliceLightMinionHost minion in _minions)
                Assert.That(minion.MaxHp, Is.EqualTo(400).And.EqualTo(Mathf.RoundToInt(s.LightCreatureMaxHp)), minion.name + " can");
            Assert.That(_md.MechanicsBossVitals.MaxHp, Is.EqualTo(15000f).And.EqualTo(s.BossMaxHp), "boss can");

            _md.MechanicsBossDirector.ApplyPoiseDamage(s.BossPoiseBar * 0.5f);
            Assert.That(_md.MechanicsBossDirector.PoiseRatio, Is.EqualTo(0.5f).Within(1e-4f),
                "boss denge çubuğu scale.boss_poise_bar değil");
        }

        [UnityTest]
        public IEnumerator Scale_FlagOn_HundredHit_TakesTenPercentOfPlayerHp()
        {
            FreezeBoss();
            yield return null;
            RuleEngineV4Scale s = _catalog.Catalog.Scale;
            PlayerVitalsHost vitals = AuthoredPlayerVitals();
            vitals.ApplyHeal(vitals.MaxHp);
            int before = vitals.Hp;
            Assert.That(before, Is.EqualTo(vitals.MaxHp), "oyuncu dolu canla başlamıyor");

            _md._playerStatus.ApplyDamage(s.BaseDamage, dodgeable: false, pierceArmor: true);

            int lost = before - vitals.Hp;
            Assert.That(lost, Is.EqualTo(100), "100'lük vuruş CombatScale ile büyütüldü");
            Assert.That(lost / (float)vitals.MaxHp, Is.EqualTo(TenPercent).Within(1e-4f), "100 vuruş canın %10'u değil");
        }

        /// <summary>Editörde DevHp havuzu yazılan canı örter; ölçek testi yazılan canı okur.</summary>
        PlayerVitalsHost AuthoredPlayerVitals()
        {
            PlayerVitalsHost vitals = _md.CachedPlayerVitals();
            vitals.SetDevHp(false);
            return vitals;
        }
    }
}
