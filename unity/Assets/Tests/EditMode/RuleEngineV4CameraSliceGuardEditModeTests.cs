using Dovus.Game.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace Dovus.Tests.EditMode
{
    /// <summary>O-5.7: boss dibinden çıkışta tutulan yaw farkı tek karede silinmez.</summary>
    public sealed class RuleEngineV4CameraSliceGuardEditModeTests
    {
        // Test girdisi (oynanış sayısı değil): dondurma 2 m, serbest 5 m, gövde yarıçapı 1,5 m.
        const float FreezeEdgeM = 2f;
        const float FreeEdgeM = 5f;
        const float BodyRadiusM = 1.5f;
        const float LiveSweepDeg = 150f;
        const float SweepStepDeg = 3f;
        const float WalkStepM = 0.1f;
        const float WalkEndEdgeM = 6f;
        /// <summary>Test toleransı, kaynakta yok: bir karede tutulan açının canlıdan fazladan sıçraması.</summary>
        const float MaxFrameJumpDeg = 15f;

        [Test]
        public void O_5_7_HoldBossYaw_5mOtesineCikarkenFarkTekKaredeSilinmez()
        {
            var boss = new GameObject("boss");
            try
            {
                var guard = new CameraSliceGuard(new CameraSliceRules(120f, FreezeEdgeM, FreeEdgeM, 1.5f, 0.35f),
                    new CameraBossBody(boss.transform, BodyRadiusM, 3f), null);
                Vector3 player = Vector3.forward * (BodyRadiusM + FreezeEdgeM);
                float prev = guard.HoldBossYaw(0f, 0f, boss.transform, player);
                // 2 m'de boss yönü 150° döner: tutulan yaw donuk kalır.
                for (float live = SweepStepDeg; live <= LiveSweepDeg; live += SweepStepDeg)
                    prev = guard.HoldBossYaw(0f, live, boss.transform, player);
                Assert.That(Mathf.Abs(prev), Is.LessThan(1f), "2 m'de yaw donmadı");

                float maxJump = 0f;
                for (float edge = FreezeEdgeM + WalkStepM; edge <= WalkEndEdgeM; edge += WalkStepM)
                {
                    player = Vector3.forward * (BodyRadiusM + edge);
                    float held = guard.HoldBossYaw(0f, LiveSweepDeg, boss.transform, player);
                    maxJump = Mathf.Max(maxJump, Mathf.Abs(Mathf.DeltaAngle(prev, held)));
                    prev = held;
                }
                Assert.That(maxJump, Is.LessThanOrEqualTo(MaxFrameJumpDeg), $"tek karede {maxJump:0}° sıçradı");
                Assert.That(prev, Is.EqualTo(LiveSweepDeg).Within(0.01f), "5 m ötesinde yaw canlıya oturmadı");
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }
    }
}
