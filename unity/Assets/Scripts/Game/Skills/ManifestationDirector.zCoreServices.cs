using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Boss;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Skills.Flow;
using Dovus.Game.Skills.Passives;
using Dovus.Game.Skills.Sync;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        MdCoreServicesHost _coreServicesHost;
        ClosingQueue _closingQueue;
        SlotPassiveRuntime _slotPassiveRuntime;
        BossDeathSequence _bossDeathSequence;
        SentenceSync _sentenceSync;

        void EnsureCoreServices()
        {
            if (_coreServicesHost != null)
                return;
            _coreServicesHost = new MdCoreServicesHost(this);
            _closingQueue = new ClosingQueue(_coreServicesHost, _pending);
            _slotPassiveRuntime = new SlotPassiveRuntime(_coreServicesHost);
            _bossDeathSequence = new BossDeathSequence(_coreServicesHost);
            _sentenceSync = new SentenceSync(_coreServicesHost);
        }

        internal List<PendingClosing> PendingList => _pending;

        sealed class MdCoreServicesHost
            : IClosingQueueHost,
                ISlotPassiveRuntimeHost,
                IBossDeathSequenceHost,
                ISentenceSyncHost
        {
            readonly ManifestationDirector _md;

            internal MdCoreServicesHost(ManifestationDirector md) => _md = md;

            public GameTuning Colors => _md._colors;
            public SkillMotor Skills => _md._skills;
            public SkillFactory SkillFactory => _md._skillFactory;
            public EquipmentItem EquippedWeapon => _md._equippedWeapon;
            public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
            public CombatTuning Combat => _md._combat;
            public Transform DirectorTransform => _md.transform;
            public GameClockHost Clock => _md._clock;

            public Skill LastFactorySkill
            {
                get => _md.LastFactorySkill;
                set => _md.LastFactorySkill = value;
            }

            public void EnsureSkillServices() => _md.EnsureSkillServices();

            public SentenceManifestationBridge SentenceBridge => _md._sentenceBridge;

            public void EnsurePresentationCatalog() => _md.EnsurePresentationCatalog();

            public PresentationCatalog PresentationCatalog => _md.PresentationCatalog;

            public void StampScar(LivingEffectView view, ClosingHit closing) => _md.StampScar(view, closing);

            public void EnsureCastPort() => _md._castPort ??= new CastPort(_md);

            public void RunBasicClosing(PendingClosing p)
            {
                LivingEffect logic = p.View != null ? p.View.Logic : null;
                EnsureCastPort();
                _md._castPort.BeginClosing(logic);
                _md._castPipeline.RunBasic(p, _md._castPort);
            }

            public void RunSkillClosing(PendingClosing p, LivingEffect logic)
            {
                EnsureCastPort();
                _md._castPort.BeginClosing(logic);
                _md._castPipeline.RunSkill(p, _md._castPort);
            }

            public void DestroyUnityObjectAfter(Object obj, float delaySeconds) =>
                Object.Destroy(obj, delaySeconds);

            public SlotPassiveDirector SlotPassives => _md._slotPassives;
            public SentenceEngine Engine => _md._engine;
            public int SlotQueryCastId
            {
                get => _md._slotQueryCastId;
                set => _md._slotQueryCastId = value;
            }

            public BossVitals BossVitals => _md._bossVitals;
            public DamageNumberHud DamageHud => _md._damageHud;
            public ReactionReadoutHud Readout => _md._readout;
            public PassiveHud PassiveHud => _md._passiveHud;

            public bool IsHealSkill(SkillResolution skill) => ManifestationDirector.IsHealSkill(skill);

            public void ApplyClosingHeal(ClosingHit closing, SkillResolution skill, float power, float chain) =>
                _md.ApplyClosingHeal(closing, skill, power, chain);

            public float ApplyClosingDamage(
                ClosingHit closing,
                SkillResolution skill,
                bool isBasicStrike,
                float slash,
                float power,
                float chain) =>
                _md.ApplyClosingDamage(closing, skill, isBasicStrike, slash, power, chain);

            public Vector3? BossHitPoint() => _md.BossHitPoint();
            public Color? DamageTint() => _md.DamageTint();

            public BossDirector BossDirector => _md._bossDirector;
            public BossReactorController Boss => _md._boss;

            public void NoteShieldBlockIfGuarding() => _md.NoteShieldBlockIfGuarding();
            public void OnJsonShieldBlocked() => _md.OnJsonShieldBlocked();

            public HexagonInputController Input => _md._input;
            public PlayerStateMachine PlayerStates => _md._playerStates;
            public ActorStatusHost PlayerStatus => _md._playerStatus;
            public int PendingClosingCount => _md._pending.Count;

            public PlayerVitalsHost CachedPlayerVitals() => _md.CachedPlayerVitals();
            public bool SwapDrawUnlocked(double worldMs) => _md.SwapDrawUnlocked(worldMs);

            public PlaceholderFactory Placeholders => _md.Placeholders;
        }
    }
}
