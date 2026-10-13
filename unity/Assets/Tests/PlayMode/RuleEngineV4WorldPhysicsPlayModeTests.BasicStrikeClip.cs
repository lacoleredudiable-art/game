using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dovus.Core.Grammar;
using Dovus.Game.Casting;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// O-4.14 (envanter B1): dilimde merkeze gerçek dokunuşla düz vuruş. Telefondaki gibi art arda dokunulur;
    /// her karede oynayan klip ve Animator state'i kaydedilir. Kılıç yalnız Kilic_VUR, Yay yalnız Yay_VUR oynar;
    /// eski yumruk yolu (BasicStrike / CastSlam state'leri, k3'te Cekic_VUR) hiç oynamaz; [V4-FALLBACK] yazılmaz.
    /// Hedef hem vuruş menzilinde hem uzakta (eski motorun düz vuruş adımı devreye girer) denenir.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string FallbackTag = "[V4-FALLBACK]";
        /// <summary>Telefon logcat'inde (12 Ekim, a3) art arda düz vuruşlar ~0,19 s arayla geliyor.</summary>
        const float PhoneTapIntervalSec = 0.2f;
        const int PhoneTapCount = 10;
        /// <summary>Son dokunuştan sonra kapanış (vuruş anı) ve klip sonu da izlenir.</summary>
        const float TapTailWatchSec = 1.2f;
        const string LocoClipPrefix = "Loko_";
        /// <summary>Kılıç düz vuruşu kesiştir: iz penceresinde uç gövde etrafında en az bu kadar yatay yay çizer.</summary>
        const float BasicStrikeSweepMinDeg = 90f;
        /// <summary>Hedef uzaklıkları: vuruş menzili içinde ve dışında.</summary>
        static readonly float[] BasicStrikeTargetDistancesM = { 2f, 8f };
        /// <summary>Eski yumruk yolu: PulseBasicStrike state döngüsü ve CastSlam (k3'te Cekic_VUR takma adı).</summary>
        static readonly string[] PunchStates = { "BasicStrike", "BasicStrikeB", "BasicStrikeC", "CastSlam", "UpperBasicStrike", "UpperCastSlam" };
        static readonly string[] BasicStrikeTimelineStates =
            { SwordStrikeClip, BowStrikeClip, HammerStrikeClip, "BasicStrike", "CastSlam", "CastSweep", "CastShoot", "CastPierce", "Locomotion" };

        sealed class BasicStrikeRun
        {
            public string Label;
            public readonly HashSet<string> ActionClips = new();
            public readonly HashSet<string> PunchStatesSeen = new();
            public readonly List<string> BridgeClips = new();
            public readonly List<string> Fallbacks = new();
            public readonly StringBuilder Timeline = new();
            public int Taps;
            public int Strikes;
        }

        [UnityTest]
        public IEnumerator O_4_14_KilicDuzVurus_KilicVUROynar()
        {
            foreach (float targetM in BasicStrikeTargetDistancesM)
            {
                BasicStrikeRun run = null;
                yield return RunPhoneBasicStrikes(Kilic, targetM, r => run = r);
                AssertWeaponStrikeClip(run, SwordStrikeClip);
            }
        }

        [UnityTest]
        public IEnumerator O_4_14_YayDuzVurus_YayVUROynar()
        {
            foreach (float targetM in BasicStrikeTargetDistancesM)
            {
                BasicStrikeRun run = null;
                yield return RunPhoneBasicStrikes(Yay, targetM, r => run = r);
                AssertWeaponStrikeClip(run, BowStrikeClip);
            }
        }

        [UnityTest]
        public IEnumerator O_4_14_DuzVurus_V4Fallback0()
        {
            foreach (int weapon in new[] { Kilic, Yay })
            foreach (float targetM in BasicStrikeTargetDistancesM)
            {
                BasicStrikeRun run = null;
                yield return RunPhoneBasicStrikes(weapon, targetM, r => run = r);
                Assert.That(run.Fallbacks, Is.Empty,
                    $"{run.Label}: {run.Fallbacks.Count} [V4-FALLBACK]:\n" + string.Join("\n", run.Fallbacks.Distinct()));
            }
        }

        [UnityTest]
        public IEnumerator O_4_14_DilimdeYumrukKlibiHicOynamaz()
        {
            foreach (int weapon in new[] { Kilic, Yay })
            foreach (float targetM in BasicStrikeTargetDistancesM)
            {
                BasicStrikeRun run = null;
                yield return RunPhoneBasicStrikes(weapon, targetM, r => run = r);
                Assert.That(run.PunchStatesSeen, Is.Empty,
                    $"{run.Label}: eski yumruk state'i oynadı ({string.Join(",", run.PunchStatesSeen)})\n{run.Timeline}");
                Assert.That(run.ActionClips, Does.Not.Contain(HammerStrikeClip),
                    $"{run.Label}: yumruk klibi ({HammerStrikeClip}) oynadı\n{run.Timeline}");
            }
        }

        /// <summary>
        /// Kesiş: tek dokunuşla Kilic_VUR'un iz penceresinde (klibin Trail_On → Trail_Off olayları) kılıç ucu gövde
        /// etrafında yatayda en az <see cref="BasicStrikeSweepMinDeg"/> net yay çizer; Impact karesinde uç gövdenin önündedir.
        /// Uca ileri uzanıp aynı yoldan geri dönen klip (yumruk görünümü) net yayda kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator O_4_14_KilicDuzVurus_KesisSupurur()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * BasicStrikeTargetDistancesM[0]);
            Physics.SyncTransforms();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            Animator anim = PlayerAnimator();
            var events = anim.GetComponent<Dovus.Game.Vfx.SkillAnimVfxEventHost>();
            Assert.That(events, Is.Not.Null, "oyuncuda SkillAnimVfxEventHost yok");
            AnimationClip clip = anim.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == SwordStrikeClip);
            Assert.That(clip, Is.Not.Null, $"{SwordStrikeClip} controller'da yok");
            float trailOn = clip.events.Where(e => e.functionName == "Trail_On").Select(e => e.time).DefaultIfEmpty(-1f).First();
            float trailOff = clip.events.Where(e => e.functionName == "Trail_Off").Select(e => e.time).DefaultIfEmpty(-1f).First();
            Assert.That(trailOn, Is.GreaterThanOrEqualTo(0f).And.LessThan(trailOff), $"{SwordStrikeClip}: iz olayları yok ya da ters");

            int impactBefore = events.ImpactEvents;
            input.Pointer.BeginPointer(CenterPos(input));
            input.Pointer.EndPointer(cancelled: false);
            Assert.That(RuleEngineV4SkillAnimBridge.LastBasicStrikeClip, Is.EqualTo(SwordStrikeClip), "düz vuruş Kilic_VUR oynatmadı");

            var log = new StringBuilder();
            float? firstDeg = null, lastDeg = null, prevRaw = null, unwrapped = 0f, impactFwdM = null;
            float end = Time.realtimeSinceStartup + TapTailWatchSec;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (!Dovus.Game.Weapons.WeaponBladePoints.TryActiveBladePoints(anim.transform, out _, out Transform tip))
                    continue;
                AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
                Vector3 fwd = Flat(anim.transform.forward).normalized;
                Vector3 rel = Flat(tip.position - anim.transform.position);
                if (events.ImpactEvents > impactBefore && impactFwdM == null)
                    impactFwdM = Vector3.Dot(rel, fwd);
                if (!st.IsName(SwordStrikeClip))
                    continue;
                float t = st.normalizedTime * clip.length;
                if (t < trailOn || t > trailOff)
                    continue;
                float raw = Vector3.SignedAngle(fwd, rel, Vector3.up);
                if (prevRaw != null)
                    unwrapped += Mathf.DeltaAngle(prevRaw.Value, raw);
                prevRaw = raw;
                firstDeg ??= unwrapped;
                lastDeg = unwrapped;
                if (log.Length < TimelineMaxChars)
                    log.Append($"[t={t:0.000} uç açısı={raw:0} net={unwrapped:0}] ");
            }
            float sweep = firstDeg == null ? 0f : Mathf.Abs(lastDeg.Value - firstDeg.Value);
            Debug.Log($"[O-4.14] Kilic_VUR kesiş: iz penceresi {trailOn:0.000}-{trailOff:0.000} s net yay={sweep:0.0}° " +
                      $"Impact'te uç ileri={impactFwdM?.ToString("0.00") ?? "-"} m\n{log}");
            Assert.That(firstDeg, Is.Not.Null, $"iz penceresinde örnek alınamadı\n{log}");
            Assert.That(sweep, Is.GreaterThanOrEqualTo(BasicStrikeSweepMinDeg), $"kılıç ucu yatayda {sweep:0.0}° yay çizdi (kesiş değil)\n{log}");
            Assert.That(impactFwdM, Is.Not.Null, "Impact olayı gelmedi");
            Assert.That(impactFwdM.Value, Is.GreaterThan(0f), $"Impact'te kılıç ucu gövdenin arkasında ({impactFwdM:0.00} m)");
            yield return WaitSentenceIdle(input);
        }

        static void AssertWeaponStrikeClip(BasicStrikeRun run, string clip)
        {
            Assert.That(run.Strikes, Is.EqualTo(run.Taps), $"{run.Label}: {run.Taps} dokunuşun {run.Strikes}'i düz vuruş oldu");
            Assert.That(run.BridgeClips, Is.All.EqualTo(clip),
                $"{run.Label}: dokunuş başına oynayan klip [{string.Join(",", run.BridgeClips)}]\n{string.Join("\n", run.Fallbacks.Distinct())}");
            Assert.That(run.ActionClips, Is.EquivalentTo(new[] { clip }),
                $"{run.Label}: animatörde oynayan vuruş klipleri {string.Join(",", run.ActionClips)}\n{run.Timeline}");
        }

        /// <summary>
        /// Gerçek yol: PointerRouter merkeze bas/bırak → SentenceEngine → düz vuruş köprüsü. Dokunuş başına köprünün
        /// oynattığı klip, her karede ağırlıklı klipler ve Animator state'i, LogError'daki [V4-FALLBACK] satırları.
        /// </summary>
        IEnumerator RunPhoneBasicStrikes(int weapon, float targetM, System.Action<BasicStrikeRun> done)
        {
            FreezeBoss();
            Equip(weapon);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * targetM);
            Physics.SyncTransforms();
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            Animator anim = PlayerAnimator();
            var run = new BasicStrikeRun { Label = $"w{weapon} hedef {targetM:0} m" };
            int errorsBefore = _errors.Count;
            int basicDot = input.Tuning.Input.BasicStrikeDot;
            void OnCompleted(CompletedSentence s)
            {
                if (s.Words.Count == 1 && (int)s.Words[0].Rune == basicDot)
                    run.Strikes++;
            }
            input.Engine.SentenceCompleted += OnCompleted;
            float end = float.MaxValue;
            float nextTap = Time.realtimeSinceStartup;
            int frame = 0;
            try
            {
                while (Time.realtimeSinceStartup < end)
                {
                    if (run.Taps < PhoneTapCount && Time.realtimeSinceStartup >= nextTap)
                    {
                        int strikesBefore = run.Strikes;
                        input.Pointer.BeginPointer(CenterPos(input));
                        input.Pointer.EndPointer(cancelled: false);
                        run.Taps++;
                        string bridgeClip = RuleEngineV4SkillAnimBridge.LastBasicStrikeClip;
                        run.BridgeClips.Add(run.Strikes == strikesBefore ? "(düz vuruş yok)"
                            : string.IsNullOrEmpty(bridgeClip) ? "-" : bridgeClip);
                        nextTap += PhoneTapIntervalSec;
                        if (run.Taps == PhoneTapCount)
                            end = Time.realtimeSinceStartup + TapTailWatchSec;
                    }
                    yield return null;
                    frame++;
                    SampleBasicStrikeFrame(anim, run, frame);
                }
            }
            finally
            {
                input.Engine.SentenceCompleted -= OnCompleted;
            }
            for (int i = errorsBefore; i < _errors.Count; i++)
            {
                if (_errors[i].Contains(FallbackTag))
                    run.Fallbacks.Add(_errors[i]);
            }
            Debug.Log($"[O-4.14] {run.Label}: dokunuş={run.Taps} düz vuruş={run.Strikes} köprü klipleri=[{string.Join(",", run.BridgeClips)}] " +
                      $"vuruş klipleri=[{string.Join(",", run.ActionClips)}] yumruk state'leri=[{string.Join(",", run.PunchStatesSeen)}] " +
                      $"[V4-FALLBACK]={run.Fallbacks.Count}\n{string.Join("\n", run.Fallbacks.Distinct())}\n{run.Timeline}");
            done(run);
            yield return WaitSentenceIdle(input);
        }

        static void SampleBasicStrikeFrame(Animator anim, BasicStrikeRun run, int frame)
        {
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(layer);
                foreach (string s in PunchStates)
                {
                    if (st.IsName(s) || (anim.IsInTransition(layer) && anim.GetNextAnimatorStateInfo(layer).IsName(s)))
                        run.PunchStatesSeen.Add(s);
                }
            }
            var clips = new List<string>();
            foreach (AnimatorClipInfo ci in anim.GetCurrentAnimatorClipInfo(0))
            {
                if (ci.weight <= ClipActiveWeight)
                    continue;
                clips.Add(ci.clip.name);
                if (!ci.clip.name.StartsWith(LocoClipPrefix, System.StringComparison.Ordinal))
                    run.ActionClips.Add(ci.clip.name);
            }
            if (run.Timeline.Length < TimelineMaxChars)
            {
                AnimatorStateInfo st0 = anim.GetCurrentAnimatorStateInfo(0);
                string stName = BasicStrikeTimelineStates.FirstOrDefault(st0.IsName) ?? st0.shortNameHash.ToString();
                run.Timeline.Append($"[{frame} <{stName}> {string.Join("+", clips)} n={st0.normalizedTime:0.00}] ");
            }
        }
    }
}
