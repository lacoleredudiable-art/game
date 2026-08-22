using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Sade can göstergeleri (T9, §6/§11). Oyuncu barı `PlayerVitals`'tan gerçek HP okur.
    ///
    /// Boss barı KOZMETİKTİR: Core/Game hiçbir yerde boss hasarı tutmuyor — T7 durum.md'de
    /// "boss fiziksel tepki verir (geri tepme/sarsılma/kabuk), hasar yok" diye kayıtlı, ve bu
    /// görevin YASAKLAR'ı yeni oyun mekaniği eklemeyi kapatıyor. Bar her zaman dolu görünür;
    /// gerçek bir boss-HP sistemi ayrı bir görev/karar gerektirir (bkz. durum.md T9 sapmaları).
    ///
    /// Ölçüler dp: canvas ConstantPixelSize olduğu için `PentagonLayoutScreen.DpToPixels`'ten
    /// geçer — beşgen/dodge diski ile aynı yol. Ham piksel yazılsaydı yüksek yoğunluklu
    /// telefonda barlar yarıdan küçük görünürdü.
    /// </summary>
    public sealed class VitalsHud : MonoBehaviour
    {
        PlayerVitals _vitals;
        PrototypeTuning _tuning;
        RectTransform _root;
        RectTransform _bossBg;
        RectTransform _playerBg;
        Image _playerFill;
        Image _bossFill;

        // T10 canlı paneli bu ölçüleri oynatacak; uygulanan değer saklanıp karşılaştırılıyor.
        float _appliedWidthDp = -1f;
        float _appliedHeightDp = -1f;
        float _appliedSpacingDp = -1f;
        float _appliedMarginDp = -1f;
        Color _appliedBossColor;
        Color _appliedPlayerColor;

        public void Configure(PlayerVitals vitals, PrototypeTuning tuning, Transform canvasRoot)
        {
            _vitals = vitals;
            _tuning = tuning;

            var go = new GameObject("VitalsHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.02f, 1f);
            _root.anchorMax = new Vector2(0.02f, 1f);
            _root.pivot = new Vector2(0f, 1f);

            // Boss üstte (§10'un okuma sırasına uygun: önce tehdit), oyuncu altta.
            _bossFill = CreateBar(go.transform, "Boss", out _bossBg);
            _playerFill = CreateBar(go.transform, "Player", out _playerBg);
            ApplyTuningLayout();
        }

        static Image CreateBar(Transform parent, string name, out RectTransform bgRect)
        {
            var bg = new GameObject(name + "Bg");
            bg.transform.SetParent(parent, false);
            bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            bgImg.raycastTarget = false;

            var fillGo = new GameObject(name + "Fill");
            fillGo.transform.SetParent(bg.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;
            return fillImg;
        }

        /// <summary>
        /// Ölçü/renk ayarlarını uygular. Değişmeyen karede maliyeti dört float karşılaştırması;
        /// T10 paneli bir slider'ı oynattığında bar aynı karede yeniden ölçülür.
        /// </summary>
        void ApplyTuningLayout()
        {
            bool sizeChanged =
                !Mathf.Approximately(_tuning.VitalsBarWidthDp, _appliedWidthDp) ||
                !Mathf.Approximately(_tuning.VitalsBarHeightDp, _appliedHeightDp) ||
                !Mathf.Approximately(_tuning.VitalsBarSpacingDp, _appliedSpacingDp) ||
                !Mathf.Approximately(_tuning.VitalsMarginDp, _appliedMarginDp);

            if (sizeChanged)
            {
                _appliedWidthDp = _tuning.VitalsBarWidthDp;
                _appliedHeightDp = _tuning.VitalsBarHeightDp;
                _appliedSpacingDp = _tuning.VitalsBarSpacingDp;
                _appliedMarginDp = _tuning.VitalsMarginDp;

                float w = PentagonLayoutScreen.DpToPixels(_appliedWidthDp);
                float h = PentagonLayoutScreen.DpToPixels(_appliedHeightDp);
                float spacing = PentagonLayoutScreen.DpToPixels(_appliedSpacingDp);

                _root.anchoredPosition = new Vector2(0f, -PentagonLayoutScreen.DpToPixels(_appliedMarginDp));
                _root.sizeDelta = new Vector2(w, h * 2f + spacing);

                _bossBg.anchoredPosition = Vector2.zero;
                _bossBg.sizeDelta = new Vector2(w, h);
                _playerBg.anchoredPosition = new Vector2(0f, -(h + spacing));
                _playerBg.sizeDelta = new Vector2(w, h);
            }

            if (_appliedBossColor != _tuning.BossVitalsColor)
            {
                _appliedBossColor = _tuning.BossVitalsColor;
                _bossFill.color = _appliedBossColor;
            }

            if (_appliedPlayerColor != _tuning.PlayerColor)
            {
                _appliedPlayerColor = _tuning.PlayerColor;
                _playerFill.color = _appliedPlayerColor;
            }
        }

        void LateUpdate()
        {
            if (_tuning == null)
                return;

            ApplyTuningLayout();

            if (_vitals != null && _playerFill != null)
                _playerFill.fillAmount = _vitals.MaxHp > 0 ? Mathf.Clamp01((float)_vitals.Hp / _vitals.MaxHp) : 0f;

            // Boss: hasar mekaniği yok (yukarıdaki not) — kozmetik, her zaman dolu.
            if (_bossFill != null)
                _bossFill.fillAmount = 1f;
        }
    }
}
