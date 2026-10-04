using Dovus.Game.Actors;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine
{
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
