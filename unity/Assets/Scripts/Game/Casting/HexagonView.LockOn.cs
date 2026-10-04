using Dovus.Game.Cameras;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    public sealed partial class HexagonView
    {
        RectTransform _lockOn;
        Image _lockOnFace;
        Image _lockOnRim;
        Text _lockOnLabel;
        Button _lockOnButton;
        FollowCamera _lockOnCamera;

        public RectTransform LockOnButtonRect => _lockOn;
        public Button LockOnButton => _lockOnButton;

        public void BindLockOn(FollowCamera camera)
        {
            _lockOnCamera = camera;
            RefreshLockOnVisual();
        }

        void BuildLockOnButton(Sprite disc, Transform parent)
        {
            _lockOn = CreateLayeredDisc(
                "LockOnButton", disc, disc, _tuning.Input.LockOnButtonColor, parent, out _lockOnFace,
                new Color(0.75f, 0.9f, 1f, 0.75f));
            _lockOnRim = _lockOn.Find("Rim")?.GetComponent<Image>();
            _lockOnFace.color = new Color(
                _tuning.Input.LockOnButtonColor.r,
                _tuning.Input.LockOnButtonColor.g,
                _tuning.Input.LockOnButtonColor.b,
                0.92f);
            _lockOnLabel = CreateLabel(_lockOn, "LOCK");
            _lockOnLabel.fontSize = 13;
            _lockOnLabel.fontStyle = FontStyle.Bold;
            _lockOnLabel.color = Color.white;
            var outline = _lockOnLabel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.12f, 0.2f, 0.85f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);

            _lockOnButton = _lockOn.gameObject.AddComponent<Button>();
            _lockOnButton.targetGraphic = _lockOnFace;
            _lockOnButton.onClick.AddListener(ToggleLockOnFromHud);
        }

        void ToggleLockOnFromHud()
        {
            if (_lockOnCamera == null)
                return;
            _lockOnCamera.LockOnActive = !_lockOnCamera.LockOnActive;
            RefreshLockOnVisual();
        }

        public void RefreshLockOnVisual()
        {
            if (_lockOnFace == null || _lockOnCamera == null)
                return;
            bool on = _lockOnCamera.LockOnActive;
            Color c = on ? _tuning.Input.LockOnButtonActiveColor : _tuning.Input.LockOnButtonColor;
            _lockOnFace.color = new Color(c.r, c.g, c.b, on ? 0.98f : 0.92f);
            if (_lockOnRim != null)
            {
                Color rim = on ? new Color(1f, 0.92f, 0.55f, 0.95f) : new Color(0.75f, 0.9f, 1f, 0.75f);
                _lockOnRim.color = rim;
            }
        }

        void LayoutLockOnButton(int w, int h)
        {
            if (_lockOn == null)
                return;
            Vector2 p = HexagonLayoutScreen.LockOnButtonPx(_tuning, w, h);
            float r = HexagonLayoutScreen.LockOnButtonRadiusPx(_tuning);
            Place(_lockOn, p, r * 2f, w, h);
            if (_lockOnLabel != null)
                _lockOnLabel.fontSize = Mathf.RoundToInt(r * 0.36f);
        }
    }
}
