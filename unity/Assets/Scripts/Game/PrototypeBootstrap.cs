using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// Tek sahne kökü: arena, oyuncu, boss, kamera ve ışığı çalışma anında kurar.
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        const float PlayerRadiusM = 0.5f;
        const float PlayerHeightM = 2f;
        const float BossRadiusM = 0.85f;
        const float BossHeightM = 2.6f;
        const int PentagonInkLayer = 5; // Unity built-in UI layer

        [SerializeField] PrototypeTuning _tuning = new();

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
            _tuning.EnsureRuntimeDefaults();
            ApplyFrameRateTarget();
            BuildWorld();
        }

        /// <summary>
        /// T11 hedefi sabit 60 fps. Android'de varsayılan tavan cihazın ekran tazeleme hızıdır
        /// (120 Hz bir telefonda oyun 120'ye tırmanmaya çalışır ve kare süresi dalgalanır), o
        /// yüzden tavan açıkça yazılır. vSync sayacı sıfırlanmazsa targetFrameRate yok sayılır.
        /// </summary>
        void ApplyFrameRateTarget()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(1, _tuning.TargetFrameRateHz);
        }

        void BuildWorld()
        {
            var combat = new CombatTuning();

            // T10: kayıtlı ayar, DÜNYA kurulmadan önce combat/_tuning'in İÇİNE kopyalanır
            // (CopyFrom yolu — referans kimliği korunur). Böylece arena/oyuncu/boss ilk kareden
            // kaydedilmiş değerlerle doğar, sonradan "sıçrayan" bir düzeltme karesi olmaz.
            var tuningConfig = TuningConfig.Create(combat, _tuning);
            tuningConfig.TryLoad();

            var clock = gameObject.AddComponent<GameClock>();
            clock.Bind(combat.Slowmo);

            CreateArena();

            var player = CreateCapsule(
                "Player",
                new Vector3(0f, PlayerHeightM * 0.5f, -2f),
                PlayerRadiusM,
                PlayerHeightM,
                _tuning.PlayerColor);

            var boss = CreateCapsule(
                "Boss",
                new Vector3(0f, BossHeightM * 0.5f, 5f),
                BossRadiusM,
                BossHeightM,
                _tuning.BossColor);

            player.AddComponent<MoveInput>().Tuning = _tuning;

            var motor = player.AddComponent<KinematicMotor>();
            motor.Tuning = _tuning;
            motor.BodyRadiusM = PlayerRadiusM;

            var pose = player.AddComponent<ActorPose>();
            pose.Tuning = _tuning;
            pose.CaptureBase();

            var vitals = player.AddComponent<PlayerVitals>();
            vitals.Bind(combat.Boss, _tuning.PlayerMaxHp);

            var afterimage = player.AddComponent<AfterimageTrail>();
            afterimage.Bind(combat.Feel, _tuning);

            var dodgeMotion = player.AddComponent<DodgeMotion>();

            var reactor = boss.AddComponent<BossReactor>();
            reactor.Tuning = _tuning;
            reactor.BodyRadiusM = BossRadiusM;
            reactor.CaptureHome();

            var telegraph = boss.AddComponent<BossTelegraph>();
            telegraph.Bind(_tuning, combat.Boss, boss.transform);

            CreateSun();
            FollowCamera follow = CreateCamera(player.transform);
            CreatePentagon(clock, combat, player.transform, pose, reactor, dodgeMotion, afterimage, vitals, telegraph, follow, tuningConfig);
        }

        void CreatePentagon(
            GameClock clock,
            CombatTuning combat,
            Transform player,
            ActorPose pose,
            BossReactor boss,
            DodgeMotion dodgeMotion,
            AfterimageTrail afterimage,
            PlayerVitals vitals,
            BossTelegraph telegraph,
            FollowCamera follow,
            TuningConfig tuningConfig)
        {
            var root = new GameObject("Pentagon");
            root.transform.SetParent(transform, false);

            var mainCam = Camera.main;
            if (mainCam != null)
                mainCam.cullingMask &= ~(1 << PentagonInkLayer);

            var overlayGo = new GameObject("InkOverlayCam");
            overlayGo.transform.SetParent(root.transform, false);
            var overlay = overlayGo.AddComponent<PentagonOverlayCamera>();
            overlay.Build(PentagonInkLayer);
            AttachOverlayToMain(mainCam, overlay.Cam);

            var view = root.AddComponent<PentagonView>();
            view.Build(_tuning, overlay.Cam);

            var inkGo = new GameObject("InkTrail");
            inkGo.transform.SetParent(root.transform, false);
            inkGo.layer = PentagonInkLayer;
            var ink = inkGo.AddComponent<InkTrail>();
            ink.Configure(_tuning, overlay, PentagonInkLayer);

            var syllable = root.AddComponent<SyllableFeedback>();
            syllable.Configure(_tuning);
            var debug = root.AddComponent<SentenceDebugHud>();

            var input = root.AddComponent<PentagonInput>();
            input.Tuning = _tuning;
            input.Combat = combat;
            input.Bind(clock, ink, syllable, debug);
            debug.Configure(input.Engine, view.CanvasRoot);
            debug.BindVitals(vitals);
            input.BindVitals(vitals);

            var readout = root.AddComponent<ReactionReadout>();
            readout.Configure(combat.Feel, _tuning, view.CanvasRoot);

            var vitalsHud = root.AddComponent<VitalsHud>();
            vitalsHud.Configure(vitals, _tuning, view.CanvasRoot);

            var lockHud = root.AddComponent<RecoveryLockHud>();
            lockHud.Configure(input.Engine, combat, _tuning, view.CanvasRoot);

            var frameHud = root.AddComponent<FrameTimeHud>();
            frameHud.Configure(_tuning, view.CanvasRoot);

            dodgeMotion.Bind(clock, input, boss.transform, afterimage);

            var feelGo = new GameObject("CombatFeel");
            feelGo.transform.SetParent(transform, false);
            var feel = feelGo.AddComponent<CombatFeel>();
            feel.Bind(clock, follow, combat, _tuning, overlay.Cam, debug, readout);

            var directorGo = boss.gameObject;
            var bossDir = directorGo.AddComponent<BossDirector>();
            bossDir.Bind(clock, combat, _tuning, boss, input, player, vitals, telegraph, feel);

            var scarsGo = new GameObject("GroundScars");
            scarsGo.transform.SetParent(transform, false);
            var scars = scarsGo.AddComponent<GroundScarField>();
            scars.Configure(_tuning);

            var manGo = new GameObject("Manifestation");
            manGo.transform.SetParent(transform, false);
            var director = manGo.AddComponent<ManifestationDirector>();
            director.Bind(clock, input, player, pose, boss, scars, _tuning);

            CreateTuningPanel(tuningConfig, vitals);
        }

        /// <summary>
        /// T10: uGUI Slider/Button ilk kez sahneye giriyor — proje şimdiye kadar hep elle
        /// hit-test eden EnhancedTouch kullanıyordu (PentagonInput/MoveInput). Standart Slider
        /// bir EventSystem + bir input modülü ister; InputSystemUIInputModule seçildi çünkü
        /// proje zaten Yeni Input System üstünde (Unity.InputSystem asmdef referansı).
        /// </summary>
        static void CreateTuningPanel(TuningConfig tuningConfig, PlayerVitals vitals)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var panelGo = new GameObject("TuningPanel");
            var panel = panelGo.AddComponent<TuningPanel>();
            panel.Configure(tuningConfig, vitals);
        }

        static void AttachOverlayToMain(Camera main, Camera overlay)
        {
            if (main == null || overlay == null)
                return;

            var mainData = main.GetComponent<UniversalAdditionalCameraData>();
            if (mainData == null)
                mainData = main.gameObject.AddComponent<UniversalAdditionalCameraData>();

            var overlayData = overlay.GetComponent<UniversalAdditionalCameraData>();
            if (overlayData == null)
                overlayData = overlay.gameObject.AddComponent<UniversalAdditionalCameraData>();

            overlayData.renderType = CameraRenderType.Overlay;
            if (!mainData.cameraStack.Contains(overlay))
                mainData.cameraStack.Add(overlay);
        }

        void CreateArena()
        {
            var ground = CreateMeshObject("Arena", PrimitiveType.Plane);
            // Plane mesh 10x10 m; ArenaHalfSizeM yarım kenar uzunluğu.
            float scale = _tuning.ArenaHalfSizeM / 5f;
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            ApplyColor(ground, _tuning.GroundColor);
        }

        static GameObject CreateCapsule(string name, Vector3 position, float radius, float height, Color color)
        {
            var capsule = CreateMeshObject(name, PrimitiveType.Capsule);
            capsule.transform.position = position;
            capsule.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyColor(capsule, color);
            return capsule;
        }

        /// <summary>
        /// Mesh'i doğrudan ata (MeshFilter+MeshRenderer) — CreatePrimitive'in otomatik
        /// Collider'ı hiç oluşmaz (teknoloji-kararlari §4).
        /// </summary>
        static GameObject CreateMeshObject(string name, PrimitiveType type)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(type);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        Light CreateSun()
        {
            var sunGo = new GameObject("Sun");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = light;
            return light;
        }

        FollowCamera CreateCamera(Transform target)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _tuning.BackgroundColor;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 120f;

            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var follow = camGo.AddComponent<FollowCamera>();
            follow.Tuning = _tuning;
            follow.Target = target;
            camGo.transform.position = target.position + _tuning.CameraOffset;
            return follow;
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            renderer.sharedMaterial = material;
        }
    }
}
