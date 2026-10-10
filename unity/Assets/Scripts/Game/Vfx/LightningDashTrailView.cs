using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.1 ATIL: yol boyunca ince kırık şimşek şerit (0,25 s).
    /// </summary>
    public sealed class LightningDashTrailView : MonoBehaviour
    {
        LineRenderer _line;
        Material _mat;
        float _life;
        float _age;
        Vector3 _from;
        Vector3 _to;
        Color _core;

        public void Play(Vector3 from, Vector3 to, in VfxColorRgb color, float lifeSec)
        {
            Ensure();
            _from = from;
            _to = to;
            _core = new Color(
                color.R * RuleVfxArtDefaults.CoreHdrHot,
                color.G * RuleVfxArtDefaults.CoreHdrHot,
                color.B * RuleVfxArtDefaults.CoreHdrHot,
                1f);
            _life = Mathf.Max(RuleVfxArtDefaults.MeshLifeMin, lifeSec);
            _age = 0f;
            RebuildJag();
            _line.enabled = true;
            Apply(1f);
        }

        public void ExtendTip(Vector3 tip)
        {
            if (_line == null || !_line.enabled)
                return;
            _to = tip;
            RebuildJag();
        }

        void Update()
        {
            if (_line == null || !_line.enabled)
                return;
            _age += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(_age / _life);
            Apply(k);
            if (_age >= _life)
                _line.enabled = false;
        }

        void Ensure()
        {
            if (_line != null)
                return;
            var go = new GameObject("LightningDashTrail");
            go.transform.SetParent(transform, false);
            _line = go.AddComponent<LineRenderer>();
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.useWorldSpace = true;
            _line.alignment = LineAlignment.View;
            _line.textureMode = LineTextureMode.Stretch;
            _line.widthMultiplier = RuleVfxDefaults.TrailWidthM;
            _line.positionCount = RuleVfxDefaults.LightningSegments;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            _mat = new Material(sh);
            _line.sharedMaterial = _mat;
            _line.enabled = false;
        }

        void RebuildJag()
        {
            Vector3 delta = _to - _from;
            Vector3 side = Vector3.Cross(Vector3.up, delta.normalized);
            if (side.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                side = Vector3.right;
            side.Normalize();
            int n = RuleVfxDefaults.LightningSegments;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(_from, _to, t);
                if (i > 0 && i < n - 1)
                {
                    float jag = (((i * RuleVfxArtDefaults.LightningJagHashMul) % RuleVfxArtDefaults.LightningJagMod)
                        / RuleVfxArtDefaults.LightningJagDiv - 0.5f) * 2f * RuleVfxDefaults.LightningJagM;
                    p += side * jag;
                    p.y += RuleVfxDefaults.TrailHeightM * RuleVfxArtDefaults.LightningTipLift
                        + Mathf.Abs(jag) * RuleVfxArtDefaults.LightningJagLift;
                }
                else
                    p.y += RuleVfxDefaults.TrailHeightM * RuleVfxArtDefaults.LightningEndLift;
                _line.SetPosition(i, p);
            }
        }

        void Apply(float alpha)
        {
            Color c = _core;
            c.a = alpha;
            _line.startColor = c;
            _line.endColor = new Color(c.r, c.g, c.b, alpha * RuleVfxArtDefaults.LightningEndAlpha);
            _line.widthMultiplier = RuleVfxDefaults.TrailWidthM
                * Mathf.Lerp(RuleVfxArtDefaults.LightningWidthMin, 1f, alpha);
            if (_mat != null)
            {
                if (_mat.HasProperty("_CoreColor"))
                    _mat.SetColor("_CoreColor", c);
                if (_mat.HasProperty("_Intensity"))
                    _mat.SetFloat("_Intensity", RuleVfxArtDefaults.LightningIntensityBase + alpha);
                if (_mat.HasProperty("_Color"))
                    _mat.SetColor("_Color", c);
            }
        }

        void OnDestroy()
        {
            if (_mat != null)
                Destroy(_mat);
        }
    }
}
