using Dovus.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Ayar sahnesi kuklası: isim + son vuruş hasarı; can tükenmez (yüksek tavan + anında iyileşme).
    /// </summary>
    public sealed class TrainingDummy : MonoBehaviour
    {
        BossVitals _vitals;
        Text _label;
        Transform _cam;
        string _title = "KUKLA";
        float _lastDamage;
        int _hitCount;

        public BossVitals Vitals => _vitals;
        public Transform Transform => transform;

        public void Bind(BossVitals vitals, string displayTitle)
        {
            _vitals = vitals ?? throw new System.ArgumentNullException(nameof(vitals));
            _title = string.IsNullOrEmpty(displayTitle) ? "KUKLA" : displayTitle;
            _vitals.HpChanged += OnHpChanged;
            BuildBillboard();
            RefreshLabel();
        }

        void OnDestroy()
        {
            if (_vitals != null)
                _vitals.HpChanged -= OnHpChanged;
        }

        void OnHpChanged(float before, float after)
        {
            float dealt = before - after;
            if (dealt <= 0f)
                return;
            _lastDamage = dealt;
            _hitCount++;
            RefreshLabel();
        }

        void LateUpdate()
        {
            if (_label == null)
                return;
            if (_cam == null)
                _cam = Camera.main != null ? Camera.main.transform : null;
            if (_cam != null)
                _label.transform.rotation = Quaternion.LookRotation(_label.transform.position - _cam.position);
        }

        void BuildBillboard()
        {
            var canvasGo = new GameObject("DummyLabel");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 96f);
            canvasGo.transform.localScale = Vector3.one * 0.012f;

            _label = CreateLabel(canvasGo.transform, string.Empty, 22);
            _label.alignment = TextAnchor.MiddleCenter;
            _label.fontStyle = FontStyle.Bold;
        }

        void RefreshLabel()
        {
            if (_label == null)
                return;
            _label.text = $"{_title}\nSon: {_lastDamage:0.#}  ·  Vuruş: {_hitCount}";
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
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = fontSize;
            t.color = new Color(0.95f, 0.88f, 0.55f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
