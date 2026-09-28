using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// v6 implementation ladder controls: deterministic 1-1 smoke cast and element paint cycle.
    /// Build seçimi <see cref="BuildSelectScreen"/>'dedir (B kısayolu onu açar).
    /// </summary>
    public sealed class V611DebugPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        HexagonInput _input;
        ManifestationDirector _manifestation;
        BuildSelectScreen _buildSelect;
        GameObject _panel;
        Text _status;
        Text _element;
        Text _weapon;

        public void Configure(
            HexagonInput input,
            ManifestationDirector manifestation,
            BuildSelectScreen buildSelect,
            Transform canvasRoot)
        {
            _input = input;
            _manifestation = manifestation;
            _buildSelect = buildSelect;

            Build(canvasRoot);
            Refresh();
        }

        void Build(Transform canvasRoot)
        {
            // Üst şeritte AYAR'ın solu: sol üst köşe oyuncu barlarının başlığını örtüyordu.
            var toggle = CreateButton(canvasRoot, "V6", new Vector2(0.80f, 0.90f), new Vector2(0.855f, 0.975f));
            toggle.onClick.AddListener(Toggle);

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

            Text heading = CreateLabel(_panel.transform, "V6.1.1 — BASİT DOĞRULAMA", 17);
            heading.alignment = TextAnchor.MiddleCenter;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            Button build = CreateRowButton(_panel.transform, "BUILD SEÇ (B)");
            build.onClick.AddListener(OpenBuildSelect);
            Button smoke = CreateRowButton(_panel.transform, "TEST 1-1 (F1)");
            smoke.onClick.AddListener(SmokeCast);
            Button element = CreateRowButton(_panel.transform, "ELEMENT DEĞİŞTİR (E)");
            element.onClick.AddListener(CycleElement);
            Button weapon = CreateRowButton(_panel.transform, "SİLAH DEĞİŞTİR (F2)");
            weapon.onClick.AddListener(CycleWeapon);

            _element = CreateLabel(_panel.transform, string.Empty, 15);
            _element.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            _weapon = CreateLabel(_panel.transform, string.Empty, 15);
            _weapon.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            _status = CreateLabel(_panel.transform, string.Empty, 14);
            _status.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

            _panel.SetActive(false);
            IsOpen = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.f1Key.wasPressedThisFrame)
                SmokeCast();
            if (keyboard.f2Key.wasPressedThisFrame)
                CycleWeapon();
            if (keyboard.bKey.wasPressedThisFrame)
                OpenBuildSelect();
        }

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
                ? "1-1 aynı canlı cast yoluna gönderildi."
                : "1-1 için Saldırı rünü build'de olmalı.");
            if (accepted && IsOpen)
                Toggle();
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
                SetStatus("Silah: " + weapon.Name + " · F1 ile 1-1 dene");
        }

        void Refresh()
        {
            ElementPaintNode? paint = _manifestation?.SelectedElementPaint;
            if (_element != null)
                _element.text = paint.HasValue ? "Element boya: " + paint.Value.Name : "Element boya: —";
            RefreshWeapon(_manifestation?.EquippedWeapon);
            SetStatus("B build · F1 smoke · F2 silah · E basılı: element radial");
        }

        void RefreshWeapon(EquipmentItem weapon)
        {
            if (_weapon == null)
                return;
            if (weapon == null)
            {
                _weapon.text = "Silah: —";
                return;
            }
            string route = Dovus.Core.Execution.SkillExecutorRouter.IsRangedWeapon(weapon)
                ? "projectile"
                : "melee";
            _weapon.text = $"Silah: {weapon.Name} ({route})";
        }

        void SetStatus(string value)
        {
            if (_status != null)
                _status.text = value;
        }

        void OnDestroy()
        {
            IsOpen = false;
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
