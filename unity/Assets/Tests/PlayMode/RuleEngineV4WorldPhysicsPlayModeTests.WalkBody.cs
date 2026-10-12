using System.Collections;
using System.Collections.Generic;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Görünen konum: karedeki bütün yazıcılardan sonra (en son LateUpdate) oyuncu ve gövde merkezinin yatay uzaklığı.
    /// Kare ortasında okunan konum görünen konum değildir (dondurulmuş boss'u reaktör Update'te evine, LateUpdate
    /// başka bir yere yazar).
    /// </summary>
    [DefaultExecutionOrder(int.MaxValue)]
    public sealed class WalkBodyRenderedProbe : MonoBehaviour
    {
        public Transform Walker;
        public Transform Body;
        public Vector3 WalkerXZ;
        public Vector3 BodyXZ;
        public int Frames;

        void LateUpdate()
        {
            if (Walker == null || Body == null)
                return;
            WalkerXZ = new Vector3(Walker.position.x, 0f, Walker.position.z);
            BodyXZ = new Vector3(Body.position.x, 0f, Body.position.z);
            Frames++;
        }
    }

    /// <summary>
    /// Yürüme gövdeye girmez (O-4.9, O-6.10, O-1.12): boss ve dost dünya fiziği yarıçaplı dikey silindirdir;
    /// yürüyen oyuncunun merkezi gövde merkezine yarıçaplar toplamından (boss 2,5 + 0,35 = 2,85 m) yaklaşmaz.
    /// Gövdeye doğru yürümeye devam eden oyuncu yerinde titremez; yan açıyla gelen çevresinden kayarak geçer.
    /// Girdi gerçek yoldan: çubuk. Ölçü görünen konumdan (<see cref="WalkBodyRenderedProbe"/>).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float BodyWalkSec = 3f;
        /// <summary>Yürüme başlangıcı temas mesafesinin bu kadar ötesinde.</summary>
        const float BodyWalkStartExtraM = 2f;
        /// <summary>Yan geçiş: düz yol gövde merkezinden temas mesafesinin yarısı kadar yanda (gövdenin içinden geçer).</summary>
        const float BodyWalkSideOffsetFrac = 0.5f;
        /// <summary>Titreme sayımında bundan kısa kare adımı duruş sayılır.</summary>
        const float BodyWalkStepEpsM = 0.001f;

        [UnityTest]
        public IEnumerator O_4_9_BossaYuruyen_GovdeyeGirmez_Titremez()
        {
            FreezeBoss();
            ParkAll();
            yield return BodyHeadOnWalk("boss'a doğru yürüme", _boss, BossBodyM);
        }

        [UnityTest]
        public IEnumerator O_4_9_DostaYuruyen_GovdeyeGirmez_Titremez()
        {
            FreezeBoss();
            ParkAll();
            Place(_allies[0], Origin);
            Physics.SyncTransforms();
            yield return BodyHeadOnWalk("dosta doğru yürüme", _allies[0], PlayerBodyM);
        }

        [UnityTest]
        public IEnumerator O_4_9_BossaYanAciylaYuruyen_CevresindenKayar()
        {
            FreezeBoss();
            ParkAll();
            Vector3 bossC = Flat(_boss.position);
            Vector3 toCenter = BossToCenter();
            Vector3 dir = Vector3.Cross(Vector3.up, toCenter).normalized;
            float contact = BossBodyM + PlayerBodyM;
            Vector3 start = bossC + toCenter * (contact * BodyWalkSideOffsetFrac) - dir * (contact + BodyWalkStartExtraM);
            _label = "boss'a yan açıyla yürüme";
            MoveInputController move = BeginScenario(_label, start, dir, out _, out _);
            WalkBodyRenderedProbe probe = StartProbe(_boss);
            float nearest = float.MaxValue;
            float along = Vector3.Dot(Flat(_player.position) - bossC, dir);
            try
            {
                double endMs = _md.MechanicsClock.Director.WorldTimeMs + BodyWalkSec * 1000.0;
                float guard = Time.realtimeSinceStartup + BodyWalkSec * 4f + 2f;
                while (along < contact && _md.MechanicsClock.Director.WorldTimeMs < endMs
                       && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    yield return null;
                    if (probe.Frames == 0)
                        continue;
                    nearest = Mathf.Min(nearest, (probe.WalkerXZ - probe.BodyXZ).magnitude);
                    along = Vector3.Dot(probe.WalkerXZ - probe.BodyXZ, dir);
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
                Object.Destroy(probe.gameObject);
            }

            Debug.Log($"[O-4.9] {_label}: boss merkezine en yakın {nearest:0.000} m (temas {contact:0.000} m), " +
                      $"boss'un ötesine {along:0.00} m");
            var fails = new List<string>();
            if (nearest < contact - PosTolM)
                fails.Add($"{_label}: boss merkezine {nearest:0.000} m yaklaştı < {contact:0.000} m " +
                          $"({(contact - nearest) * 100f:0.0} cm içeride)");
            if (along < contact)
                fails.Add($"{_label}: {BodyWalkSec:0} sn içinde boss'un çevresinden geçemedi " +
                          $"(boss'un ötesine {along:0.00} m, gereken {contact:0.00} m)");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>
        /// Oyuncu gövdeye bakarak başlar ve çubuk <see cref="BodyWalkSec"/> boyunca gövdeye doğru tutulur.
        /// Ölçüt: en yakın an ≥ yarıçaplar toplamı; oyuncu temas mesafesine varır (görünmez duvar yok);
        /// temasta ileri-geri gidip gelmez (ardışık iki adım ters yönde = titreme).
        /// </summary>
        IEnumerator BodyHeadOnWalk(string label, Transform body, float bodyR)
        {
            Vector3 c0 = Flat(body.position);
            Vector3 toCenter = Flat(Vector3.zero - c0);
            toCenter = toCenter.sqrMagnitude > PosTolM * PosTolM ? toCenter.normalized : Vector3.back;
            float contact = bodyR + PlayerBodyM;
            Vector3 start = c0 + toCenter * (contact + BodyWalkStartExtraM);
            Vector3 dir = -toCenter;
            MoveInputController move = BeginScenario(label, start, dir, out _, out _);
            WalkBodyRenderedProbe probe = StartProbe(body);
            float nearest = float.MaxValue;
            float final = float.MaxValue;
            int reversals = 0;
            try
            {
                double endMs = _md.MechanicsClock.Director.WorldTimeMs + BodyWalkSec * 1000.0;
                float guard = Time.realtimeSinceStartup + BodyWalkSec * 4f + 2f;
                int seen = 0;
                Vector3 last = Vector3.zero;
                Vector3 lastStep = Vector3.zero;
                while (_md.MechanicsClock.Director.WorldTimeMs < endMs && Time.realtimeSinceStartup < guard)
                {
                    Steer(move, dir);
                    yield return null;
                    if (probe.Frames == seen)
                        continue;
                    seen = probe.Frames;
                    Vector3 p = probe.WalkerXZ;
                    final = (p - probe.BodyXZ).magnitude;
                    nearest = Mathf.Min(nearest, final);
                    Vector3 step = seen > 1 ? p - last : Vector3.zero;
                    if (final <= contact + PosTolM && step.magnitude > BodyWalkStepEpsM)
                    {
                        if (lastStep.magnitude > BodyWalkStepEpsM && Vector3.Dot(step, lastStep) < 0f)
                            reversals++;
                        lastStep = step;
                    }
                    last = p;
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
                Object.Destroy(probe.gameObject);
            }

            Debug.Log($"[O-4.9] {_label}: merkeze en yakın {nearest:0.000} m, bitiş {final:0.000} m " +
                      $"(temas {contact:0.000} m = gövde {bodyR:0.00} + oyuncu {PlayerBodyM:0.00}), temasta ters adım {reversals}");
            var fails = new List<string>();
            if (nearest < contact - PosTolM)
                fails.Add($"{_label}: merkeze {nearest:0.000} m yaklaştı < {contact:0.000} m " +
                          $"({(contact - nearest) * 100f:0.0} cm içeride)");
            if (nearest > contact + PosTolM)
                fails.Add($"{_label}: temas mesafesine {nearest - contact:0.000} m kala durdu, görünmez duvar");
            if (reversals > 0)
                fails.Add($"{_label}: temasta {reversals} kez ileri-geri gitti (titreme)");
            Assert.That(fails, Is.Empty, string.Join("\n", fails));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        WalkBodyRenderedProbe StartProbe(Transform body)
        {
            var probe = new GameObject("WalkBodyRenderedProbe").AddComponent<WalkBodyRenderedProbe>();
            probe.Walker = _player;
            probe.Body = body;
            return probe;
        }
    }
}
