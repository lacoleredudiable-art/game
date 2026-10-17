using System.Collections;
using System.Collections.Generic;
using System.Text;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Yay yürüyüşü: ping gibi geri atma + kameranın yetişememesi. Gerçek yol takas + çubuk.
    /// Kılıç referans; Yay'da kare adımı yürüme hızını aşmaz, geri adım (stick'e ters) yok, kamera
    /// takip mesafesi Kılıç'la aynı mertebede, duvar O-7.2'de durur.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>O-1.23: ayak tabanı zemin düzleminin 0–3 cm üstünde; kök için aynı pay.</summary>
        const float GroundYTolM = 0.03f;
        /// <summary>FootLock görsel ofseti bu kadar aşarsa "uçuyor" sayılır (ölçüm eşiği).</summary>
        const float VisualLiftMaxM = 0.15f;
        const float WeaponWallWalkSec = 3f;
        /// <summary>Açık alanda yürüme: ping / hız patlaması ölçümü.</summary>
        const float WeaponOpenWalkSec = 2f;
        /// <summary>Kare adımı WalkSpeed×dt×bu çarpanı aşarsa rubber-band / ışınlanma.</summary>
        const float MaxStepFactor = 2.5f;
        /// <summary>Stick yönüne ters kare adımı (geri atma) bu eşiği aşarsa ping.</summary>
        const float ReverseStepMinM = 0.05f;
        /// <summary>Kamera–oyuncu yatay mesafe, Kılıç ortalamasının bu katını aşarsa kamera yetişmiyor.</summary>
        const float CameraLagRatioMax = 1.5f;
        /// <summary>Kalça kökten bu kadar öne kaçarsa mesh “kamerayı geride bırakır” (O-1.21 civarı).</summary>
        const float MaxHipDriftM = 0.35f;

        sealed class WeaponWalkSample
        {
            public string Label;
            public float StartRootY;
            public float EndRootY;
            public float MaxRootY;
            public float MinRootY;
            public float MaxVisualLocalY;
            public float MinVisualLocalY;
            public float MaxFeetY;
            public float MinFeetY;
            public float StopRadiusM;
            public float WantRadiusM;
            public float MaxStepM;
            public float MeanSpeedMps;
            public int ReverseSteps;
            public float MeanCamLagM;
            public float MaxCamLagM;
            public int SelfColliders;
            /// <summary>Kalça kemik XZ'nin kökten uzaklığı (mesh kameranın takip ettiği kökten öne kaçma).</summary>
            public float MaxHipDriftM;
            public readonly StringBuilder Log = new();
        }

        [UnityTest]
        public IEnumerator O_1_23_YayYuruyusu_KokYVeDuvar_KilicGibi()
        {
            FreezeBoss();
            ParkAll();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            SetSmokeLoadout(Kilic, Yay);
            Assert.That(_md.WeaponSwap, Is.Not.Null, "dilimde silah takası kurulmadı");

            Vector3 dir = Vector3.right;
            Vector3 wallStart = dir * ArenaEdgeStartM;
            Vector3 openStart = Origin;
            float want = VisibleInnerFaceM - PlayerBodyM;
            var fails = new List<string>();

            WeaponWalkSample kilicOpen = null;
            yield return WalkOpenWithWeapon(Kilic, openStart, dir, s => kilicOpen = s);
            WeaponWalkSample kilicWall = null;
            yield return WalkWallWithWeapon(Kilic, wallStart, dir, want, s => kilicWall = s);
            JudgeWeaponWalk(kilicWall, fails);
            JudgeRubberBand(kilicOpen, null, fails);

            yield return SwapByButton(input, Yay);
            WeaponWalkSample yayOpen = null;
            yield return WalkOpenWithWeapon(Yay, openStart, dir, s => yayOpen = s);
            WeaponWalkSample yayWall = null;
            yield return WalkWallWithWeapon(Yay, wallStart, dir, want, s => yayWall = s);
            JudgeWeaponWalk(yayWall, fails);
            JudgeRubberBand(yayOpen, kilicOpen, fails);

            Debug.Log("[O-1.23] silah yürüyüşü açık:\n" + kilicOpen.Log + "\n" + yayOpen.Log
                      + "\nduvar:\n" + kilicWall.Log + "\n" + yayWall.Log);
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
        }

        void JudgeWeaponWalk(WeaponWalkSample s, List<string> fails)
        {
            if (s == null)
            {
                fails.Add("örnek yok");
                return;
            }
            if (Mathf.Abs(s.EndRootY - s.StartRootY) > GroundYTolM
                || s.MaxRootY - s.StartRootY > GroundYTolM
                || s.StartRootY - s.MinRootY > GroundYTolM)
            {
                fails.Add($"{s.Label}: kök Y uçtu start={s.StartRootY:0.000} end={s.EndRootY:0.000} " +
                          $"min={s.MinRootY:0.000} max={s.MaxRootY:0.000} (izin ±{GroundYTolM:0.00} m)");
            }
            float visualLift = s.MaxVisualLocalY - s.MinVisualLocalY;
            if (visualLift > VisualLiftMaxM)
            {
                fails.Add($"{s.Label}: görsel local Y salınımı {visualLift:0.000} m " +
                          $"(min={s.MinVisualLocalY:0.000} max={s.MaxVisualLocalY:0.000}, izin {VisualLiftMaxM:0.00} m)");
            }
            if (Mathf.Abs(s.StopRadiusM - s.WantRadiusM) > PosTolM)
            {
                fails.Add($"{s.Label}: duvarda {s.StopRadiusM:0.000} m'de durdu, " +
                          $"beklenen {s.WantRadiusM:0.000} m ± {PosTolM:0.00}");
            }
        }

        void JudgeRubberBand(WeaponWalkSample s, WeaponWalkSample kilicRef, List<string> fails)
        {
            if (s == null)
            {
                fails.Add("açık alan örneği yok");
                return;
            }
            var motor = _player.GetComponent<KinematicMotorController>();
            float walk = motor != null ? motor.Tuning.Player.WalkSpeedMps : 6.4f;
            float maxStep = walk * (1f / 30f) * MaxStepFactor; // 30 Hz alt sınır
            if (s.MaxStepM > maxStep)
                fails.Add($"{s.Label}: kare adımı {s.MaxStepM:0.000} m > {maxStep:0.000} m (ping/hız patlaması)");
            if (s.ReverseSteps > 0)
                fails.Add($"{s.Label}: stick'e ters {s.ReverseSteps} kare (kamera merkezine geri atma)");
            if (s.SelfColliders > 0)
                fails.Add($"{s.Label}: oyuncu altında {s.SelfColliders} collider (self-hit rubber-band)");
            if (kilicRef != null && kilicRef.MeanCamLagM > 0.01f
                && s.MeanCamLagM > kilicRef.MeanCamLagM * CameraLagRatioMax)
            {
                fails.Add($"{s.Label}: kamera gecikmesi {s.MeanCamLagM:0.00} m " +
                          $"(Kılıç {kilicRef.MeanCamLagM:0.00} m, oran > {CameraLagRatioMax:0.0})");
            }
            if (kilicRef != null && kilicRef.MeanSpeedMps > 0.1f
                && s.MeanSpeedMps > kilicRef.MeanSpeedMps * CameraLagRatioMax)
            {
                fails.Add($"{s.Label}: ortalama hız {s.MeanSpeedMps:0.00} m/s " +
                          $"(Kılıç {kilicRef.MeanSpeedMps:0.00} m/s)");
            }
            if (s.MaxHipDriftM > MaxHipDriftM)
            {
                fails.Add($"{s.Label}: kalça kökten {s.MaxHipDriftM:0.00} m önde " +
                          $"(izin {MaxHipDriftM:0.00} m) — mesh kamera merkezinden kaçar / geri düşer");
            }
        }

        IEnumerator SwapByButton(HexagonInputController input, int weapon)
        {
            float swapWaitSec = _md.WeaponSwap.Rules.CooldownSec + _md.WeaponSwap.Rules.AnimationSec + WeaponSwapSlackSec;
            Vector2 swapPx = HexagonLayoutScreen.WeaponSwapButtonPx(input.Tuning, Screen.width, Screen.height);
            float end = Time.realtimeSinceStartup + swapWaitSec;
            float nextPress = 0f;
            while (_md.EquippedWeaponNumber() != weapon && Time.realtimeSinceStartup < end)
            {
                if (!_md.WeaponSwap.IsSwapping && Time.realtimeSinceStartup >= nextPress)
                {
                    input.Pointer.BeginPointer(swapPx);
                    input.Pointer.EndPointer(cancelled: false);
                    nextPress = Time.realtimeSinceStartup + WeaponSwapRetrySec;
                }
                yield return null;
            }
            Assert.That(_md.EquippedWeaponNumber(), Is.EqualTo(weapon),
                $"takas düğmesi silahı {weapon} yapmadı (şimdi {_md.EquippedWeaponNumber()})");
        }

        IEnumerator WalkOpenWithWeapon(int weapon, Vector3 start, Vector3 dir, System.Action<WeaponWalkSample> done)
        {
            yield return WalkSample(weapon, start, dir, WeaponOpenWalkSec, wantRadius: -1f, done);
        }

        IEnumerator WalkWallWithWeapon(
            int weapon, Vector3 start, Vector3 dir, float wantRadius, System.Action<WeaponWalkSample> done)
        {
            yield return WalkSample(weapon, start, dir, WeaponWallWalkSec, wantRadius, done);
        }

        IEnumerator WalkSample(
            int weapon, Vector3 start, Vector3 dir, float sec, float wantRadius, System.Action<WeaponWalkSample> done)
        {
            Assert.That(_md.EquippedWeaponNumber(), Is.EqualTo(weapon), "takılı silah");
            var grounding = _player.GetComponent<ActorGroundingController>();
            Assert.That(grounding, Is.Not.Null, "ActorGroundingController");
            Transform visual = ResolveVisual();
            Assert.That(visual, Is.Not.Null, "görsel çocuk yok");
            var follow = Object.FindAnyObjectByType<FollowCameraController>();
            Animator anim = _player.GetComponentInChildren<Animator>();
            Transform hips = anim != null ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;

            Place(_player, start);
            _player.rotation = Quaternion.LookRotation(dir);
            Physics.SyncTransforms();
            // Kamera Place sıçramasından toparlasın (Yay maxCamLag artefaktı).
            for (int i = 0; i < 30; i++)
                yield return null;

            var sample = new WeaponWalkSample
            {
                Label = wantRadius < 0f ? $"w{weapon}-açık" : $"w{weapon}-duvar",
                StartRootY = _player.position.y,
                EndRootY = _player.position.y,
                MaxRootY = _player.position.y,
                MinRootY = _player.position.y,
                MaxVisualLocalY = visual.localPosition.y,
                MinVisualLocalY = visual.localPosition.y,
                MaxFeetY = ActorGroundingController.MeasureFeet(_player),
                MinFeetY = ActorGroundingController.MeasureFeet(_player),
                WantRadiusM = wantRadius,
                SelfColliders = CountSelfColliders(_player),
            };

            var move = _player.GetComponent<MoveInputController>();
            Assert.That(move, Is.Not.Null, "MoveInputController");
            Vector3 prev = Flat(_player.position);
            float distSum = 0f;
            float timeSum = 0f;
            float camLagSum = 0f;
            int frames = 0;
            try
            {
                double endMs = _md.MechanicsClock.Director.WorldTimeMs + sec * 1000.0;
                float guard = Time.realtimeSinceStartup + sec * 4f + 2f;
                while (_md.MechanicsClock.Director.WorldTimeMs < endMs && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    yield return null;
                    float dt = Time.deltaTime > 1e-4f ? Time.deltaTime : 1f / 60f;
                    Vector3 now = Flat(_player.position);
                    Vector3 step = now - prev;
                    float stepM = step.magnitude;
                    sample.MaxStepM = Mathf.Max(sample.MaxStepM, stepM);
                    if (stepM > 1e-4f && Vector3.Dot(step, Flat(dir)) < -ReverseStepMinM)
                        sample.ReverseSteps++;
                    distSum += stepM;
                    timeSum += dt;
                    frames++;
                    if (follow != null)
                    {
                        float lag = Flat(follow.transform.position - _player.position).magnitude;
                        sample.MaxCamLagM = Mathf.Max(sample.MaxCamLagM, lag);
                        camLagSum += lag;
                    }
                    float rootY = _player.position.y;
                    float visY = visual.localPosition.y;
                    float feet = ActorGroundingController.MeasureFeet(_player);
                    sample.MaxRootY = Mathf.Max(sample.MaxRootY, rootY);
                    sample.MinRootY = Mathf.Min(sample.MinRootY, rootY);
                    sample.MaxVisualLocalY = Mathf.Max(sample.MaxVisualLocalY, visY);
                    sample.MinVisualLocalY = Mathf.Min(sample.MinVisualLocalY, visY);
                    if (!float.IsInfinity(feet))
                    {
                        sample.MaxFeetY = Mathf.Max(sample.MaxFeetY, feet);
                        sample.MinFeetY = Mathf.Min(sample.MinFeetY, feet);
                    }
                    if (hips != null)
                    {
                        Vector3 hipFlat = Flat(hips.position);
                        float drift = (hipFlat - now).magnitude;
                        sample.MaxHipDriftM = Mathf.Max(sample.MaxHipDriftM, drift);
                    }
                    prev = now;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }

            sample.EndRootY = _player.position.y;
            sample.StopRadiusM = Flat(_player.position).magnitude;
            sample.MeanSpeedMps = timeSum > 1e-4f ? distSum / timeSum : 0f;
            sample.MeanCamLagM = frames > 0 ? camLagSum / frames : 0f;
            sample.SelfColliders = CountSelfColliders(_player);
            sample.Log.Append(
                $"{sample.Label}: stopR={sample.StopRadiusM:0.000} want={sample.WantRadiusM:0.000} " +
                $"rootY={sample.StartRootY:0.000}→{sample.EndRootY:0.000} " +
                $"visLocal {sample.MinVisualLocalY:0.000}..{sample.MaxVisualLocalY:0.000} " +
                $"maxStep={sample.MaxStepM:0.000} meanV={sample.MeanSpeedMps:0.00} " +
                $"rev={sample.ReverseSteps} camLag mean={sample.MeanCamLagM:0.00} max={sample.MaxCamLagM:0.00} " +
                $"selfCol={sample.SelfColliders} hipDrift={sample.MaxHipDriftM:0.00}");
            done(sample);
        }

        Transform ResolveVisual()
        {
            Transform visual = _player.Find("Visual");
            if (visual != null)
                return visual;
            Animator anim = _player.GetComponentInChildren<Animator>();
            return anim != null && anim.transform != _player ? anim.transform : null;
        }

        static int CountSelfColliders(Transform player)
        {
            int n = 0;
            foreach (Collider c in player.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || !c.enabled || c.isTrigger)
                    continue;
                Transform t = c.transform;
                while (t != null && t != player)
                {
                    string name = t.name;
                    if (name.StartsWith("Silah_", System.StringComparison.Ordinal)
                        || name.StartsWith("SM_Item_", System.StringComparison.Ordinal)
                        || name.IndexOf("Placeholder", System.StringComparison.Ordinal) >= 0)
                    {
                        n++;
                        break;
                    }
                    t = t.parent;
                }
            }
            return n;
        }
    }
}
