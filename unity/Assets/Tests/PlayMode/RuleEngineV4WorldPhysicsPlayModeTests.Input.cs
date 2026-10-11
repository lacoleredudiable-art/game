using System.Collections;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>G: dilim girdisi gerçek PointerRouter üzerinden — tavan 2, merkezde kalkış iptal, merkez dokunuşu onay, çizerken tam hız.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float InputSettleSec = 2f;
        const float DrawSampleSec = 0.3f;
        const float SpeedTolMps = 0.001f;

        HexagonInputController InputController()
        {
            var input = Object.FindAnyObjectByType<HexagonInputController>();
            Assert.That(input, Is.Not.Null, "HexagonInputController");
            Assert.That(input.Pointer, Is.Not.Null, "PointerRouter");
            Assert.That(input.Engine, Is.Not.Null, "SentenceEngine");
            return input;
        }

        static Vector2 DotPos(HexagonInputController input, int dot) =>
            HexagonLayoutScreen.DotPx(dot, input.Tuning, Screen.width, Screen.height);

        static Vector2 CenterPos(HexagonInputController input) =>
            HexagonLayoutScreen.CenterPx(input.Tuning, Screen.width, Screen.height);

        IEnumerator WaitSentenceIdle(HexagonInputController input)
        {
            float end = Time.realtimeSinceStartup + InputSettleSec;
            while (input.Engine.State.Phase != SentencePhase.Idle && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(input.Engine.State.Phase, Is.EqualTo(SentencePhase.Idle), "cümle boşa dönmedi");
        }

        const int AttackVerb = 1;

        static int DotForRune(HexagonInputController input, int runeId)
        {
            for (int dot = 1; dot <= Dovus.Core.Input.HexagonLayout.DotCount; dot++)
            {
                if (input.Engine.Loadout.RuneIdAtSlot(dot) == runeId)
                    return dot;
            }
            Assert.Fail($"rün {runeId} için nokta yok");
            return 0;
        }

        /// <summary>Yüklemede AK fiili + dilimdeki ilk başka rün sıfat: v4 yolundan çıkan kombo.</summary>
        static void SliceComboDots(HexagonInputController input, out int verbDot, out int adjDot)
        {
            verbDot = DotForRune(input, AttackVerb);
            adjDot = 0;
            for (int dot = 1; dot <= Dovus.Core.Input.HexagonLayout.DotCount && adjDot == 0; dot++)
            {
                int rune = input.Engine.Loadout.RuneIdAtSlot(dot);
                if (dot != verbDot && RuleEngineV4Slice.IsSliceCombo(AttackVerb, rune))
                    adjDot = dot;
            }
            Assert.That(adjDot, Is.Not.Zero, "yüklemede dilim sıfatı yok");
        }

        static int OtherDot(HexagonInputController input, int a, int b)
        {
            for (int dot = 1; dot <= Dovus.Core.Input.HexagonLayout.DotCount; dot++)
            {
                if (dot != a && dot != b)
                    return dot;
            }
            return 0;
        }

        /// <summary>Kılıç kombosu için menzilde önde hedef; tek nokta testleri hedefsiz kalır.</summary>
        void ArrangeComboTarget()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * (WeaponRange(Kilic) * InRangeFrac));
            Physics.SyncTransforms();
        }

        static string Dots(CompletedSentence s) =>
            string.Join("-", System.Linq.Enumerable.Select(s.Words, w => w.Dot));

        /// <summary>Verilen noktalardan geçen tek çizgi; parmak son noktada kalır.</summary>
        IEnumerator DrawThroughDots(HexagonInputController input, params int[] dots)
        {
            input.Pointer.BeginPointer(DotPos(input, dots[0]));
            yield return null;
            for (int i = 1; i < dots.Length; i++)
            {
                input.Pointer.MovePointer(DotPos(input, dots[i]));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Input_SecondDot_LaunchesAndFlashesAtOnce_ThirdDotIgnored()
        {
            ArrangeComboTarget();
            HexagonInputController input = InputController();
            Assert.That(input.Combat.Sentence.MaxSentenceDots, Is.EqualTo(RuleEngineV4CastContext.VerbAdjectiveComboLen),
                "dilimde cümle tavanı");
            yield return WaitSentenceIdle(input);

            DragonRuneFlashView runes = RuneFlash();
            int flashesBefore = runes.PlayCount;
            bool runningAtResolve = false;
            int flashesAtResolve = 0;
            var done = new List<CompletedSentence>();
            void OnDone(CompletedSentence s)
            {
                done.Add(s);
                if (done.Count != 1)
                    return;
                runningAtResolve = _cast.IsRunning;
                flashesAtResolve = runes.PlayCount - flashesBefore;
            }
            input.Engine.SentenceCompleted += OnDone;
            try
            {
                SliceComboDots(input, out int verb, out int adj);
                yield return DrawThroughDots(input, verb, adj, OtherDot(input, verb, adj));
                input.Pointer.EndPointer(cancelled: false);
                Assert.That(done, Has.Count.EqualTo(1), "2. noktada kombo çözülmedi");
                Assert.That(done[0].Phase, Is.EqualTo(SentencePhase.Resolved));
                Assert.That(Dots(done[0]), Is.EqualTo($"{verb}-{adj}"), "kombo 2 kalmalı");
                Assert.That(runningAtResolve, Is.True, "skill çözüm anında çıkmadı");
                Assert.That(flashesAtResolve, Is.EqualTo(1), "harf flaşı çözüm anında yanmadı");

                float end = Time.realtimeSinceStartup + InputSettleSec;
                while (Time.realtimeSinceStartup < end)
                    yield return null;
                Assert.That(done, Has.Count.EqualTo(1), "3. nokta yeni cümle başlattı");
            }
            finally
            {
                input.Engine.SentenceCompleted -= OnDone;
            }
        }

        [UnityTest]
        public IEnumerator Input_OneDotReleaseOnCenter_AbortsWithoutSkill()
        {
            FreezeBoss();
            Arrange(Vector3.right);
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);

            var done = new List<CompletedSentence>();
            input.Engine.SentenceCompleted += done.Add;
            try
            {
                yield return DrawThroughDots(input, 1);
                Assert.That(input.Engine.State.Words.Count, Is.EqualTo(1), "çizim başlamadı");
                input.Pointer.MovePointer(CenterPos(input));
                yield return null;
                input.Pointer.EndPointer(cancelled: false);

                Assert.That(done, Has.Count.EqualTo(1), "iptal cümlesi");
                Assert.That(done[0].Phase, Is.EqualTo(SentencePhase.Aborted));
                Assert.That(done[0].Closing, Is.Null, "iptal skill üretmemeli");

                float end = Time.realtimeSinceStartup + InputSettleSec;
                while (Time.realtimeSinceStartup < end)
                {
                    Assert.That(_cast.IsRunning, Is.False, "iptalden sonra skill koştu");
                    yield return null;
                }
                Assert.That(done, Has.Count.EqualTo(1), "iptalden sonra ek cümle");
            }
            finally
            {
                input.Engine.SentenceCompleted -= done.Add;
            }
        }

        [UnityTest]
        public IEnumerator Input_AfterSecondDot_CenterReleaseDoesNotCancel()
        {
            ArrangeComboTarget();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);

            var done = new List<CompletedSentence>();
            input.Engine.SentenceCompleted += done.Add;
            try
            {
                SliceComboDots(input, out int verb, out int adj);
                yield return DrawThroughDots(input, verb, adj);
                input.Pointer.MovePointer(CenterPos(input));
                yield return null;
                input.Pointer.EndPointer(cancelled: false);

                Assert.That(done, Has.Count.EqualTo(1));
                Assert.That(done[0].Phase, Is.EqualTo(SentencePhase.Resolved), $"2. noktadan sonra iptal edildi ({Dots(done[0])})");
                Assert.That(done[0].Words.Count, Is.EqualTo(2));
                Assert.That(_cast.IsRunning, Is.True, "skill çıkmadı");
            }
            finally
            {
                input.Engine.SentenceCompleted -= done.Add;
            }
        }

        [UnityTest]
        public IEnumerator Input_CenterTapWithOneDot_Confirms()
        {
            FreezeBoss();
            Arrange(Vector3.right);
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);

            var done = new List<CompletedSentence>();
            input.Engine.SentenceCompleted += done.Add;
            try
            {
                yield return DrawThroughDots(input, 1);
                input.Pointer.EndPointer(cancelled: false);
                Assert.That(input.Engine.State.Phase, Is.EqualTo(SentencePhase.Building), "tek nokta beklemede değil");
                Assert.That(done, Is.Empty);

                input.Pointer.BeginPointer(CenterPos(input));
                input.Pointer.EndPointer(cancelled: false);

                Assert.That(done, Has.Count.EqualTo(1), "merkez dokunuşu onaylamadı");
                Assert.That(done[0].Phase, Is.EqualTo(SentencePhase.Resolved));
                Assert.That(done[0].Words.Count, Is.EqualTo(1));
            }
            finally
            {
                input.Engine.SentenceCompleted -= done.Add;
            }
        }

        [UnityTest]
        public IEnumerator Input_WhileDrawing_PlayerMovesAtFullSpeed()
        {
            FreezeBoss();
            Arrange(Vector3.right);
            HexagonInputController input = InputController();
            var move = _player.GetComponent<MoveInputController>();
            var motor = _player.GetComponent<KinematicMotorController>();
            var status = _player.GetComponent<ActorStatusHost>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            Assert.That(motor, Is.Not.Null, "KinematicMotorController");
            yield return WaitSentenceIdle(input);

            float fullSpeed = motor.Tuning.Player.WalkSpeedMps;
            float peak = 0f;
            bool sawDrawing = false;
            move.SetScriptedDirection(Vector2.right);
            try
            {
                input.Pointer.BeginPointer(DotPos(input, 1));
                float end = Time.realtimeSinceStartup + DrawSampleSec;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    if (input.Engine.State.Phase != SentencePhase.Building)
                        continue;
                    sawDrawing = true;
                    if (status != null)
                        Assert.That(status.EffectiveMoveSpeedMult, Is.EqualTo(1f), "çizerken yavaşlama");
                    peak = Mathf.Max(peak, motor.Velocity.magnitude);
                }
            }
            finally
            {
                input.Pointer.EndPointer(cancelled: true);
                move.SetScriptedDirection(null);
            }

            Assert.That(sawDrawing, Is.True, "çizim durumu görülmedi");
            Assert.That(peak, Is.EqualTo(fullSpeed).Within(SpeedTolMps), "çizerken tam hız");
        }
    }
}
