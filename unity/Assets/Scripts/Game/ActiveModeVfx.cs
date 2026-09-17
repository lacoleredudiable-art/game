using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Ulti visual.aura / screen_edges — oyuncu camgöbeği/mor (kırmızı-turuncu yasak).
    /// Koddan kurulur; prefab yok.
    /// </summary>
    public sealed class ActiveModeVfx : MonoBehaviour
    {
        Transform _player;
        Transform _auraRing;
        Image _edgeL;
        Image _edgeR;
        Image _edgeT;
        Image _edgeB;
        Canvas _canvas;
        Color _tint = new Color(0.45f, 0.85f, 1f, 0.55f);
        bool _active;
        float _pulse;

        public void Bind(Transform player, Transform canvasRoot)
        {
            _player = player;
            EnsureAura();
            EnsureEdges(canvasRoot);
            Hide();
        }

        public void Show(string auraLabel, string screenEdgesLabel, Color elementTint)
        {
            _tint = MapPlayerSafeTint(auraLabel, screenEdgesLabel, elementTint);
            _active = true;
            _pulse = 0f;

            if (_auraRing != null)
            {
                _auraRing.gameObject.SetActive(true);
                SetRingColor(_tint);
            }

            bool edges = !string.IsNullOrEmpty(screenEdgesLabel);
            SetEdgesVisible(edges);
            if (edges)
                SetEdgesColor(_tint);
        }

        public void Hide()
        {
            _active = false;
            if (_auraRing != null)
                _auraRing.gameObject.SetActive(false);
            SetEdgesVisible(false);
        }

        void LateUpdate()
        {
            if (!_active || _player == null)
                return;

            _pulse += Time.unscaledDeltaTime;
            if (_auraRing != null)
            {
                _auraRing.position = _player.position + Vector3.up * 0.05f;
                float s = 2.4f + 0.15f * Mathf.Sin(_pulse * 4f);
                _auraRing.localScale = new Vector3(s, 0.06f, s);
                var c = _tint;
                c.a = 0.35f + 0.2f * (0.5f + 0.5f * Mathf.Sin(_pulse * 3f));
                SetRingColor(c);
            }

            if (_edgeL != null && _edgeL.enabled)
            {
                float a = 0.25f + 0.2f * (0.5f + 0.5f * Mathf.Sin(_pulse * 2.5f));
                SetEdgesColor(new Color(_tint.r, _tint.g, _tint.b, a));
            }
        }

        void EnsureAura()
        {
            if (_auraRing != null)
                return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "UltiAuraRing";
            Object.Destroy(go.GetComponent<Collider>());
            _auraRing = go.transform;
            _auraRing.SetParent(transform, false);
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
                rend.sharedMaterial = new Material(sh);
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void EnsureEdges(Transform canvasRoot)
        {
            if (_canvas != null || canvasRoot == null)
                return;

            var go = new GameObject("UltiScreenEdges");
            go.transform.SetParent(canvasRoot, false);
            go.layer = canvasRoot.gameObject.layer;
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 40;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            _edgeL = MakeEdge(go.transform, "L", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(72f, 0f));
            _edgeR = MakeEdge(go.transform, "R", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(72f, 0f));
            _edgeT = MakeEdge(go.transform, "T", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 56f));
            _edgeB = MakeEdge(go.transform, "B", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f));
        }

        static Image MakeEdge(Transform parent, string name, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin;
            rt.anchorMax = amax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(0.4f, 0.7f, 1f, 0.3f);
            return img;
        }

        void SetEdgesVisible(bool on)
        {
            if (_edgeL != null) _edgeL.enabled = on;
            if (_edgeR != null) _edgeR.enabled = on;
            if (_edgeT != null) _edgeT.enabled = on;
            if (_edgeB != null) _edgeB.enabled = on;
        }

        void SetEdgesColor(Color c)
        {
            if (_edgeL != null) _edgeL.color = c;
            if (_edgeR != null) _edgeR.color = c;
            if (_edgeT != null) _edgeT.color = c;
            if (_edgeB != null) _edgeB.color = c;
        }

        void SetRingColor(Color c)
        {
            if (_auraRing == null)
                return;
            var rend = _auraRing.GetComponent<Renderer>();
            if (rend == null || rend.sharedMaterial == null)
                return;
            var mat = rend.sharedMaterial;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            else
                mat.color = c;
        }

        /// <summary>JSON aura metni → oyuncu paleti (kırmızı-turuncu boss tehdidine düşmez).</summary>
        public static Color MapPlayerSafeTint(string aura, string edges, Color elementTint)
        {
            string s = ((aura ?? "") + " " + (edges ?? "")).ToLowerInvariant();
            if (s.Contains("altın") || s.Contains("ışık") || s.Contains("kutsal"))
                return new Color(0.95f, 0.85f, 0.45f, 0.55f); // krem-altın
            if (s.Contains("mavi") || s.Contains("rüzgar") || s.Contains("şifa"))
                return new Color(0.35f, 0.85f, 1f, 0.55f); // camgöbeği
            if (s.Contains("kaya") || s.Contains("kahverengi"))
                return new Color(0.55f, 0.4f, 0.7f, 0.55f); // mor-taş
            if (s.Contains("kan") || s.Contains("alev") || s.Contains("yanan") || s.Contains("kırmızı"))
                return new Color(0.75f, 0.25f, 0.85f, 0.55f); // mor-magenta (kırmızı değil)
            // Element rengi de kırmızıya kaçmasın — doygunluğu mora/camgöbeğine çek.
            float max = Mathf.Max(elementTint.r, Mathf.Max(elementTint.g, elementTint.b));
            if (max < 0.01f)
                return new Color(0.45f, 0.75f, 1f, 0.55f);
            if (elementTint.r > elementTint.b && elementTint.r > elementTint.g)
                return new Color(0.7f, 0.3f, 0.9f, 0.55f);
            return new Color(elementTint.r, elementTint.g, elementTint.b, 0.55f);
        }
    }
}
