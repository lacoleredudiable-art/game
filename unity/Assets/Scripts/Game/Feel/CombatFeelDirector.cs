using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Cameras;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Vfx;
using Dovus.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Feel
{
    /// <summary>
    /// Sıyırma/vurulma hissi: hitstop, impact frame, vinyet, kamera yumruğu.
    /// Ekran katmanı Overlay değil — Overlay kamera üzerinde Screen Space Camera (§10).
    ///
    /// T8.1: kullanılmayan tam ekran katman KAPALI tutulur (alfa 0 bir Image yine de geometri
    /// üretip harmanlanır — mobilde üç kat overdraw). Vinyet artık düz dolgu değil kenardan
    /// içeri sönen bir maske: §10'un "telegraf en okunabilir katman" kuralı için ekranın
    /// ortası açık kalmak zorunda. Renkler `GameTuning`'den — ikinci kopya yok.
    /// </summary>
    public sealed class CombatFeelDirector : MonoBehaviour
    {
        const float ThreatHoldSec = 0.05f;

        CombatTuning _combat;
        GameTuning _colors;
        GameClockHost _clock;
        FollowCameraController _follow;
        SentenceDebugHud _hud;
        ReactionReadoutHud _readout;

        Canvas _canvas;
        Image _impact;
        Image _vignette;
        Image _threatFlash;
        float _impactUntil;
        float _vignetteUntil;
        float _threatUntil;

        HitFlashView _playerFlash;
        HitFlashView _bossFlash;
        VisualFreezeView _visualFreeze;
        AfterimageTrailView _afterimage;
        Transform _playerTransform;
        float _lastBossHitstopUnscaled = -CombatFeelDirectorDefaults.BossHitstopSentinelSec;

        public ExchangeResult? LastExchange { get; private set; }

        /// <summary>Boss vuruşu çözüldü (dodge / isabet / güvenli) — ses sunumu dinler.</summary>
        public event System.Action<ExchangeResult> Exchanged;

        public void BindActors(HitFlashView playerFlash, HitFlashView bossFlash)
        {
            _playerFlash = playerFlash;
            _bossFlash = bossFlash;
        }

        public void BindPresentation(VisualFreezeView visualFreeze, AfterimageTrailView afterimage, Transform playerTransform)
        {
            _visualFreeze = visualFreeze;
            _afterimage = afterimage;
            _playerTransform = playerTransform;
        }

        /// <summary>
        /// Oyuncu vuruşu bossa değdi: görsel hitstop + sarsıntı + kırmızı gövde parlaması.
        /// Art arda isabetler <see cref="FeelTuning.BossHitHitstopMinGapMs"/> içinde hitstop yığmaz.
        /// </summary>
        public void OnBossStruck(bool isCrit, bool allowHitstop = true, string weaponArchetype = null)
        {
            FeelTuning feel = _combat != null ? _combat.Feel : null;
            Color bossRed = new(0.92f, 0.12f, 0.14f, 1f);
            Color bossCritRed = new(1f, 0.35f, 0.32f, 1f);
            int flashMs = feel != null ? feel.BossHitFlashMs : 90;
            _bossFlash?.Flash(isCrit ? bossCritRed : bossRed, flashMs);
            if (_combat == null || feel == null)
                return;

            string archetype = string.IsNullOrEmpty(weaponArchetype)
                ? WeaponArchetypeMap.SwordShield
                : weaponArchetype;
            float shakePx = FeelWeaponPresentation.BossHitShakePx(feel, archetype, isCrit);
            if (shakePx > 0f)
                _follow?.AddShakePxAtLeast(shakePx, feel.ShakeDecay);

            if (!allowHitstop)
                return;

            float now = Time.unscaledTime;
            if ((now - _lastBossHitstopUnscaled) * 1000f < feel.BossHitHitstopMinGapMs)
                return;
            _lastBossHitstopUnscaled = now;

            int stopMs = FeelWeaponPresentation.BossHitstopMs(feel, archetype);
            if (stopMs <= 0)
                return;
            if (_visualFreeze != null)
            {
                _visualFreeze.Trigger(stopMs / 1000f);
                DebugConfig.DevLog(
                    $"[Feel2Verify] boss-hit visual-freeze {stopMs}ms archetype={archetype} shake={shakePx:0.#}px");
            }
        }

        public void Bind(
            GameClockHost clock,
            FollowCameraController follow,
            CombatTuning combat,
            GameTuning colors,
            Camera overlayCam,
            SentenceDebugHud hud,
            ReactionReadoutHud readout = null)
        {
            _clock = clock;
            _follow = follow;
            _combat = combat;
            _colors = colors;
            _hud = hud;
            _readout = readout;

            BuildCanvas(overlayCam);
        }

        /// <summary>Windup tehdidi sıcak telegraf rengiyle; ekran kenarında, ortası açık.</summary>
        public void ShowThreat(float progress01)
        {
            if (_threatFlash == null)
                return;

            float p = Mathf.Clamp01(progress01);
            float hz = Mathf.Lerp(_colors.Hud.ThreatPulseHzMin, _colors.Hud.ThreatPulseHzMax, p);
            float pulse = CombatFeelDirectorDefaults.VignettePulseBase + CombatFeelDirectorDefaults.VignettePulseAmplitude * Mathf.Sin(Time.unscaledTime * hz);
            Color c = Color.Lerp(_colors.Visuals.TelegraphWarm, _colors.Visuals.TelegraphHot, p);
            c.a = _colors.Hud.ThreatAlphaMax * p * pulse;
            Show(_threatFlash, c);
            _threatUntil = Time.unscaledTime + ThreatHoldSec;
        }

        public void ClearThreat() => Hide(_threatFlash);

        public void OnExchange(ExchangeResult result)
        {
            LastExchange = result;
            FeelTuning feel = _combat.Feel;

            if (result.Outcome == ExchangeOutcome.Dodged)
            {
                if (_visualFreeze != null)
                    _visualFreeze.Trigger(feel.HitstopPerfectMs / 1000f);

                float kick = result.Grade == DodgeGrade.Mukemmel
                    ? feel.CameraPerfectZoomKick
                    : feel.CameraDodgeZoomKick;
                _follow?.Punch(kick, feel.CameraRollDeg, feel.ShakePerfectPx, feel.ShakeDecay);
                _impactUntil = Time.unscaledTime + feel.ImpactFrameMs / 1000f;

                if (result.Grade == DodgeGrade.Mukemmel)
                {
                    FeelHaptics.Pulse(feel.PerfectDodgeHapticMs);
                    if (_afterimage != null && _playerTransform != null)
                    {
                        _afterimage.EmitBurst(
                            _playerTransform.position,
                            _playerTransform.rotation,
                            _playerTransform.lossyScale,
                            feel.PerfectDodgeAfterimageCount,
                            feel.PerfectDodgeAfterimageLifeMs);
                    }

                    DebugConfig.DevLog(
                        $"[Feel2Verify] perfect-dodge haptic={feel.PerfectDodgeHapticMs}ms afterimages={feel.PerfectDodgeAfterimageCount}");
                }
            }
            else if (result.Outcome == ExchangeOutcome.Hit)
            {
                if (_visualFreeze != null)
                    _visualFreeze.Trigger(feel.HitstopPlayerHitMs / 1000f);
                _follow?.Punch(feel.CameraDodgeZoomKick, feel.CameraRollDeg, feel.ShakeHitPx, feel.ShakeDecay);
                _playerFlash?.Flash(_colors.Visuals.TelegraphHot);
                float hold = feel.PlayerHitVignetteSec > 0f ? feel.PlayerHitVignetteSec : _colors.Hud.VignetteHoldSec;
                _vignetteUntil = Time.unscaledTime + hold;
                FeelHaptics.Pulse(feel.PlayerHitHapticMs);
                DebugConfig.DevLog(
                    $"[Feel2Verify] player-hit vignette={hold:0.00}s haptic={feel.PlayerHitHapticMs}ms shake={feel.ShakeHitPx}px");
            }

            // Safe de yazılır (T8.1): dodge oyuncuyu etki hacminin dışına taşıdığında ekranda
            // hiçbir şey olmaması "neden derece almadım" sorusunu cevapsız bırakıyordu (§6).
            _hud?.NoteExchange(result);
            // Büyük tepki yazısı kaldırıldı (feel-2): MÜKEMMEL / geç kaldın metni yok.
            Exchanged?.Invoke(result);
        }

        void LateUpdate()
        {
            float now = Time.unscaledTime;

            float impactLeft = _impactUntil - now;
            if (impactLeft > 0f)
                Show(_impact, new Color(1f, 1f, 1f, Mathf.Clamp01(impactLeft / _colors.Hud.ImpactFadeSec)));
            else
                Hide(_impact);

            float vignetteLeft = _vignetteUntil - now;
            if (vignetteLeft > 0f)
            {
                Color c = _colors.Visuals.TelegraphHot;
                c.a = _colors.Hud.VignetteAlpha * Mathf.Clamp01(vignetteLeft / _colors.Hud.VignetteFadeSec);
                Show(_vignette, c);
            }
            else
            {
                Hide(_vignette);
            }

            if (now > _threatUntil)
                Hide(_threatFlash);
        }

        static void Show(Image img, Color color)
        {
            if (img == null)
                return;

            img.color = color;
            if (!img.enabled)
                img.enabled = true;
        }

        static void Hide(Image img)
        {
            if (img != null && img.enabled)
                img.enabled = false;
        }

        void BuildCanvas(Camera overlayCam)
        {
            var go = new GameObject("FeelCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = overlayCam != null
                ? RenderMode.ScreenSpaceCamera
                : RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = overlayCam;
            _canvas.planeDistance = CombatFeelDirectorDefaults.CanvasPlaneDistance;
            _canvas.sortingOrder = 200;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            Sprite edgeMask = CreateEdgeMaskSprite();
            _impact = CreateFull(go.transform, "Impact", null);
            _vignette = CreateFull(go.transform, "Vignette", edgeMask);
            _threatFlash = CreateFull(go.transform, "ThreatFlash", edgeMask);

            if (overlayCam != null)
                SetLayerRecursively(go, FirstLayer(overlayCam.cullingMask));
        }

        static Image CreateFull(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.clear;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        /// <summary>Kenardan içeri sönen maske: ekranın ortası (ve boss telegrafı) açık kalır.</summary>
        static Sprite CreateEdgeMaskSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / CombatFeelDirectorDefaults.VignetteRadiusNormDivisor);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CombatFeelDirectorDefaults.VignetteSmoothStart, 1f, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), CombatFeelDirectorDefaults.VignetteSpritePpu);
        }

        static int FirstLayer(int mask)
        {
            for (int i = 0; i < CombatFeelDirectorDefaults.VignetteTextureSize; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
