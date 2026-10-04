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
    public sealed partial class TuningPanelHud
    {
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

        void AddReadout(string label, Func<string> getter)
        {
            var go = new GameObject("Readout_" + label);
            go.transform.SetParent(_scrollContent, false);
            go.AddComponent<LayoutElement>().preferredHeight = RowHeight * 0.65f;
            var labelText = CreateLabel(go.transform, string.Empty, 14);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = new Color(0.82f, 0.88f, 0.95f, 0.9f);
            void Refresh() => labelText.text = $"{label}: {getter()}";
            Refresh();
            _refreshActions.Add(Refresh);
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
            _vitals?.SetMaxHp(_config.Prototype.Player.PlayerMaxHp);
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
    }
}
