using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Casting;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    /// <summary>Dodge düğmesinin üstünde iki hak pip'i; dolmakta olan pip fill ile akar.</summary>
    public sealed class DodgeChargeHud : MonoBehaviour
    {
        HexagonInputController _input;
        HexagonView _view;
        Image[] _pips;
        RectTransform _anchor;
        Sprite _sprite;
        int _builtFor = -1;
        float _builtWidth = -1f;

        public void Bind(HexagonInputController input, HexagonView view)
        {
            _input = input;
            _view = view;
        }

        void Update()
        {
            if (_input == null || _view == null)
                return;
            RectTransform dodge = _view.DodgeButtonRect;
            if (dodge == null)
                return;
            DodgeChargeBank bank = _input.Charges;
            if (bank == null)
                return;
            Ensure(dodge, bank.Max);
            if (_pips == null)
                return;
            for (int i = 0; i < _pips.Length; i++)
            {
                float fill = i < bank.Ready ? 1f : (i == bank.Ready ? bank.Fill01 : 0f);
                Image pip = _pips[i];
                pip.fillAmount = fill;
                Color c = fill >= DodgeChargeHudDefaults.ChargeFillReadyThreshold
                    ? new Color(0.55f, 0.95f, 1f, 0.95f)
                    : new Color(0.25f, 0.35f, 0.45f, 0.9f);
                pip.color = c;
            }
        }

        void Ensure(RectTransform dodge, int count)
        {
            float width = dodge.rect.width;
            if (_anchor == dodge && _builtFor == count && _pips != null && Mathf.Abs(_builtWidth - width) < 1f)
                return;
            Clear();
            _anchor = dodge;
            _builtFor = count;
            _builtWidth = width;
            _sprite ??= WhiteSprite();
            _pips = new Image[count];
            float span = width > 1f ? width : DodgeChargeHudDefaults.DefaultBarWidthPx;
            float pip = Mathf.Clamp(span * DodgeChargeHudDefaults.PipWidthSpanMult, DodgeChargeHudDefaults.PipMinPx, DodgeChargeHudDefaults.PipMaxPx);
            float gap = pip * DodgeChargeHudDefaults.PipGapMult;
            float total = count * pip + (count - 1) * gap;
            float start = -total * 0.5f + pip * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("DodgePip" + i);
                go.transform.SetParent(dodge, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(pip, pip);
                rect.anchoredPosition = new Vector2(start + i * (pip + gap), 4f);
                var image = go.AddComponent<Image>();
                image.sprite = _sprite;
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Vertical;
                image.fillOrigin = 0;
                image.raycastTarget = false;
                _pips[i] = image;
            }
        }

        void Clear()
        {
            if (_pips == null)
                return;
            for (int i = 0; i < _pips.Length; i++)
            {
                if (_pips[i] != null)
                    Destroy(_pips[i].gameObject);
            }
            _pips = null;
        }

        static Sprite WhiteSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), DodgeChargeHudDefaults.SquareSpritePpu);
        }

        void OnDestroy() => Clear();
    }
}
