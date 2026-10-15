using System;
using Dovus.Core.RuleEngineV4;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// O-17.8, Y4, Y10: dilimde değeri kural-motoru-v4.json'dan gelen ayarın panel satırı. Kaydırıcı gri ve etkileşimsiz,
    /// dinleyicisi yok; etiket oyunun kullandığı değeri (ör. oyuncu canı 1000) ve "JSON"u gösterir.
    /// </summary>
    static class JsonLockedSliderRow
    {
        static readonly Color LockedColor = new(0.45f, 0.47f, 0.50f, 0.6f);
        static readonly Color LockedLabelColor = new(0.58f, 0.61f, 0.66f, 0.85f);

        /// <returns>Paneldeki diğer satırlar gibi yenileme eylemi (bir kez çalıştırılmış).</returns>
        public static Action Lock(
            string label,
            float min,
            float max,
            Func<float> getter,
            Func<float, string> formatter,
            Slider slider,
            Text labelText,
            RuleEngineV4SliceTuningSources.Entry json)
        {
            slider.interactable = false;
            ColorBlock colors = slider.colors;
            colors.disabledColor = LockedColor;
            slider.colors = colors;
            if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
                fill.color = LockedColor;
            labelText.color = LockedLabelColor;

            void Refresh()
            {
                float v = json.HasJson || !json.MissingFallsBackToTuning ? json.Value : getter();
                slider.SetValueWithoutNotify(Mathf.Clamp(v, min, max));
                string tag = json.HasJson ? "JSON" : "JSON'da yok, kod";
                labelText.text = $"{label}: {formatter(v)} · {tag}";
            }

            Refresh();
            return Refresh;
        }
    }
}
