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
using Dovus.Game.Diagnostics;
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
        FollowCameraController _follow;
        BossReactorController _defaultBoss;

        public void BindTheme(HudTheme theme) => _theme = theme;
        Canvas _canvas;
        readonly List<Floater> _pool = new();
        int _next;

        struct Floater
        {
            public GameObject Go;
            public RectTransform Rect;
            public Text Text;
            public Outline Outline;
            public Shadow Shadow;
            public float BornUnscaled;
            public Vector3 World;
            public float JitterX;
            public bool Alive;
            public bool Crit;
            public bool Heal;
        }

        public void BindMainCamera(Camera camera, FollowCameraController follow = null)
        {
            _cam = camera;
            _follow = follow;
        }

        public void BindDefaultBoss(BossReactorController boss) => _defaultBoss = boss;

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
            text.fontStyle = FontStyle.Bold;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(DamageNumberHudDefaults.OutlineEffectDistancePx, -DamageNumberHudDefaults.OutlineEffectDistancePx);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(DamageNumberHudDefaults.ShadowEffectDistancePx, -DamageNumberHudDefaults.ShadowEffectDistancePx);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return new Floater { Go = go, Rect = rect, Text = text, Outline = outline, Shadow = shadow };
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
                ? _cam.transform.position + _cam.transform.forward * DamageNumberHudDefaults.FallbackCamForwardM
                : Vector3.zero;
            if (worldPos.HasValue)
            {
                world = worldPos.Value;
            }
            else
            {
                if (_defaultBoss != null)
                {
                    if (BossDamageAnchor.TryGetHeadTopWorldY(_defaultBoss, out float headY))
                        world = new Vector3(_defaultBoss.transform.position.x, headY + DamageNumberHudDefaults.BossHeadTopExtraOffsetM, _defaultBoss.transform.position.z);
                    else
                        world = _defaultBoss.transform.position + Vector3.up * DamageNumberHudDefaults.FallbackBossHeightOffsetM;
                }
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
                f.Text.color = isCrit ? th.BossDamageCritColor : th.BossDamageColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(
                    isCrit ? _tuning.Hud.DamageFloatCritFontDp : _tuning.Hud.DamageFloatFontDp));
                ApplyChrome(f, th.BossDamageOutlineColor, th.BossDamageOutlineColor);
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
                f.Text.text = "-" + DamageNumberFormat.Format(amount);
                f.Text.color = th.PlayerHitColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(
                    isCrit ? _tuning.Hud.DamageFloatCritFontDp : _tuning.Hud.DamageFloatFontDp));
                ApplyChrome(f, th.PlayerHitOutlineColor, th.PlayerHitOutlineColor);
            }
            else
            {
                f.Text.text = DamageNumberFormat.Format(amount);
                // Element rengi beyaza doğru açılır: koyu element tonları da okunur kalsın.
                f.Text.color = tint.HasValue ? Color.Lerp(tint.Value, th.DamageTextColor, DamageNumberHudDefaults.DamageTextColorLerp) : th.DamageTextColor;
                f.Text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(_tuning.Hud.DamageFloatFontDp));
                ApplyChrome(f, new Color(0f, 0f, 0f, 0.82f), new Color(0f, 0f, 0f, 0.72f));
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

                float riseT = Mathf.Clamp01(age / life);
                float riseY = rise * (1f - Mathf.Pow(1f - riseT, DamageNumberHudDefaults.RiseEaseOutPower));
                float scale = age < DamageNumberHudDefaults.PunchDurationSec
                    ? PunchScale(age, punch)
                    : 1f;
                float alpha = age < hold ? 1f : 1f - Mathf.Clamp01((age - hold) / Mathf.Max(DamageNumberHudDefaults.FadeDurationMinSec, fade));

                Vector3 screen = _cam != null
                    ? _cam.WorldToScreenPoint(f.World)
                    : new Vector3(Screen.width * 0.5f, Screen.height * DamageNumberHudDefaults.FallbackScreenHeightFrac, 1f);

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

        static void ApplyChrome(Floater f, Color outlineColor, Color shadowColor)
        {
            if (f.Outline != null)
                f.Outline.effectColor = outlineColor;
            if (f.Shadow != null)
                f.Shadow.effectColor = shadowColor;
        }

        static float PunchScale(float ageSec, float punchPeak)
        {
            float dur = DamageNumberHudDefaults.PunchDurationSec;
            if (dur <= 0f)
                return 1f;
            float u = Mathf.Clamp01(ageSec / dur);
            float peak = punchPeak * DamageNumberHudDefaults.PunchPeakMultiplier;
            const float snapFrac = 0.32f;
            if (u < snapFrac)
                return Mathf.Lerp(DamageNumberHudDefaults.PunchBirthScale, peak, u / snapFrac);
            return Mathf.LerpUnclamped(peak, 1f, EaseOutBack((u - snapFrac) / (1f - snapFrac)));
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
