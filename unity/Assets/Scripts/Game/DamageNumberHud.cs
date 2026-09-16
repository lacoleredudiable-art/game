using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Floating hasar: dünya→ekran pop, punch + rise + fade. Pool.
    /// </summary>
    public sealed class DamageNumberHud : MonoBehaviour
    {
        const int PoolSize = 12;

        PrototypeTuning _tuning;
        Camera _cam;
        Canvas _canvas;
        readonly List<Floater> _pool = new();
        int _next;

        struct Floater
        {
            public GameObject Go;
            public RectTransform Rect;
            public Text Text;
            public float BornUnscaled;
            public Vector3 World;
            public float JitterX;
            public bool Alive;
            public bool Crit;
            public bool Heal;
        }

        public void Configure(PrototypeTuning tuning, Transform canvasRoot)
        {
            _tuning = tuning;
            _cam = Camera.main;

            var host = new GameObject("DamageNumberHud");
            host.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                host.layer = canvasRoot.gameObject.layer;
            _canvas = canvasRoot != null ? canvasRoot.GetComponentInParent<Canvas>() : null;

            for (int i = 0; i < PoolSize; i++)
                _pool.Add(CreateFloater(host.transform, i));
        }

        Floater CreateFloater(Transform parent, int i)
        {
            var go = new GameObject("Float_" + i);
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 48f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return new Floater { Go = go, Rect = rect, Text = text };
        }

        /// <summary>Negatif amount = heal.</summary>
        public void ShowDamage(float amount, bool isCrit = false)
        {
            if (_tuning == null || !_tuning.ShowDamageNumbers)
                return;

            Vector3 world = _cam != null
                ? _cam.transform.position + _cam.transform.forward * 6f
                : Vector3.zero;
            // Boss üstü — FollowCamera hedefi yoksa ekran ortası-üstü.
            var boss = Object.FindAnyObjectByType<BossReactor>();
            if (boss != null)
                world = boss.transform.position + Vector3.up * 2.2f;

            ShowAt(world, amount, isCrit);
        }

        public void ShowAt(Vector3 worldPos, float amount, bool isCrit = false)
        {
            if (_tuning == null || !_tuning.ShowDamageNumbers)
                return;

            Floater f = _pool[_next];
            int idx = _next;
            _next = (_next + 1) % PoolSize;

            bool heal = amount < 0f;
            f.Alive = true;
            f.BornUnscaled = Time.unscaledTime;
            f.World = worldPos;
            f.JitterX = Random.Range(-36f, 36f);
            f.Crit = isCrit && !heal;
            f.Heal = heal;
            f.Go.SetActive(true);

            if (heal)
            {
                f.Text.text = "+" + (-amount).ToString("0.#");
                f.Text.color = new Color(0.45f, 0.9f, 0.75f, 1f);
                f.Text.fontSize = Mathf.RoundToInt(PentagonLayoutScreen.DpToPixels(_tuning.DamageFloatFontDp));
            }
            else if (isCrit)
            {
                f.Text.text = Mathf.RoundToInt(amount).ToString();
                f.Text.color = new Color(0.95f, 0.88f, 0.55f, 1f);
                f.Text.fontSize = Mathf.RoundToInt(PentagonLayoutScreen.DpToPixels(_tuning.DamageFloatCritFontDp));
            }
            else
            {
                f.Text.text = Mathf.RoundToInt(amount).ToString();
                f.Text.color = new Color(0.95f, 0.93f, 0.88f, 1f);
                f.Text.fontSize = Mathf.RoundToInt(PentagonLayoutScreen.DpToPixels(_tuning.DamageFloatFontDp));
            }

            _pool[idx] = f;
        }

        void LateUpdate()
        {
            if (_tuning == null)
                return;
            if (_cam == null)
                _cam = Camera.main;

            float hold = _tuning.DamageFloatHoldSec;
            float fade = _tuning.DamageFloatFadeSec;
            float rise = _tuning.DamageFloatRisePx;
            float punch = _tuning.DamageFloatPunchScale;

            for (int i = 0; i < _pool.Count; i++)
            {
                Floater f = _pool[i];
                if (!f.Alive)
                    continue;

                float age = Time.unscaledTime - f.BornUnscaled;
                float life = hold + fade;
                if (age >= life)
                {
                    f.Alive = false;
                    f.Go.SetActive(false);
                    _pool[i] = f;
                    continue;
                }

                float t = age / life;
                float riseY = rise * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / life));
                float scale = age < 0.12f
                    ? Mathf.Lerp(punch, 1f, age / 0.12f)
                    : 1f;
                float alpha = age < hold ? 1f : 1f - Mathf.Clamp01((age - hold) / Mathf.Max(0.01f, fade));

                Vector3 screen = _cam != null
                    ? _cam.WorldToScreenPoint(f.World)
                    : new Vector3(Screen.width * 0.5f, Screen.height * 0.7f, 1f);

                if (screen.z < 0f)
                {
                    f.Go.SetActive(false);
                    continue;
                }

                f.Rect.position = new Vector3(screen.x + f.JitterX, screen.y + riseY, 0f);
                f.Rect.localScale = Vector3.one * scale;
                Color c = f.Text.color;
                c.a = alpha;
                f.Text.color = c;
                f.Go.SetActive(true);
                _pool[i] = f;
            }
        }
    }
}
