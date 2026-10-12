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
        const string GripRight = "Grip_R";
        const string GripLeft = "Grip_L";
        const string SwordObject = "Silah_Kilic";
        const string BowObject = "Silah_Yay";
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
            yield return CastAndPlayOut(1, 2, SwordStrikeClip, anim, bridge);
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>Kılıç ATIL: kök XZ pose'a gömülmez; kalça oyuncu nesnesinden ayrılmaz.</summary>
        const float ClipActiveWeight = 0.5f;
        /// <summary>Animatör kararı: kalça kayması tavanı klipler.csv'den okunur (Assets'e göre yol).</summary>
        const string ClipCsvRelativePath = "Art/Characters/k3/klipler.csv";
        const string HipMaxOffsetColumn = "kalca_kayma_m";

        static float ClipCsvFloat(string clip, string column)
        {
            string[] lines = File.ReadAllLines(Path.Combine(Application.dataPath, ClipCsvRelativePath));
            int col = Array.IndexOf(lines[0].Split(';'), column);
            Assert.That(col, Is.GreaterThanOrEqualTo(0), $"klipler.csv: {column} sütunu yok");
            string[] row = lines.Skip(1).Select(l => l.Split(';')).FirstOrDefault(f => f[0] == clip);
            Assert.That(row, Is.Not.Null, $"klipler.csv: {clip} satırı yok");
            return float.Parse(row[col], System.Globalization.CultureInfo.InvariantCulture);
        }

        [UnityTest]
        [Ignore("Eski hata: WaitForEndOfFrame batchmode'da gelmiyor, 12 Ekim")]
        public IEnumerator K3_SwordDash_HipsStayOnPlayer()
        {
            FreezeBoss();
            Equip(Kilic);
            Animator anim = PlayerAnimator();
            Assert.That(anim.applyRootMotion, Is.False, "oyuncu Animator'ında applyRootMotion açık");
            Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            _label = "3-1 kılıç ATIL";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();
            Assert.That(_cast.TryLaunchCombo(3, 1, out _), Is.True, $"{_label} cast edilmedi");
            AnimationClip clip = anim.runtimeAnimatorController.animationClips.First(c => c.name == SwordDashClip);
            float maxM = 0f;
            float maxAtN = 0f;
            int frames = 0;
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (Time.realtimeSinceStartup < end)
            {
                yield return new WaitForEndOfFrame();
                if (!anim.GetCurrentAnimatorClipInfo(0).Any(c => c.clip == clip && c.weight > ClipActiveWeight))
                {
                    if (frames > 0)
                        break;
                    continue;
                }
                frames++;
                Vector3 d = hips.position - _player.position;
                d.y = 0f;
                if (d.magnitude > maxM)
                {
                    maxM = d.magnitude;
                    maxAtN = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
                }
            }
            Debug.Log($"[k3kanit] ATIL kalça-oyuncu en büyük yatay uzaklık={maxM:0.000} m (klip n={maxAtN:0.00}) kare={frames}");
            Assert.That(frames, Is.GreaterThan(0), $"{SwordDashClip} oynamadı");
            float hipMaxM = ClipCsvFloat(SwordDashClip, HipMaxOffsetColumn);
            Assert.That(maxM, Is.LessThanOrEqualTo(hipMaxM), $"ATIL kalçası oyuncudan {maxM:0.000} m kaydı (tavan {hipMaxM:0.00} m)");
            yield return WaitUntilIdle();
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator K3_OwnWeapons_AtZeroOnHoldingHandGrip()
        {
            Equip(Kilic);
            yield return null;
            Animator anim = PlayerAnimator();
            Transform rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform sword = ChildPath(rightHand, GripRight, SwordObject);
            Transform bow = ChildPath(leftHand, GripLeft, BowObject);
            Assert.That(bow.localPosition.magnitude, Is.LessThan(1e-4f), "Yay Grip_L'de sıfır konumda değil");
            Assert.That(Quaternion.Angle(bow.localRotation, Quaternion.identity), Is.LessThan(0.01f), "Yay Grip_L'de sıfır açıda değil");
            Assert.That(sword.gameObject.activeInHierarchy, Is.True, "Kılıç seçiliyken Silah_Kilic görünmüyor");
            Assert.That(bow.gameObject.activeSelf, Is.False, "Kılıç seçiliyken Silah_Yay görünüyor");
            Assert.That(sword.localPosition.magnitude, Is.LessThan(1e-4f), "Kılıç Grip_R'de sıfır konumda değil");
            Assert.That(Quaternion.Angle(sword.localRotation, Quaternion.identity), Is.LessThan(0.01f), "Kılıç Grip_R'de sıfır açıda değil");
            string[] rightProps = rightHand.Cast<Transform>().Select(c => c.name)
                .Where(n => !n.StartsWith("mixamorig", StringComparison.Ordinal)).ToArray();
            Assert.That(rightProps, Is.EqualTo(new[] { GripRight }), "sağ elde Grip_R dışında prop");

            Equip(Yay);
            yield return null;
            Assert.That(bow.gameObject.activeInHierarchy, Is.True, "Yay seçiliyken Silah_Yay görünmüyor");
            Assert.That(sword.gameObject.activeSelf, Is.False, "Yay seçiliyken Silah_Kilic görünüyor");
        }

        [UnityTest]
        public IEnumerator K3_Sword_NoShieldOnLeftArm()
        {
            Equip(Kilic);
            yield return null;
            Transform leftArm = PlayerAnimator().GetBoneTransform(HumanBodyBones.LeftLowerArm);
            string[] visible = leftArm.GetComponentsInChildren<Renderer>()
                .Where(r => r.enabled).Select(r => r.transform.parent.name + "/" + r.name).ToArray();
            Assert.That(visible, Is.Empty, "Kılıç seçiliyken sol kolda görünen prop (kalkan)");
        }

        static Transform ChildPath(Transform t, params string[] names)
        {
            foreach (string n in names)
            {
                Transform next = t.Find(n);
                Assert.That(next, Is.Not.Null, $"{t.name}/{n} yok");
                t = next;
            }
            return t;
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
