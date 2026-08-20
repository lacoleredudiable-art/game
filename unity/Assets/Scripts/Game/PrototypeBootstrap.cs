using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// Tek sahne kökü: arena, oyuncu, boss, kamera ve ışığı çalışma anında kurar.
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] PrototypeTuning _tuning = new();

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
            BuildWorld();
        }

        void BuildWorld()
        {
            gameObject.AddComponent<GameClock>();

            CreateArena();
            var player = CreateCapsule("Player", new Vector3(0f, 1f, -2f), 0.5f, 2f, new Color(0.25f, 0.85f, 0.92f));
            CreateCapsule("Boss", new Vector3(0f, 1.2f, 5f), 0.85f, 2.6f, new Color(1f, 0.38f, 0.12f));

            var moveInput = player.AddComponent<MoveInput>();
            CopyTuning(moveInput.Tuning);

            var motor = player.AddComponent<KinematicMotor>();
            motor.SpeedMps = _tuning.WalkSpeedMps;

            CreateSun();
            CreateCamera(player.transform);
        }

        void CopyTuning(PrototypeTuning target)
        {
            target.ArenaHalfSizeM = _tuning.ArenaHalfSizeM;
            target.WalkSpeedMps = _tuning.WalkSpeedMps;
            target.JoystickMaxRadiusDp = _tuning.JoystickMaxRadiusDp;
            target.JoystickDeadZone = _tuning.JoystickDeadZone;
            target.FollowSmoothTimeSec = _tuning.FollowSmoothTimeSec;
            target.LookAheadM = _tuning.LookAheadM;
            target.CameraOffset = _tuning.CameraOffset;
        }

        void CreateArena()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena";
            float scale = _tuning.ArenaHalfSizeM / 5f;
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            ApplyColor(ground, new Color(0.38f, 0.4f, 0.44f));
        }

        GameObject CreateCapsule(string name, Vector3 position, float radius, float height, Color color)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.position = position;
            capsule.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyColor(capsule, color);
            return capsule;
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
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 120f;

            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var follow = camGo.AddComponent<FollowCamera>();
            follow.Target = target;
            CopyTuning(follow.Tuning);
            camGo.transform.position = target.position + _tuning.CameraOffset;
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            renderer.sharedMaterial = material;
        }
    }
}
