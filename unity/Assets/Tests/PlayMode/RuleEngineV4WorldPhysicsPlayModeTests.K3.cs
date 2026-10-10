using System;
using System.Collections;
using System.IO;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>k3 dilim oyuncusu: görsel bağlama, controller ve klip olaylarının alıcısı.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string K3PrefabPath = "Assets/Art/Characters/k3/k3.prefab";
        const string K3ControllerName = "k3";
        const string MissingEventReceiver = "has no receiver";
        const string SwordDashClip = "Kilic_ATIL";
        const string SwordStrikeClip = "Kilic_VUR";
        /// <summary>Komut satırı: -k3Capture &lt;klasör&gt; verilirse dilim sahnesinin görüntüsü kaydedilir.</summary>
        const string CaptureArg = "-k3Capture";
        const int CaptureW = 1280;
        const int CaptureH = 720;

        static GameObject LoadSlicePlayerVisual()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(K3PrefabPath);
            Assert.That(prefab, Is.Not.Null, K3PrefabPath);
            return prefab;
#else
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator K3_IsSlicePlayerVisual()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Animator anim = PlayerAnimator();
            Assert.That(anim.runtimeAnimatorController.name, Is.EqualTo(K3ControllerName), "oyuncu controller'ı");
            Assert.That(anim.GetComponent<SkillAnimVfxEventHost>(), Is.Not.Null, "Animator objesinde SkillAnimVfxEventHost yok");
            Assert.That(anim.isHuman, Is.True, "k3 Humanoid değil");
#if UNITY_EDITOR
            var ctrl = (UnityEditor.Animations.AnimatorController)anim.runtimeAnimatorController;
            var noWriteDefaults = ctrl.layers.SelectMany(l => l.stateMachine.states)
                .Where(s => !s.state.writeDefaultValues).Select(s => s.state.name).ToArray();
            Assert.That(noWriteDefaults, Is.Empty, "Write Defaults kapalı state'ler");
#endif
            yield return CaptureIfRequested("k3_dilim_sahne.png");
        }

        [UnityTest]
        public IEnumerator K3_SwordCombos_PlayK3Clips_NoMissingEventReceiver()
        {
            FreezeBoss();
            Equip(Kilic);
            Animator anim = PlayerAnimator();
            AnimationBridge bridge = _md.AnimationBridge;
            foreach (int adj in RuleEngineV4Slice.Runes)
                yield return CastAndPlayOut(3, adj, SwordDashClip, anim, bridge);
            // Kilic_ATIL olay taşımıyor; olaylı Kilic_VUR da alıcıyı sınar.
            yield return CastAndPlayOut(1, 2, SwordStrikeClip, anim, bridge);
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        IEnumerator CastAndPlayOut(int verb, int adj, string clipName, Animator anim, AnimationBridge bridge)
        {
            _label = $"{verb}-{adj} kılıç";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();
            Assert.That(_cast.TryLaunchCombo(verb, adj, out _), Is.True, $"{_label} cast edilmedi");
            Assert.That(bridge.LastClipName, Is.EqualTo(clipName), $"{_label} klibi");
            Assert.That(bridge.LastExactClipFound && bridge.LastPlayApplied, Is.True, $"{_label} k3 klibi oynamadı");
            AnimationClip clip = anim.runtimeAnimatorController.animationClips.First(c => c.name == clipName);
            yield return WaitUntilIdle();
            yield return WaitWorld(clip.length);
        }

        Animator PlayerAnimator()
        {
            Animator anim = _player.GetComponentInChildren<Animator>();
            Assert.That(anim, Is.Not.Null, "oyuncuda Animator yok");
            Assert.That(anim.runtimeAnimatorController, Is.Not.Null, "oyuncu Animator'ında controller yok");
            return anim;
        }

        IEnumerator CaptureIfRequested(string fileName)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, CaptureArg);
            if (i < 0 || i + 1 >= args.Length)
                yield break;
            // Batchmode'da WaitForEndOfFrame dönmeyebilir; Camera.Render kare ortasında da çalışır.
            yield return null;
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "ana kamera yok");
            var rt = new RenderTexture(CaptureW, CaptureH, 24);
            RenderTexture prev = cam.targetTexture;
            // Elle Render batchmode'da paket shader içe aktarım hatası basabilir; yakalama oyun hatası sayılmaz.
            int errorsBefore = _errors.Count;
            LogAssert.ignoreFailingMessages = true;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            if (_errors.Count > errorsBefore)
            {
                Debug.LogWarning("k3 yakalama sırasında: " + string.Join("\n", _errors.Skip(errorsBefore)));
                _errors.RemoveRange(errorsBefore, _errors.Count - errorsBefore);
            }
            RenderTexture.active = rt;
            var tex = new Texture2D(CaptureW, CaptureH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, CaptureW, CaptureH), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(args[i + 1]);
            File.WriteAllBytes(Path.Combine(args[i + 1], fileName), tex.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(rt);
        }
    }
}
