using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Tek yaşayan etkinin prosedürel çizimi — LineRenderer + az sayıda küre (overdraw yok).
    /// </summary>
    public sealed class LivingEffectView : MonoBehaviour
    {
        const int RingSegments = 48;

        LivingEffect _logic;
        ManifestationTuning _tuning;
        PrototypeTuning _colors;
        LineRenderer _line;
        Transform[] _blobs;
        Transform _needle;
        Material _lineMat;
        Material _blobMat;
        bool _scarred;

        public LivingEffect Logic => _logic;
        public bool Scarred
        {
            get => _scarred;
            set => _scarred = value;
        }

        public bool TravelHitDone { get; set; }

        public void Bind(
            LivingEffect logic,
            ManifestationTuning tuning,
            PrototypeTuning colors)
        {
            _logic = logic;
            _tuning = tuning;
            _colors = colors;
            BuildVisuals();
            SyncVisual(1f);
        }

        public void TickVisual(float dtSec)
        {
            if (_logic == null || !_logic.IsAlive)
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                return;
            }

            SyncVisual(dtSec);
        }

        void OnDestroy()
        {
            if (_lineMat != null) Destroy(_lineMat);
            if (_blobMat != null) Destroy(_blobMat);
        }

        void BuildVisuals()
        {
            _lineMat = MakeMat(_colors.InkCyan);
            _blobMat = MakeMat(_colors.InkPurple);

            var lineGo = new GameObject("WaveLine");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            _line.sharedMaterial = _lineMat;
            _line.widthMultiplier = 0.18f;
            _line.positionCount = 0;
            _line.useWorldSpace = true;
            _line.loop = false;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.numCapVertices = 2;

            int n = Mathf.Max(1, _tuning.MaxSwarmBlobs);
            _blobs = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Blob" + i;
                Object.Destroy(s.GetComponent<Collider>());
                s.transform.SetParent(transform, false);
                s.GetComponent<Renderer>().sharedMaterial = _blobMat;
                s.SetActive(false);
                _blobs[i] = s.transform;
            }

            var needle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            needle.name = "Needle";
            Object.Destroy(needle.GetComponent<Collider>());
            needle.transform.SetParent(transform, false);
            needle.GetComponent<Renderer>().sharedMaterial = _lineMat;
            needle.SetActive(false);
            _needle = needle.transform;
        }

        void SyncVisual(float dtSec)
        {
            _ = dtSec;
            var s = _logic.Current;
            float alpha = _logic.Phase == LivingEffectPhase.Fading
                ? 1f - _logic.FadeT
                : (_logic.Phase == LivingEffectPhase.Banging ? 1f : 0.95f);

            Color cyan = _colors.InkCyan;
            cyan.a = alpha;
            Color purple = _colors.InkPurple;
            purple.a = alpha;
            SetMatColor(_lineMat, Color.Lerp(cyan, purple, 0.35f + 0.4f * s.Spread));
            SetMatColor(_blobMat, purple);

            float y = 0.08f + 0.35f * s.Lift;
            Vector3 origin = new Vector3(_logic.OriginX, y, _logic.OriginZ);
            Vector3 dir = new Vector3(_logic.DirX, 0f, _logic.DirZ);
            float dist = _logic.TipDistance;

            DrawWave(origin, dir, dist, s, y);
            DrawNeedle(origin, dir, dist, s);
            DrawSwarm(origin, dir, dist, s);

            if (_logic.Phase == LivingEffectPhase.Banging)
                PulseBang(origin, dir, dist, s);
        }

        void DrawWave(Vector3 origin, Vector3 dir, float radius, EffectSilhouette s, float y)
        {
            if (_logic.Verb != Rune.Sarsinti && s.Focus < 0.2f && _logic.Verb != Rune.Suru)
            {
                _line.positionCount = 0;
                return;
            }

            // İĞNE fiilinde ana gövde iğne; dalga çizgisi yok
            if (_logic.Verb == Rune.Igne && s.Spread < 0.2f)
            {
                _line.positionCount = 0;
                return;
            }

            float focus = s.Focus;
            if (focus < 0.35f)
            {
                // Halka
                _line.loop = true;
                _line.positionCount = RingSegments;
                for (int i = 0; i < RingSegments; i++)
                {
                    float a = (i / (float)RingSegments) * Mathf.PI * 2f;
                    Vector3 p = origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    p.y = y;
                    _line.SetPosition(i, p);
                }
            }
            else if (focus < 0.75f)
            {
                // Yaya daralma — boss yönüne doğru koridor
                float halfArc = Mathf.Lerp(Mathf.PI, 0.35f, (focus - 0.35f) / 0.4f);
                float facing = Mathf.Atan2(dir.x, dir.z);
                int segs = 24;
                _line.loop = false;
                _line.positionCount = segs;
                for (int i = 0; i < segs; i++)
                {
                    float t = i / (segs - 1f);
                    float a = facing - halfArc + t * (halfArc * 2f);
                    Vector3 p = origin + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
                    p.y = y;
                    _line.SetPosition(i, p);
                }
            }
            else
            {
                // Tek hat (fay hattı)
                _line.loop = false;
                _line.positionCount = 2;
                Vector3 tip = origin + dir * radius;
                tip.y = y;
                _line.SetPosition(0, origin);
                _line.SetPosition(1, tip);
                _line.widthMultiplier = Mathf.Lerp(0.35f, 0.12f, s.Pierce);
            }

            if (_logic.Verb == Rune.Sarsinti)
                _line.widthMultiplier = Mathf.Lerp(0.22f, 0.1f, focus);
        }

        void DrawNeedle(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            bool show = _logic.Verb == Rune.Igne || s.Pierce > 0.45f;
            if (!show || _needle == null)
            {
                if (_needle != null) _needle.gameObject.SetActive(false);
                return;
            }

            _needle.gameObject.SetActive(true);
            Vector3 tip = origin + dir * dist;
            _needle.position = tip;
            if (dir.sqrMagnitude > 1e-4f)
                _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            float thick = Mathf.Lerp(0.35f, 0.14f, s.Pierce);
            float len = 0.7f + 0.5f * s.Pierce;
            _needle.localScale = new Vector3(thick, len * 0.5f, thick);
        }

        void DrawSwarm(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            int count = Mathf.RoundToInt(s.Spread * _tuning.MaxSwarmBlobs);
            count = Mathf.Clamp(count, 0, _blobs.Length);
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            if (right.sqrMagnitude < 1e-4f)
                right = Vector3.right;

            for (int i = 0; i < _blobs.Length; i++)
            {
                if (i >= count)
                {
                    _blobs[i].gameObject.SetActive(false);
                    continue;
                }

                _blobs[i].gameObject.SetActive(true);
                float u = (i + 1) / (count + 1f);
                float along = dist * u;
                float side = (i % 2 == 0 ? 1f : -1f) * (0.35f + (1f - s.Focus) * 1.4f)
                             * (0.4f + s.Spread);
                // Odaklıysa hat boyunca diz; değilse yayvan halka
                Vector3 p;
                if (s.Focus > 0.45f || _logic.Verb == Rune.Igne)
                    p = origin + dir * along + right * side * (1f - s.Focus * 0.7f);
                else
                {
                    float a = u * Mathf.PI * 2f + _logic.AgeSec * 2.2f;
                    float r = dist * (0.35f + 0.55f * u);
                    p = origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                }

                p.y = 0.35f + 0.5f * s.Lift * Mathf.Abs(Mathf.Sin(_logic.AgeSec * 6f + i));
                _blobs[i].position = p;
                float sc = 0.28f + 0.12f * s.Spread;
                _blobs[i].localScale = Vector3.one * sc;
            }
        }

        void PulseBang(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            float pulse = 1f + 0.8f * Mathf.Sin(_logic.BangAgeSec * 28f);
            _line.widthMultiplier *= pulse;
            if (_needle != null && _needle.gameObject.activeSelf)
                _needle.localScale *= 1f + 0.15f * pulse;
        }

        static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            SetMatColor(mat, c);
            return mat;
        }

        static void SetMatColor(Material mat, Color c)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
        }
    }
}
