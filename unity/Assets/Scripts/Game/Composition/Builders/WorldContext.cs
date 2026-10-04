using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>Paylaşılan sahne kurulum referansları; kurucular sırayla doldurur.</summary>
    public sealed class WorldContext
    {
        public readonly PrototypeBootstrap Host;
        public readonly Transform SceneRoot;

        public CombatTuning Combat;
        public TuningConfig TuningConfig;
        public GameClock Clock;
        public GameObject Arena;
        public float WalkHalf;
        public float SpawnMaxR;

        public GameObject Player;
        public GameObject Ally;
        public GameObject Boss;
        public AllyDummy AllyDummy;
        public ActorPose PlayerPose;
        public PlayerVitals PlayerVitals;
        public PlayerResource PlayerResource;
        public PlayerCooldown PlayerCooldown;
        public ActorStatus PlayerStatus;
        public ActorStatus BossStatus;
        public DodgeMotion DodgeMotion;
        public AfterimageTrail Afterimage;
        public BossReactor BossReactor;
        public BossVitals BossVitals;
        public BossTelegraph BossTelegraph;
        public FollowCamera FollowCamera;
        public Light Sun;

        public GameObject HexagonRoot;
        public HexagonView HexagonView;
        public HexagonOverlayCamera Overlay;
        public Camera OverlayCamera;
        public HexagonInput HexagonInput;
        public InkTrail InkTrail;
        public SyllableFeedback SyllableFeedback;
        public SentenceDebugHud SentenceDebugHud;
        public ReactionReadout ReactionReadout;
        public VitalsHud VitalsHud;
        public DamageNumberHud DamageNumberHud;
        public PassiveHud PassiveHud;
        public SkillMotor Skills;
        public RuneManager RuneManager;
        public RuneLoadout RuneLoadout;
        public ElementSystemDesign ElementDesign;
        public ElementSystemAssetCatalog AssetCatalog;
        public SkillFactory SkillFactory;
        public EquipmentItem EquippedWeapon;
        public EquipmentBonusResolver EquipmentBonus;
        public ManifestationDirector ManifestationDirector;
        public BossDirector BossDirector;
        public CombatFeel CombatFeel;
        public GroundScarField GroundScars;

        public WorldContext(PrototypeBootstrap host)
        {
            Host = host;
            SceneRoot = host.transform;
        }

        public PrototypeTuning Tuning => Host.SceneTuning;
        public int PrototypeMainClassId => Host.PrototypeMainClassId;
        public int[] PrototypePassiveRuneIds => Host.PrototypePassiveRuneIds;
        public GameObject PlayerVisualPrefab => Host.PlayerVisualPrefab;
        public GameObject BossVisualPrefab => Host.BossVisualPrefab;
    }
}
