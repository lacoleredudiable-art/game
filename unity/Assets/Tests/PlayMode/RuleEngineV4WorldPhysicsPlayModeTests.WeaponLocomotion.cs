using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Envanter B2: silah değişince bekleme/yürüme klibi o silahın adını taşır (Loko_Kilic_*, Loko_Yay_*).
    /// Gerçek yol: silah takımı değişir, oyuncu durur; her karede ağırlıklı Loko_ klipleri kaydedilir.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Silah değiştikten sonra klibin oturması için tanınan süre (envanter B2: 1 s).</summary>
        const float WeaponLocoSettleSec = 1f;
        const float WeaponLocoWatchSec = 1f;

        [UnityTest]
        public IEnumerator B2_SilahDegisinceBeklemeKlibiSilahinAdiniTasir()
        {
            FreezeBoss();
            Arrange(Vector3.right);
            Animator anim = PlayerAnimator();
            foreach ((int weapon, string name) in new[] { (Kilic, "Kilic"), (Yay, "Yay"), (Kilic, "Kilic") })
            {
                Equip(weapon);
                yield return new WaitForSecondsRealtime(WeaponLocoSettleSec);
                var seen = new HashSet<string>();
                float end = Time.realtimeSinceStartup + WeaponLocoWatchSec;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    foreach (AnimatorClipInfo ci in anim.GetCurrentAnimatorClipInfo(0))
                    {
                        if (ci.weight > ClipActiveWeight && ci.clip.name.StartsWith(LocoClipPrefix, System.StringComparison.Ordinal))
                            seen.Add(ci.clip.name);
                    }
                }
                Assert.That(seen, Is.Not.Empty, $"w{weapon}: Loko_ klibi oynamadı");
                Assert.That(seen.All(c => c.Split('_').Contains(name)), Is.True,
                    $"w{weapon}: oynayan bekleme/yürüme klipleri [{string.Join(",", seen)}], beklenen {name}");
            }
        }
    }
}
