using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Tek yaşayan etkinin prosedürel çizimi — LineRenderer + az sayıda küre/kapsül.
    /// T14: ayrım hareket karakterinden (İĞNE fırlar, SÜRÜ üşüşür, SARSINTI yükselir);
    /// düz vuruşun ayrı jab silüeti var. Overdraw yok (özet §6).
    /// </summary>
    public sealed class LivingEffectView : MonoBehaviour
    {
        const int RingSegments = 48;
        const int NeedleAfterimageCount = 2;

        LivingEffect _logic;
        ManifestationTuning _tuning;
        PrototypeTuning _colors;
        LineRenderer _line;
        float _lineBaseWidth;
        Transform[] _blobs;
        Transform _needle;
        Transform[] _needleGhosts;
        Material _lineMat;
        Material _blobMat;
        Material _ghostMat;
        bool _scarred;
        bool _basicStrike;
        float _windowRemaining01 = 1f;
        bool _hasSkillTint;
        Color _skillLine = Color.cyan;
        Color _skillBlob = Color.magenta;

        public LivingEffect Logic => _logic;
        public bool IsBasicStrike => _basicStrike;
        public bool Scarred
        {
            get => _scarred;
            set => _scarred = value;
        }

        public bool TravelHitDone { get; set; }

        public void Bind(
            LivingEffect logic,
            ManifestationTuning tuning,
            PrototypeTuning colors,
            bool basicStrike = false)
        {
            _logic = logic;
            _tuning = tuning;
            _colors = colors;
            _basicStrike = basicStrike;
            _hasSkillTint = false;
            BuildVisuals();
            SyncVisual(1f);
        }

        /// <summary>SkillMotor aile rengi — şekil aynı kalsa bile iş ayrımı okunur.</summary>
        public void SetSkillTint(Color line, Color blob)
        {
            _hasSkillTint = true;
            _skillLine = line;
            _skillBlob = blob;
            if (_lineMat != null)
                SetMatColor(_lineMat, line);
            if (_blobMat != null)
                SetMatColor(_blobMat, blob);
            if (_ghostMat != null)
                SetMatColor(_ghostMat, line);
        }

        /// <summary>
        /// İptal penceresinin kalan oranı (1 = taze, 0 = kapanıyor). §8/T2: dalganın
        /// Travel/MaxRange'si ile birleşip nabız/solma ipucu olur.
        /// </summary>
        public void SetWindowCue(float remaining01)
        {
            _windowRemaining01 = Mathf.Clamp01(remaining01);
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
            if (_ghostMat != null) Destroy(_ghostMat);
        }

        void BuildVisuals()
        {
            _lineMat = MakeMat(_colors.InkCyan);
            _blobMat = MakeMat(_colors.InkPurple);
            _ghostMat = MakeMat(_colors.InkCyan);

            var lineGo = new GameObject("WaveLine");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            _line.sharedMaterial = _lineMat;
            _lineBaseWidth = _colors.EffectLineWidthDefaultM;
            _line.widthMultiplier = _lineBaseWidth;
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
                // Ezilmiş küre = enerji damlası (eski sert top sürü değil).
                var s = CreateMeshObject("Wisp" + i, PrimitiveType.Sphere);
                s.GetComponent<Renderer>().sharedMaterial = _blobMat;
                s.SetActive(false);
                _blobs[i] = s.transform;
            }

            var needle = CreateMeshObject("Needle", PrimitiveType.Capsule);
            needle.GetComponent<Renderer>().sharedMaterial = _lineMat;
            needle.SetActive(false);
            _needle = needle.transform;

            _needleGhosts = new Transform[NeedleAfterimageCount];
            for (int i = 0; i < NeedleAfterimageCount; i++)
            {
                var g = CreateMeshObject("NeedleGhost" + i, PrimitiveType.Capsule);
                g.GetComponent<Renderer>().sharedMaterial = _ghostMat;
                g.SetActive(false);
                _needleGhosts[i] = g.transform;
            }

            EnsureBangBurst();
        }

        ParticleSystem _bangPs;

        void EnsureBangBurst()
        {
            if (_bangPs != null)
                return;
            var go = new GameObject("BangBurst");
            go.transform.SetParent(transform, false);
            _bangPs = go.AddComponent<ParticleSystem>();
            var main = _bangPs.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.startLifetime = 0.35f;
            main.startSpeed = 3.5f;
            main.startSize = 0.22f;
            main.maxParticles = 36;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = _bangPs.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var sh = _bangPs.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;
            var col = _bangPs.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(_colors != null ? _colors.InkCyan : Color.cyan, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = grad;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
                pr.sharedMaterial = new Material(shader);
        }

        /// <summary>
        /// Mesh'i doğrudan ata (MeshFilter+MeshRenderer) — GameObject.CreatePrimitive'in
        /// otomatik eklediği Collider hiç oluşmaz. Teknoloji kararları §4: fizik dışarıda,
        /// bir karelik Destroy edilmiş collider bile yanlış kullanıma davet çıkarır.
        /// </summary>
        GameObject CreateMeshObject(string name, PrimitiveType type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(type);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        void SyncVisual(float dtSec)
        {
            _ = dtSec;
            var s = _logic.Current;
            float alpha = _logic.Phase == LivingEffectPhase.Fading
                ? 1f - _logic.FadeT
                : (_logic.Phase == LivingEffectPhase.Banging ? 1f : 0.95f);

            float travel01 = _logic.MaxRange > 0.01f
                ? Mathf.Clamp01(_logic.Travel / _logic.MaxRange)
                : 0f;
            float urgent = 1f - _windowRemaining01;
            if (_windowRemaining01 > _colors.WindowCueUrgentRatio)
                urgent *= 0.45f;
            float place = Mathf.Max(urgent, travel01 * 0.35f);
            float hz = Mathf.Lerp(_colors.WindowCuePulseHz, _colors.WindowCueUrgentHz, place);
            // Nabız AŞAĞI modüle eder: yukarı çarpmak taban alfa 0.95 iken Clamp01'e takılıyor
            // ve ipucu hiç görünmüyordu (T8.1). §8/T2 "pencereyi dalgadan oku" buna bağlı.
            float wave = 0.5f + 0.5f * Mathf.Sin(_logic.AgeSec * hz * Mathf.PI * 2f);
            alpha = Mathf.Clamp01(alpha * (1f - _colors.WindowCuePulseAmp * place * wave));

            Color cyan = _hasSkillTint ? _skillLine : _colors.InkCyan;
            cyan.a = alpha;
            Color purple = _hasSkillTint ? _skillBlob : _colors.InkPurple;
            purple.a = alpha;
            SetMatColor(_lineMat, Color.Lerp(cyan, purple, _hasSkillTint ? 0.2f : 0.35f + 0.4f * s.Spread));
            SetMatColor(_blobMat, purple);

            Color ghost = cyan;
            ghost.a = alpha * _colors.EffectNeedleAfterimageAlpha;
            SetMatColor(_ghostMat, ghost);

            // Düz vuruş: kısa jab — halka/sürü/iğne cümle silüetlerinden ayrı.
            if (_basicStrike)
            {
                HideSwarm();
                HideNeedleGhosts();
                DrawBasicStrike(s, alpha);
                return;
            }

            // SARSINTI yerden yükselir; diğer fiillerde Lift hâlâ hafif yükseltir.
            float y = EffectHeight(s, travel01);
            Vector3 origin = new Vector3(_logic.OriginX, y, _logic.OriginZ);
            Vector3 dir = new Vector3(_logic.DirX, 0f, _logic.DirZ);
            float dist = _logic.TipDistance;

            DrawWave(origin, dir, dist, s, y);
            DrawNeedle(origin, dir, dist, s, travel01);
            DrawSwarm(origin, dir, dist, s);

            if (_logic.Phase == LivingEffectPhase.Banging)
                PulseBang(origin, dir, dist, s);
        }

        float EffectHeight(EffectSilhouette s, float travel01)
        {
            if (_logic.Verb == Rune.Toprak)
            {
                // Aşağıdan yukarı: genişlerken yükselir (kütle / yerden çıkış).
                float peak = _tuning.WaveRiseHeightM * (0.55f + 0.9f * s.Lift);
                return Mathf.Lerp(_colors.EffectSarsintiGroundY, peak, travel01);
            }

            return 0.08f + 0.35f * s.Lift;
        }

        void DrawBasicStrike(EffectSilhouette s, float alpha)
        {
            _ = s;
            _ = alpha;
            _line.positionCount = 0;
            _lineBaseWidth = _colors.EffectLineWidthDefaultM;

            if (_needle == null)
                return;

            Vector3 dir = new Vector3(_logic.DirX, 0f, _logic.DirZ);
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;

            float dist = Mathf.Min(_logic.TipDistance, _tuning.BasicStrikeRangeM);
            float y = _colors.EffectBasicStrikeHeightM;
            Vector3 origin = new Vector3(_logic.OriginX, y, _logic.OriginZ);
            // Uç, kısa menzilin ortasına yakın — "tek vuruşluk jab", uçan iğne değil.
            Vector3 tip = origin + dir * Mathf.Max(0.35f, dist * 0.55f);

            _needle.gameObject.SetActive(true);
            _needle.position = tip;
            _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            float thick = _colors.EffectBasicStrikeThickM;
            float len = _colors.EffectBasicStrikeLenM;
            if (_logic.Phase == LivingEffectPhase.Banging)
                len *= 1f + 0.25f * Mathf.Sin(_logic.BangAgeSec * 40f);
            _needle.localScale = new Vector3(thick, len * 0.5f, thick);
        }

        void DrawWave(Vector3 origin, Vector3 dir, float radius, EffectSilhouette s, float y)
        {
            if (_logic.Verb != Rune.Toprak && s.Focus < _colors.EffectShowMinFocus && _logic.Verb != Rune.Hava && _logic.Verb != Rune.Karanlik)
            {
                _line.positionCount = 0;
                _lineBaseWidth = _colors.EffectLineWidthDefaultM;
                return;
            }

            // İĞNE fiilinde ana gövde iğne; dalga çizgisi yok
            if (_logic.Verb == Rune.Ates && s.Spread < _colors.EffectIgneShowMinSpread)
            {
                _line.positionCount = 0;
                _lineBaseWidth = _colors.EffectLineWidthDefaultM;
                return;
            }

            // SÜRÜ: cephe çizgisi yok — dağınık bulut blobs ile okunur.
            if ((_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik) && s.Focus < _colors.EffectFocusSwarmAlongLineMin)
            {
                _line.positionCount = 0;
                _lineBaseWidth = _colors.EffectLineWidthDefaultM;
                return;
            }

            float focus = s.Focus;
            float baseWidth = _colors.EffectLineWidthDefaultM;
            if (focus < _colors.EffectFocusRingMax)
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
            else if (focus < _colors.EffectFocusArcMax)
            {
                // Yaya daralma — boss yönüne doğru koridor
                float halfArc = Mathf.Lerp(Mathf.PI, 0.35f, (focus - _colors.EffectFocusRingMax) / (_colors.EffectFocusArcMax - _colors.EffectFocusRingMax));
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
                baseWidth = Mathf.Lerp(_colors.EffectLineWidthWideM, _colors.EffectLineWidthNarrowM, s.Pierce);
            }

            if (_logic.Verb == Rune.Toprak)
            {
                baseWidth = Mathf.Lerp(_colors.EffectSarsintiWidthWideM, _colors.EffectSarsintiWidthNarrowM, focus);
                // Kütle: geniş halka daha kalın okunur.
                baseWidth *= Mathf.Lerp(1f, _colors.EffectSarsintiMassWidthMul, 1f - focus);
            }

            // Taban her karede burada baştan hesaplanır (birikmez) — PulseBang bunun üstüne
            // çarpar, DrawWave'in kendisi asla çarpımı miras almaz.
            _lineBaseWidth = baseWidth;
            _line.widthMultiplier = baseWidth;
        }

        void DrawNeedle(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s, float travel01)
        {
            bool show = _logic.Verb == Rune.Ates || _logic.Verb == Rune.Aydinlik
                || s.Pierce > _colors.EffectPierceNeedleShowMin;
            if (!show || _needle == null)
            {
                if (_needle != null) _needle.gameObject.SetActive(false);
                HideNeedleGhosts();
                return;
            }

            _needle.gameObject.SetActive(true);
            float thick = Mathf.Lerp(_colors.EffectNeedleThickWideM, _colors.EffectNeedleThickNarrowM, s.Pierce);
            float len = _colors.EffectNeedleLenBaseM + _colors.EffectNeedleLenPerPierceM * s.Pierce;

            if (_logic.Verb == Rune.Ates || _logic.Verb == Rune.Aydinlik)
            {
                DrawIgneZenitsu(origin, dir, dist, thick, len, travel01);
                return;
            }

            // Sıfat olarak iğne: uca oturur (5-1 hattı vb.)
            HideNeedleGhosts();
            Vector3 tip = origin + dir * dist;
            _needle.position = tip;
            if (dir.sqrMagnitude > 1e-4f)
                _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            _needle.localScale = new Vector3(thick, len * 0.5f, thick);
        }

        /// <summary>
        /// Özet §6 Zenitsu küçük hâli: gerilme → 2–3 kare gidiş → donmuş varış.
        /// </summary>
        void DrawIgneZenitsu(
            Vector3 origin,
            Vector3 dir,
            float dist,
            float thick,
            float len,
            float travel01)
        {
            float windup = Mathf.Max(0.01f, _tuning.NeedleWindupSec);
            float age = _logic.AgeSec;
            bool arrived = travel01 >= 0.98f || _logic.Travel >= _logic.MaxRange - 0.05f;

            if (age < windup)
            {
                // Gerilme: kökte uzar, yerinde — henüz fırlamadı.
                HideNeedleGhosts();
                float t = age / windup;
                float stretch = Mathf.Lerp(1f, _colors.EffectNeedleWindupLenMul, t);
                _needle.position = origin + dir * (len * 0.25f * t);
                if (dir.sqrMagnitude > 1e-4f)
                    _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                float thin = thick * Mathf.Lerp(1f, 0.7f, t);
                _needle.localScale = new Vector3(thin, len * 0.5f * stretch, thin);
                return;
            }

            if (!arrived)
            {
                // Gidiş: uç TipDistance'ta; 2 hayalet smear (afterimage — az mesh).
                Vector3 tip = origin + dir * dist;
                _needle.position = tip;
                if (dir.sqrMagnitude > 1e-4f)
                    _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                _needle.localScale = new Vector3(thick * 0.75f, len * 0.45f, thick * 0.75f);

                for (int i = 0; i < _needleGhosts.Length; i++)
                {
                    float back = (i + 1) / (_needleGhosts.Length + 1f);
                    Vector3 gp = Vector3.Lerp(origin, tip, 1f - back * 0.85f);
                    _needleGhosts[i].gameObject.SetActive(true);
                    _needleGhosts[i].position = gp;
                    _needleGhosts[i].rotation = _needle.rotation;
                    float gScale = 1f - back * 0.45f;
                    _needleGhosts[i].localScale = new Vector3(
                        thick * 0.55f * gScale,
                        len * 0.35f * gScale,
                        thick * 0.55f * gScale);
                }

                return;
            }

            // Varış: donmuş poz — kısa, sert.
            HideNeedleGhosts();
            Vector3 end = origin + dir * dist;
            _needle.position = end;
            if (dir.sqrMagnitude > 1e-4f)
                _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            float holdLen = len * _colors.EffectNeedleArrivalLenMul;
            _needle.localScale = new Vector3(thick * 1.05f, holdLen * 0.5f, thick * 1.05f);
        }

        void DrawSwarm(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            int count;
            if (_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik)
            {
                // Fiil SÜRÜ: her zaman dağınık bulut — sıfır yayılmada bile birkaç gövde.
                float minB = _colors.EffectSwarmMinBlobs;
                count = Mathf.RoundToInt(Mathf.Lerp(minB, _blobs.Length, Mathf.Clamp01(s.Spread)));
            }
            else
            {
                count = Mathf.RoundToInt(s.Spread * _tuning.MaxSwarmBlobs);
            }

            count = Mathf.Clamp(count, 0, _blobs.Length);
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            if (right.sqrMagnitude < 1e-4f)
                right = Vector3.right;

            float stagger = Mathf.Max(0f, _tuning.SwarmStaggerSec);

            for (int i = 0; i < _blobs.Length; i++)
            {
                if (i >= count)
                {
                    _blobs[i].gameObject.SetActive(false);
                    continue;
                }

                // Kademeli varış: gövdeler aynı anda değil sırayla görünür.
                float appearAt = i * stagger;
                if ((_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik) && _logic.AgeSec < appearAt)
                {
                    _blobs[i].gameObject.SetActive(false);
                    continue;
                }

                _blobs[i].gameObject.SetActive(true);
                float localAge = Mathf.Max(0f, _logic.AgeSec - appearAt);
                float reach = dist;
                if ((_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik) && stagger > 1e-4f)
                {
                    // Her gövde kendi gecikmesiyle uca yetişir — cephe değil bulut.
                    float catchUp = Mathf.Clamp01(localAge / (stagger * count + 0.15f));
                    reach = dist * Mathf.Lerp(0.15f, 1f, catchUp);
                }

                float u = (i + 1) / (count + 1f);
                float along = reach * u;
                float jitter = _colors.EffectSwarmJitterM;
                // Düzensiz ofset (sabit hash) — düzenli halka değil.
                float jx = Pseudo(i, 1) * jitter * (0.5f + s.Spread);
                float jz = Pseudo(i, 2) * jitter * (0.5f + s.Spread);
                float side = (i % 2 == 0 ? 1f : -1f) * (0.35f + (1f - s.Focus) * 1.4f)
                             * (0.4f + s.Spread);

                Vector3 p;
                if (s.Focus > _colors.EffectFocusSwarmAlongLineMin || _logic.Verb == Rune.Ates)
                {
                    p = origin + dir * along + right * (side * (1f - s.Focus * 0.7f) + jx * 0.35f)
                        + dir * jz * 0.2f;
                }
                else
                {
                    // Dağınık bulut: açı + yarıçap jitter
                    float a = u * Mathf.PI * 2f + Pseudo(i, 3) * 1.7f + localAge * 1.1f;
                    float r = reach * (0.25f + 0.7f * u) + jz;
                    p = origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r
                        + right * jx * 0.5f;
                }

                p.y = origin.y + 0.18f + 0.35f * s.Lift * Mathf.Abs(Mathf.Sin(localAge * 5.5f + i));
                _blobs[i].position = p;
                float sc = _colors.EffectBlobScaleBaseM + _colors.EffectBlobScalePerSpreadM * s.Spread;
                sc *= 0.85f + 0.3f * (0.5f + 0.5f * Pseudo(i, 4));
                // Yatay wisp — eski “top sürü” silüetini kırar.
                _blobs[i].localScale = new Vector3(sc * 1.35f, sc * 0.35f, sc * 1.35f);
            }
        }

        void HideSwarm()
        {
            if (_blobs == null) return;
            for (int i = 0; i < _blobs.Length; i++)
                _blobs[i].gameObject.SetActive(false);
        }

        void HideNeedleGhosts()
        {
            if (_needleGhosts == null) return;
            for (int i = 0; i < _needleGhosts.Length; i++)
                _needleGhosts[i].gameObject.SetActive(false);
        }

        void PulseBang(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            _ = s;
            float pulse = 1f + 0.8f * Mathf.Sin(_logic.BangAgeSec * 28f);
            _line.widthMultiplier = _lineBaseWidth * pulse;
            if (_needle != null && _needle.gameObject.activeSelf)
                _needle.localScale *= 1f + 0.15f * pulse;

            if (_bangPs != null && _logic.BangAgeSec < 0.05f && !_bangPs.isPlaying)
            {
                Vector3 tip = origin + (dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward) * dist;
                tip.y = origin.y + 0.3f;
                _bangPs.transform.position = tip;
                var main = _bangPs.main;
                main.startColor = _hasSkillTint ? _skillLine : (_colors != null ? _colors.InkCyan : Color.cyan);
                _bangPs.Play();
            }
        }

        /// <summary>[-1,1] sabit gürültü — Random değil, morph sırasında zıplamaz.</summary>
        static float Pseudo(int i, int salt)
        {
            float x = Mathf.Sin(i * 12.9898f + salt * 78.233f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        static Material MakeMat(Color c)
        {
            var shader = FindTransparentUnlitShader();
            var mat = new Material(shader);
            ConfigureTransparentFallback(mat);
            SetMatColor(mat, c);
            return mat;
        }

        // "Sprites/Default" alfa'yı gerçekten harmanlar (bkz. InkTrail.EnsureMaterial); URP
        // Unlit varsayılan OPAK'tır ve alfa'ya yazılan hiçbir değeri (sönme, iz saydamlığı)
        // ekrana yansıtmaz. Aynı shader'ı kullanmak repodaki tek doğru desenle tutarlı kalır.
        static Shader FindTransparentUnlitShader()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return shader != null ? shader : Shader.Find("Hidden/Internal-Colored");
        }

        // Yalnızca yukarıdaki tercih zinciri URP Unlit'e düşerse devreye girer: yüzeyi
        // gerçekten saydama çevirir (_Surface/_Blend + blend modu + render queue).
        static void ConfigureTransparentFallback(Material mat)
        {
            if (!mat.HasProperty("_Surface"))
                return;

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 1f); // Additive glow
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
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
