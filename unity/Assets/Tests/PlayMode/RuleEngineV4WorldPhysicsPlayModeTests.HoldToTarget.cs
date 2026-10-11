using System.Collections;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Casting.Input;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>7: basılı tutarak hedef seçme — kombo parmak kalkınca çıkar; hold_to_target_sec sonra kaydırıp hedef seç.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float HoldMarginSec = 0.1f;
        const float CancelWatchSec = 0.5f;
        const float LockedFarFrac = 0.95f;
        const float EmptyGroundAheadM = 4f;
        const float ColorTol = 0.01f;

        RuleEngineV4HeldCast Held => _md._castPort.RuleEngineV4Bridge.HeldCast;

        float HoldToTargetSec => _catalog.Catalog.Globals.HoldToTargetSec;

        IEnumerator WaitHoldOpen()
        {
            yield return new WaitForSecondsRealtime(HoldToTargetSec + HoldMarginSec);
            Assert.That(Held.PickOpen, Is.True, "hedef seçme açılmadı");
        }

        static Vector2 ScreenOf(Vector3 world)
        {
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "Camera.main");
            Vector3 p = cam.WorldToScreenPoint(world);
            Assert.That(p.z, Is.GreaterThan(0), "nokta kameranın arkasında");
            return new Vector2(p.x, p.y);
        }

        static Vector2 ScreenOf(Component actor)
        {
            Collider c = actor.GetComponentInChildren<Collider>();
            return ScreenOf(c != null ? c.bounds.center : actor.transform.position);
        }

        static void AssertOffHexagon(HexagonInputController input, Vector2 screen)
        {
            Assert.That(HexagonPointerHits.HitCenter(screen, input.Tuning), Is.False, "nokta altıgen merkezinde");
            Assert.That(HexagonPointerHits.HitDot(screen, input.Tuning).HasValue, Is.False, "nokta bir rünün üstünde");
        }

        /// <summary>Kılıç kombosu: önde minyon 0 (oto); arkada minyon 1 (seçilecek / kilit).</summary>
        TargetableHost ArrangeTwoTargets(float behindFrac)
        {
            ArrangeComboTarget();
            Place(_minions[1].transform, Origin - Vector3.right * (WeaponRange(Kilic) * behindFrac));
            Physics.SyncTransforms();
            var behind = _minions[1].GetComponent<TargetableHost>();
            Assert.That(behind, Is.Not.Null, "minyon 1 TargetableHost");
            return behind;
        }

        [UnityTest]
        public IEnumerator Hold_FastRelease_LaunchesAtOnce()
        {
            ArrangeComboTarget();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            DragonRuneFlashView runes = RuneFlash();
            int flashesBefore = runes.PlayCount;

            SliceComboDots(input, out int verb, out int adj);
            yield return DrawThroughDots(input, verb, adj);
            Assert.That(Held.IsHolding, Is.True, "kombo tutulmadı");
            Assert.That(_cast.IsRunning, Is.False, "skill parmak kalkmadan çıktı");
            Assert.That(runes.PlayCount - flashesBefore, Is.EqualTo(1), "harf flaşı 2. noktada yanmadı");
            Assert.That(Held.PickOpen, Is.False, "test eşikten önce bırakmalı");

            input.Pointer.EndPointer(cancelled: false);
            Assert.That(Held.LastRelease, Is.EqualTo(RuleEngineV4HeldRelease.LaunchDefault));
            Assert.That(_cast.IsRunning, Is.True, "hızlı bırakmada skill çıkmadı");
            Assert.That(_cast.LastFocus, Is.EqualTo(_minions[0].transform), "oto hedef");
            Assert.That(runes.PlayCount - flashesBefore, Is.EqualTo(1), "harf flaşı iki kez yandı");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Hold_AfterThreshold_SlideToTarget_LaunchesAtPicked()
        {
            TargetableHost picked = ArrangeTwoTargets(InRangeFrac);
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);

            SliceComboDots(input, out int verb, out int adj);
            yield return DrawThroughDots(input, verb, adj);
            yield return WaitHoldOpen();
            Assert.That(_cast.IsRunning, Is.False, "hedef seçerken skill çıktı");

            Vector2 screen = ScreenOf(_minions[1]);
            AssertOffHexagon(input, screen);
            input.Pointer.MovePointer(screen);
            yield return null;
            yield return null;

            RuleEngineV4TargetPickRingView ring = Held.Ring;
            Assert.That(ring, Is.Not.Null, "hedef halkası yok");
            Assert.That(ring.Visible, Is.True, "hedef halkası görünmüyor");
            Assert.That(ring.ShownTarget, Is.EqualTo(picked), "halka parmağın altındaki hedefte değil");
            Assert.That(ring.RingMaterial.shader.name, Is.EqualTo(RuleVfxDefaults.KorShaderName), "halka VFX_Kor değil");
            Color core = ring.RingMaterial.GetColor("_CoreColor");
            Color owner = _cast.OwnerColor * RuleVfxArtDefaults.CoreHdrMult;
            Assert.That(core.r, Is.EqualTo(owner.r).Within(ColorTol), "halka sahip renginde değil (r)");
            Assert.That(core.g, Is.EqualTo(owner.g).Within(ColorTol), "halka sahip renginde değil (g)");
            Assert.That(core.b, Is.EqualTo(owner.b).Within(ColorTol), "halka sahip renginde değil (b)");

            input.Pointer.EndPointer(cancelled: false);
            Assert.That(Held.LastRelease, Is.EqualTo(RuleEngineV4HeldRelease.LaunchAtPicked));
            Assert.That(_cast.IsRunning, Is.True, "seçilen hedefe skill çıkmadı");
            Assert.That(_cast.LastFocus, Is.EqualTo(picked.transform), "skill seçilen hedefe gitmedi");
            Assert.That(ring.Visible, Is.False, "bırakınca halka kalmadı");
            Assert.That(_md._targeting.Selected, Is.Not.EqualTo(picked), "basılı seçim kalıcı kilit olmamalı");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Hold_AfterThreshold_CenterRelease_CancelsWithoutCost()
        {
            ArrangeComboTarget();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);

            SliceComboDots(input, out int verb, out int adj);
            string key = RuleEngineV4CastEconomy.ComboKey(AttackVerb, input.Engine.Loadout.RuneIdAtSlot(adj));
            float manaBefore = _cast.Economy.Mana(NowMs);
            yield return DrawThroughDots(input, verb, adj);
            yield return WaitHoldOpen();
            input.Pointer.MovePointer(CenterPos(input));
            yield return null;
            input.Pointer.EndPointer(cancelled: false);

            Assert.That(Held.LastRelease, Is.EqualTo(RuleEngineV4HeldRelease.Cancel));
            Assert.That(Held.IsHolding, Is.False);
            float end = Time.realtimeSinceStartup + CancelWatchSec;
            while (Time.realtimeSinceStartup < end)
            {
                Assert.That(_cast.IsRunning, Is.False, "iptalden sonra skill koştu");
                yield return null;
            }
            Assert.That(_cast.Economy.Mana(NowMs), Is.GreaterThanOrEqualTo(manaBefore - ManaTol), "iptal mana harcadı");
            Assert.That(_cast.Economy.CooldownRemainingSec(key, NowMs), Is.Zero, "iptal bekleme başlattı");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Hold_AfterThreshold_ReleaseOnEmpty_LaunchesAuto()
        {
            TargetableHost locked = ArrangeTwoTargets(LockedFarFrac);
            _md._targeting.SweepSelect(locked);
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            try
            {
                SliceComboDots(input, out int verb, out int adj);
                yield return DrawThroughDots(input, verb, adj);
                yield return WaitHoldOpen();

                Vector2 empty = ScreenOf(Origin + Vector3.forward * EmptyGroundAheadM);
                AssertOffHexagon(input, empty);
                _md._targeting.TryPickWorld(empty, out TargetableHost under, out _);
                Assert.That(under, Is.Null, "boşluk noktasında hedef var");
                input.Pointer.MovePointer(empty);
                yield return null;
                Assert.That(Held.Ring == null || !Held.Ring.Visible, Is.True, "boşlukta halka gösterildi");

                input.Pointer.EndPointer(cancelled: false);
                Assert.That(Held.LastRelease, Is.EqualTo(RuleEngineV4HeldRelease.LaunchAuto));
                Assert.That(_cast.IsRunning, Is.True, "boşluğa bırakınca skill çıkmadı");
                Assert.That(_cast.LastFocus, Is.EqualTo(_minions[0].transform), "boşlukta oto en yakın (kilit değil)");
                Assert.That(_md._targeting.Selected, Is.EqualTo(locked), "kalıcı kilit bozuldu");
                Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
            }
            finally
            {
                _md._targeting.SweepSelect(null);
            }
        }

        [UnityTest]
        public IEnumerator Hold_WhileHolding_PlayerWalksAndSkillWaits()
        {
            ArrangeComboTarget();
            HexagonInputController input = InputController();
            var move = _player.GetComponent<MoveInputController>();
            var motor = _player.GetComponent<KinematicMotorController>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");
            yield return WaitSentenceIdle(input);

            float fullSpeed = motor.Tuning.Player.WalkSpeedMps;
            float peak = 0f;
            SliceComboDots(input, out int verb, out int adj);
            yield return DrawThroughDots(input, verb, adj);
            Assert.That(Held.IsHolding, Is.True, "kombo tutulmadı");
            move.SetScriptedDirection(Vector2.left);
            try
            {
                float end = Time.realtimeSinceStartup + HoldToTargetSec + HoldMarginSec;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    Assert.That(_cast.IsRunning, Is.False, "basılıyken skill çıktı");
                    Assert.That(input.SkillHeldForRelease, Is.True, "basılı durum düştü");
                    peak = Mathf.Max(peak, motor.Velocity.magnitude);
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
                input.Pointer.EndPointer(cancelled: true);
            }

            Assert.That(peak, Is.EqualTo(fullSpeed).Within(SpeedTolMps), "basılıyken tam hız yürümeli");
            Assert.That(Held.LastRelease, Is.EqualTo(RuleEngineV4HeldRelease.Cancel), "iptal dokunuşu skill çıkarmamalı");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }
    }
}
