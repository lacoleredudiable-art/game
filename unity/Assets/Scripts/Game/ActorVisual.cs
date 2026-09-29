using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Humanoid Animator kancası. Mixamo/Synty controller state'lerine
    /// <see cref="Animator.Play"/> ile gider (SetTrigger yok — controller'da trigger şart değil).
    /// </summary>
    public sealed class ActorVisual : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        /// <summary>Locomotion state hız çarpanı: koşu klibinin doğal hızı aşılınca adım da hızlanır.</summary>
        public const string ParamLocoPlayback = "LocoPlayback";
        /// <summary>Koşu klibinin ölçülmüş zemin hızı (model birimi/sn); binder klipten yazar.</summary>
        public const string ParamLocoRunSpeed = "LocoRunSpeed";
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
        public const string TriggerBasicStrike = StateBasicStrike;

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
            if (_animator != null && _hideWhenVisualPresent != null)
            {
                for (int i = 0; i < _hideWhenVisualPresent.Length; i++)
                {
                    if (_hideWhenVisualPresent[i] != null)
                        _hideWhenVisualPresent[i].enabled = false;
                }
            }
        }

        public void PulseRune(Rune rune, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            CastBodyFamily family = CastBodyMapper.FromVerbAndSilhouette(rune, silhouette);
            ApplyAxes(silhouette);
            FireFamily(family);
        }

        public void PulseFamily(CastBodyFamily family, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

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

        /// <summary>Kuşanılmış silah menzilli teslim yolu mu (mermi cast'i atış klibine düşer).</summary>
        public bool RangedDelivery { get; set; }

        /// <summary>Düz vuruş döngüsü sıfırlanma süresi (sn).</summary>
        public float StrikeComboResetSec { get; set; } = 1.2f;

        /// <summary>Speed parametresi bu eşiğin üstündeyse aksiyon üst gövde katmanına gider.</summary>
        public float UpperBodyMinSpeed { get; set; } = 0.15f;

        int _strikeIndex;
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
            if (s < 0.08f)
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
        public void SetLocomotion(float worldSpeedMps, float normalizeRefMps, float dampSec, float maxPlaybackMult)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            if (!HasFloat(ParamLocoRunSpeed) || !HasFloat(ParamLocoPlayback))
            {
                SetSpeed(worldSpeedMps / Mathf.Max(0.01f, normalizeRefMps), dampSec);
                return;
            }

            float run = Mathf.Max(0.01f, _animator.GetFloat(ParamLocoRunSpeed));
            float model = worldSpeedMps / Mathf.Max(0.01f, _animator.transform.lossyScale.y);
            if (model < run * 0.08f)
                model = 0f;
            float playback = Mathf.Clamp(model / run, 1f, Mathf.Max(1f, maxPlaybackMult));
            if (dampSec > 0f && Time.deltaTime > 0f)
            {
                _animator.SetFloat(ParamSpeed, Mathf.Min(model, run), dampSec, Time.deltaTime);
                _animator.SetFloat(ParamLocoPlayback, playback, dampSec, Time.deltaTime);
            }
            else
            {
                _animator.SetFloat(ParamSpeed, Mathf.Min(model, run));
                _animator.SetFloat(ParamLocoPlayback, playback);
            }
        }

        bool HasFloat(string name)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Float)
                    return true;
            }
            return false;
        }

        /// <summary>Aksiyon state'ine crossfade süresi (0 = sert kesim).</summary>
        public float CrossFadeSec { get; set; }

        public void ResetToLocomotion()
        {
            PlayState(StateLocomotion);
        }

        public static string AnimationTypeToState(string animationTypeId)
        {
            if (string.IsNullOrEmpty(animationTypeId))
                return "CastChannel";

            return animationTypeId switch
            {
                "cast_projectile" => "CastPierce",
                "cast_aoe" => "CastSweep",
                "cast_self" => "CastChannel",
                "channel" => "CastChannel",
                "melee_thrust" => "CastPierce",
                "melee_slash" => "CastSweep",
                "melee_punch" => "CastSlam",
                "dash" => StateDodge,
                "instant" => "CastSlam",
                "summon" => "CastChannel",
                _ => AnimationBridge.MapToQuaterniusState(animationTypeId)
            };
        }

        void ApplyAxes(EffectSilhouette s)
        {
            SafeSetFloat(ParamFocus, s.Focus);
            SafeSetFloat(ParamPierce, s.Pierce);
            SafeSetFloat(ParamSpread, s.Spread);
            SafeSetFloat(ParamLift, s.Lift);
        }

        void FireFamily(CastBodyFamily family)
        {
            int i = (int)family;
            if (i <= 0 || i >= FamilyStates.Length)
                return;
            PlayState(FamilyStates[i]);
        }

        void PlayState(string stateName) => PlayAction(stateName);

        /// <summary>
        /// Aksiyon state'i oynatır. Menzilli teslimde mermi cast'i atış klibine düşer; karakter
        /// hareket ederken Cast/vuruş state'i varsa üst gövde katmanında oynar (bacaklar
        /// Locomotion'da kalır). State yoksa false.
        /// </summary>
        public bool PlayAction(string stateName)
        {
            if (_animator == null || !_animator.isActiveAndEnabled
                || _animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(stateName))
                return false;

            if (RangedDelivery && stateName == StateCastPierce
                && _animator.HasState(0, Animator.StringToHash(StateCastShoot)))
                stateName = StateCastShoot;

            if (IsMoving() && TryPlayUpper(stateName))
                return true;

            int hash = Animator.StringToHash(stateName);
            if (!_animator.HasState(0, hash))
                return false;

            CrossFadeOrRestart(0, hash);
            ClearUpper();
            return true;
        }

        void ClearUpper()
        {
            if (_upperLayer == -2)
                _upperLayer = _animator.GetLayerIndex(UpperLayerName);
            if (_upperLayer < 0)
                return;
            int empty = Animator.StringToHash("Empty");
            if (_animator.HasState(_upperLayer, empty)
                && _animator.GetCurrentAnimatorStateInfo(_upperLayer).shortNameHash != empty)
                _animator.CrossFadeInFixedTime(empty, CrossFadeSec, _upperLayer, 0f);
        }

        bool IsMoving()
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == ParamSpeed && p.type == AnimatorControllerParameterType.Float)
                    return _animator.GetFloat(ParamSpeed) >= UpperBodyMinSpeed;
            }
            return false;
        }

        bool TryPlayUpper(string stateName)
        {
            if (!stateName.StartsWith("Cast") && !stateName.StartsWith(StateBasicStrike))
                return false;
            if (_upperLayer == -2)
                _upperLayer = _animator.GetLayerIndex(UpperLayerName);
            if (_upperLayer < 0)
                return false;
            int hash = Animator.StringToHash(UpperStatePrefix + stateName);
            if (!_animator.HasState(_upperLayer, hash))
                return false;
            CrossFadeOrRestart(_upperLayer, hash);
            return true;
        }

        void CrossFadeOrRestart(int layer, int hash)
        {
            bool sameState = _animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == hash
                             || (_animator.IsInTransition(layer)
                                 && _animator.GetNextAnimatorStateInfo(layer).shortNameHash == hash);
            if (CrossFadeSec > 0f && !sameState)
            {
                _animator.CrossFadeInFixedTime(hash, CrossFadeSec, layer, 0f);
                return;
            }

            _animator.Play(hash, layer, 0f);
            _animator.Update(0f);
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
    }
}
