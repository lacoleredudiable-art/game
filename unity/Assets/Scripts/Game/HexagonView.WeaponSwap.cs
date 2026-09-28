using Dovus.Core.Equipment;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// ui_rules.swap_indicator: weapon_icons — aktif silah + bekleme. İkon asset'i yok;
    /// silah adı + küçük yedek adı + radial bekleme örtüsü.
    /// </summary>
    public sealed partial class HexagonView
    {
        RectTransform _swap;
        Image _swapFace;
        Image _swapFill;
        Text _swapActive;
        Text _swapReserve;
        ManifestationDirector _swapSource;
        GameClock _swapClock;

        public void BindWeaponSwap(ManifestationDirector source, GameClock clock)
        {
            _swapSource = source;
            _swapClock = clock;
            RefreshWeaponSwapButton();
        }

        void BuildWeaponSwapButton(Sprite disc, Transform parent)
        {
            _swap = CreateLayeredDisc(
                "WeaponSwapButton", disc, disc, new Color(0.22f, 0.24f, 0.3f, 0.92f), parent,
                out _swapFace, new Color(0.95f, 0.8f, 0.45f, 0.75f));

            var fillGo = new GameObject("SwapCooldown");
            fillGo.transform.SetParent(_swap, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _swapFill = fillGo.AddComponent<Image>();
            _swapFill.sprite = disc;
            _swapFill.type = Image.Type.Filled;
            _swapFill.fillMethod = Image.FillMethod.Radial360;
            _swapFill.fillOrigin = (int)Image.Origin360.Top;
            _swapFill.fillClockwise = false;
            _swapFill.color = new Color(0f, 0f, 0f, 0.55f);
            _swapFill.raycastTarget = false;
            _swapFill.fillAmount = 0f;

            _swapActive = CreateLabel(_swap, "—");
            _swapActive.fontSize = 13;
            _swapActive.fontStyle = FontStyle.Bold;
            _swapActive.color = Color.white;
            _swapActive.rectTransform.offsetMin = new Vector2(0f, 6f);
            var outline = _swapActive.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            _swapReserve = CreateLabel(_swap, string.Empty);
            _swapReserve.fontSize = 10;
            _swapReserve.color = new Color(1f, 0.9f, 0.65f, 0.9f);
            _swapReserve.alignment = TextAnchor.LowerCenter;
            _swapReserve.rectTransform.offsetMin = new Vector2(0f, 4f);

            _swap.SetAsLastSibling();
        }

        void LayoutWeaponSwapButton(int w, int h)
        {
            if (_swap == null)
                return;
            Vector2 p = HexagonLayoutScreen.WeaponSwapButtonPx(_tuning, w, h);
            Place(_swap, p, HexagonLayoutScreen.WeaponSwapButtonRadiusPx(_tuning) * 2f, w, h);
        }

        void RefreshWeaponSwapButton()
        {
            if (_swap == null)
                return;
            WeaponSwapState swap = _swapSource != null ? _swapSource.WeaponSwap : null;
            bool visible = swap != null && swap.Reserve != null;
            _swap.gameObject.SetActive(visible);
            if (!visible)
                return;

            double worldMs = _swapClock != null ? _swapClock.Director.WorldTimeMs : 0;
            _swapActive.text = ShortName(swap.Active?.Name);
            _swapReserve.text = "⇄ " + ShortName(swap.Reserve?.Name);
            _swapFill.fillAmount = swap.Cooldown01(worldMs);
            _swapFace.color = swap.IsSwapping
                ? new Color(0.85f, 0.65f, 0.25f, 0.95f)
                : new Color(0.22f, 0.24f, 0.3f, 0.92f);
        }

        static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "—";
            return name.Length <= 7 ? name : name.Substring(0, 6) + ".";
        }
    }
}
