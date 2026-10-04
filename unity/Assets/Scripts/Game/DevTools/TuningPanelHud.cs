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
    /// <summary>
    /// T10: oyun içi ayar paneli (teknoloji-kararlari §6). Açılıp kapanan, gruplanmış
    /// slider'lı bir katman; her slider `TuningConfig.Combat`/`Prototype`nin GERÇEK çalışma-anı
    /// alanlarını yazar (kopya yok) — bir sonraki karede ilgili sistem doğrudan görür.
    ///
    /// Girdi çakışması: proje hiçbir yerde EventSystem/uGUI Slider kullanmıyordu (hepsi
    /// EnhancedTouch ile elle hit-test ediliyordu — HexagonInputController/MoveInputController). Bu panel standart
    /// Slider/Button kullanıyor (EventSystem + InputSystemUIInputModule Bootstrap'te bir kez
    /// kuruluyor), AMA EnhancedTouch'ın global `Touch.onFingerDown` akışı UI raycast'inden habersiz
    /// olduğu için panel açıkken aynı dokunuş altıgeni/çubuğu da tetikleyebilirdi. Çözüm iki parçalı:
    /// 1) panel açıkken `IsOpen` bayrağı HexagonInputController/MoveInputController'u tamamen susturuyor,
    /// 2) paneli AÇAN/KAPATAN dokunuşun kendisi (bayrak henüz değişmeden önceki kare) için
    ///    `HitToggleButton` sabit bir köşeyi (sağ-alt) her iki girdi katmanında da hariç tutuyor
    ///    (dodge düğmesi/merkezin hit-sırası deseniyle aynı yaklaşım, §2).
    /// </summary>
    public sealed partial class TuningPanelHud : MonoBehaviour
    {
        // Sağ-alt köşe: altıgen (merkez y≈0.40, yarıçap ~100dp) ve dodge düğmesinin altında,
        // SentenceDebugHud/ReactionReadoutHud'un (y>0.56) dışında kalan boş bölge. Spec'te konum/
        // boyut yok — uydurma, durum.md'ye T10 sapması olarak geçildi.
        const float ToggleRadiusDp = 26f;
        const float ToggleMarginDp = 10f;
        /// <summary>BUILD düğmesi (BuildSelectHud, 1600×900 referans) ekranın sağ ~%8'inde.</summary>
        const float ToggleRightEdgeNorm = 0.905f;

        const float RowHeight = 58f;
        const float RowSpacing = 4f;
        const float HeaderHeight = 40f;
        const float SaveDebounceSec = 0.35f;

        public static bool IsOpen { get; private set; }

        public static bool HitToggleButton(Vector2 screenPos)
        {
            float r = HexagonLayoutScreen.DpToPixels(ToggleRadiusDp);
            return Vector2.Distance(screenPos, ToggleCenterPx()) <= r;
        }

        static Vector2 ToggleCenterPx()
        {
            float margin = HexagonLayoutScreen.DpToPixels(ToggleMarginDp);
            float r = HexagonLayoutScreen.DpToPixels(ToggleRadiusDp);
            return new Vector2(Screen.width - margin - r, margin + r);
        }

        TuningConfig _config;
        PlayerVitalsHost _vitals;
        FollowCameraController _followCamera;
        GameObject _contentRoot;
        GameObject _toggleGo;
        Text _toggleLabel;
        RectTransform _scrollContent;
        Text _statusText;
        float _statusUntil;

        readonly List<Action> _refreshActions = new();
        bool _dirty;
        float _saveDebounceRemaining;

        public void Configure(TuningConfig config, PlayerVitalsHost vitals, FollowCameraController followCamera = null)
        {
            _config = config;
            _vitals = vitals;
            _followCamera = followCamera;

            var canvasGo = new GameObject("TuningPanelCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // En üstte: oyun içi HER şeyin (altıgen 50, his katmanı 200) üstünde açılan modal.
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

#if UNITY_EDITOR || DOVUS_DEBUG
            DebugPanelsChrome.Register(ApplyChrome);
            ApplyChrome(DebugPanelsChrome.Visible);
#endif
        }

#if UNITY_EDITOR || DOVUS_DEBUG
        void ApplyChrome(bool visible)
        {
            if (_toggleGo != null)
                _toggleGo.SetActive(visible);
            if (!visible)
            {
                IsOpen = false;
                if (_contentRoot != null)
                    _contentRoot.SetActive(false);
            }
        }

#endif


        // ---- Satır inşası -----------------------------------------------------------------


        void Update()
        {
            if (_toggleGo != null)
            {
                // Build ekranı sağ üstte kendi başlığını taşıyor; AYAR onun üstüne biniyordu.
                bool showToggle = IsOpen || !BuildSelectHud.IsOpen;
                if (_toggleGo.activeSelf != showToggle)
                    _toggleGo.SetActive(showToggle);
            }

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

        void OnDestroy()
        {
#if UNITY_EDITOR || DOVUS_DEBUG
            DebugPanelsChrome.Unregister(ApplyChrome);
#endif
            FlushSaveIfDirty();
        }

        // ---- Düşük seviye UI yardımcıları ---------------------------------------------------

    }
}
