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
    /// İnsansı kemikler aynı adlı çocuk transform'lardan bulunur; ActorGroundingController ayak ölçümü böyle yapılır.
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
}
