using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// DOTween'siz UI juice: punch-scale, nabız, sarsıntı, fade. <c>unscaledDeltaTime</c> ile
    /// akar — hitstop/pause UI'yi dondurmaz. Aynı hedefe yeni tween eskisini değiştirir.
    /// </summary>
    public static class UiJuice
    {
        enum Kind { Punch, Shake, Fade }

        struct Tween
        {
            public Kind Kind;
            public Transform Target;
            public CanvasGroup Group;
            public float Amount;
            public float Duration;
            public float Age;
            public Vector3 BaseScale;
            public Vector3 BasePos;
            public float From;
            public float To;
        }

        static readonly List<Tween> _tweens = new();
        static Runner _runner;

        /// <summary>Ölçeği <paramref name="peak"/>'e zıplatıp geri yayar (0.88 = bas, 1.18 = pop).</summary>
        public static void PunchScale(Transform target, float peak, float durationSec)
        {
            if (target == null || durationSec <= 0f)
                return;
            Vector3 baseScale = RemoveExisting(target, Kind.Punch, out Tween old) ? old.BaseScale : target.localScale;
            Add(new Tween { Kind = Kind.Punch, Target = target, Amount = peak, Duration = durationSec, BaseScale = baseScale });
        }

        /// <summary>Konumu <paramref name="amplitudePx"/> kadar sönen gürültüyle sarsar.</summary>
        public static void Shake(Transform target, float amplitudePx, float durationSec)
        {
            if (target == null || durationSec <= 0f)
                return;
            Vector3 basePos = RemoveExisting(target, Kind.Shake, out Tween old) ? old.BasePos : target.localPosition;
            Add(new Tween { Kind = Kind.Shake, Target = target, Amount = amplitudePx, Duration = durationSec, BasePos = basePos });
        }

        public static void Fade(CanvasGroup group, float to, float durationSec)
        {
            if (group == null)
                return;
            RemoveExisting(group.transform, Kind.Fade, out _);
            if (durationSec <= 0f)
            {
                group.alpha = to;
                return;
            }
            Add(new Tween { Kind = Kind.Fade, Target = group.transform, Group = group, From = group.alpha, To = to, Duration = durationSec });
        }

        /// <summary>0..1 sinüs nabzı (unscaled zaman).</summary>
        public static float Pulse01(float hz) => 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);

        static bool RemoveExisting(Transform target, Kind kind, out Tween old)
        {
            for (int i = 0; i < _tweens.Count; i++)
            {
                if (_tweens[i].Target == target && _tweens[i].Kind == kind)
                {
                    old = _tweens[i];
                    _tweens.RemoveAt(i);
                    return true;
                }
            }
            old = default;
            return false;
        }

        static void Add(Tween t)
        {
            if (_runner == null)
            {
                var go = new GameObject("UiJuice") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
            _tweens.Add(t);
        }

        static void Step(float dt)
        {
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                Tween t = _tweens[i];
                if (t.Target == null)
                {
                    _tweens.RemoveAt(i);
                    continue;
                }
                t.Age += dt;
                float u = Mathf.Clamp01(t.Age / t.Duration);
                switch (t.Kind)
                {
                    case Kind.Punch:
                        // Hızlı çıkış, yumuşak dönüş.
                        float k = u < UiJuiceDefaults.k ? u / UiJuiceDefaults.k : 1f - (u - UiJuiceDefaults.k) / UiJuiceDefaults.PulseFalloffSpan;
                        k = Mathf.SmoothStep(0f, 1f, k);
                        t.Target.localScale = t.BaseScale * Mathf.LerpUnclamped(1f, t.Amount, k);
                        if (u >= 1f)
                            t.Target.localScale = t.BaseScale;
                        break;
                    case Kind.Shake:
                        float a = t.Amount * (1f - u);
                        t.Target.localPosition = t.BasePos + new Vector3(
                            (Mathf.PerlinNoise(t.Age * UiJuiceDefaults.AConst, 0f) - 0.5f) * 2f * a,
                            (Mathf.PerlinNoise(0f, t.Age * UiJuiceDefaults.ShakeNoiseFreqHz) - 0.5f) * 2f * a, 0f);
                        if (u >= 1f)
                            t.Target.localPosition = t.BasePos;
                        break;
                    case Kind.Fade:
                        if (t.Group != null)
                            t.Group.alpha = Mathf.Lerp(t.From, t.To, u);
                        break;
                }
                if (u >= 1f)
                    _tweens.RemoveAt(i);
                else
                    _tweens[i] = t;
            }
        }

        sealed class Runner : MonoBehaviour
        {
            void Update() => Step(Time.unscaledDeltaTime);
        }
    }
}
