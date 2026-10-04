using Dovus.Core.Boss;
using Dovus.Core.Element;
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
using Dovus.Game.Audio;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Team;
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
        public readonly GameBootstrapHost Host;
        public readonly Transform SceneRoot;

        public CombatTuning Combat;
        public TuningConfig TuningConfig;
        public GameClockHost Clock;
        public AssetCatalog Assets;
        public TeamComboAccess TeamAccess;
        public SfxDirector Sfx;
        public ElementRadialMenuHud ElementMenu;
        public GameObject Arena;
        public float WalkHalf;
        public float SpawnMaxR;

        public GameObject Player;
        public GameObject Ally;
        public GameObject Boss;
        public AllyDummyController AllyDummyController;
        public ActorPoseView PlayerPose;
        public PlayerVitalsHost PlayerVitalsHost;
        public PlayerResourceHost PlayerResourceHost;
        public PlayerCooldownHost PlayerCooldownHost;
        public ActorStatusHost PlayerStatus;
        public ActorStatusHost BossStatus;
        public DodgeMotionController DodgeMotionController;
        public AfterimageTrailView Afterimage;
        public BossReactorController BossReactorController;
        public BossVitals BossVitals;
        public BossTelegraphView BossTelegraphView;
        public FollowCameraController FollowCameraController;
        public Camera MainCamera;
        public Light Sun;

        public GameObject HexagonRoot;
        public HexagonView HexagonView;
        public HexagonOverlayCameraView Overlay;
        public Camera OverlayCamera;
        public HexagonInputController HexagonInputController;
        public PlayerTargetingController PlayerTargetingController;
        public CameraOrbitController CameraOrbitController;
        public InkTrailView InkTrailView;
        public SyllableFeedbackView SyllableFeedbackView;
        public SentenceDebugHud SentenceDebugHud;
        public ReactionReadoutHud ReactionReadoutHud;
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
        public CombatFeelDirector CombatFeelDirector;
        public GroundScarFieldView GroundScars;

        public WorldContext(GameBootstrapHost host)
        {
            Host = host;
            SceneRoot = host.transform;
        }

        public GameTuning Tuning => Host.SceneTuning;
        public int PrototypeMainClassId => Host.PrototypeMainClassId;
        public int[] PrototypePassiveRuneIds => Host.PrototypePassiveRuneIds;
        public GameObject PlayerVisualPrefab => Host.PlayerVisualPrefab;
        public GameObject BossVisualPrefab => Host.BossVisualPrefab;
    }
}
