using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// O-2.19 / O-3.7 (envanter B2): dilimde silah takas düğmesine gerçek dokunuşla Kılıç → Yay → Kılıç.
    /// Her silahta takas bittikten sonra 1 s içinde oynayan bekleme klibi, sonra çubukla yürürken 1 s içinde
    /// oynayan koşu klibi o silahın <c>Loko_&lt;Silah&gt;_*</c> setinden gelir; başka silahın seti hiç oynamaz;
    /// [V4-FALLBACK] yazılmaz.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Envanter B2 ölçütü: takastan sonra doğru klip bu süre içinde oynar.</summary>
        const float WeaponLocoWindowSec = 1f;
        /// <summary>Takas düğmesi soğumadaysa oyuncu gibi yeniden basılır; soğuma + takas süresine eklenen pay.</summary>
        const float WeaponSwapSlackSec = 2f;
        const float WeaponSwapRetrySec = 0.25f;
        static readonly (int Weapon, string ClipSet)[] WeaponLocoSteps =
        {
            (Kilic, "Kilic"),
            (Yay, "Yay"),
            (Kilic, "Kilic"),
        };
        static readonly string[] WeaponLocoSets = { "Kilic", "Yay" };

        sealed class WeaponLocoStep
        {
            public string Label;
            public string ClipSet;
            public bool Swapped;
            public string IdleClip;
            public string RunClip;
            public readonly HashSet<string> Wrong = new();
            public readonly StringBuilder Timeline = new();
        }

        sealed class WeaponLocoRun
        {
            public readonly List<WeaponLocoStep> Steps = new();
            public readonly List<string> Fallbacks = new();
        }

        [UnityTest]
        public IEnumerator O_2_19_SilahDegisimi_BeklemeVeYurumeKlibiSilahin()
        {
            WeaponLocoRun run = null;
            yield return RunWeaponSwapLocomotion(r => run = r);
            foreach (WeaponLocoStep step in run.Steps)
            {
                string own = LocoClip(step.ClipSet, "Idle");
                Assert.That(step.Swapped, Is.True, $"{step.Label}: takas düğmesi silahı değiştirmedi\n{step.Timeline}");
                Assert.That(step.IdleClip, Is.EqualTo(own),
                    $"{step.Label}: {WeaponLocoWindowSec} s içinde bekleme klibi {own} değil\n{step.Timeline}");
                Assert.That(step.RunClip, Is.EqualTo(LocoClip(step.ClipSet, "Run")),
                    $"{step.Label}: yürürken {WeaponLocoWindowSec} s içinde koşu klibi silahın değil\n{step.Timeline}");
                Assert.That(step.Wrong, Is.Empty,
                    $"{step.Label}: başka silahın yürüme klibi oynadı ({string.Join(",", step.Wrong)})\n{step.Timeline}");
            }
        }

        [UnityTest]
        public IEnumerator O_2_19_SilahDegisimi_V4Fallback0()
        {
            WeaponLocoRun run = null;
            yield return RunWeaponSwapLocomotion(r => run = r);
            Assert.That(run.Fallbacks, Is.Empty,
                $"{run.Fallbacks.Count} [V4-FALLBACK]:\n" + string.Join("\n", run.Fallbacks.Distinct()));
        }

        static string LocoClip(string set, string kind) => $"{LocoClipPrefix}{set}_{kind}";

        /// <summary>
        /// Gerçek yol: ilk adım yükleme (Kılıç birincil, Yay yedek), sonraki adımlar takas düğmesine PointerRouter
        /// dokunuşu → ManifestationDirector takası → ActorView.SetWeapon. Bekleme ve yürüyüş (MoveInputController
        /// çubuğu) sırasında her karede Animator'ın 0. katmanında ağırlığı yüksek klipler okunur.
        /// </summary>
        IEnumerator RunWeaponSwapLocomotion(System.Action<WeaponLocoRun> done)
        {
            FreezeBoss();
            Arrange(Vector3.right);
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            var move = _player.GetComponent<MoveInputController>();
            Assert.That(move, Is.Not.Null, "oyuncuda MoveInputController yok");
            SetSmokeLoadout(Kilic, Yay);
            Assert.That(_md.WeaponSwap, Is.Not.Null, "dilimde silah takası kurulmadı");
            float swapWaitSec = _md.WeaponSwap.Rules.CooldownSec + _md.WeaponSwap.Rules.AnimationSec + WeaponSwapSlackSec;
            Vector2 swapPx = HexagonLayoutScreen.WeaponSwapButtonPx(input.Tuning, Screen.width, Screen.height);
            Animator anim = PlayerAnimator();
            var run = new WeaponLocoRun();
            int errorsBefore = _errors.Count;
            try
            {
                for (int i = 0; i < WeaponLocoSteps.Length; i++)
                {
                    (int weapon, string set) = WeaponLocoSteps[i];
                    var step = new WeaponLocoStep { Label = $"adım {i + 1} → w{weapon}", ClipSet = set, Swapped = i == 0 };
                    run.Steps.Add(step);
                    if (i > 0)
                    {
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
                        step.Swapped = _md.EquippedWeaponNumber() == weapon;
                        if (!step.Swapped)
                            continue;
                    }
                    yield return SampleLoco(anim, step, "Idle", null, c => step.IdleClip ??= c);
                    Vector2 stick = i % 2 == 0 ? Vector2.left : Vector2.right;
                    yield return SampleLoco(anim, step, "Run", move, c => step.RunClip ??= c, stick);
                }
            }
            finally
            {
                move.SetScriptedDirection(null);
            }
            for (int i = errorsBefore; i < _errors.Count; i++)
            {
                if (_errors[i].Contains(FallbackTag))
                    run.Fallbacks.Add(_errors[i]);
            }
            Debug.Log("[O-2.19] silah takası yürüme klipleri: " + string.Join(" | ", run.Steps.Select(s =>
                $"{s.Label} takas={s.Swapped} bekleme={s.IdleClip ?? "-"} koşu={s.RunClip ?? "-"} yanlış=[{string.Join(",", s.Wrong)}]"))
                + $" [V4-FALLBACK]={run.Fallbacks.Count}\n" + string.Join("\n", run.Steps.Select(s => s.Timeline.ToString())));
            done(run);
        }

        /// <summary>
        /// <see cref="WeaponLocoWindowSec"/> boyunca her karede 0. katmanda ağırlığı yüksek <c>Loko_*</c> klipleri;
        /// istenen türün (Idle/Run) ilk görüleni <paramref name="seen"/>'e, başka silahın seti Wrong'a yazılır.
        /// </summary>
        IEnumerator SampleLoco(Animator anim, WeaponLocoStep step, string kind, MoveInputController move,
            System.Action<string> seen, Vector2 stick = default)
        {
            move?.SetScriptedDirection(stick);
            float end = Time.realtimeSinceStartup + WeaponLocoWindowSec;
            int frame = 0;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                frame++;
                var clips = new List<string>();
                foreach (AnimatorClipInfo ci in anim.GetCurrentAnimatorClipInfo(0))
                {
                    if (ci.weight <= ClipActiveWeight || !ci.clip.name.StartsWith(LocoClipPrefix, System.StringComparison.Ordinal))
                        continue;
                    clips.Add(ci.clip.name);
                    foreach (string other in WeaponLocoSets)
                    {
                        if (other != step.ClipSet && ci.clip.name.StartsWith(LocoClipPrefix + other + "_", System.StringComparison.Ordinal))
                            step.Wrong.Add(ci.clip.name);
                    }
                    if (ci.clip.name.EndsWith("_" + kind, System.StringComparison.Ordinal))
                        seen(ci.clip.name);
                }
                if (step.Timeline.Length < TimelineMaxChars)
                    step.Timeline.Append($"[{kind} {frame} {string.Join("+", clips)}] ");
            }
            move?.SetScriptedDirection(null);
        }
    }
}
