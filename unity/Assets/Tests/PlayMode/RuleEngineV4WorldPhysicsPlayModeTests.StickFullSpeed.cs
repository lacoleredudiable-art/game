using System.Collections;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// O-1.17 / O-4.2 (15 Ekim [K]): joystick analog değil. Ölü bölgenin (0,12) hemen üstündeki çubuk da tam hızla yürütür.
    /// Ölçülen, dünyada görünen sonuç: motorun gerçek hızı.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float StickSettleSec = 1.2f;

        [UnityTest]
        public IEnumerator O_1_17_Cubuk_OluBolgeUstu_TamHiz()
        {
            FreezeBoss();
            Arrange(Vector3.right);
            var move = _player.GetComponent<MoveInputController>();
            var motor = _player.GetComponent<KinematicMotorController>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");

            float fullSpeed = motor.Tuning.Player.WalkSpeedMps;
            foreach (float mag in new[] { 0.2f, 0.5f })
            {
                float peak = 0f;
                move.SetScriptedDirection(new Vector2(mag, 0f));
                try
                {
                    float end = Time.realtimeSinceStartup + StickSettleSec;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        peak = Mathf.Max(peak, motor.Velocity.magnitude);
                    }
                }
                finally
                {
                    move.SetScriptedDirection(null);
                }

                Assert.That(peak, Is.EqualTo(fullSpeed).Within(SpeedTolMps), $"çubuk {mag}: ölü bölge üstü tam hız");
                yield return null;
            }
        }
    }
}
