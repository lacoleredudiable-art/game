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
    /// </summary>
    public sealed class VitalsHud : MonoBehaviour
    {
        PlayerVitals _vitals;
        PrototypeTuning _tuning;
        Image _playerFill;
        Image _bossFill;

        public void Configure(PlayerVitals vitals, PrototypeTuning tuning, Transform canvasRoot)
        {
            _vitals = vitals;
            _tuning = tuning;

            var go = new GameObject("VitalsHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 1f);
            rect.anchorMax = new Vector2(0.02f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(tuning.VitalsBarWidthDp, tuning.VitalsBarHeightDp * 2f + tuning.VitalsBarSpacingDp);

            // Boss üstte (§10'un okuma sırasına uygun: önce tehdit), oyuncu altta.
            _bossFill = CreateBar(go.transform, "Boss", 0, tuning.BossVitalsColor);
            _playerFill = CreateBar(go.transform, "Player", 1, tuning.PlayerColor);
        }

        Image CreateBar(Transform parent, string name, int row, Color color)
        {
            float h = _tuning.VitalsBarHeightDp;
            float spacing = _tuning.VitalsBarSpacingDp;

            var bg = new GameObject(name + "Bg");
            bg.transform.SetParent(parent, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(0f, -row * (h + spacing));
            bgRect.sizeDelta = new Vector2(_tuning.VitalsBarWidthDp, h);
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
            fillImg.color = color;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;
            return fillImg;
        }

        void LateUpdate()
        {
            if (_vitals != null && _playerFill != null)
                _playerFill.fillAmount = _vitals.MaxHp > 0 ? Mathf.Clamp01((float)_vitals.Hp / _vitals.MaxHp) : 0f;

            // Boss: hasar mekaniği yok (yukarıdaki not) — kozmetik, her zaman dolu.
            if (_bossFill != null)
                _bossFill.fillAmount = 1f;
        }
    }
}
