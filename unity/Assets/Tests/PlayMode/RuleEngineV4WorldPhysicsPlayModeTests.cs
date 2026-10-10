using System.Collections;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Dilim sahnesi dünya fiziği senaryoları. Unity batchmode ile koşulur (CI/editör).
    /// Bu VM'de Unity yoksa dosya yine derlenir; koşum yapılmaz.
    /// </summary>
    public sealed class RuleEngineV4WorldPhysicsPlayModeTests
    {
        [UnityTest]
        public IEnumerator Dash_StopsAtWall_StubSceneRequired()
        {
            Assert.Ignore("PlayMode: GameBootstrapHost + kural_motoru_v4 dilim sahnesi gerekir.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossPush_AppliesPoiseNotDisplacement_Stub()
        {
            Assert.Ignore("PlayMode: boss RuleEngineV4PhysicsBodyHost Boss tier doğrulaması.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Missile_FirstHitOnly_Stub()
        {
            Assert.Ignore("PlayMode: iki düşman hizasında ilk collider.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Area_ForwardHemisphere_Stub()
        {
            Assert.Ignore("PlayMode: arkadaki düşman alan hasarı almamalı.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Odakli_LowestHpTarget_Stub()
        {
            Assert.Ignore("PlayMode: iki minion farklı can — Odaklı Zarar düşük cana.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Structure_LimitsFiveTwentyFive_Stub()
        {
            Assert.Ignore("PlayMode: 6. Sabit yapı reddedilmeli.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Diminish_NonDamageNotHeal_Stub()
        {
            Assert.Ignore("PlayMode: üst üste itme azalır, hasar azalmaz.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MotionHandoff_NewCutsOld_Stub()
        {
            Assert.Ignore("PlayMode: ardışık dash hız tavanı 1,5×.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Slice108_AllCastWithoutError_Stub()
        {
            Assert.Ignore("PlayMode: 108 kombo TryLaunch hata logu yok.");
            yield return null;
        }
    }
}
