using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.DevTools
{
    public sealed partial class TuningPanelHud
    {
        void BuildToggleButton(Transform parent)
        {
            var go = new GameObject("ToggleButton");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            // Üst şeritte BUILD'in solu: sağ-alt köşe DODGE'un üstüne düşüyordu (29 Eyl telefon).
            rect.anchorMin = new Vector2(ToggleRightEdgeNorm, 1f);
            rect.anchorMax = new Vector2(ToggleRightEdgeNorm, 1f);
            rect.pivot = new Vector2(1f, 1f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.55f, 0.62f, 0.72f, 0.55f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(TogglePanel);

            var label = CreateLabel(go.transform, "AYAR", 16);
            label.alignment = TextAnchor.MiddleCenter;
            _toggleGo = go;
            _toggleLabel = label;

            // Ekran boyutu değişebilir (döndürme/Device Simulator) — her karede yeniden konumla.
            var follower = go.AddComponent<ScreenAnchoredCornerView>();
            follower.Configure(rect, ToggleMarginDp, ToggleRadiusDp);
        }

        void TogglePanel()
        {
            IsOpen = !IsOpen;
            _contentRoot.SetActive(IsOpen);
            if (_toggleLabel != null)
                _toggleLabel.text = IsOpen ? "KAPAT" : "AYAR";
            if (IsOpen)
                RefreshAll();
        }

        void BuildContent(Transform parent)
        {
            _contentRoot = new GameObject("PanelContent");
            _contentRoot.transform.SetParent(parent, false);
            var rootRect = _contentRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Tam ekran koyu perde: hem odak hem de altındaki oyunu dokunuştan korur.
            var backdrop = _contentRoot.AddComponent<Image>();
            backdrop.color = new Color(0.04f, 0.05f, 0.07f, 0.82f);
            backdrop.raycastTarget = true;

            var card = new GameObject("Card");
            card.transform.SetParent(_contentRoot.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.03f, 0.04f);
            cardRect.anchorMax = new Vector2(0.97f, 0.96f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.10f, 0.11f, 0.15f, 0.97f);

            var title = CreateLabel(card.transform, "AYAR PANELİ", 24);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.945f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-16f, 0f);
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;

            // Köşedeki disk panel açıkken alttaki "JSON'U KOPYALA" düğmesinin üstüne biniyor;
            // modalın kendi kapatma tutamacı başlık çubuğunda olsun.
            var (closeButton, _) = CreateButton(card.transform, "KAPAT");
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.86f, 0.945f);
            closeRect.anchorMax = new Vector2(0.995f, 0.998f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            closeButton.onClick.AddListener(TogglePanel);

            BuildFooter(card.transform, out float footerTop01);
            BuildScrollView(card.transform, footerTop01);
        }

        void BuildFooter(Transform parent, out float footerTop01)
        {
            // İki satır: hazır setler + sıfırla/kopyala. En altta sabit.
            footerTop01 = 0.155f;

            var presetsRow = new GameObject("Presets");
            presetsRow.transform.SetParent(parent, false);
            var presetsRect = presetsRow.AddComponent<RectTransform>();
            presetsRect.anchorMin = new Vector2(0f, 0.08f);
            presetsRect.anchorMax = new Vector2(1f, footerTop01);
            presetsRect.offsetMin = new Vector2(12f, 2f);
            presetsRect.offsetMax = new Vector2(-12f, -2f);
            AddHorizontalButtons(presetsRow.transform, new (string, Action)[]
            {
                ("AĞIR", () => ApplyPreset(TuningPreset.Agir)),
                ("ÇEVİK", () => ApplyPreset(TuningPreset.Cevik)),
                ("ANİME", () => ApplyPreset(TuningPreset.Anime)),
            });

            var actionsRow = new GameObject("Actions");
            actionsRow.transform.SetParent(parent, false);
            var actionsRect = actionsRow.AddComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(0f, 0f);
            actionsRect.anchorMax = new Vector2(1f, 0.08f);
            actionsRect.offsetMin = new Vector2(12f, 2f);
            actionsRect.offsetMax = new Vector2(-12f, -2f);
            AddHorizontalButtons(actionsRow.transform, new (string, Action)[]
            {
                ("SIFIRLA", ResetToDefaults),
                ("JSON'U KOPYALA", CopyJsonToClipboard),
            });

            var status = CreateLabel(parent, string.Empty, 14);
            _statusText = status;
            var statusRect = status.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0.155f);
            statusRect.anchorMax = new Vector2(1f, 0.185f);
            statusRect.offsetMin = new Vector2(16f, 0f);
            statusRect.offsetMax = new Vector2(-16f, 0f);
            status.alignment = TextAnchor.MiddleLeft;
            status.color = new Color(0.6f, 0.95f, 0.75f, 0.9f);
        }

        void BuildScrollView(Transform parent, float footerTop01)
        {
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, footerTop01 + 0.015f);
            scrollRect.anchorMax = new Vector2(1f, 0.94f);
            scrollRect.offsetMin = new Vector2(8f, 0f);
            scrollRect.offsetMax = new Vector2(-8f, 0f);

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _scrollContent = contentGo.AddComponent<RectTransform>();
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.offsetMin = new Vector2(0f, 0f);
            _scrollContent.offsetMax = new Vector2(0f, 0f);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(4, 4, 4, 12);

            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = _scrollContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            BuildAllGroups();
        }

        // ---- Gruplar --------------------------------------------------------------------

        void BuildAllGroups()
        {
            var c = _config.Combat;
            var p = _config.Prototype;

            AddHeader("DODGE (§6)");
            AddIntSlider("Startup", 0, 150, () => c.Dodge.StartupMs, v => c.Dodge.StartupMs = v, "ms");
            AddIntSlider("İ-frame başlangıcı", 0, 150, () => c.Dodge.IframeStartMs, v => c.Dodge.IframeStartMs = v, "ms");
            AddIntSlider("İ-frame süresi", 50, 500, () => c.Dodge.IframeMs, v => c.Dodge.IframeMs = v, "ms");
            AddFloatSlider("Mesafe", 0.5f, 8f, () => c.Dodge.DistanceM, v => c.Dodge.DistanceM = v, "m", "0.00");
            AddIntSlider("Süre", 50, 600, () => c.Dodge.DurationMs, v => c.Dodge.DurationMs = v, "ms");
            AddFloatSlider("Eğri üssü", 1f, 6f, () => c.Dodge.CurveExp, v => c.Dodge.CurveExp = v, "", "0.00");
            AddIntSlider("Kayma kuyruğu", 0, 500, () => c.Dodge.GlideTailMs, v => c.Dodge.GlideTailMs = v, "ms");
            AddFloatSlider("Kayma hızı", 0f, 10f, () => p.Player.DodgeGlideSpeedMps, v => p.Player.DodgeGlideSpeedMps = v, "m/s", "0.00");
            AddIntSlider("Tap hareket eşiği", 4, 40, () => c.Dodge.TapMaxMoveDp, v => c.Dodge.TapMaxMoveDp = v, "dp");
            AddIntSlider("Hak dolumu", 1000, 12000, () => c.Dodge.ChargeRechargeMs, v => c.Dodge.ChargeRechargeMs = v, "ms");
            AddIntSlider("Çift basış penceresi", 50, 500, () => c.Dodge.DoubleTapWindowMs, v => c.Dodge.DoubleTapWindowMs = v, "ms");
            AddIntSlider("Birleşik i-frame", 200, 1200, () => c.Dodge.CombinedIframeMs, v => c.Dodge.CombinedIframeMs = v, "ms");
            AddFloatSlider("Birleşik mesafe ×", 1f, 3f, () => c.Dodge.CombinedDistanceMult, v => c.Dodge.CombinedDistanceMult = v, "", "0.00");
            AddFloatSlider("Birleşik süre ×", 1f, 3f, () => c.Dodge.CombinedDurationMult, v => c.Dodge.CombinedDurationMult = v, "", "0.00");
            // O2: tek mükemmel pencere (PERFECT derecesi + iade + sonraki vuruş).
            AddIntSlider("PERFECT penceresi", 20, 300, () => c.Dodge.PerfectWindowMs, v => c.Dodge.PerfectWindowMs = v, "ms");
            AddIntSlider("HARİKA eşiği", 40, 350, () => c.Grade.HarikaGapMaxMs, v => c.Grade.HarikaGapMaxMs = v, "ms");
            // §6: bu eşik IframeMs'den küçük kalmalı yoksa SIYIRDI bandı hiç üretilemez.
            AddIntSlider("TEMİZ eşiği", 60, 400, () => c.Grade.TemizGapMaxMs,
                v => c.Grade.TemizGapMaxMs = Mathf.Min(v, c.Dodge.IframeMs - 1), "ms");

            AddHeader("CÜMLE (§3, §5)");
            AddIntSlider("Bekletme (dwell)", 50, 600, () => c.Sentence.DwellMs, v => c.Sentence.DwellMs = v, "ms");
            AddIntSlider("Bekletme max yığın", 0, 4, () => c.Sentence.DwellMaxStacks, v => c.Sentence.DwellMaxStacks = v, "");
            AddIntSlider("Fiil penceresi", 100, 900, () => c.Sentence.CancelWindowMs[0], v => c.Sentence.CancelWindowMs[0] = v, "ms");
            AddIntSlider("1. sıfat penceresi", 100, 800, () => c.Sentence.CancelWindowMs[1], v => c.Sentence.CancelWindowMs[1] = v, "ms");
            AddIntSlider("2. sıfat penceresi", 100, 700, () => c.Sentence.CancelWindowMs[2], v => c.Sentence.CancelWindowMs[2] = v, "ms");
            AddFloatSlider("Toparlanma · 1 nokta", 0.02f, 1.0f, () => c.Sentence.Steps[0].RecoverySec, v => c.Sentence.Steps[0].RecoverySec = v, "sn", "0.00");
            AddFloatSlider("Toparlanma · 2 nokta", 0.02f, 1.2f, () => c.Sentence.Steps[1].RecoverySec, v => c.Sentence.Steps[1].RecoverySec = v, "sn", "0.00");
            AddFloatSlider("Toparlanma · 3 nokta", 0.02f, 1.4f, () => c.Sentence.Steps[2].RecoverySec, v => c.Sentence.Steps[2].RecoverySec = v, "sn", "0.00");
            AddFloatSlider("Toparlanma · 4 nokta", 0.02f, 1.6f, () => c.Sentence.Steps[3].RecoverySec, v => c.Sentence.Steps[3].RecoverySec = v, "sn", "0.00");

            AddHeader("KAMERA (§8)");
            AddBoolButton(
                "Lock-on (Tab)",
                () => _followCamera != null && _followCamera.LockOnActive,
                v => { if (_followCamera != null) _followCamera.LockOnActive = v; },
                "AÇIK", "KAPALI");
            AddFloatSlider("Takip yumuşatma", 0.02f, 0.5f, () => p.Camera.FollowSmoothTimeSec, v => p.Camera.FollowSmoothTimeSec = v, "sn", "0.00");
            AddFloatSlider("Önden bakış", 0f, 4f, () => p.Camera.LookAheadM, v => p.Camera.LookAheadM = v, "m", "0.00");
            AddFloatSlider("Mesafe (varsayılan)", 3f, 10f, () => p.Camera.CameraDistanceM, v => p.Camera.CameraDistanceM = v, "m", "0.00");
            AddFloatSlider("Bakış yüksekliği", 0f, 1.5f, () => p.Camera.CameraLookHeightM, v => p.Camera.CameraLookHeightM = v, "m", "0.00");
            AddFloatSlider("Pitch (varsayılan)", 0f, 35f, () => p.Camera.CameraDefaultPitchDeg, v => p.Camera.CameraDefaultPitchDeg = v, "°", "0.0");
            AddFloatSlider("Boss bakış yüksekliği", 0.5f, 3f, () => p.Camera.CameraBossAimHeightM, v => p.Camera.CameraBossAimHeightM = v, "m", "0.00");
            AddFloatSlider("Lock-on min mesafe", 3f, 9f, () => p.Camera.CameraLockOnMinDistanceM, v => p.Camera.CameraLockOnMinDistanceM = v, "m", "0.00");
            AddFloatSlider("Lock-on max mesafe", 4f, 12f, () => p.Camera.CameraLockOnMaxDistanceM, v => p.Camera.CameraLockOnMaxDistanceM = v, "m", "0.00");
            AddFloatSlider("Lock-on mesafe / ayrım", 0f, 0.3f, () => p.Camera.CameraLockOnDistancePerSepM, v => p.Camera.CameraLockOnDistancePerSepM = v, "", "0.00");
            AddFloatSlider("Lock-on ekstra tavan", 0f, 5f, () => p.Camera.CameraLockOnMaxExtraDistanceM, v => p.Camera.CameraLockOnMaxExtraDistanceM = v, "m", "0.00");
            AddFloatSlider("Lock-on omuz yatay", 0.5f, 1.6f, () => p.Camera.CameraLockOnShoulderSideM, v => p.Camera.CameraLockOnShoulderSideM = v, "m", "0.00");
            AddFloatSlider("Çarpışma yarıçapı", 0.08f, 0.5f, () => p.Camera.CameraCollisionSphereRadiusM, v => p.Camera.CameraCollisionSphereRadiusM = v, "m", "0.00");
            AddFloatSlider("Çarpışma payı", 0f, 0.35f, () => p.Camera.CameraCollisionMarginM, v => p.Camera.CameraCollisionMarginM = v, "m", "0.00");
            AddFloatSlider("Çarpışma min mesafe", 0.6f, 2.5f, () => p.Camera.CameraCollisionMinDistanceM, v => p.Camera.CameraCollisionMinDistanceM = v, "m", "0.00");
            AddFloatSlider("Çarpışma içeri (sn)", 0.01f, 0.15f, () => p.Camera.CameraCollisionPullInSmoothSec, v => p.Camera.CameraCollisionPullInSmoothSec = v, "sn", "0.00");
            AddFloatSlider("Çarpışma dışarı (sn)", 0.1f, 0.8f, () => p.Camera.CameraCollisionPullOutSmoothSec, v => p.Camera.CameraCollisionPullOutSmoothSec = v, "sn", "0.00");
            if (_followCamera != null)
                AddReadout("CollisionPulledInM", () => $"{_followCamera.CollisionPulledInM:F2} m");
            AddFloatSlider("Windup mesafe çarpanı", 1f, 1.8f, () => p.Camera.CameraWindupDistanceMul, v => p.Camera.CameraWindupDistanceMul = v, "×", "0.00");
            AddFloatSlider("Windup ekstra yükseklik", 0f, 1.5f, () => p.Camera.CameraWindupExtraHeightM, v => p.Camera.CameraWindupExtraHeightM = v, "m", "0.00");
            AddFloatSlider("Piksel→metre (sarsıntı)", 0.001f, 0.05f, () => p.Hud.CameraShakePxToM, v => p.Hud.CameraShakePxToM = v, "", "0.000");
            AddFloatSlider("Mükemmel FOV sıçraması", 0f, 0.4f, () => c.Feel.CameraPerfectZoomKick, v => c.Feel.CameraPerfectZoomKick = v, "", "0.00");
            AddFloatSlider("Dodge FOV sıçraması", 0f, 0.4f, () => c.Feel.CameraDodgeZoomKick, v => c.Feel.CameraDodgeZoomKick = v, "", "0.00");
            AddFloatSlider("Kamera roll", 0f, 8f, () => c.Feel.CameraRollDeg, v => c.Feel.CameraRollDeg = v, "°", "0.00");
            AddFloatSlider("Sıyırma sarsıntısı", 0f, 30f, () => c.Feel.ShakePerfectPx, v => c.Feel.ShakePerfectPx = v, "px", "0.0");
            AddFloatSlider("Vurulma sarsıntısı", 0f, 40f, () => c.Feel.ShakeHitPx, v => c.Feel.ShakeHitPx = v, "px", "0.0");
            AddFloatSlider("Sarsıntı sönme hızı", 1f, 15f, () => c.Feel.ShakeDecay, v => c.Feel.ShakeDecay = v, "", "0.0");
            AddIntSlider("Mükemmel hitstop (görsel)", 0, 300, () => c.Feel.HitstopPerfectMs, v => c.Feel.HitstopPerfectMs = v, "ms");
            AddIntSlider("Vurulma hitstop (görsel)", 0, 400, () => c.Feel.HitstopPlayerHitMs, v => c.Feel.HitstopPlayerHitMs = v, "ms");
            AddIntSlider("Bossa isabet hitstop", 0, 300, () => c.Feel.HitstopBossHitMs, v => c.Feel.HitstopBossHitMs = v, "ms");
            AddIntSlider("Boss hitstop · hafif", 0, 120, () => c.Feel.HitstopBossLightMs, v => c.Feel.HitstopBossLightMs = v, "ms");
            AddIntSlider("Boss hitstop · kılıç", 0, 150, () => c.Feel.HitstopBossSwordMs, v => c.Feel.HitstopBossSwordMs = v, "ms");
            AddIntSlider("Boss hitstop · ağır", 0, 180, () => c.Feel.HitstopBossHeavyMs, v => c.Feel.HitstopBossHeavyMs = v, "ms");
            AddIntSlider("Boss hitstop · çekiç", 0, 200, () => c.Feel.HitstopBossHammerMs, v => c.Feel.HitstopBossHammerMs = v, "ms");
            AddFloatSlider("Boss sarsıntı · hafif", 0f, 20f, () => c.Feel.ShakeBossLightPx, v => c.Feel.ShakeBossLightPx = v, "px", "0.0");
            AddFloatSlider("Boss sarsıntı · orta", 0f, 25f, () => c.Feel.ShakeBossMediumPx, v => c.Feel.ShakeBossMediumPx = v, "px", "0.0");
            AddFloatSlider("Boss sarsıntı · ağır", 0f, 35f, () => c.Feel.ShakeBossHeavyPx, v => c.Feel.ShakeBossHeavyPx = v, "px", "0.0");
            AddFloatSlider("Boss slam sarsıntı", 0f, 40f, () => c.Feel.ShakeBossSlamPx, v => c.Feel.ShakeBossSlamPx = v, "px", "0.0");
            AddIntSlider("Vuruş parlaması", 0, 250, () => c.Feel.HitFlashMs, v => c.Feel.HitFlashMs = v, "ms");
            AddFloatSlider("Parlama gücü", 0f, 1.5f, () => c.Feel.HitFlashStrength, v => c.Feel.HitFlashStrength = v, "", "0.00");
            AddFloatSlider("Vurulma vignette", 0f, 1.5f, () => c.Feel.PlayerHitVignetteSec, v => c.Feel.PlayerHitVignetteSec = v, "sn", "0.00");
            AddBoolButton("İsabet parçacıkları", () => c.Feel.HitImpactEnabled, v => c.Feel.HitImpactEnabled = v, "AÇIK", "KAPALI");
            AddIntSlider("Parçacık tavanı", 0, 20, () => c.Feel.HitImpactMaxConcurrent, v => c.Feel.HitImpactMaxConcurrent = v, "");
            AddFloatSlider("Parçacık ömrü", 0.05f, 1.2f, () => c.Feel.HitImpactLifeSec, v => c.Feel.HitImpactLifeSec = v, "sn", "0.00");
            AddBoolButton("Dokunsal (haptic)", () => c.Feel.FeelHapticsEnabled, v => c.Feel.FeelHapticsEnabled = v, "AÇIK", "KAPALI");
            AddIntSlider("Mükemmel dodge haptic", 0, 80, () => c.Feel.PerfectDodgeHapticMs, v => c.Feel.PerfectDodgeHapticMs = v, "ms");
            AddIntSlider("Vurulma haptic", 0, 80, () => c.Feel.PlayerHitHapticMs, v => c.Feel.PlayerHitHapticMs = v, "ms");
            AddIntSlider("Impact frame", 0, 100, () => c.Feel.ImpactFrameMs, v => c.Feel.ImpactFrameMs = v, "ms");
            AddIntSlider("Vuruş sonrası sessizlik", 0, 400, () => c.Feel.PostHitSilenceMs, v => c.Feel.PostHitSilenceMs = v, "ms");
            AddIntSlider("Afterimage sayısı", 0, 15, () => c.Feel.AfterimageCount, v => c.Feel.AfterimageCount = v, "");
            AddIntSlider("Afterimage ömrü", 0, 1000, () => c.Feel.AfterimageLifeMs, v => c.Feel.AfterimageLifeMs = v, "ms");

            AddHeader("YAZI (§6 gösterim)");
            AddFloatSlider("Punto tavanı", 24f, 180f, () => c.Feel.ReadoutSizePx, v => c.Feel.ReadoutSizePx = v, "px", "0");
            AddFloatSlider("Glow şiddeti", 0f, 80f, () => c.Feel.ReadoutGlow, v => c.Feel.ReadoutGlow = v, "", "0");
            AddIntSlider("Tutma süresi", 100, 3000, () => c.Feel.ReadoutHoldMs, v => c.Feel.ReadoutHoldMs = v, "ms");
            AddIntSlider("Sönme süresi", 50, 1500, () => c.Feel.ReadoutFadeMs, v => c.Feel.ReadoutFadeMs = v, "ms");
            AddFloatSlider("Giriş vuruşu ölçeği", 1f, 2.5f, () => c.Feel.ReadoutPunchScale, v => c.Feel.ReadoutPunchScale = v, "", "0.00");
            AddFloatSlider("Giriş vuruşu süresi", 0.02f, 0.5f, () => p.Hud.ReadoutPunchInSec, v => p.Hud.ReadoutPunchInSec = v, "sn", "0.00");
            AddBoolButton("Yazının kenarı", () => p.Hud.ReadoutAnchorRight, v => p.Hud.ReadoutAnchorRight = v, "SAĞ", "SOL");

            AddHeader("BOSS (§11)");
            AddIntSlider("YAKIN windup", 100, 2000, () => c.Boss.WindupMs, v => c.Boss.WindupMs = v, "ms");
            AddFloatSlider("YAKIN yarıçap", 1f, 12f, () => c.Boss.RadiusM, v => c.Boss.RadiusM = v, "m", "0.00");
            AddIntSlider("GEÇ windup", 100, 2000, () => c.Boss.GecWindupMs, v => c.Boss.GecWindupMs = v, "ms");
            AddFloatSlider("GEÇ yarıçap", 1f, 12f, () => c.Boss.GecRadiusM, v => c.Boss.GecRadiusM = v, "m", "0.00");
            AddIntSlider("GENİŞ windup", 100, 2000, () => c.Boss.GenisWindupMs, v => c.Boss.GenisWindupMs = v, "ms");
            AddFloatSlider("GENİŞ yarıçap", 1f, 12f, () => c.Boss.GenisRadiusM, v => c.Boss.GenisRadiusM = v, "m", "0.00");
            AddIntSlider("Aynı varyant üst üste", 1, 5, () => c.Boss.MaxSameVariantStreak, v => c.Boss.MaxSameVariantStreak = v, "");
            AddIntSlider("Aktif pencere", 20, 300, () => c.Boss.ActiveMs, v => c.Boss.ActiveMs = v, "ms");
            AddIntSlider("Toparlanma", 100, 2000, () => c.Boss.RecoveryMs, v => c.Boss.RecoveryMs = v, "ms");
            AddIntSlider("Hasar", 1, 60, () => c.Boss.Damage, v => c.Boss.Damage = v, "");
            AddIntSlider("Bekleme min", 100, 3000, () => c.Boss.IdleMinMs, v => c.Boss.IdleMinMs = v, "ms");
            AddIntSlider("Bekleme max", 100, 4000, () => c.Boss.IdleMaxMs, v => c.Boss.IdleMaxMs = v, "ms");
            AddFloatSlider("Yaklaşma hızı", 0f, 6f, () => c.Boss.ApproachSpeedMps, v => c.Boss.ApproachSpeedMps = v, "m/s", "0.00");
            AddFloatSlider("Ölüm cezası", 0.2f, 6f, () => c.Boss.RespawnMaxSec, v => c.Boss.RespawnMaxSec = v, "sn", "0.00");
            AddFloatSlider("Yaklaşma durma payı", 0f, 2f, () => p.Boss.BossApproachStopPadM, v => p.Boss.BossApproachStopPadM = v, "m", "0.00");
            AddIntSlider("Oyuncu can tavanı", 1, 100, () => p.Player.PlayerMaxHp, v =>
            {
                p.Player.PlayerMaxHp = v;
                _vitals?.SetMaxHp(v);
            }, "");

            AddHeader("ÖLÇÜM (T11/T12)");
            AddBoolButton("Kare süresi göstergesi", () => p.Hud.ShowFrameTimeHud, v => p.Hud.ShowFrameTimeHud = v, "AÇIK", "KAPALI");
            AddBoolButton("Hasar sayısı", () => p.Hud.ShowDamageNumbers, v => p.Hud.ShowDamageNumbers = v, "AÇIK", "KAPALI");
            AddBoolButton("Mana/Soğuma (JSON)",
                () => c.EnforceResourceCost && c.EnforceCooldown,
                v => { c.EnforceResourceCost = v; c.EnforceCooldown = v; },
                "AÇIK", "KAPALI");
        }
    }
}
