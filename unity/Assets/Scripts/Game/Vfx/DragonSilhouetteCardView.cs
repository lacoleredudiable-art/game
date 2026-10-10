using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.4: tek mesh kartı + dissolve. Kılıç ATIL → atlas hücre F (kuyruk yayı),
    /// 0,25 s, atılış yolu boyunca. Yer tutucu atlas (prosedürel alpha).
    /// Ekranda en çok 2; havuz bu bileşenin örneğiyle sınırlı.
    /// </summary>
    public sealed class DragonSilhouetteCardView : MonoBehaviour
    {
        static int _liveCount;

        MeshRenderer _mr;
        Material _mat;
        Texture2D _atlas;
        float _life;
        float _age;
        bool _active;

        public static int LiveCount => _liveCount;

        public void PlayAlongLine(Vector3 from, Vector3 to, in VfxColorRgb color, float lifeSec, int atlasCell)
        {
            if (_liveCount >= VfxPlanDefaults.EjderSiluetEkranMax && !_active)
                return;

            Ensure();
            Vector3 mid = (from + to) * 0.5f;
            mid.y += RuleVfxDefaults.SilhouetteLiftM;
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = transform.forward;
            dir.Normalize();
            transform.position = mid;
            // Yatay an: kesiş düzlemine yatırılmış kart (§3.4).
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up)
                * Quaternion.Euler(90f, 0f, 0f);
            float len = Mathf.Clamp(Vector3.Distance(from, to), 1.2f, RuleVfxDefaults.SilhouetteWidthM);
            transform.localScale = new Vector3(len, RuleVfxDefaults.SilhouetteHeightM, 1f);

            _life = Mathf.Max(0.05f, lifeSec);
            _age = 0f;
            if (!_active)
            {
                _active = true;
                _liveCount++;
            }

            ApplyAtlasCell(atlasCell);
            Color c = new Color(color.R * 2f, color.G * 2f, color.B * 2f, 1f);
            if (_mat.HasProperty("_CoreColor"))
                _mat.SetColor("_CoreColor", c);
            if (_mat.HasProperty("_Intensity"))
                _mat.SetFloat("_Intensity", color.Intensity);
            if (_mat.HasProperty("_Dissolve"))
                _mat.SetFloat("_Dissolve", 1f);
            _mr.enabled = true;
        }

        void Update()
        {
            if (!_active)
                return;
            _age += Time.deltaTime;
            float u = Mathf.Clamp01(_age / _life);
            // 0,05 s kenardan yanarak belirir; son %40 dissolve ile söner.
            float dissolve;
            if (_age < VfxPlanDefaults.EjderDissolveInSec)
                dissolve = 1f - Mathf.Clamp01(_age / VfxPlanDefaults.EjderDissolveInSec);
            else if (u > 1f - VfxPlanDefaults.EjderDissolveOutFrac)
            {
                float outU = (u - (1f - VfxPlanDefaults.EjderDissolveOutFrac))
                    / VfxPlanDefaults.EjderDissolveOutFrac;
                dissolve = outU;
            }
            else
                dissolve = 0f;

            if (_mat != null && _mat.HasProperty("_Dissolve"))
                _mat.SetFloat("_Dissolve", dissolve);

            if (_age >= _life)
                Release();
        }

        void Release()
        {
            if (!_active)
                return;
            _active = false;
            _liveCount = Mathf.Max(0, _liveCount - 1);
            if (_mr != null)
                _mr.enabled = false;
        }

        void OnDisable() => Release();

        void Ensure()
        {
            if (_mr != null)
                return;
            gameObject.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mr.shadowCastingMode = ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            _mat = new Material(sh);
            _atlas = BuildPlaceholderAtlas();
            if (_mat.HasProperty("_MainTex"))
                _mat.SetTexture("_MainTex", _atlas);
            if (_mat.HasProperty("_DissolveTex"))
                _mat.SetTexture("_DissolveTex", _atlas);
            _mr.sharedMaterial = _mat;
            _mr.enabled = false;
        }

        void ApplyAtlasCell(int cell)
        {
            // 1024×512, 8 hücre 256² — UV satırı tek.
            int idx = Mathf.Clamp(cell, 0, 7);
            float u0 = idx / 8f;
            float u1 = (idx + 1) / 8f;
            if (_mat != null && _mat.HasProperty("_MainTex_ST"))
                _mat.SetVector("_MainTex_ST", new Vector4(u1 - u0, 1f, u0, 0f));
        }

        static Texture2D BuildPlaceholderAtlas()
        {
            const int w = 512;
            const int h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "DragonSilhouetteAtlas_Placeholder",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[w * h];
            for (int cell = 0; cell < 8; cell++)
            {
                int x0 = cell * (w / 8);
                int x1 = x0 + w / 8;
                for (int y = 0; y < h; y++)
                for (int x = x0; x < x1; x++)
                {
                    float nx = (x - x0) / (float)(w / 8) * 2f - 1f;
                    float ny = y / (float)h * 2f - 1f;
                    // Hücre F (5): kuyruk yayı silüeti — kavisli alpha.
                    float arc = cell == 5
                        ? Mathf.Exp(-Mathf.Pow(ny - 0.15f * Mathf.Sin(nx * 3.2f), 2f) * 18f)
                          * Mathf.Exp(-nx * nx * 1.2f)
                        : Mathf.Exp(-(nx * nx + ny * ny) * 3.5f);
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(arc * 255f), 0, 255);
                    pixels[y * w + x] = new Color32(255, 200, 120, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        void OnDestroy()
        {
            Release();
            if (_mat != null)
                Destroy(_mat);
            if (_atlas != null)
                Destroy(_atlas);
        }
    }
}
