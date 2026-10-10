using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.4: tek mesh kartı + dissolve. Kılıç ATIL → atlas hücre F (kuyruk yayı),
    /// 0,25 s, atılış yolu boyunca. Yer tutucu atlas (prosedürel alpha).
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
            if (dir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                dir = transform.forward;
            dir.Normalize();
            transform.position = mid;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up)
                * Quaternion.Euler(90f, 0f, 0f);
            float len = Mathf.Clamp(
                Vector3.Distance(from, to),
                RuleVfxDefaults.SilhouetteMinLenM,
                RuleVfxDefaults.SilhouetteWidthM);
            transform.localScale = new Vector3(len, RuleVfxDefaults.SilhouetteHeightM, 1f);

            _life = Mathf.Max(RuleVfxArtDefaults.MeshLifeMin, lifeSec);
            _age = 0f;
            if (!_active)
            {
                _active = true;
                _liveCount++;
            }

            ApplyAtlasCell(atlasCell);
            Color c = new Color(
                color.R * RuleVfxArtDefaults.CoreHdrMult,
                color.G * RuleVfxArtDefaults.CoreHdrMult,
                color.B * RuleVfxArtDefaults.CoreHdrMult,
                1f);
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
            _atlas = RuleVfxArtDefaults.BuildSilhouetteAtlas();
            if (_mat.HasProperty("_MainTex"))
                _mat.SetTexture("_MainTex", _atlas);
            if (_mat.HasProperty("_DissolveTex"))
                _mat.SetTexture("_DissolveTex", _atlas);
            _mr.sharedMaterial = _mat;
            _mr.enabled = false;
        }

        void ApplyAtlasCell(int cell)
        {
            int idx = Mathf.Clamp(cell, 0, RuleVfxArtDefaults.SilhouetteCells - 1);
            float u0 = idx / RuleVfxArtDefaults.SilhouetteUvScale;
            float u1 = (idx + 1) / RuleVfxArtDefaults.SilhouetteUvScale;
            if (_mat != null && _mat.HasProperty("_MainTex_ST"))
                _mat.SetVector("_MainTex_ST", new Vector4(u1 - u0, 1f, u0, 0f));
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
