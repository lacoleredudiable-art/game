using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// YERE ÇAKMA telegrafı: hazırlık pozu + yer diski + yükselen ses (§11).
    /// Sıcak telegraf rengi kullanılır. Varyant tell'leri windup'ta okunur:
    /// GEÇ = daha yavaş ton + uzun tutulan poz; GENİŞ = disk baştan büyük.
    ///
    /// Disk boss transform'unun ÇOCUĞU DEĞİL (T8.1): bossun (1.7, 1.3, 1.7) ölçeği ve
    /// hazırlık squash'ı diski çarpıyor, etki yarıçapı ekranda yalan söylüyordu.
    /// Mesh Cylinder: yerde yatan gerçek daire, yarıçapı doğrudan okunuyor.
    /// </summary>
    public sealed class BossTelegraph : MonoBehaviour
    {
        const float DiscHeightY = 0.03f;
        const float DiscThicknessScale = 0.02f;

        const int ConeSegments = 28;

        PrototypeTuning _colors;
        Transform _bossXform;
        Transform _disc;
        Material _discMat;
        Transform _cone;
        Mesh _coneMesh;
        float _coneMeshArc = -1f;
        float _arcHalfDeg = 180f;
        AudioSource _tone;
        AudioClip _clip;
        Vector3 _baseScale;

        public void Bind(PrototypeTuning colors, BossTuning boss, Transform bossXform)
        {
            _colors = colors;
            _bossXform = bossXform;
            _baseScale = bossXform.localScale;
            BuildDisc();
            BuildTone();
            Hide();
        }

        /// <summary>
        /// Windup başında etki hacminin şekli: 180 = tam daire (Slam), daha küçük = koni (FireCone).
        /// Koni yönü windup başında kilitlenen boss bakışıdır (ResolveStrike ile aynı eksen).
        /// </summary>
        public void SetShape(float arcHalfAngleDeg) => _arcHalfDeg = arcHalfAngleDeg;

        /// <summary>
        /// Windup: p 0→1. radiusM aktif varyantın etki yarıçapı.
        /// GENİŞ disk baştan tam boyutta; GEÇ tonu yavaş yükselir, poz erken gerilip tutulur.
        /// </summary>
        public void SetProgress(float progress01, float radiusM, SlamVariant variant)
        {
            float p = Mathf.Clamp01(progress01);

            float drawnRadius = variant == SlamVariant.Genis
                ? radiusM
                : radiusM * Mathf.Max(0.12f, p);

            DrawDisc(
                drawnRadius,
                Color.Lerp(_colors.TelegraphWarm, _colors.TelegraphHot, p),
                0.35f + 0.5f * p);

            // GEÇ: hazırlık pozu daha erken dolup uzun tutulur (windup zaten 900 ms).
            float poseT = variant == SlamVariant.Gec
                ? Mathf.Clamp01(p * 1.35f)
                : p;
            ApplyPose(1f - _colors.TelegraphSquash * poseT, 1f + _colors.TelegraphStretch * poseT);

            if (_tone != null)
            {
                if (!_tone.isPlaying)
                    _tone.Play();

                // GEÇ: ton progress'e göre daha yavaş yükselir (concave eğri + uzun windup).
                float toneT = variant == SlamVariant.Gec ? p * p : p;
                _tone.pitch = Mathf.Lerp(_colors.TelegraphTonePitchMin, _colors.TelegraphTonePitchMax, toneT);
                _tone.volume = Mathf.Lerp(_colors.TelegraphToneVolumeMin, _colors.TelegraphToneVolumeMax, toneT);
            }
        }

        /// <summary>
        /// Çakma anı. Gerilmiş poz vururken kalmamalı — aşağı squash (T8.1); eskiden aktif
        /// pencere boyunca boss hâlâ "hazırlanıyor" pozundaydı.
        /// </summary>
        public void Slam(float radiusM)
        {
            DrawDisc(radiusM, _colors.TelegraphHot, 0.9f);
            ApplySlamPose(1f);
            if (_tone != null && _tone.isPlaying)
                _tone.Stop();
        }

        /// <summary>Toparlanma: t 1→0. Poz tabana döner, disk söner.</summary>
        public void Recover(float t01, float radiusM)
        {
            float t = Mathf.Clamp01(t01);
            if (t <= 0.02f)
            {
                Hide();
                return;
            }

            DrawDisc(radiusM, _colors.TelegraphHot, 0.5f * t);
            ApplySlamPose(t);
        }

        public void Hide()
        {
            if (_disc != null && _disc.gameObject.activeSelf)
                _disc.gameObject.SetActive(false);
            if (_cone != null && _cone.gameObject.activeSelf)
                _cone.gameObject.SetActive(false);
            ApplyPose(1f, 1f);
            if (_tone != null && _tone.isPlaying)
                _tone.Stop();
        }

        void OnDestroy()
        {
            if (_disc != null)
                Destroy(_disc.gameObject);
            if (_cone != null)
                Destroy(_cone.gameObject);
            if (_coneMesh != null)
                Destroy(_coneMesh);
            if (_discMat != null)
                Destroy(_discMat);
            if (_clip != null)
                Destroy(_clip);
        }

        void DrawDisc(float radiusM, Color color, float alpha)
        {
            if (_disc == null || _bossXform == null)
                return;

            if (_arcHalfDeg < 180f)
            {
                DrawCone(radiusM, color, alpha);
                return;
            }

            if (_cone != null && _cone.gameObject.activeSelf)
                _cone.gameObject.SetActive(false);
            if (!_disc.gameObject.activeSelf)
                _disc.gameObject.SetActive(true);

            _disc.position = new Vector3(_bossXform.position.x, DiscHeightY, _bossXform.position.z);
            _disc.localScale = new Vector3(radiusM * 2f, DiscThicknessScale, radiusM * 2f);
            Color c = color;
            c.a = alpha;
            SetMatColor(_discMat, c);
        }

        void DrawCone(float radiusM, Color color, float alpha)
        {
            if (_disc.gameObject.activeSelf)
                _disc.gameObject.SetActive(false);
            if (_cone == null)
                BuildCone();
            if (!Mathf.Approximately(_coneMeshArc, _arcHalfDeg))
                RebuildConeMesh(_arcHalfDeg);
            if (!_cone.gameObject.activeSelf)
                _cone.gameObject.SetActive(true);

            Vector3 fwd = _bossXform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Vector3.forward;
            _cone.SetPositionAndRotation(
                new Vector3(_bossXform.position.x, DiscHeightY, _bossXform.position.z),
                Quaternion.LookRotation(fwd.normalized, Vector3.up));
            _cone.localScale = new Vector3(radiusM, 1f, radiusM);
            Color c = color;
            c.a = alpha;
            SetMatColor(_discMat, c);
        }

        void BuildCone()
        {
            var go = new GameObject("FireConeTelegraph");
            _coneMesh = new Mesh { name = "TelegraphCone" };
            go.AddComponent<MeshFilter>().sharedMesh = _coneMesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = _discMat;
            _cone = go.transform;
        }

        /// <summary>Birim yarıçaplı yelpaze: +Z ekseni etrafında ±arcHalfDeg.</summary>
        void RebuildConeMesh(float arcHalfDeg)
        {
            _coneMeshArc = arcHalfDeg;
            var verts = new Vector3[ConeSegments + 2];
            var tris = new int[ConeSegments * 3];
            verts[0] = Vector3.zero;
            float half = arcHalfDeg * Mathf.Deg2Rad;
            for (int i = 0; i <= ConeSegments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)ConeSegments);
                verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
            for (int i = 0; i < ConeSegments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            _coneMesh.Clear();
            _coneMesh.vertices = verts;
            _coneMesh.triangles = tris;
            _coneMesh.RecalculateNormals();
            _coneMesh.RecalculateBounds();
        }

        void ApplyPose(float xz, float y)
        {
            if (_bossXform == null)
                return;

            _bossXform.localScale = new Vector3(_baseScale.x * xz, _baseScale.y * y, _baseScale.z * xz);
        }

        void ApplySlamPose(float t)
        {
            float squash = _colors.TelegraphSlamSquash * t;
            ApplyPose(1f + squash * 0.5f, 1f - squash);
        }

        void BuildDisc()
        {
            var go = new GameObject("SlamDisc");
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Cylinder);
            var rend = go.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            _discMat = MakeDiscMat();
            rend.sharedMaterial = _discMat;
            _disc = go.transform;
        }

        void BuildTone()
        {
            _tone = gameObject.AddComponent<AudioSource>();
            _tone.playOnAwake = false;
            _tone.loop = true;
            _tone.spatialBlend = 0.35f;
            _clip = MakeTone(220);
            _tone.clip = _clip;
        }

        Material MakeDiscMat()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            SetMatColor(mat, _colors.TelegraphWarm);
            return mat;
        }

        static void SetMatColor(Material mat, Color c)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
        }

        static AudioClip MakeTone(int hz)
        {
            const int sampleRate = 44100;
            int samples = sampleRate;
            var clip = AudioClip.Create("TelegraphTone", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                data[i] = 0.35f * Mathf.Sin(2f * Mathf.PI * hz * t);
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
