using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Prosedürel efekt ömrü: halka genişlemesi (LineRenderer) veya düz yüzey solması
    /// (MeshRenderer), sonra yok olur. Unscaled değil — hitstop'ta efekt de donar.
    /// </summary>
    public sealed class FxTweenView : MonoBehaviour
    {
        const int RingSegments = 40;

        LineRenderer _ring;
        Renderer _surface;
        MaterialPropertyBlock _mpb;
        Color _color;
        float _radius;
        float _width;
        float _grow;
        float _hold;
        float _fade;
        float _age;

        public static FxTweenView Ring(LineRenderer ring, Color color, float radiusM, float widthM, float growSec)
        {
            var t = ring.gameObject.AddComponent<FxTweenView>();
            t._ring = ring;
            t._color = color;
            t._radius = radiusM;
            t._width = widthM;
            t._grow = Mathf.Max(VfxDefaults.FxTweenGrowMinSec, growSec);
            ring.positionCount = RingSegments;
            ring.loop = true;
            ring.useWorldSpace = false;
            t.Apply();
            return t;
        }

        public static FxTweenView Surface(Renderer surface, Color color, float holdSec, float fadeSec)
        {
            var t = surface.gameObject.AddComponent<FxTweenView>();
            t._surface = surface;
            t._color = color;
            t._hold = Mathf.Max(0f, holdSec);
            t._fade = Mathf.Max(VfxDefaults.FxTweenFadeMinSec, fadeSec);
            t.Apply();
            return t;
        }

        void Update()
        {
            _age += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (_ring != null)
            {
                float k = Mathf.Clamp01(_age / _grow);
                float r = _radius * (1f - (1f - k) * (1f - k));
                for (int i = 0; i < RingSegments; i++)
                {
                    float a = i / (float)RingSegments * Mathf.PI * 2f;
                    _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                Color c = _color;
                c.a *= 1f - k;
                _ring.startColor = c;
                _ring.endColor = c;
                _ring.widthMultiplier = _width * Mathf.Lerp(1f, VfxDefaults.RingTweenWidthMult, k);
                if (k >= 1f)
                    Destroy(gameObject);
                return;
            }

            if (_surface != null)
            {
                float fadeK = Mathf.Clamp01((_age - _hold) / _fade);
                Color c = _color;
                c.a *= 1f - fadeK;
                _mpb ??= new MaterialPropertyBlock();
                _surface.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", c);
                _mpb.SetColor("_Color", c);
                _surface.SetPropertyBlock(_mpb);
                if (fadeK >= 1f)
                    Destroy(gameObject);
            }
        }
    }
}
