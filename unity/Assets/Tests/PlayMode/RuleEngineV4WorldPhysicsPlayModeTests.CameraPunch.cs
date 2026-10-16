using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Element;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Kamera vuruşu FOV'u yalnız orana göre daraltır (O-5.4; Gözcü <c>kamera_fov_gecersiz</c>): Punch FOV'u
    /// taban × (1 − oran × t) ile hesaplar, oran 0–0,25.
    /// Kombo kapanış vuruşu ve mükemmel dodge FOV'u bu tabanın altına indirmez, [V4-FALLBACK] yazdırmaz.
    /// Girdi gerçek yoldan: altıgen çizimi, dodge düğmesine dokunuş.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float PunchWatchSec = 1f;
        const float FovFloorTolDeg = 0.05f;
        /// <summary>12-kusur-envanteri A1: Punch FOV oranı 0–0,25.</summary>
        const float PunchFovKickMaxRatio = 0.25f;

        float FovFloorDeg() => JsonNumber(SliceCameraJson(), "fov_deg") * (1f - PunchFovKickMaxRatio);

        static string FovReport(List<CameraRenderedProbe.Sample> samples, float floor, out float min)
        {
            min = float.PositiveInfinity;
            int below = 0;
            string first = "-";
            foreach (CameraRenderedProbe.Sample s in samples)
            {
                min = Mathf.Min(min, s.Fov);
                if (s.Fov >= floor - FovFloorTolDeg)
                    continue;
                if (below++ == 0)
                    first = $"kare {s.Frame}: FOV {s.Fov:0.00}°";
            }
            return $"{samples.Count} karenin {below}'inde FOV {floor:0.00}° tabanının altında; en düşük {min:0.00}°, ilk {first}";
        }

        [UnityTest]
        public IEnumerator O_5_4_KomboKameraVurusu_FovOranTabanininAltinaInmez()
        {
            FreezeBoss();
            float floor = FovFloorDeg();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            RuneLoadout saved = input.Engine.Loadout;
            Assert.That(input.Engine.TrySetLoadout(new RuneLoadout(RuleEngineV4Slice.Runes.ToArray())), Is.True, "build seçilemedi");
            SetSmokeLoadout(Kilic, Yay);
            CameraRenderedProbe probe = AddCameraProbe(Camera.main, 0f);
            int cast = 0;
            try
            {
                foreach (int verb in RuleEngineV4Slice.Runes)
                {
                    int adj = 0;
                    foreach (int a in RuleEngineV4Slice.Runes)
                    {
                        if (adj == 0 && RuleEngineV4Slice.IsSliceCombo(verb, a))
                            adj = a;
                    }
                    if (adj == 0)
                        continue;
                    _label = $"kombo {verb}-{adj}";
                    yield return SmokeReady(input);
                    SmokePlaceMinions();
                    int verbDot = DotForRune(input, verb);
                    int adjDot = DotForRune(input, adj);
                    input.Pointer.BeginPointer(DotPos(input, verbDot));
                    yield return null;
                    if (adjDot == verbDot)
                    {
                        input.Pointer.MovePointer(Vector2.Lerp(DotPos(input, verbDot), CenterPos(input), SmokeOffDotFrac));
                        yield return null;
                    }
                    input.Pointer.MovePointer(DotPos(input, adjDot));
                    yield return null;
                    input.Pointer.EndPointer(cancelled: false);
                    cast += _cast.IsRunning ? 1 : 0;
                    yield return WaitUntilIdle();
                    yield return new WaitForSecondsRealtime(PunchWatchSec);
                }
            }
            finally
            {
                input.Engine.TrySetLoadout(saved);
            }
            List<CameraRenderedProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);

            Assert.That(cast, Is.GreaterThan(0), "çizimle hiç kombo atılmadı");
            string report = FovReport(samples, floor, out float min);
            Assert.That(min, Is.GreaterThanOrEqualTo(floor - FovFloorTolDeg), $"{cast} kombo: {report}");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_5_4_Dodge_FovOranTabanininAltinaInmez()
        {
            FreezeBoss();
            ParkAll();
            float floor = FovFloorDeg();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            CameraRenderedProbe probe = AddCameraProbe(Camera.main, 0f);
            _label = "dodge";
            yield return PressDodgeUntilStarted(input);
            // Gelen vuruş hasar borusunun girişinden: dodge penceresinde yutulur.
            Assert.That(PlayerDodgeController.BlocksIncoming(_player), Is.True, "dodge gelen vuruşu yutmadı");
            yield return new WaitForSecondsRealtime(PunchWatchSec);
            List<CameraRenderedProbe.Sample> samples = probe.Samples;
            Object.Destroy(probe.gameObject);

            string report = FovReport(samples, floor, out float min);
            Assert.That(min, Is.GreaterThanOrEqualTo(floor - FovFloorTolDeg), report);
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }
    }
}
