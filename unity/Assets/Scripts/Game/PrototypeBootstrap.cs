using Dovus.Core.Tuning;
using UnityEngine;
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
            BuildWorld();
        }

        void BuildWorld()
        {
            var clock = gameObject.AddComponent<GameClock>();

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

            var reactor = boss.AddComponent<BossReactor>();
            reactor.Tuning = _tuning;
            reactor.BodyRadiusM = BossRadiusM;
            reactor.CaptureHome();

            CreateSun();
            CreateCamera(player.transform);
            CreatePentagon(clock, player.transform, pose, reactor);
        }

        void CreatePentagon(GameClock clock, Transform player, ActorPose pose, BossReactor boss)
        {
            var root = new GameObject("Pentagon");
            root.transform.SetParent(transform, false);

            var view = root.AddComponent<PentagonView>();
            view.Build(_tuning);

            var mainCam = Camera.main;
            if (mainCam != null)
                mainCam.cullingMask &= ~(1 << PentagonInkLayer);

            var overlayGo = new GameObject("InkOverlayCam");
            overlayGo.transform.SetParent(root.transform, false);
            var overlay = overlayGo.AddComponent<PentagonOverlayCamera>();
            overlay.Build(PentagonInkLayer);
            AttachOverlayToMain(mainCam, overlay.Cam);

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
            input.Combat = new CombatTuning();
            input.Bind(clock, ink, syllable, debug);
            debug.Configure(input.Engine, view.CanvasRoot);

            var scarsGo = new GameObject("GroundScars");
            scarsGo.transform.SetParent(transform, false);
            var scars = scarsGo.AddComponent<GroundScarField>();
            scars.Configure(_tuning);

            var manGo = new GameObject("Manifestation");
            manGo.transform.SetParent(transform, false);
            var director = manGo.AddComponent<ManifestationDirector>();
            director.Bind(clock, input, player, pose, boss, scars, _tuning);
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
            var ground = CreateMeshObject("Arena", "Plane.fbx");
            // Plane mesh 10x10 m; ArenaHalfSizeM yarım kenar uzunluğu.
            float scale = _tuning.ArenaHalfSizeM / 5f;
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            ApplyColor(ground, _tuning.GroundColor);
        }

        static GameObject CreateCapsule(string name, Vector3 position, float radius, float height, Color color)
        {
            var capsule = CreateMeshObject(name, "Capsule.fbx");
            capsule.transform.position = position;
            capsule.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyColor(capsule, color);
            return capsule;
        }

        /// <summary>
        /// Mesh'i doğrudan ata (MeshFilter+MeshRenderer) — CreatePrimitive'in otomatik
        /// Collider'ı hiç oluşmaz (teknoloji-kararlari §4).
        /// </summary>
        static GameObject CreateMeshObject(string name, string builtinMeshName)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>(builtinMeshName);
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

        void CreateCamera(Transform target)
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
