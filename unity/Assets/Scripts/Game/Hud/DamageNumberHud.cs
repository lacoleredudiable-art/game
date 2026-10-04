using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// Floating hasar: dünya→ekran pop, punch + rise + fade. Pool.
    /// </summary>
    public sealed class DamageNumberHud : MonoBehaviour
    {
        const int PoolSize = 24;

        GameTuning _tuning;
        HudTheme _theme;
        Camera _cam;
        FollowCamera _follow;
        BossReactor _defaultBoss;

        public void BindTheme(HudTheme theme) => _theme = theme;
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

        public void BindMainCamera(Camera camera, FollowCamera follow = null)
        {
            _cam = camera;
            _follow = follow;
        }

        public void BindDefaultBoss(BossReactor boss) => _defaultBoss = boss;

        public void Configure(GameTuning tuning, Transform canvasRoot)
        {
            _tuning = tuning;

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
            text.font = HudTheme.LegacyFont;
            text.fontStyle = FontStyle.Normal;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(DamageNumberHudDefaults.effectDistance, -DamageNumberHudDefaults.effectDistance);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return new Floater { Go = go, Rect = rect, Text = text };
        }

        /// <summary>
        /// Negatif amount = heal. <paramref name="worldPos"/> isabet noktası (yoksa boss üstü);
        /// <paramref name="tint"/> element rengi (kritik altın, heal yeşil kalır).
        /// </summary>
        public void ShowDamage(float amount, bool isCrit = false, Vector3? worldPos = null, Color? tint = null, bool victimIsPlayer = false, bool victimIsBoss = false)
        {
            if (_tuning == null || !_tuning.Hud.ShowDamageNumbers)
                return;

            Vector3 world = _cam != null
                ? _cam.transform.position + _cam.transform.forward * DamageNumberHudDefaults.ForwardConst
                : Vector3.zero;
            if (worldPos.HasValue)
            {
                world = worldPos.Value;
            }
            else
            {
                if (_defaultBoss != null)
                    world = _defaultBoss.transform.position + Vector3.up * DamageNumberHudDefaults.world;
            }

            ShowAt(world, amount, isCrit, tint, victimIsPlayer, victimIsBoss);
        }

        public void ShowAt(Vector3 worldPos, float amount, bool isCrit = false, Color? tint = null, bool victimIsPlayer = false, bool victimIsBoss = false)
        {
            if (_tuning == null || !_tuning.Hud.ShowDamageNumbers)
                return;

            Floater f = _pool[_next];
            int idx = _next;
            _next = (_next + 1) % PoolSize;

            bool heal = amount < 0f;
            f.Alive = true;
            f.BornUnscaled = Time.unscaledTime;
            f.World = worldPos;
            f.JitterX = Random.Range(-DamageNumberHudDefaults.JitterX, DamageNumberHudDefaults.JitterX);
            f.Crit = isCrit && !heal;
            f.Heal = heal;
            f.Go.SetActive(true);

            HudTheme th = _theme;
            if (heal)
            {
                f.Text.text = "+" + DamageNumberFormat.Format(-amount);
                f.Text.color = th.HealColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(_tuning.Hud.DamageFloatFontDp));
            }
            else if (victimIsBoss)
            {
                f.Text.text = isCrit ? DamageNumberFormat.Format(amount) + "!" : DamageNumberFormat.Format(amount);
                f.Text.color = isCrit
                    ? new Color(1f, 0.42f, 0.38f, 1f)
                    : new Color(0.88f, 0.14f, 0.16f, 1f);
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(
                    isCrit ? _tuning.Hud.DamageFloatCritFontDp : _tuning.Hud.DamageFloatFontDp));
                var outline = f.Text.GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = new Color(0.04f, 0.02f, 0.02f, 0.9f);
                DebugConfig.DevLog($"[Feel2Verify] boss-damage-number crit={isCrit} color={f.Text.color}");
            }
            else if (isCrit)
            {
                f.Text.text = DamageNumberFormat.Format(amount) + "!";
                f.Text.color = th.CritTextColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(_tuning.Hud.DamageFloatCritFontDp));
            }
            else if (victimIsPlayer)
            {
                f.Text.text = DamageNumberFormat.Format(amount);
                f.Text.color = th.PlayerHitColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(_tuning.Hud.DamageFloatFontDp));
            }
            else
            {
                f.Text.text = DamageNumberFormat.Format(amount);
                // Element rengi beyaza doğru açılır: koyu element tonları da okunur kalsın.
                f.Text.color = tint.HasValue ? Color.Lerp(tint.Value, th.DamageTextColor, DamageNumberHudDefaults.color) : th.DamageTextColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(_tuning.Hud.DamageFloatFontDp));
            }

            _pool[idx] = f;
        }

        void LateUpdate()
        {
            if (_tuning == null)
                return;
            ResolveMainCamera();

            float hold = _tuning.Hud.DamageFloatHoldSec;
            float fade = _tuning.Hud.DamageFloatFadeSec;
            float rise = _tuning.Hud.DamageFloatRisePx;
            float punch = _tuning.Hud.DamageFloatPunchScale;

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
                float scale = age < DamageNumberHudDefaults.scale
                    ? Mathf.Lerp(punch, 1f, age / DamageNumberHudDefaults.AgeConst)
                    : 1f;
                float alpha = age < hold ? 1f : 1f - Mathf.Clamp01((age - hold) / Mathf.Max(DamageNumberHudDefaults.alpha, fade));

                Vector3 screen = _cam != null
                    ? _cam.WorldToScreenPoint(f.World)
                    : new Vector3(Screen.width * 0.5f, Screen.height * DamageNumberHudDefaults.HeightConst, 1f);

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

        void ResolveMainCamera()
        {
            if (_cam != null)
                return;
            if (_follow != null)
                _cam = _follow.ViewCamera;
        }
    }
}
