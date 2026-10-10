using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Game.Skills;
using Dovus.Game.Weapons;
using Dovus.Game.Assets;
using UnityEngine;


namespace Dovus.Game.Actors
{
    /// <summary>
    /// Humanoid Animator kancası. Mixamo/Synty controller state'lerine
    /// <see cref="Animator.Play"/> ile gider (SetTrigger yok — controller'da trigger şart değil).
    /// </summary>
    public sealed partial class ActorView : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        /// <summary>Locomotion state hız çarpanı: koşu klibinin doğal hızı aşılınca adım da hızlanır.</summary>
        public const string ParamLocoPlayback = "LocoPlayback";
        /// <summary>Koşu klibinin ölçülmüş zemin hızı (model birimi/sn); binder klipten yazar.</summary>
        public const string ParamLocoRunSpeed = "LocoRunSpeed";
        /// <summary>O-anim(c): CastChannel döngüsü sürerken true — binder'daki dönüş geçişini kilitler.</summary>
        public const string ParamChannelHold = "ChannelHold";
        /// <summary>O-anim(c): CastGuard (blok) döngüsü sürerken true — binder'daki dönüş geçişini kilitler.</summary>
        public const string ParamGuardHold = "GuardHold";
        /// <summary>Düz vuruş klip hız çarpanı (state speedParameter); hasar zamanlamasından bağımsız.</summary>
        public const string ParamStrikeSpeed = "StrikeSpeed";
        public const string ParamForward = "Forward";
        public const string ParamStrafe = "Strafe";
        public const string ParamFocus = "Focus";
        public const string ParamPierce = "Pierce";
        public const string ParamSpread = "Spread";
        public const string ParamLift = "Lift";
        public const string StateDodge = "Dodge";
        public const string StateHit = "Hit";
        public const string StateDeath = "Death";
        public const string StateBasicStrike = "BasicStrike";
        public const string StateLocomotion = "Locomotion";
        public const string StateCastPierce = "CastPierce";
        public const string StateCastShoot = "CastShoot";
        public const string UpperLayerName = "UpperBody";
        public const string UpperStatePrefix = "Upper";

        static readonly string[] StrikeCycle = { StateBasicStrike, "BasicStrikeB", "BasicStrikeC" };

        // Eski sabit isimleri — Trigger*() çağrıları artık Play(state).
        public const string TriggerDodge = StateDodge;
        public const string TriggerHit = StateHit;
        public const string TriggerDeath = StateDeath;

        static readonly string[] FamilyStates =
        {
            null,
            "CastPierce",
            "CastSweep",
            "CastSlam",
            "CastChannel",
            "CastGuard",
        };

        [SerializeField] Animator _animator;
        [SerializeField] Renderer[] _hideWhenVisualPresent;

        public Animator Animator => _animator;

        static readonly int HashBasic = Animator.StringToHash(StateBasicStrike);
        static readonly int HashBasicB = Animator.StringToHash("BasicStrikeB");
        static readonly int HashBasicC = Animator.StringToHash("BasicStrikeC");
        static readonly int HashPierce = Animator.StringToHash(StateCastPierce);
        static readonly int HashSweep = Animator.StringToHash("CastSweep");
        static readonly int HashSlam = Animator.StringToHash("CastSlam");
        static readonly int HashChannel = Animator.StringToHash("CastChannel");
        static readonly int HashGuard = Animator.StringToHash("CastGuard");
        static readonly int HashShoot = Animator.StringToHash(StateCastShoot);
        static readonly int HashUpperBasic = Animator.StringToHash("UpperBasicStrike");
        static readonly int HashUpperBasicB = Animator.StringToHash("UpperBasicStrikeB");
        static readonly int HashUpperBasicC = Animator.StringToHash("UpperBasicStrikeC");
        static readonly int HashUpperPierce = Animator.StringToHash("UpperCastPierce");
        static readonly int HashUpperSweep = Animator.StringToHash("UpperCastSweep");
        static readonly int HashUpperSlam = Animator.StringToHash("UpperCastSlam");
        static readonly int HashUpperChannel = Animator.StringToHash("UpperCastChannel");
        static readonly int HashUpperGuard = Animator.StringToHash("UpperCastGuard");
        static readonly int HashUpperShoot = Animator.StringToHash("UpperCastShoot");

        /// <summary>Vuruş veya skill klibi hâlâ oynuyorsa true. Yürüme ve dodge sayılmaz.</summary>
        public bool IsAttackPose
        {
            get
            {
                if (_animator == null || !_animator.isActiveAndEnabled
                    || _animator.runtimeAnimatorController == null)
                    return false;
                if (_upperLayer == -2)
                    _upperLayer = _animator.GetLayerIndex(UpperLayerName);
                if (LayerAttacking(_upperLayer))
                    return true;
                return LayerAttacking(0);
            }
        }

        bool LayerAttacking(int layer)
        {
            if (layer < 0)
                return false;
            if (_animator.IsInTransition(layer)
                && IsAttackHash(_animator.GetNextAnimatorStateInfo(layer).shortNameHash))
                return true;
            return IsAttackHash(_animator.GetCurrentAnimatorStateInfo(layer).shortNameHash);
        }

        static bool IsAttackHash(int hash) =>
            hash == HashBasic || hash == HashBasicB || hash == HashBasicC
            || hash == HashPierce || hash == HashSweep || hash == HashSlam
            || hash == HashChannel || hash == HashGuard || hash == HashShoot
            || hash == HashUpperBasic || hash == HashUpperBasicB || hash == HashUpperBasicC
            || hash == HashUpperPierce || hash == HashUpperSweep || hash == HashUpperSlam
            || hash == HashUpperChannel || hash == HashUpperGuard || hash == HashUpperShoot;

        public void Bind(Animator animator, params Renderer[] hideWhenPresent)
        {
            _animator = animator;
            _upperLayer = -2;
            _hideWhenVisualPresent = hideWhenPresent;
            _baseController = animator != null ? animator.runtimeAnimatorController : null;
            _currentWeaponKey = null;
            _handProps = null;
            _strikeSpeedParamCached = -1;
            if (_animator != null && _hideWhenVisualPresent != null)
            {
                for (int i = 0; i < _hideWhenVisualPresent.Length; i++)
                {
                    if (_hideWhenVisualPresent[i] != null)
                        _hideWhenVisualPresent[i].enabled = false;
                }
            }
        }

        // --- Silah arketipi: controller override (sunum) --------------------------------------

        RuntimeAnimatorController _baseController;
        WeaponVisualRegistry _weaponRegistry;
        bool _weaponRegistryLoaded;
        string _currentWeaponKey;
        WeaponHandPropsView _handProps;

        /// <summary>Ağır silah arketiplerinde (Çekiç/Top) donuk his: temel hız çarpanı.</summary>
        const float HeavyAnimSpeed = 0.9f;

        /// <summary>
        /// docs/element-sistemi.json weapons[].animations_key — arketip override controller'ını
        /// uygular (yoksa temel controller'da kalır, hata yok — Mixamo override'lar bu PC dışında
        /// gitignored olduğundan boş olabilir). İdempotent: aynı anahtar tekrar gelirse no-op.
        /// </summary>
        public void SetWeapon(string animationsKey) => SetWeapon(animationsKey, force: false);

        public void SetWeapon(string animationsKey, bool force)
        {
            animationsKey ??= string.Empty;
            if (!force && string.Equals(_currentWeaponKey, animationsKey, System.StringComparison.Ordinal))
                return;
            _currentWeaponKey = animationsKey;

            if (!_weaponRegistryLoaded)
            {
                _weaponRegistry = AssetLoader.Load<WeaponVisualRegistry>("Animation/WeaponVisualRegistry", null);
                _weaponRegistryLoaded = true;
            }

            string archetype = WeaponArchetypeMap.ArchetypeFor(animationsKey);
            ApplyArchetypeController(archetype);

            float speed = archetype is WeaponArchetypeMap.Hammer or WeaponArchetypeMap.Gun ? HeavyAnimSpeed : 1f;
            if (_animator != null)
                _animator.speed = speed;
            _savedAnimatorSpeed = speed;

            ApplyHandProps(animationsKey, force);
        }

        /// <summary>Elde silah prop'u: sağ/sol el kemiğine takılı mesh, SetWeapon ile birlikte değişir.</summary>
        void ApplyHandProps(string animationsKey, bool force = false)
        {
            if (_animator == null)
                return;
            if (_handProps == null)
                _handProps = _animator.GetComponent<WeaponHandPropsView>();
            if (_handProps == null)
                _handProps = _animator.gameObject.AddComponent<WeaponHandPropsView>();
            if (force)
                _handProps.ForceApply(animationsKey);
            else
                _handProps.Apply(animationsKey);
        }

        void ApplyArchetypeController(string archetypeKey)
        {
            if (_animator == null)
                return;
            RuntimeAnimatorController ctrl = _weaponRegistry != null ? _weaponRegistry.FindOverride(archetypeKey) : null;
            // Override yalnız kendi temel controller'ında anlamlı; başka iskeletin controller'ını ezmez.
            bool foreign = ctrl is AnimatorOverrideController ovr && ovr.runtimeAnimatorController != _baseController;
            RuntimeAnimatorController target = ctrl != null && !foreign ? ctrl : _baseController;
            if (_animator.runtimeAnimatorController == target)
                return;
            _animator.runtimeAnimatorController = target;
            _upperLayer = -2;
            _motionKey = string.Empty;
            _strikeSpeedParamCached = -1;
        }

        public void PulseRune(Rune rune, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            CastBodyFamily family = CastBodyMapper.FromVerbAndSilhouette(rune, silhouette);
            ApplyAxes(silhouette);
            FireFamily(family);
        }

        /// <summary>
        /// element-sistemi <c>animation_type</c> → controller state (skill başına farklı clip).
        /// </summary>
        public void PulseAnimationType(string animationTypeId, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            ApplyAxes(silhouette);
            string state = AnimationTypeToState(animationTypeId);
            PlayState(state);
        }

        /// <summary>
        /// Merkez düz vuruş. Yalnız görsel: yakın silahta A→B→C klip döngüsü, menzilli silahta
        /// atış klibi. Hasar/zamanlama buradan etkilenmez.
        /// </summary>
        public void PulseBasicStrike()
        {
            ApplyStrikeSpeedBeforeAction();
            if (RangedDelivery && PlayAction(StateCastShoot))
                return;

            float now = Time.time;
            if (now - _lastStrikeTime > StrikeComboResetSec)
                _strikeIndex = 0;
            _lastStrikeTime = now;
            string state = StrikeCycle[_strikeIndex % StrikeCycle.Length];
            _strikeIndex++;
            if (!PlayAction(state))
                PlayAction(StateBasicStrike);
        }

        public void ResetBasicChain() => _strikeIndex = 0;

        /// <summary>Kuşanılmış silah menzilli teslim yolu mu (mermi cast'i atış klibine düşer).</summary>
        public bool RangedDelivery { get; set; }

        /// <summary>Düz vuruş döngüsü sıfırlanma süresi (sn).</summary>
        public float StrikeComboResetSec { get; set; } = ActorViewDefaults.StrikeComboResetSec;

        /// <summary>Düz vuruş klip hızı (Animator StrikeSpeed); HeavyAnimSpeed ile çarpılmaz.</summary>
        public float BasicStrikeAnimSpeed { get; set; } = 1f;

        /// <summary>Speed parametresi bu eşiğin üstündeyse aksiyon üst gövde katmanına gider.</summary>
        public float UpperBodyMinSpeed { get; set; } = ActorViewDefaults.UpperBodyMinSpeed;

        int _strikeIndex;
        /// <summary>-1 = bilinmiyor, 0 = parametre yok, 1 = var.</summary>
        int _strikeSpeedParamCached = -1;
        float _lastStrikeTime = float.NegativeInfinity;
        int _upperLayer = -2;

        public void Trigger(string triggerOrStateName)
        {
            // Eski API: isim state olarak Play edilir (Mixamo'da trigger yok).
            PlayState(triggerOrStateName);
        }

        /// <summary>0 = idle (sabit), 1 ≈ koşu. Küçük stick gürültüsü idle fidget’e sızmasın.</summary>
        public void SetSpeed(float normalized01) => SetSpeed(normalized01, 0f);

        /// <summary>Sönümlü Speed; <paramref name="dampSec"/> 0 ise anında.</summary>
        public void SetSpeed(float normalized01, float dampSec)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            float s = Mathf.Clamp01(normalized01);
            if (s < ActorViewDefaults.IdleSpeedCutoff)
                s = 0f;
            if (dampSec > 0f && Time.deltaTime > 0f)
                _animator.SetFloat(ParamSpeed, s, dampSec, Time.deltaTime);
            else
                _animator.SetFloat(ParamSpeed, s);
            if (s == 0f && HasFloat(ParamLocoPlayback))
                _animator.SetFloat(ParamLocoPlayback, 1f);
        }

        /// <summary>
        /// Gerçek hızla locomotion: blend eşikleri kliplerin ölçülmüş zemin hızı (model birimi) olduğundan
        /// ayak yere bastığı yerde kalır. Koşu hızı aşılınca klip en çok <paramref name="maxPlaybackMult"/>
        /// kat hızlanır. Controller eski (ölçümsüz) ise normalize <see cref="SetSpeed(float,float)"/>'e düşer.
        /// </summary>
        void ApplyTemplateLocomotion(in LocoBlend blend, float refMps, float dampSec, float maxPlayback)
        {
            SetLocomotion(blend.SpeedMps, refMps, dampSec, maxPlayback);
            if (blend.Forward < ActorViewDefaults.BackwardForwardThreshold && blend.SpeedMps > ActorViewDefaults.BackwardSpeedMinMps && HasFloat(ParamLocoPlayback))
            {
                float playback = _animator.GetFloat(ParamLocoPlayback);
                _animator.SetFloat(ParamLocoPlayback, -Mathf.Abs(playback < ActorViewDefaults.MinPositive ? 1f : playback));
            }
            SafeSetFloat(ParamForward, blend.Forward);
            SafeSetFloat(ParamStrafe, blend.Strafe);
        }

        void TryTrigger(string trigger)
        {
            if (string.IsNullOrEmpty(trigger) || _animator == null)
                return;
            foreach (var p in _animator.parameters)
            {
                if (p.name == trigger && p.type == AnimatorControllerParameterType.Trigger)
                {
                    _animator.SetTrigger(trigger);
                    return;
                }
            }
            DesignWarnings.Once(
                "motion.anim.trigger." + trigger,
                "Animator tetikleyicisi yok: " + trigger + ". State oynatılıyor.");
        }

        void SafeSetFloat(string name, float value)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Float)
                {
                    _animator.SetFloat(name, value);
                    return;
                }
            }
        }

        /// <summary>
        /// O-anim(c): ManifestationDirector'daki var olan sürdürülen cast / kalkan sinyallerini
        /// Animator'a iletir — binder bu bool'lara göre CastChannel/CastGuard'ın otomatik dönüş
        /// geçişini kilitler (bkz. MixamoAnimatorBind). Parametre yoksa no-op.
        /// </summary>
        public void SetHoldFlags(bool channelHeld, bool guardHeld)
        {
            SafeSetBool(ParamChannelHold, channelHeld);
            SafeSetBool(ParamGuardHold, guardHeld);
        }

        void SafeSetBool(string name, bool value)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            foreach (var p in _animator.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(name, value);
                    return;
                }
            }
        }
    }
}
