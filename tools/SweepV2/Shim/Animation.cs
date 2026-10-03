using Dovus.Game.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine
{
    public enum HumanBodyBones
    {
        Hips = 0, LeftUpperLeg = 1, RightUpperLeg = 2, LeftLowerLeg = 3, RightLowerLeg = 4,
        LeftFoot = 5, RightFoot = 6, Spine = 7, Chest = 8, Neck = 9, Head = 10,
        LeftShoulder = 11, RightShoulder = 12, LeftUpperArm = 13, RightUpperArm = 14,
        LeftLowerArm = 15, RightLowerArm = 16, LeftHand = 17, RightHand = 18,
        LeftToes = 19, RightToes = 20, LeftEye = 21, RightEye = 22, Jaw = 23,
        UpperChest = 54, LastBone = 55,
    }

    public enum AnimatorCullingMode { AlwaysAnimate, CullUpdateTransforms, CullCompletely }
    public enum AnimatorUpdateMode { Normal, AnimatePhysics, UnscaledTime, Fixed = 1 }
    public enum AnimatorControllerParameterType { Float = 1, Int = 3, Bool = 4, Trigger = 9 }
    public enum WrapMode { Once = 1, Loop = 2, PingPong = 4, Default = 0, ClampForever = 8 }

    public class Motion : Object { }

    public class AnimationClip : Motion
    {
        public float length { get; set; }
        public float frameRate { get; set; } = 30f;
        public bool isLooping { get; set; }
        public WrapMode wrapMode { get; set; }
        public bool legacy { get; set; }
        public Vector3 averageSpeed { get; set; }
        public bool humanMotion { get; set; }
        public void SampleAnimation(GameObject go, float time) { }
    }

    public class RuntimeAnimatorController : Object
    {
        public virtual AnimationClip[] animationClips => Array.Empty<AnimationClip>();
    }

    public class AnimatorOverrideController : RuntimeAnimatorController
    {
        readonly Dictionary<string, AnimationClip> _overrides = new();

        public AnimatorOverrideController() { }
        public AnimatorOverrideController(RuntimeAnimatorController controller) { runtimeAnimatorController = controller; }

        public RuntimeAnimatorController runtimeAnimatorController { get; set; }
        public int overridesCount => _overrides.Count;

        public AnimationClip this[string name]
        {
            get => _overrides.TryGetValue(name, out AnimationClip c) ? c : null;
            set => _overrides[name] = value;
        }

        public AnimationClip this[AnimationClip clip]
        {
            get => clip != null ? this[clip.name] : null;
            set
            {
                if (clip != null) this[clip.name] = value;
            }
        }

        public override AnimationClip[] animationClips =>
            runtimeAnimatorController != null ? runtimeAnimatorController.animationClips : Array.Empty<AnimationClip>();

        public void GetOverrides(List<KeyValuePair<AnimationClip, AnimationClip>> overrides) => overrides.Clear();
        public void ApplyOverrides(IList<KeyValuePair<AnimationClip, AnimationClip>> overrides) { }
    }

    public class Avatar : Object
    {
        public bool isHuman { get; internal set; }
        public bool isValid => true;
    }

    public class AnimatorControllerParameter
    {
        public string name = "";
        public AnimatorControllerParameterType type;
        public float defaultFloat;
        public int defaultInt;
        public bool defaultBool;
        public int nameHash => Animator.StringToHash(name);
    }

    public struct AnimatorStateInfo
    {
        internal int m_Hash;
        internal float m_Norm;
        internal float m_Length;
        public int shortNameHash => m_Hash;
        public int fullPathHash => m_Hash;
        public int nameHash => m_Hash;
        public float normalizedTime => m_Norm;
        public float length => m_Length;
        public float speed => 1f;
        public float speedMultiplier => 1f;
        public bool loop => false;
        public int tagHash => 0;
        public bool IsName(string name) => Animator.StringToHash(name) == m_Hash;
        public bool IsTag(string tag) => false;
    }

    public struct AnimatorClipInfo
    {
        public AnimationClip clip => null;
        public float weight => 0f;
    }

    /// <summary>
    /// Başsız Animator: kontrolcü yoktur (Play'deki Mixamo kontrolcüsü yalnız görsel).
    /// İnsansı kemikler aynı adlı çocuk transform'lardan bulunur; ActorGrounding ayak ölçümü böyle yapılır.
    /// </summary>
    public class Animator : Behaviour
    {
        readonly Dictionary<int, float> _floats = new();
        readonly Dictionary<int, int> _ints = new();
        readonly Dictionary<int, bool> _bools = new();

        public RuntimeAnimatorController runtimeAnimatorController { get; set; }
        public Avatar avatar { get; set; }
        public bool isHuman { get; set; }
        public bool hasBoundPlayables => false;
        public float speed { get; set; } = 1f;
        public float humanScale { get; set; } = 1f;
        public bool applyRootMotion { get; set; }
        public AnimatorCullingMode cullingMode { get; set; }
        public AnimatorUpdateMode updateMode { get; set; }
        public int layerCount => 1;
        public bool keepAnimatorStateOnDisable { get; set; }
        public bool writeDefaultValuesOnDisable { get; set; }
        public bool isInitialized => true;
        public bool fireEvents { get; set; } = true;
        public bool logWarnings { get; set; }
        public Vector3 deltaPosition => Vector3.zero;
        public Quaternion deltaRotation => Quaternion.identity;
        public AnimatorControllerParameter[] parameters => Array.Empty<AnimatorControllerParameter>();
        public int parameterCount => 0;

        public Transform GetBoneTransform(HumanBodyBones bone)
        {
            if (!isHuman) return null;
            return FindDeep(transform, bone.ToString());
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        public static int StringToHash(string name)
        {
            byte[] data = Encoding.UTF8.GetBytes(name ?? "");
            uint crc = 0xFFFFFFFFu;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
            return unchecked((int)~crc);
        }

        public float GetFloat(string name) => GetFloat(StringToHash(name));
        public float GetFloat(int id) => _floats.TryGetValue(id, out float v) ? v : 0f;
        public void SetFloat(string name, float value) => _floats[StringToHash(name)] = value;
        public void SetFloat(int id, float value) => _floats[id] = value;
        public void SetFloat(string name, float value, float dampTime, float deltaTime) => SetFloat(name, value);
        public void SetFloat(int id, float value, float dampTime, float deltaTime) => SetFloat(id, value);
        public int GetInteger(string name) => GetInteger(StringToHash(name));
        public int GetInteger(int id) => _ints.TryGetValue(id, out int v) ? v : 0;
        public void SetInteger(string name, int value) => _ints[StringToHash(name)] = value;
        public void SetInteger(int id, int value) => _ints[id] = value;
        public bool GetBool(string name) => GetBool(StringToHash(name));
        public bool GetBool(int id) => _bools.TryGetValue(id, out bool v) && v;
        public void SetBool(string name, bool value) => _bools[StringToHash(name)] = value;
        public void SetBool(int id, bool value) => _bools[id] = value;
        public void SetTrigger(string name) { }
        public void SetTrigger(int id) { }
        public void ResetTrigger(string name) { }
        public void ResetTrigger(int id) { }

        public bool HasState(int layerIndex, int stateId) => false;
        public int GetLayerIndex(string layerName) => -1;
        public string GetLayerName(int layerIndex) => "Base Layer";
        public float GetLayerWeight(int layerIndex) => layerIndex == 0 ? 1f : 0f;
        public void SetLayerWeight(int layerIndex, float weight) { }
        public bool IsInTransition(int layerIndex) => false;
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex) => default;
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex) => default;
        public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex) => Array.Empty<AnimatorClipInfo>();

        public void Play(string stateName) { }
        public void Play(string stateName, int layer) { }
        public void Play(string stateName, int layer, float normalizedTime) { }
        public void Play(int stateNameHash) { }
        public void Play(int stateNameHash, int layer) { }
        public void Play(int stateNameHash, int layer, float normalizedTime) { }
        public void PlayInFixedTime(int stateNameHash, int layer, float fixedTime) { }
        public void CrossFade(string stateName, float duration) { }
        public void CrossFade(string stateName, float duration, int layer) { }
        public void CrossFade(int stateHash, float duration, int layer) { }
        public void CrossFade(int stateHash, float duration, int layer, float normalizedTimeOffset) { }
        public void CrossFadeInFixedTime(string stateName, float duration) { }
        public void CrossFadeInFixedTime(string stateName, float duration, int layer) { }
        public void CrossFadeInFixedTime(int stateHash, float duration) { }
        public void CrossFadeInFixedTime(int stateHash, float duration, int layer) { }
        public void CrossFadeInFixedTime(int stateHash, float duration, int layer, float fixedTimeOffset) { }
        public void CrossFadeInFixedTime(int stateHash, float duration, int layer, float fixedTimeOffset, float normalizedTransitionTime) { }
        public void Update(float deltaTime) { }
        public void Rebind() { }
        public void WriteDefaultValues() { }
        public void StartPlayback() { }
        public void StopPlayback() { }
    }

    public class Animation : Behaviour
    {
        public bool Play() => false;
        public bool Play(string name) => false;
        public void Stop() { }
        public bool isPlaying => false;
        public AnimationClip clip { get; set; }
    }

    // ---------------------------------------------------------------- parçacık

    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemScalingMode { Hierarchy, Local, Shape }
    public enum ParticleSystemStopAction { None, Disable, Destroy, Callback }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemShapeType { Sphere = 0, Hemisphere = 2, Cone = 4, Box = 5, Mesh = 6, Circle = 10, SingleSidedEdge = 12, Rectangle = 18 }
    public enum ParticleSystemRenderMode { Billboard = 0, Stretch = 1, HorizontalBillboard = 2, VerticalBillboard = 3, Mesh = 4, None = 5 }
    public enum ParticleSystemGradientMode { Color, Gradient, TwoColors, TwoGradients, RandomColor }
    public enum ParticleSystemCurveMode { Constant, Curve, TwoCurves, TwoConstants }
    public enum ParticleSystemRenderSpace { View, World, Local, Facing, Velocity }
    public enum ParticleSystemEmitterVelocityMode { Transform, Rigidbody, Custom }

    public class ParticleSystemRenderer : Renderer
    {
        public ParticleSystemRenderMode renderMode { get; set; }
        public float velocityScale { get; set; }
        public float lengthScale { get; set; } = 2f;
        public float cameraVelocityScale { get; set; }
        public float minParticleSize { get; set; }
        public float maxParticleSize { get; set; } = 0.5f;
        public ParticleSystemRenderSpace alignment { get; set; }
        public Material trailMaterial { get; set; }
        public Mesh mesh { get; set; }
        public float sortingFudge { get; set; }
    }

    /// <summary>
    /// Görsel parçacık: simülasyon yoktur. stopAction=Destroy ise Play/Stop sonrası
    /// süre + ömür dolunca Unity gibi nesneyi yok eder (sahne kök sayısı Play ile aynı kalsın).
    /// </summary>
    [RequireComponent(typeof(ParticleSystemRenderer))]
    public class ParticleSystem : Component
    {
        internal float Duration = 5f;
        internal bool Loop = true;
        internal bool PlayOnAwake = true;
        internal float Lifetime = 5f;
        internal ParticleSystemStopAction StopAction;
        double _startedAt = -1;
        double _stoppedAt = -1;
        bool _playing;

        public MainModule main => new(this);
        public EmissionModule emission => new(this);
        public ShapeModule shape => new(this);
        public ColorOverLifetimeModule colorOverLifetime => new(this);
        public SizeOverLifetimeModule sizeOverLifetime => new(this);
        public VelocityOverLifetimeModule velocityOverLifetime => new(this);
        public LimitVelocityOverLifetimeModule limitVelocityOverLifetime => new(this);
        public NoiseModule noise => new(this);
        public TrailModule trails => new(this);
        public RotationOverLifetimeModule rotationOverLifetime => new(this);
        public TextureSheetAnimationModule textureSheetAnimation => new(this);
        public CollisionModule collision => new(this);

        public bool isPlaying => _playing;
        public bool isEmitting => _playing;
        public bool isStopped => !_playing;
        public bool isPaused => false;
        public int particleCount => 0;
        public float time { get; set; }
        public bool useAutoRandomSeed { get; set; } = true;
        public uint randomSeed { get; set; }

        public void Play() => Play(true);
        public void Play(bool withChildren)
        {
            _playing = true;
            _startedAt = Time.timeAsDouble;
            _stoppedAt = -1;
            World.TrackParticle(this);
        }

        public void Stop() => Stop(true, ParticleSystemStopBehavior.StopEmitting);
        public void Stop(bool withChildren) => Stop(withChildren, ParticleSystemStopBehavior.StopEmitting);
        public void Stop(bool withChildren, ParticleSystemStopBehavior stopBehavior)
        {
            if (!_playing && _stoppedAt >= 0) return;
            _playing = false;
            _stoppedAt = Time.timeAsDouble - (stopBehavior == ParticleSystemStopBehavior.StopEmittingAndClear ? Lifetime : 0f);
            World.TrackParticle(this);
        }

        public void Pause() { }
        public void Pause(bool withChildren) { }
        public void Clear() { }
        public void Clear(bool withChildren) { }
        public void Emit(int count) { }
        public void Emit(EmitParams emitParams, int count) { }
        public void Simulate(float t) { }
        public void Simulate(float t, bool withChildren, bool restart) { }
        public bool IsAlive() => _playing;
        public bool IsAlive(bool withChildren) => _playing;

        /// <summary>Unity: tüm parçacıklar ölünce stopAction çalışır.</summary>
        internal bool Finished(double now)
        {
            if (_playing)
            {
                if (Loop || _startedAt < 0) return false;
                return now >= _startedAt + Duration + Lifetime;
            }
            return _stoppedAt >= 0 && now >= _stoppedAt + Lifetime;
        }

        internal void OnAwakeAutoPlay()
        {
            if (PlayOnAwake) Play();
        }

        public struct EmitParams
        {
            public Vector3 position { get; set; }
            public Vector3 velocity { get; set; }
            public float startSize { get; set; }
            public float startLifetime { get; set; }
            public Color32 startColor { get; set; }
        }

        public struct Burst
        {
            public float time { get; set; }
            public MinMaxCurve count { get; set; }
            public int cycleCount { get; set; }
            public float repeatInterval { get; set; }
            public float probability { get; set; }
            public Burst(float time, short count) : this() { this.time = time; this.count = count; cycleCount = 1; probability = 1f; }
            public Burst(float time, float count) : this() { this.time = time; this.count = count; cycleCount = 1; probability = 1f; }
            public Burst(float time, short minCount, short maxCount) : this() { this.time = time; count = new MinMaxCurve(minCount, maxCount); cycleCount = 1; probability = 1f; }
            public Burst(float time, MinMaxCurve count) : this() { this.time = time; this.count = count; cycleCount = 1; probability = 1f; }
        }

        public struct MinMaxCurve
        {
            public ParticleSystemCurveMode mode { get; set; }
            public float constant { get; set; }
            public float constantMin { get; set; }
            public float constantMax { get; set; }
            public float curveMultiplier { get; set; }
            public AnimationCurve curve { get; set; }
            public AnimationCurve curveMin { get; set; }
            public AnimationCurve curveMax { get; set; }

            public MinMaxCurve(float constant) : this() { this.constant = constant; constantMax = constant; }
            public MinMaxCurve(float min, float max) : this() { constantMin = min; constantMax = max; mode = ParticleSystemCurveMode.TwoConstants; }
            public MinMaxCurve(float multiplier, AnimationCurve curve) : this() { curveMultiplier = multiplier; this.curve = curve; mode = ParticleSystemCurveMode.Curve; }
            public MinMaxCurve(float multiplier, AnimationCurve min, AnimationCurve max) : this() { curveMultiplier = multiplier; curveMin = min; curveMax = max; mode = ParticleSystemCurveMode.TwoCurves; }

            public static implicit operator MinMaxCurve(float constant) => new(constant);

            public float Evaluate(float time) => mode switch
            {
                ParticleSystemCurveMode.TwoConstants => (constantMin + constantMax) * 0.5f,
                ParticleSystemCurveMode.Curve => curve != null ? curve.Evaluate(time) * curveMultiplier : 0f,
                _ => constant,
            };
        }

        public struct MinMaxGradient
        {
            public ParticleSystemGradientMode mode { get; set; }
            public Color color { get; set; }
            public Color colorMin { get; set; }
            public Color colorMax { get; set; }
            public Gradient gradient { get; set; }
            public Gradient gradientMin { get; set; }
            public Gradient gradientMax { get; set; }

            public MinMaxGradient(Color color) : this() { this.color = color; mode = ParticleSystemGradientMode.Color; }
            public MinMaxGradient(Color min, Color max) : this() { colorMin = min; colorMax = max; mode = ParticleSystemGradientMode.TwoColors; }
            public MinMaxGradient(Gradient gradient) : this() { this.gradient = gradient; mode = ParticleSystemGradientMode.Gradient; }
            public MinMaxGradient(Gradient min, Gradient max) : this() { gradientMin = min; gradientMax = max; mode = ParticleSystemGradientMode.TwoGradients; }

            public static implicit operator MinMaxGradient(Color color) => new(color);
            public static implicit operator MinMaxGradient(Gradient gradient) => new(gradient);
            public Color Evaluate(float time) => color;
        }

        public struct MainModule
        {
            readonly ParticleSystem _ps;
            internal MainModule(ParticleSystem ps) { _ps = ps; }

            public float duration { get => _ps.Duration; set => _ps.Duration = value; }
            public bool loop { get => _ps.Loop; set => _ps.Loop = value; }
            public bool playOnAwake { get => _ps.PlayOnAwake; set => _ps.PlayOnAwake = value; }
            public ParticleSystemStopAction stopAction { get => _ps.StopAction; set => _ps.StopAction = value; }

            public MinMaxCurve startLifetime
            {
                get => new(_ps.Lifetime);
                set => _ps.Lifetime = value.mode == ParticleSystemCurveMode.TwoConstants ? value.constantMax : value.constant;
            }

            public float startLifetimeMultiplier { get => _ps.Lifetime; set => _ps.Lifetime = value; }
            public MinMaxCurve startSpeed { get; set; }
            public float startSpeedMultiplier { get; set; }
            public MinMaxCurve startSize { get; set; }
            public float startSizeMultiplier { get; set; }
            public bool startSize3D { get; set; }
            public MinMaxCurve startSizeX { get; set; }
            public MinMaxCurve startSizeY { get; set; }
            public MinMaxCurve startSizeZ { get; set; }
            public MinMaxCurve startRotation { get; set; }
            public float startRotationMultiplier { get; set; }
            public MinMaxCurve startDelay { get; set; }
            public MinMaxGradient startColor { get; set; }
            public MinMaxCurve gravityModifier { get; set; }
            public float gravityModifierMultiplier { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
            public Transform customSimulationSpace { get; set; }
            public float simulationSpeed { get; set; }
            public ParticleSystemScalingMode scalingMode { get; set; }
            public int maxParticles { get; set; }
            public bool prewarm { get; set; }
            public bool useUnscaledTime { get; set; }
            public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; set; }
            public float flipRotation { get; set; }
        }

        public struct EmissionModule
        {
            internal EmissionModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxCurve rateOverTime { get; set; }
            public float rateOverTimeMultiplier { get; set; }
            public MinMaxCurve rateOverDistance { get; set; }
            public float rateOverDistanceMultiplier { get; set; }
            public int burstCount { get; set; }
            public void SetBursts(Burst[] bursts) { }
            public void SetBursts(Burst[] bursts, int size) { }
            public void SetBurst(int index, Burst burst) { }
            public Burst GetBurst(int index) => default;
        }

        public struct ShapeModule
        {
            internal ShapeModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public ParticleSystemShapeType shapeType { get; set; }
            public float radius { get; set; }
            public float radiusThickness { get; set; }
            public float angle { get; set; }
            public float arc { get; set; }
            public float length { get; set; }
            public Vector3 position { get; set; }
            public Vector3 rotation { get; set; }
            public Vector3 scale { get; set; }
            public float randomDirectionAmount { get; set; }
            public float sphericalDirectionAmount { get; set; }
            public bool alignToDirection { get; set; }
            public Mesh mesh { get; set; }
        }

        public struct ColorOverLifetimeModule
        {
            internal ColorOverLifetimeModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxGradient color { get; set; }
        }

        public struct SizeOverLifetimeModule
        {
            internal SizeOverLifetimeModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxCurve size { get; set; }
            public float sizeMultiplier { get; set; }
            public bool separateAxes { get; set; }
        }

        public struct VelocityOverLifetimeModule
        {
            internal VelocityOverLifetimeModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxCurve x { get; set; }
            public MinMaxCurve y { get; set; }
            public MinMaxCurve z { get; set; }
            public MinMaxCurve radial { get; set; }
            public MinMaxCurve orbitalY { get; set; }
            public MinMaxCurve speedModifier { get; set; }
            public ParticleSystemSimulationSpace space { get; set; }
        }

        public struct LimitVelocityOverLifetimeModule
        {
            internal LimitVelocityOverLifetimeModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxCurve limit { get; set; }
            public float limitMultiplier { get; set; }
            public float dampen { get; set; }
            public MinMaxCurve drag { get; set; }
        }

        public struct NoiseModule
        {
            internal NoiseModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxCurve strength { get; set; }
            public float strengthMultiplier { get; set; }
            public float frequency { get; set; }
            public MinMaxCurve scrollSpeed { get; set; }
            public bool damping { get; set; }
            public int octaveCount { get; set; }
        }

        public struct TrailModule
        {
            internal TrailModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public float ratio { get; set; }
            public MinMaxCurve lifetime { get; set; }
            public MinMaxCurve widthOverTrail { get; set; }
            public MinMaxGradient colorOverLifetime { get; set; }
            public MinMaxGradient colorOverTrail { get; set; }
            public bool dieWithParticles { get; set; }
            public bool inheritParticleColor { get; set; }
            public float minVertexDistance { get; set; }
        }

        public struct RotationOverLifetimeModule
        {
            internal RotationOverLifetimeModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public MinMaxCurve z { get; set; }
            public float zMultiplier { get; set; }
        }

        public struct TextureSheetAnimationModule
        {
            internal TextureSheetAnimationModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
            public int numTilesX { get; set; }
            public int numTilesY { get; set; }
        }

        public struct CollisionModule
        {
            internal CollisionModule(ParticleSystem ps) { }
            public bool enabled { get; set; }
        }
    }
}
