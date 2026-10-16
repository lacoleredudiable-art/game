using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Damage;
using Dovus.Core.Dodge;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Hud;
using Dovus.Core.Input;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Audio;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Launch;
using Dovus.Game.Skills.Presentation;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Hosts
{
    /// <summary>CastSideEffects + SkillPresentation için ince bağlayıcı (eski launch host yok).</summary>
    internal sealed class RuleEngineV4SentenceLaunchHost : ICastSideEffectsHost, ISkillPresentationHost
    {
        readonly RuleEngineV4SentenceHost _md;

        internal RuleEngineV4SentenceLaunchHost(RuleEngineV4SentenceHost md) => _md = md;

        public PlayerResourceHost PlayerResourceHost => _md._playerResource;
        public ActorStatusHost PlayerStatus => _md._playerStatus;
        public MobilityCcData MobilityCc => _md._mobilityCc;
        public EquipmentItem EquippedWeapon => _md._equippedWeapon;
        public GameClockHost Clock => _md._clock;
        public CombatTuning Combat => _md._combat;
        public SkillMotor Skills => _md._skills;
        public SkillFactory SkillFactory => _md._skillFactory;
        public HexagonView HexagonView => _md._hexagonView;
        public PlayerCooldownHost PlayerCooldownHost => _md._playerCooldown;
        public Transform Player => _md._player;
        public Transform BossTransform => _md._boss != null ? _md._boss.transform : null;
        public BossVitals BossVitals => _md._bossVitals;
        public GameTuning Colors => _md._colors;
        public VerbExecutionData VerbData => _md._verbData;
        public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
        public Skill LastFactorySkill { get => _md.LastFactorySkill; set => _md.LastFactorySkill = value; }
        public MechanicPlan LastMechanicPlan => default;
        public ISentenceDebugSink DebugHud => _md._debugHud;

        public bool TryTakeFreeMana() => _md.TryTakeFreeMana();
        public float WeaponCooldownMult() => _md.WeaponCooldownMult();
        public SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words) =>
            _md.ResolveSkillWords(words);

        public ActorView Visual => _md._visual;
        public AnimationBridge AnimationBridge => _md._animationBridge;
        public AnimationDatabase AnimationDatabase => _md._animationDatabase;
        public FollowCameraController Camera => _md._camera;
        public SfxDirector Sfx => _md._sfx;
        public HashSet<string> MissingAnimationBindings => _md._missingAnimationBindings;

        public string LastAnimationTypeId { set => _md.LastAnimationTypeId = value; }
        public string LastAnimationState { set => _md.LastAnimationState = value; }
        public bool LastAnimationPlayApplied { set => _md.LastAnimationPlayApplied = value; }
        public string LastAnimationClip { set => _md.LastAnimationClip = value; }
        public bool LastAnimationUsedFallback { set => _md.LastAnimationUsedFallback = value; }

        public void SyncVisualDelivery() => _md.SyncVisualDelivery();
        public void StartCastVfxTimer(SkillResolution skill, IReadOnlyList<SentenceWord> words) =>
            _md.StartCastVfxTimer(skill, words);
    }
}
