using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Game.Skills;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Humanoid Animator kancasÄ±. Mixamo/Synty controller state'lerine
    /// <see cref="Animator.Play"/> ile gider (SetTrigger yok â€” controller'da trigger ÅŸart deÄŸil).
    /// </summary>
    public sealed class ActorVisual : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        /// <summary>Locomotion state hÄ±z Ã§arpanÄ±: koÅŸu klibinin doÄŸal hÄ±zÄ± aÅŸÄ±lÄ±nca adÄ±m da hÄ±zlanÄ±r.</summary>
        public const string ParamLocoPlayback = "LocoPlayback";
        /// <summary>KoÅŸu klibinin Ã¶lÃ§Ã¼lmÃ¼ÅŸ zemin hÄ±zÄ± (model birimi/sn); binder klipten yazar.</summary>
        public const string ParamLocoRunSpeed = "LocoRunSpeed";
        /// <summary>O-anim(c): CastChannel dÃ¶ngÃ¼sÃ¼ sÃ¼rerken true â€” binder'daki dÃ¶nÃ¼ÅŸ geÃ§iÅŸini kilitler.</summary>
        public const string ParamChannelHold = "ChannelHold";
        /// <summary>O-anim(c): CastGuard (blok) dÃ¶ngÃ¼sÃ¼ sÃ¼rerken true â€” binder'daki dÃ¶nÃ¼ÅŸ geÃ§iÅŸini kilitler.</summary>
        public const string ParamGuardHold = "GuardHold";
        /// <summary>DÃ¼z vuruÅŸ klip hÄ±z Ã§arpanÄ± (state speedParameter); hasar zamanlamasÄ±ndan baÄŸÄ±msÄ±z.</summary>
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

        // Eski sabit isimleri â€” Trigger*() Ã§aÄŸrÄ±larÄ± artÄ±k Play(state).
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

        /// <summary>VuruÅŸ veya skill klibi hÃ¢lÃ¢ oynuyorsa true. YÃ¼rÃ¼me ve dodge sayÄ±lmaz.</summary>
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
        WeaponHandProps _handProps;

        /// <summary>AÄŸÄ±r silah arketiplerinde (Ã‡ekiÃ§/Top) donuk his: temel hÄ±z Ã§arpanÄ±.</summary>
        const float HeavyAnimSpeed = 0.9f;

        /// <summary>
        /// docs/element-sistemi.json weapons[].animations_key â€” arketip override controller'Ä±nÄ±
        /// uygular (yoksa temel controller'da kalÄ±r, hata yok â€” Mixamo override'lar bu PC dÄ±ÅŸÄ±nda
        /// gitignored olduÄŸundan boÅŸ olabilir). Ä°dempotent: aynÄ± anahtar tekrar gelirse no-op.
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
                _weaponRegistry = Resources.Load<WeaponVisualRegistry>("Animation/WeaponVisualRegistry");
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

        /// <summary>Elde silah prop'u: saÄŸ/sol el kemiÄŸine takÄ±lÄ± mesh, SetWeapon ile birlikte deÄŸiÅŸir.</summary>
        void ApplyHandProps(string animationsKey, bool force = false)
        {
            if (_animator == null)
                return;
            if (_handProps == null)
                _handProps = _animator.GetComponent<WeaponHandProps>();
            if (_handProps == null)
                _handProps = _animator.gameObject.AddComponent<WeaponHandProps>();
            if (force)
                _handProps.ForceApply(animationsKey);
            else
                _handProps.Apply(animationsKey);
        }

        void ApplyArchetypeController(string archetypeKey)
        {
            if (_animator == null)
                return;
            RuntimeAnimatorController ctrl = _weaponRegistry != null
                ? _weaponRegistry.FindOverride(archetypeKey)
                : null;
            RuntimeAnimatorController target = ctrl != null ? ctrl : _baseController;
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
        /// element-sistemi <c>animation_type</c> â†’ controller state (skill baÅŸÄ±na farklÄ± clip).
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
        /// Merkez dÃ¼z vuruÅŸ. YalnÄ±z gÃ¶rsel: yakÄ±n silahta Aâ†’Bâ†’C klip dÃ¶ngÃ¼sÃ¼, menzilli silahta
        /// atÄ±ÅŸ klibi. Hasar/zamanlama buradan etkilenmez.
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

        /// <summary>KuÅŸanÄ±lmÄ±ÅŸ silah menzilli teslim yolu mu (mermi cast'i atÄ±ÅŸ klibine dÃ¼ÅŸer).</summary>
        public bool RangedDelivery { get; set; }

        /// <summary>DÃ¼z vuruÅŸ dÃ¶ngÃ¼sÃ¼ sÄ±fÄ±rlanma sÃ¼resi (sn).</summary>
        public float StrikeComboResetSec { get; set; } = 1.2f;

        /// <summary>DÃ¼z vuruÅŸ klip hÄ±zÄ± (Animator StrikeSpeed); HeavyAnimSpeed ile Ã§arpÄ±lmaz.</summary>
        public float BasicStrikeAnimSpeed { get; set; } = 1f;

        /// <summary>Speed parametresi bu eÅŸiÄŸin Ã¼stÃ¼ndeyse aksiyon Ã¼st gÃ¶vde katmanÄ±na gider.</summary>
        public float UpperBodyMinSpeed { get; set; } = 0.15f;

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

        /// <summary>0 = idle (sabit), 1 â‰ˆ koÅŸu. KÃ¼Ã§Ã¼k stick gÃ¼rÃ¼ltÃ¼sÃ¼ idle fidgetâ€™e sÄ±zmasÄ±n.</summary>
        public void SetSpeed(float normalized01) => SetSpeed(normalized01, 0f);

        /// <summary>SÃ¶nÃ¼mlÃ¼ Speed; <paramref name="dampSec"/> 0 ise anÄ±nda.</summary>
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
        /// GerÃ§ek hÄ±zla locomotion: blend eÅŸikleri kliplerin Ã¶lÃ§Ã¼lmÃ¼ÅŸ zemin hÄ±zÄ± (model birimi) olduÄŸundan
        /// ayak yere bastÄ±ÄŸÄ± yerde kalÄ±r. KoÅŸu hÄ±zÄ± aÅŸÄ±lÄ±nca klip en Ã§ok <paramref name="maxPlaybackMult"/>
        /// kat hÄ±zlanÄ±r. Controller eski (Ã¶lÃ§Ã¼msÃ¼z) ise normalize <see cref="SetSpeed(float,float)"/>'e dÃ¼ÅŸer.
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

        void ApplyStrikeSpeedBeforeAction()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            if (_strikeSpeedParamCached < 0)
                _strikeSpeedParamCached = HasFloat(ParamStrikeSpeed) ? 1 : 0;
            if (_strikeSpeedParamCached == 1)
                _animator.SetFloat(ParamStrikeSpeed, BasicStrikeAnimSpeed);
        }

        /// <summary>Aksiyon state'ine crossfade sÃ¼resi (0 = sert kesim).</summary>
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
        /// Aksiyon state'i oynatÄ±r. Menzilli teslimde mermi cast'i atÄ±ÅŸ klibine dÃ¼ÅŸer; karakter
        /// hareket ederken Cast/vuruÅŸ state'i varsa Ã¼st gÃ¶vde katmanÄ±nda oynar (bacaklar
        /// Locomotion'da kalÄ±r). State yoksa false.
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

        string _motionKey = string.Empty;
        float _savedAnimatorSpeed = 1f;

        /// <summary>
        /// KalÄ±p fazÄ±: state/trigger tablodan, bacak blend'i kalÄ±bÄ±n hÄ±zÄ±ndan.
        /// DÃ¶nÃ¼ÅŸ klibi ayrÄ±ca gÃ¶vde yaw'Ä± ile birlikte gider.
        /// </summary>
        public void DriveMotion(
            in LocoBlend blend,
            string key,
            float animSpeed,
            bool spin,
            MotionAnimTable table,
            string weaponKey,
            int verbId,
            float refMps,
            float dampSec,
            float maxPlayback)
        {
            float clipRun = ClipRunMps(refMps);
            bool tooFast = LocoBlend.NeedsDashPose(blend.SpeedMps, clipRun);
            string playKey = LocoBlend.PresentationKey(key, blend.SpeedMps, clipRun);
            LocoBlend legs = tooFast ? default : blend;
            float cap = Mathf.Min(maxPlayback > 0.05f ? maxPlayback : LocoBlend.TemplatePlaybackCap, LocoBlend.TemplatePlaybackCap);
            ApplyTemplateLocomotion(legs, refMps, dampSec, cap);
            if (string.Equals(_motionKey, playKey, System.StringComparison.Ordinal))
                return;
            _motionKey = playKey ?? string.Empty;
            MotionAnimClip clip = (table ?? MotionAnimTable.BuiltIn).Resolve(_motionKey, weaponKey, verbId);
            clip = ApplySidestepMirror(clip, blend.Strafe);
            PlayMotionClip(clip, animSpeed, spin);
        }

        /// <summary>
        /// O-anim(c): yÃ¶n zaten blend.Strafe'de ucuzca bilindiÄŸinden "Sidestep" tek klibi saÄŸa
        /// giderken Animator'Ä±n humanoid mirror'Ä±yla "SidestepRight"e dÃ¶ner; state yoksa (Ã¶r.
        /// eski override controller) solda kalÄ±r â€” <see cref="PlayAction"/> HasState ile korur.
        /// </summary>
        MotionAnimClip ApplySidestepMirror(MotionAnimClip clip, float strafe)
        {
            if (strafe <= 0.15f || !string.Equals(clip.State, "Sidestep", System.StringComparison.Ordinal))
                return clip;
            if (_animator == null || !_animator.HasState(0, Animator.StringToHash("SidestepRight")))
                return clip;
            return new MotionAnimClip(clip.Key, "SidestepRight", clip.Trigger, clip.Fallback);
        }

        public void EndMotionAnim()
        {
            _motionKey = string.Empty;
            if (_animator != null)
                _animator.speed = _savedAnimatorSpeed > 0.01f ? _savedAnimatorSpeed : 1f;
        }

        void PlayMotionClip(MotionAnimClip clip, float animSpeed, bool spin)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            TryTrigger(clip.Trigger);
            bool played = PlayAction(clip.State);
            if (!played)
            {
                DesignWarnings.Once(
                    "motion.anim.state." + clip.State,
                    "Animator state yok: " + clip.State + ". Yedek locomotion/saldÄ±rÄ± klibi.");
                played = PlayAction(clip.Fallback ? MotionAnimTable.AttackFallbackState : MotionAnimTable.FallbackState);
                if (!played)
                    PlayAction(MotionAnimTable.FallbackState);
            }

            float rate = animSpeed > 0.05f ? animSpeed : 1f;
            if (spin || !MotionAnimTable.IsLocomotionKey(clip.Key))
            {
                if (_savedAnimatorSpeed <= 0.01f)
                    _savedAnimatorSpeed = _animator.speed > 0.01f ? _animator.speed : 1f;
                _animator.speed = rate;
            }
            else
            {
                _animator.speed = _savedAnimatorSpeed > 0.01f ? _savedAnimatorSpeed : 1f;
            }
        }

        float ClipRunMps(float fallback)
        {
            if (_animator != null && _animator.isActiveAndEnabled
                && _animator.runtimeAnimatorController != null
                && HasFloat(ParamLocoRunSpeed))
            {
                float run = _animator.GetFloat(ParamLocoRunSpeed);
                if (run > 0.05f)
                    return run;
            }
            return fallback > 0.05f ? fallback : 2.24f;
        }

        void ApplyTemplateLocomotion(in LocoBlend blend, float refMps, float dampSec, float maxPlayback)
        {
            SetLocomotion(blend.SpeedMps, refMps, dampSec, maxPlayback);
            if (blend.Forward < -0.35f && blend.SpeedMps > 0.2f && HasFloat(ParamLocoPlayback))
            {
                float playback = _animator.GetFloat(ParamLocoPlayback);
                _animator.SetFloat(ParamLocoPlayback, -Mathf.Abs(playback < 0.01f ? 1f : playback));
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
                "Animator tetikleyicisi yok: " + trigger + ". State oynatÄ±lÄ±yor.");
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
        /// O-anim(c): ManifestationDirector'daki var olan sÃ¼rdÃ¼rÃ¼len cast / kalkan sinyallerini
        /// Animator'a iletir â€” binder bu bool'lara gÃ¶re CastChannel/CastGuard'Ä±n otomatik dÃ¶nÃ¼ÅŸ
        /// geÃ§iÅŸini kilitler (bkz. MixamoAnimatorBind). Parametre yoksa no-op.
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
