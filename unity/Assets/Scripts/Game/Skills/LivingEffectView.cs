using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Vfx;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// Tek yaşayan etkinin prosedürel çizimi — LineRenderer + az sayıda küre/kapsül.
    /// T14: ayrım hareket karakterinden (İĞNE fırlar, SÜRÜ üşüşür, SARSINTI yükselir);
    /// düz vuruşun ayrı jab silüeti var. Overdraw yok (özet §6).
    /// </summary>
    public sealed partial class LivingEffectView : MonoBehaviour
    {
        const int RingSegments = 48;
        const int NeedleAfterimageCount = 2;

        LivingEffect _logic;
        ManifestationTuning _tuning;
        GameTuning _colors;
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
            GameTuning colors,
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
            _lineMat = MakeMat(_colors.Visuals.InkCyan);
            _blobMat = MakeMat(_colors.Visuals.InkPurple);
            _ghostMat = MakeMat(_colors.Visuals.InkCyan);

            var lineGo = new GameObject("WaveLine");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            _line.sharedMaterial = _lineMat;
            _lineBaseWidth = _colors.Visuals.EffectLineWidthDefaultM;
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
            // AddComponent sistemi hemen oynatır; süre oynarken yazılamaz.
            _bangPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _bangPs.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = LivingEffectViewDefaults.BangPsMainDurationSec;
            main.startLifetime = LivingEffectViewDefaults.BangPsStartLifetimeSec;
            main.startSpeed = LivingEffectViewDefaults.BangPsStartSpeedMps;
            main.startSize = LivingEffectViewDefaults.BangPsStartSizeM;
            main.maxParticles = LivingEffectViewDefaults.BangPsMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = _bangPs.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, LivingEffectViewDefaults.BangPsBurstCount) });
            var sh = _bangPs.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = LivingEffectViewDefaults.BangPsShapeRadiusM;
            var col = _bangPs.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(_colors != null ? _colors.Visuals.InkCyan : Color.cyan, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = grad;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = AssetLoader.FindShader("Universal Render Pipeline/Particles/Unlit", null)
                         ?? AssetLoader.FindShader("Sprites/Default", null);
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
                : (_logic.Phase == LivingEffectPhase.Banging ? 1f : LivingEffectViewDefaults.PhaseScaleWhenNotBanging);

            float travel01 = _logic.MaxRange > LivingEffectViewDefaults.MaxRangeEpsilonM
                ? Mathf.Clamp01(_logic.Travel / _logic.MaxRange)
                : 0f;
            float urgent = 1f - _windowRemaining01;
            if (_windowRemaining01 > _colors.Hud.WindowCueUrgentRatio)
                urgent *= LivingEffectViewDefaults.UrgentPlaceMult;
            float place = Mathf.Max(urgent, travel01 * LivingEffectViewDefaults.TravelPlaceMult);
            float hz = Mathf.Lerp(_colors.Hud.WindowCuePulseHz, _colors.Hud.WindowCueUrgentHz, place);
            // Nabız AŞAĞI modüle eder: yukarı çarpmak taban alfa 0.95 iken Clamp01'e takılıyor
            // ve ipucu hiç görünmüyordu (T8.1). §8/T2 "pencereyi dalgadan oku" buna bağlı.
            float wave = 0.5f + 0.5f * Mathf.Sin(_logic.AgeSec * hz * Mathf.PI * 2f);
            alpha = Mathf.Clamp01(alpha * (1f - _colors.Hud.WindowCuePulseAmp * place * wave));

            Color cyan = _hasSkillTint ? _skillLine : _colors.Visuals.InkCyan;
            cyan.a = alpha;
            Color purple = _hasSkillTint ? _skillBlob : _colors.Visuals.InkPurple;
            purple.a = alpha;
            SetMatColor(_lineMat, Color.Lerp(cyan, purple, _hasSkillTint ? LivingEffectViewDefaults.LineTintLerpWithSkill : LivingEffectViewDefaults.LineTintLerpNoSkillBase + LivingEffectViewDefaults.LineTintSpreadMult * s.Spread));
            SetMatColor(_blobMat, purple);

            Color ghost = cyan;
            ghost.a = alpha * _colors.Visuals.EffectNeedleAfterimageAlpha;
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
                float peak = _tuning.WaveRiseHeightM * (LivingEffectViewDefaults.WavePeakLiftBaseMult + LivingEffectViewDefaults.WavePeakLiftSpreadMult * s.Lift);
                return Mathf.Lerp(_colors.Visuals.EffectSarsintiGroundY, peak, travel01);
            }

            return LivingEffectViewDefaults.WaveThicknessBaseM + LivingEffectViewDefaults.WaveThicknessLiftMult * s.Lift;
        }

        // "Sprites/Default" alfa'yı gerçekten harmanlar (bkz. InkTrailView.EnsureMaterial); URP
        // Unlit varsayılan OPAK'tır ve alfa'ya yazılan hiçbir değeri (sönme, iz saydamlığı)
        // ekrana yansıtmaz. Aynı shader'ı kullanmak repodaki tek doğru desenle tutarlı kalır.
        static Shader FindTransparentUnlitShader()
        {
            var shader = AssetLoader.FindShader("Sprites/Default", null);
            if (shader == null) shader = AssetLoader.FindShader("Universal Render Pipeline/Unlit", null);
            if (shader == null) shader = AssetLoader.FindShader("Unlit/Color", null);
            return shader != null ? shader : AssetLoader.FindShader("Hidden/Internal-Colored", null);
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
