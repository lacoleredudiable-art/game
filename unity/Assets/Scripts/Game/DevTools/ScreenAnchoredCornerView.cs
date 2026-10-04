using Dovus.Game.Casting;
using UnityEngine;

namespace Dovus.Game.DevTools
{
    /// <summary>Köşe konumunu/ölçüsünü Screen boyutuna göre her karede günceller (dp→px).</summary>
    sealed class ScreenAnchoredCornerView : MonoBehaviour
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
            float margin = HexagonLayoutScreen.DpToPixels(_marginDp);
            float d = HexagonLayoutScreen.DpToPixels(_radiusDp) * 2f;
            _rect.sizeDelta = new Vector2(d, d);
            _rect.anchoredPosition = new Vector2(-margin, -margin);
        }
    }
}
