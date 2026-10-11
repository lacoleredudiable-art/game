using System;
using System.Collections;
using System.IO;
using System.Linq;
using Dovus.Core.Element;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Casting;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// k3 kanıtı: gerçek çizim (PointerRouter) ile Kılıç ATIL ve Yay vuruşu; kor izi ve harf flaşı
    /// görülen kareler. PNG yalnız -k3Capture &lt;klasör&gt; ya da editörde SessionState
    /// "Dovus.K3CaptureDir" verilince yazılır (ekran görüntüsü: harf flaşı overlay tuvalde).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const int DashVerb = 3;
        const float EvidenceWatchSec = 1.2f;
        const string CaptureDirKey = "Dovus.K3CaptureDir";
        const string EmberTrailName = "EmberMeshTrail";

        sealed class EvidenceLog
        {
            public int Frames;
            public int TrailFrames;
            public int FlashFrames;
            public int BothFrames;
            public override string ToString() =>
                $"kare={Frames} kor izi={TrailFrames} harf flaşı={FlashFrames} ikisi={BothFrames}";
        }

        [UnityTest]
        public IEnumerator K3_RealDraw_SwordDashAndBowStrike_ShowTrailAndRuneFlash()
        {
            FreezeBoss();
            HexagonInputController input = InputController();
            DragonRuneFlashView runes = RuneFlash();
            Assert.That(PlayerAnimator().GetComponent<SkillAnimVfxEventHost>(), Is.Not.Null, "oyuncuda SkillAnimVfxEventHost yok");

            // Dilim rünleri build olarak seçilir (oyuncunun build seçimiyle aynı yol); ATIL fiili varsayılan yüklemede yok.
            yield return WaitSentenceIdle(input);
            RuneLoadout saved = input.Engine.Loadout;
            Assert.That(input.Engine.TrySetLoadout(new RuneLoadout(RuleEngineV4Slice.Runes.ToArray())), Is.True, "build seçilemedi");
            try
            {
                Equip(Yay);
                ArrangeTargetInRange(Kilic, 1);
                yield return WaitSentenceIdle(input);
                var bow = new EvidenceLog();
                yield return DrawAndWatch(input, runes, AttackVerb, "yay_vur", bow);
                Debug.Log("[k3kanit] Yay vuruş " + bow);
                Assert.That(bow.FlashFrames, Is.GreaterThan(0), "Yay: harf flaşı görülmedi");
                yield return WaitUntilIdle();

                Equip(Kilic);
                ArrangeTargetInRange(Kilic, 0);
                yield return WaitSentenceIdle(input);
                var sword = new EvidenceLog();
                yield return DrawAndWatch(input, runes, DashVerb, "kilic_atil", sword);
                Debug.Log("[k3kanit] Kılıç ATIL " + sword);
                Assert.That(sword.TrailFrames, Is.GreaterThan(0), "Kılıç ATIL: kor izi görülmedi");
                Assert.That(sword.FlashFrames, Is.GreaterThan(0), "Kılıç ATIL: harf flaşı görülmedi");
            }
            finally
            {
                input.Engine.TrySetLoadout(saved);
            }
        }

        /// <summary>Her vuruşa ayrı minyon: önceki vuruş hedefi öldürebilir.</summary>
        void ArrangeTargetInRange(int rangeWeapon, int minion)
        {
            Arrange(Vector3.right);
            Place(_minions[minion].transform, Origin + Vector3.right * (WeaponRange(rangeWeapon) * InRangeFrac));
            Physics.SyncTransforms();
        }

        IEnumerator DrawAndWatch(HexagonInputController input, DragonRuneFlashView runes, int verb, string label, EvidenceLog log)
        {
            int verbDot = DotForRune(input, verb);
            int adjDot = 0;
            for (int dot = 1; dot <= Dovus.Core.Input.HexagonLayout.DotCount && adjDot == 0; dot++)
            {
                if (dot != verbDot && RuleEngineV4Slice.IsSliceCombo(verb, input.Engine.Loadout.RuneIdAtSlot(dot)))
                    adjDot = dot;
            }
            Assert.That(adjDot, Is.Not.Zero, $"{label}: yüklemede dilim sıfatı yok");
            _label = $"{label} {verbDot}-{adjDot}";

            int resolved = 0;
            void OnResolved(Dovus.Core.Grammar.CompletedSentence s) => resolved++;
            input.Engine.SentenceCompleted += OnResolved;
            try
            {
                yield return DrawThroughDots(input, verbDot, adjDot);
            }
            finally
            {
                input.Engine.SentenceCompleted -= OnResolved;
            }
            string drawState = $"faz={input.Engine.State.Phase} kelime={input.Engine.State.Words.Count} kilit={input.SweepInputLocked}";
            input.Pointer.EndPointer(cancelled: false);
            Assert.That(_cast.IsRunning, Is.True,
                $"{_label}: çizimle skill çıkmadı; çözülen={resolved} red={_cast.LastDenial} {drawState} {Diag()}");

            string dir = CaptureDir();
            // Kayıtta altıgen paneli oyuncuyu örtüyor; çizim bittikten sonra yalnız kayıt için gizlenir.
            Canvas hex = dir != null ? Object.FindAnyObjectByType<HexagonView>()?.Canvas : null;
            if (hex != null)
                hex.enabled = false;
            float end = Time.realtimeSinceStartup + EvidenceWatchSec;
            while (Time.realtimeSinceStartup < end)
            {
                yield return new WaitForEndOfFrame();
                bool trail = EmberTrailVisible();
                bool flash = runes.VerbVisible || runes.AdjectiveVisible;
                log.Frames++;
                log.TrailFrames += trail ? 1 : 0;
                log.FlashFrames += flash ? 1 : 0;
                log.BothFrames += trail && flash ? 1 : 0;
                if (dir != null && (trail || flash))
                    SaveScreen(dir, $"{label}_{log.Frames:000}{(trail ? "_iz" : "")}{(flash ? "_flas" : "")}.png");
            }
            if (hex != null)
                hex.enabled = true;
        }

        static bool EmberTrailVisible() =>
            Object.FindObjectsByType<MeshRenderer>()
                .Any(r => r.name == EmberTrailName && r.enabled && r.gameObject.activeInHierarchy);

        static string CaptureDir()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, CaptureArg);
            if (i >= 0 && i + 1 < args.Length)
                return args[i + 1];
#if UNITY_EDITOR
            string dir = UnityEditor.SessionState.GetString(CaptureDirKey, string.Empty);
            if (!string.IsNullOrEmpty(dir))
                return dir;
#endif
            return null;
        }

        static void SaveScreen(string dir, string fileName)
        {
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, fileName), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
