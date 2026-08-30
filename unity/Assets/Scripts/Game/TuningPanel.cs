using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// T10: oyun içi ayar paneli (teknoloji-kararlari §6). Açılıp kapanan, gruplanmış
    /// slider'lı bir katman; her slider `TuningConfig.Combat`/`Prototype`nin GERÇEK çalışma-anı
    /// alanlarını yazar (kopya yok) — bir sonraki karede ilgili sistem doğrudan görür.
    ///
    /// Girdi çakışması: proje hiçbir yerde EventSystem/uGUI Slider kullanmıyordu (hepsi
    /// EnhancedTouch ile elle hit-test ediliyordu — PentagonInput/MoveInput). Bu panel standart
    /// Slider/Button kullanıyor (EventSystem + InputSystemUIInputModule Bootstrap'te bir kez
    /// kuruluyor), AMA EnhancedTouch'ın global `Touch.onFingerDown` akışı UI raycast'inden habersiz
    /// olduğu için panel açıkken aynı dokunuş beşgeni/çubuğu da tetikleyebilirdi. Çözüm iki parçalı:
    /// 1) panel açıkken `IsOpen` bayrağı PentagonInput/MoveInput'u tamamen susturuyor,
    /// 2) paneli AÇAN/KAPATAN dokunuşun kendisi (bayrak henüz değişmeden önceki kare) için
    ///    `HitToggleButton` sabit bir köşeyi (sağ-alt) her iki girdi katmanında da hariç tutuyor
    ///    (dodge düğmesi/merkezin hit-sırası deseniyle aynı yaklaşım, §2).
    /// </summary>
    public sealed class TuningPanel : MonoBehaviour
    {
        // Sağ-alt köşe: pentagon (merkez y≈0.40, yarıçap ~100dp) ve dodge düğmesinin altında,
        // SentenceDebugHud/ReactionReadout'un (y>0.56) dışında kalan boş bölge. Spec'te konum/
        // boyut yok — uydurma, durum.md'ye T10 sapması olarak geçildi.
        const float ToggleRadiusDp = 26f;
        const float ToggleMarginDp = 10f;

        const float RowHeight = 58f;
        const float RowSpacing = 4f;
        const float HeaderHeight = 40f;
        const float SaveDebounceSec = 0.35f;

        public static bool IsOpen { get; private set; }

        public static bool HitToggleButton(Vector2 screenPos)
        {
            float r = PentagonLayoutScreen.DpToPixels(ToggleRadiusDp);
            return Vector2.Distance(screenPos, ToggleCenterPx()) <= r;
        }

        static Vector2 ToggleCenterPx()
        {
            float margin = PentagonLayoutScreen.DpToPixels(ToggleMarginDp);
            float r = PentagonLayoutScreen.DpToPixels(ToggleRadiusDp);
            return new Vector2(Screen.width - margin - r, margin + r);
        }

        TuningConfig _config;
        PlayerVitals _vitals;
        GameObject _contentRoot;
        GameObject _toggleGo;
        Text _toggleLabel;
        RectTransform _scrollContent;
        Text _statusText;
        float _statusUntil;

        readonly List<Action> _refreshActions = new();
        bool _dirty;
        float _saveDebounceRemaining;

        public void Configure(TuningConfig config, PlayerVitals vitals)
        {
            _config = config;
            _vitals = vitals;

            var canvasGo = new GameObject("TuningPanelCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // En üstte: oyun içi HER şeyin (beşgen 50, his katmanı 200) üstünde açılan modal.
            canvas.sortingOrder = 1000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildToggleButton(canvasGo.transform);
            BuildContent(canvasGo.transform);

            // Aç/kapat düğmesi paneli KAPATMAK için de tek tutamaç, o yüzden modal perdenin
            // ÜSTÜNDE durmak zorunda. Aynı canvas'ta önce yaratıldığı için altta kalıyordu:
            // panel açıkken perde raycast'i yutuyor, düğmeye basılamıyordu ve panel bir daha
            // kapanmıyordu (telefonda çıktı; editörde `onClick.Invoke()` ile test edildiği için
            // raycast yolu hiç sınanmamıştı — T10 sapmalarındaki not).
            _toggleGo.transform.SetAsLastSibling();

            _contentRoot.SetActive(false);
            IsOpen = false;
        }

        void BuildToggleButton(Transform parent)
        {
            var go = new GameObject("ToggleButton");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);

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
            var follower = go.AddComponent<ScreenAnchoredCorner>();
            follower.Configure(rect, ToggleMarginDp, ToggleRadiusDp);
        }

        /// <summary>Köşe konumunu/ölçüsünü Screen boyutuna göre her karede günceller (dp→px).</summary>
        sealed class ScreenAnchoredCorner : MonoBehaviour
        {
            RectTransform _rect;
            float _marginDp;
            float _radiusDp;

            public void Configure(RectTransform rect, float marginDp, float radiusDp)
            {
                _rect = rect;
                _marginDp = marginDp;
                _radiusDp = radiusDp;
                Apply();
            }

            void LateUpdate() => Apply();

            void Apply()
            {
                float margin = PentagonLayoutScreen.DpToPixels(_marginDp);
                float d = PentagonLayoutScreen.DpToPixels(_radiusDp) * 2f;
                _rect.sizeDelta = new Vector2(d, d);
                _rect.anchoredPosition = new Vector2(-margin, margin);
            }
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
            AddFloatSlider("Kayma hızı", 0f, 10f, () => p.DodgeGlideSpeedMps, v => p.DodgeGlideSpeedMps = v, "m/s", "0.00");
            AddIntSlider("Bekleme (cooldown)", 0, 1200, () => c.Dodge.CooldownMs, v => c.Dodge.CooldownMs = v, "ms");
            AddIntSlider("Tap süresi eşiği", 50, 400, () => c.Dodge.TapMaxMs, v => c.Dodge.TapMaxMs = v, "ms");
            AddIntSlider("Tap hareket eşiği", 4, 40, () => c.Dodge.TapMaxMoveDp, v => c.Dodge.TapMaxMoveDp = v, "dp");
            AddIntSlider("MÜKEMMEL eşiği", 20, 300, () => c.Grade.MukemmelGapMaxMs, v => c.Grade.MukemmelGapMaxMs = v, "ms");
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
            AddFloatSlider("Takip yumuşatma", 0.02f, 0.5f, () => p.FollowSmoothTimeSec, v => p.FollowSmoothTimeSec = v, "sn", "0.00");
            AddFloatSlider("Önden bakış", 0f, 4f, () => p.LookAheadM, v => p.LookAheadM = v, "m", "0.00");
            AddFloatSlider("Piksel→metre (sarsıntı)", 0.001f, 0.05f, () => p.CameraShakePxToM, v => p.CameraShakePxToM = v, "", "0.000");
            AddFloatSlider("Mükemmel FOV sıçraması", 0f, 0.4f, () => c.Feel.CameraPerfectZoomKick, v => c.Feel.CameraPerfectZoomKick = v, "", "0.00");
            AddFloatSlider("Dodge FOV sıçraması", 0f, 0.4f, () => c.Feel.CameraDodgeZoomKick, v => c.Feel.CameraDodgeZoomKick = v, "", "0.00");
            AddFloatSlider("Kamera roll", 0f, 8f, () => c.Feel.CameraRollDeg, v => c.Feel.CameraRollDeg = v, "°", "0.00");
            AddFloatSlider("Sıyırma sarsıntısı", 0f, 30f, () => c.Feel.ShakePerfectPx, v => c.Feel.ShakePerfectPx = v, "px", "0.0");
            AddFloatSlider("Vurulma sarsıntısı", 0f, 40f, () => c.Feel.ShakeHitPx, v => c.Feel.ShakeHitPx = v, "px", "0.0");
            AddFloatSlider("Sarsıntı sönme hızı", 1f, 15f, () => c.Feel.ShakeDecay, v => c.Feel.ShakeDecay = v, "", "0.0");
            AddIntSlider("Mükemmel hitstop", 0, 300, () => c.Feel.HitstopPerfectMs, v => c.Feel.HitstopPerfectMs = v, "ms");
            AddIntSlider("Vurulma hitstop", 0, 400, () => c.Feel.HitstopPlayerHitMs, v => c.Feel.HitstopPlayerHitMs = v, "ms");
            AddIntSlider("Bossa isabet hitstop", 0, 300, () => c.Feel.HitstopBossHitMs, v => c.Feel.HitstopBossHitMs = v, "ms");
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
            AddFloatSlider("Giriş vuruşu süresi", 0.02f, 0.5f, () => p.ReadoutPunchInSec, v => p.ReadoutPunchInSec = v, "sn", "0.00");
            AddBoolButton("Yazının kenarı", () => p.ReadoutAnchorRight, v => p.ReadoutAnchorRight = v, "SAĞ", "SOL");

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
            AddFloatSlider("Yaklaşma durma payı", 0f, 2f, () => p.BossApproachStopPadM, v => p.BossApproachStopPadM = v, "m", "0.00");
            AddIntSlider("Oyuncu can tavanı", 1, 100, () => p.PlayerMaxHp, v =>
            {
                p.PlayerMaxHp = v;
                _vitals?.SetMaxHp(v);
            }, "");

            AddHeader("ÖLÇÜM (T11/T12)");
            AddBoolButton("Kare süresi göstergesi", () => p.ShowFrameTimeHud, v => p.ShowFrameTimeHud = v, "AÇIK", "KAPALI");
            AddBoolButton("Hasar sayısı", () => p.ShowDamageNumbers, v => p.ShowDamageNumbers = v, "AÇIK", "KAPALI");
        }

        // ---- Satır inşası -----------------------------------------------------------------

        void AddHeader(string text)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(_scrollContent, false);
            go.AddComponent<LayoutElement>().preferredHeight = HeaderHeight;
            var label = CreateLabel(go.transform, text, 20);
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.373f, 0.941f, 1f, 0.95f);
            label.alignment = TextAnchor.LowerLeft;
        }

        void AddFloatSlider(string label, float min, float max, Func<float> getter, Action<float> setter, string unit, string format)
        {
            BuildSliderRow(label, min, max, false, () => getter(), raw => setter(raw), v => FormatValue(v, unit, format));
        }

        void AddIntSlider(string label, int min, int max, Func<int> getter, Action<int> setter, string unit)
        {
            BuildSliderRow(label, min, max, true, () => getter(), raw => setter(Mathf.RoundToInt(raw)), v => FormatValue(Mathf.RoundToInt(v), unit, "0"));
        }

        static string FormatValue(float v, string unit, string format)
        {
            string num = v.ToString(format, CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(unit) ? num : $"{num} {unit}";
        }

        void BuildSliderRow(string label, float min, float max, bool wholeNumbers, Func<float> getter, Action<float> apply, Func<float, string> formatter)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(_scrollContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = RowHeight;

            var labelText = CreateLabel(row.transform, string.Empty, 15);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.52f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(6f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            labelText.alignment = TextAnchor.LowerLeft;
            labelText.color = new Color(0.9f, 0.94f, 1f, 0.92f);

            var sliderGo = new GameObject("SliderWidget");
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.05f);
            sliderRect.anchorMax = new Vector2(1f, 0.48f);
            sliderRect.offsetMin = new Vector2(6f, 0f);
            sliderRect.offsetMax = new Vector2(-6f, 0f);
            var slider = BuildSliderWidget(sliderGo.transform);
            slider.wholeNumbers = wholeNumbers;
            slider.minValue = min;
            slider.maxValue = max;

            void Refresh()
            {
                float v = Mathf.Clamp(getter(), min, max);
                slider.SetValueWithoutNotify(v);
                labelText.text = $"{label}: {formatter(v)}";
            }

            slider.onValueChanged.AddListener(v =>
            {
                apply(v);
                labelText.text = $"{label}: {formatter(getter())}";
                MarkDirty();
            });

            Refresh();
            _refreshActions.Add(Refresh);
        }

        void AddBoolButton(string label, Func<bool> getter, Action<bool> setter, string trueText, string falseText)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(_scrollContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = RowHeight;

            var labelText = CreateLabel(row.transform, string.Empty, 15);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.52f);
            labelRect.anchorMax = new Vector2(0.6f, 1f);
            labelRect.offsetMin = new Vector2(6f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            labelText.alignment = TextAnchor.LowerLeft;
            labelText.color = new Color(0.9f, 0.94f, 1f, 0.92f);

            var (button, buttonLabel) = CreateButton(row.transform, string.Empty);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.62f, 0.05f);
            buttonRect.anchorMax = new Vector2(1f, 0.9f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            void Refresh()
            {
                labelText.text = label;
                buttonLabel.text = getter() ? trueText : falseText;
            }

            button.onClick.AddListener(() =>
            {
                setter(!getter());
                Refresh();
                MarkDirty();
            });

            Refresh();
            _refreshActions.Add(Refresh);
        }

        void AddHorizontalButtons(Transform parent, (string Text, Action OnClick)[] items)
        {
            var layout = ((GameObject)parent.gameObject).AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (var item in items)
            {
                var (button, _) = CreateButton(parent, item.Text);
                button.onClick.AddListener(() => item.OnClick());
            }
        }

        // ---- Düğme/aksiyonlar --------------------------------------------------------------

        void ApplyPreset(TuningPreset preset)
        {
            _config.ApplyPreset(preset);
            RefreshAll();
            MarkDirty();
            ShowStatus($"{preset} preset'i uygulandı");
        }

        void ResetToDefaults()
        {
            _config.ResetToDefaults();
            _vitals?.SetMaxHp(_config.Prototype.PlayerMaxHp);
            RefreshAll();
            MarkDirty();
            ShowStatus("Spec varsayılanlarına sıfırlandı");
        }

        void CopyJsonToClipboard()
        {
            GUIUtility.systemCopyBuffer = _config.ToJson();
            ShowStatus("JSON panoya kopyalandı");
        }

        void ShowStatus(string text)
        {
            if (_statusText == null)
                return;
            _statusText.text = text;
            _statusUntil = Time.unscaledTime + 2.5f;
        }

        void RefreshAll()
        {
            foreach (var action in _refreshActions)
                action();
        }

        void MarkDirty()
        {
            _dirty = true;
            _saveDebounceRemaining = SaveDebounceSec;
        }

        void FlushSaveIfDirty()
        {
            if (!_dirty)
                return;
            _dirty = false;
            _config.Save();
        }

        void Update()
        {
            if (_dirty)
            {
                _saveDebounceRemaining -= Time.unscaledDeltaTime;
                if (_saveDebounceRemaining <= 0f)
                    FlushSaveIfDirty();
            }

            if (_statusText != null && _statusText.text.Length > 0 && Time.unscaledTime > _statusUntil)
                _statusText.text = string.Empty;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                FlushSaveIfDirty();
        }

        void OnApplicationQuit() => FlushSaveIfDirty();

        void OnDestroy() => FlushSaveIfDirty();

        // ---- Düşük seviye UI yardımcıları ---------------------------------------------------

        static Text CreateLabel(Transform parent, string text, int fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static (Button button, Text label) CreateButton(Transform parent, string text)
        {
            var go = new GameObject("Button_" + (string.IsNullOrEmpty(text) ? "x" : text));
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.373f, 0.941f, 1f, 0.16f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var label = CreateLabel(go.transform, text, 14);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;

            return (button, label);
        }

        /// <summary>Unity'nin varsayılan Slider hiyerarşisi: Background + Fill Area/Fill + Handle Slide Area/Handle.</summary>
        static Slider BuildSliderWidget(Transform parent)
        {
            var sliderGo = parent.gameObject;
            var slider = sliderGo.AddComponent<Slider>();

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.2f);
            bgRect.anchorMax = new Vector2(1f, 0.8f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-15f, 0f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.sizeDelta = new Vector2(10f, 0f);
            fillGo.AddComponent<Image>().color = new Color(0.373f, 0.941f, 1f, 0.9f);

            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(20f, 0f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.95f, 0.98f, 1f, 1f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.ColorTint;
            return slider;
        }
    }
}
