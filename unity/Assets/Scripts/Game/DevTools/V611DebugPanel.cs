using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// v6 implementation ladder controls: deterministic 1-1 smoke cast and element paint cycle.
    /// Build seÃ§imi <see cref="BuildSelectScreen"/>'dedir (B kÄ±sayolu onu aÃ§ar).
    /// </summary>
    public sealed class V611DebugPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        HexagonInput _input;
        ManifestationDirector _manifestation;
        BuildSelectScreen _buildSelect;
        PlayerVitals _vitals;
        GameObject _panel;
        GameObject _toggleGo;
        Text _status;
        Text _element;
        Text _weapon;
        Text _devHpLabel;

        public void Configure(
            HexagonInput input,
            ManifestationDirector manifestation,
            BuildSelectScreen buildSelect,
            Transform canvasRoot,
            PlayerVitals vitals)
        {
            _input = input;
            _manifestation = manifestation;
            _buildSelect = buildSelect;
            _vitals = vitals;

            Build(canvasRoot);
            Refresh();
        }

        void Build(Transform canvasRoot)
        {
            // Ãœst ÅŸeritte AYAR'Ä±n solu: sol Ã¼st kÃ¶ÅŸe oyuncu barlarÄ±nÄ±n baÅŸlÄ±ÄŸÄ±nÄ± Ã¶rtÃ¼yordu.
            var toggle = CreateButton(canvasRoot, "V6", new Vector2(0.80f, 0.90f), new Vector2(0.855f, 0.975f));
            toggle.onClick.AddListener(Toggle);
            _toggleGo = toggle.gameObject;

            _panel = new GameObject("V611SimpleControls");
            _panel.transform.SetParent(canvasRoot, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.01f, 0.12f);
            panelRect.anchorMax = new Vector2(0.38f, 0.87f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.06f, 0.10f, 0.94f);

            var vertical = _panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 10, 10);
            vertical.spacing = 6f;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;

            Text heading = CreateLabel(_panel.transform, "V6.1.1 â€” BASÄ°T DOÄRULAMA", 17);
            heading.alignment = TextAnchor.MiddleCenter;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            Button build = CreateRowButton(_panel.transform, "BUILD SEÃ‡ (B)");
            build.onClick.AddListener(OpenBuildSelect);
            Button smoke = CreateRowButton(_panel.transform, "TEST 1-1 (F1)");
            smoke.onClick.AddListener(SmokeCast);
            Button element = CreateRowButton(_panel.transform, "ELEMENT DEÄÄ°ÅTÄ°R (E)");
            element.onClick.AddListener(CycleElement);
            Button weapon = CreateRowButton(_panel.transform, "SÄ°LAH DEÄÄ°ÅTÄ°R (F2)");
            weapon.onClick.AddListener(CycleWeapon);
            Button devHp = CreateRowButton(_panel.transform, "Dev HP: AÃ‡IK");
            devHp.onClick.AddListener(ToggleDevHp);
            _devHpLabel = devHp.GetComponentInChildren<Text>();

            _element = CreateLabel(_panel.transform, string.Empty, 15);
            _element.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            _weapon = CreateLabel(_panel.transform, string.Empty, 15);
            _weapon.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            _status = CreateLabel(_panel.transform, string.Empty, 14);
            _status.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

            _panel.SetActive(false);
            IsOpen = false;

#if UNITY_EDITOR || DOVUS_DEBUG
            DebugPanelsChrome.Register(ApplyChrome);
            ApplyChrome(DebugPanelsChrome.Visible);
#endif
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.bKey.wasPressedThisFrame)
                OpenBuildSelect();
        }

#if UNITY_EDITOR || DOVUS_DEBUG
        void ApplyChrome(bool visible)
        {
            if (_toggleGo != null)
                _toggleGo.SetActive(visible);
            if (!visible)
            {
                IsOpen = false;
                if (_panel != null)
                    _panel.SetActive(false);
            }
        }

        void OnDestroy()
        {
            DebugPanelsChrome.Unregister(ApplyChrome);
            IsOpen = false;
        }
#endif

        void OpenBuildSelect()
        {
            if (IsOpen)
                Toggle();
            _buildSelect?.Open();
        }

        void Toggle()
        {
            IsOpen = !IsOpen;
            if (_panel != null)
                _panel.SetActive(IsOpen);
        }

        void SmokeCast()
        {
            bool accepted = _input != null && _input.TryDebugCastSkill(1, 1);
            SetStatus(accepted
                ? "1-1 aynÄ± canlÄ± cast yoluna gÃ¶nderildi."
                : "1-1 iÃ§in SaldÄ±rÄ± rÃ¼nÃ¼ build'de olmalÄ±.");
            if (accepted && IsOpen)
                Toggle();
        }

        void ToggleDevHp()
        {
            if (_vitals == null)
                return;
            _vitals.SetDevHp(!_vitals.DevHpEnabled);
            DebugConfig.DevHp = _vitals.DevHpEnabled;
            RefreshDevHp();
        }

        void RefreshDevHp()
        {
            if (_devHpLabel == null)
                return;
            bool on = _vitals != null && _vitals.DevHpEnabled;
            _devHpLabel.text = on ? "Dev HP: AÃ‡IK" : "Dev HP: KAPALI";
        }

        void CycleElement()
        {
            ElementPaintNode? paint = _manifestation?.CycleElementPaint();
            if (paint.HasValue)
                _element.text = "Element boya: " + paint.Value.Name;
        }

        void CycleWeapon()
        {
            EquipmentItem weapon = _manifestation?.CycleEquippedWeapon();
            RefreshWeapon(weapon);
            if (weapon != null)
                SetStatus("Silah: " + weapon.Name + " Â· F1 ile 1-1 dene");
        }

        void Refresh()
        {
            ElementPaintNode? paint = _manifestation?.SelectedElementPaint;
            if (_element != null)
                _element.text = paint.HasValue ? "Element boya: " + paint.Value.Name : "Element boya: â€”";
            RefreshWeapon(_manifestation?.EquippedWeapon);
            RefreshDevHp();
            SetStatus("B build Â· F1 smoke Â· F2 silah Â· E basÄ±lÄ±: element radial");
        }

        void RefreshWeapon(EquipmentItem weapon)
        {
            if (_weapon == null)
                return;
            if (weapon == null)
            {
                _weapon.text = "Silah: â€”";
                return;
            }
            string route = Dovus.Core.Casting.SkillExecutorRouter.IsRangedWeapon(weapon)
                ? "projectile"
                : "melee";
            _weapon.text = $"Silah: {weapon.Name} ({route})";
        }

        void SetStatus(string value)
        {
            if (_status != null)
                _status.text = value;
        }

        static Button CreateRowButton(Transform parent, string text)
        {
            Button button = CreateButton(parent, text, Vector2.zero, Vector2.one);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            return button;
        }

        static Button CreateButton(
            Transform parent,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(string.IsNullOrEmpty(text) ? "Button" : text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            Text label = CreateLabel(go.transform, text, 14);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static Text CreateLabel(Transform parent, string text, int fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            return label;
        }
    }
}
