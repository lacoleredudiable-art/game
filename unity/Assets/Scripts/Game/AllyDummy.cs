using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Takım arkadaşı dummy — heal denemesi için. Başlangıç can oranı varsayılan %50.
    /// Dünya üstü bar + HUD (VitalsHud) birlikte okunur.
    /// </summary>
    public sealed class AllyDummy : MonoBehaviour
    {
        int _hp;
        int _maxHp;
        Text _label;
        Image _fill;
        Transform _billboard;
        Transform _cam;

        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public float Ratio => _maxHp > 0 ? (float)_hp / _maxHp : 0f;

        public void Bind(int maxHp, float startRatio = 0.5f)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _hp = Mathf.Clamp(Mathf.RoundToInt(_maxHp * Mathf.Clamp01(startRatio)), 1, _maxHp);
            EnsureBillboard();
            RefreshLabel();
        }

        public int ApplyHeal(int amount)
        {
            if (amount <= 0 || _hp >= _maxHp)
                return 0;
            int before = _hp;
            _hp = Mathf.Min(_maxHp, _hp + amount);
            RefreshLabel();
            return _hp - before;
        }

        public bool ApplyDamage(int amount)
        {
            if (amount <= 0 || _hp <= 0)
                return false;
            _hp = Mathf.Max(0, _hp - amount);
            RefreshLabel();
            return _hp <= 0;
        }

        void EnsureBillboard()
        {
            if (_billboard != null)
                return;

            var root = new GameObject("AllyHpBillboard");
            root.transform.SetParent(transform, false);
            // Kapsül merkezi + görsel offset — kafanın üstü.
            root.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            _billboard = root.transform;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 56f);
            root.transform.localScale = Vector3.one * 0.012f;

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);
            bg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.04f, 0.2f);
            fillRt.anchorMax = new Vector2(0.96f, 0.55f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _fill = fillGo.AddComponent<Image>();
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.color = new Color(0.35f, 1f, 0.55f, 1f);
            _fill.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.45f);
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(4f, 0f);
            tr.offsetMax = new Vector2(-4f, -2f);
            _label = textGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_label.font == null)
                _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontSize = 28;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;
        }

        void RefreshLabel()
        {
            if (_label != null)
                _label.text = "ALLY " + _hp + "/" + _maxHp;
            if (_fill != null)
                _fill.fillAmount = Ratio;
        }

        void LateUpdate()
        {
            if (_billboard == null)
                return;
            if (_cam == null && Camera.main != null)
            {
                _cam = Camera.main.transform;
                var canvas = _billboard.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.worldCamera = Camera.main;
            }
            if (_cam != null)
                _billboard.rotation = Quaternion.LookRotation(
                    _billboard.position - _cam.position, Vector3.up);
        }
    }
}
