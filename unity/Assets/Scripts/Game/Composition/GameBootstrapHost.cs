using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Tuning;
using Dovus.Game.Composition.Builders;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Platform;
using Dovus.Game.Feel;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Composition
{
    /// <summary>
    /// Tek sahne kökü: arena, oyuncu, boss, kamera ve ışığı çalışma anında kurar.
    /// </summary>
    public sealed class GameBootstrapHost : MonoBehaviour
    {
        [SerializeField] GameTuning _tuning = new();

        [Header("Görsel prefab (Asset Store — boşsa kapsül)")]
        [SerializeField] GameObject _playerVisualPrefab;
        [SerializeField] GameObject _bossVisualPrefab;

        [Header("v6 build (ana_classes_80 id + 0-2 pasif rün id)")]
        [SerializeField, Min(1)] int _prototypeMainClassId = 1;
        [SerializeField] int[] _prototypePassiveRuneIds = new int[0];

        internal GameTuning SceneTuning => _tuning;
        internal GameObject PlayerVisualPrefab => _playerVisualPrefab;
        internal GameObject BossVisualPrefab => _bossVisualPrefab;
        internal int PrototypeMainClassId => _prototypeMainClassId;
        internal int[] PrototypePassiveRuneIds => _prototypePassiveRuneIds;

        void Awake()
        {
            _tuning ??= new GameTuning();
            _tuning.EnsureRuntimeDefaults();
            HexagonLayoutScreen.FitShortSideDp = _tuning.Input.HudFitShortSideDp;
#if !UNITY_EDITOR
            Debug.developerConsoleVisible = false;
#endif
            ApplyFrameRateTarget();
            BuildWorld();
        }

        void ApplyFrameRateTarget()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(1, _tuning.Hud.TargetFrameRateHz);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void OnDestroy()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        void BuildWorld()
        {
            var combat = new CombatTuning();
            var tuningConfig = TuningConfig.Create(combat, _tuning);
            if (DebugConfig.Enabled)
                tuningConfig.TryLoad();

            var ctx = new WorldContext(this)
            {
                Combat = combat,
                TuningConfig = tuningConfig,
                Clock = gameObject.AddComponent<GameClockHost>(),
            };
            ctx.Assets = AssetCatalog.Standalone;
            ctx.Runtime = new GameSceneRuntime(
                new DebugFlags(),
                VfxLibraryStandalone.Shared,
                _tuning,
                combat.Feel);
#if UNITY_EDITOR || DOVUS_DEBUG
            DevToolsRuntimeWiring.Apply(ctx.Runtime);
#endif
            ctx.TeamAccess = new TeamComboAccess();
            var teamGo = new GameObject(nameof(TeamComboHost));
            DontDestroyOnLoad(teamGo);
            ctx.TeamComboHost = teamGo.AddComponent<TeamComboHost>();
            ctx.TeamComboHost.ConfigureLiveAllies(ctx.Runtime.AllyDummies);
            ctx.TeamAccess.Configure(ctx.TeamComboHost);

            var arenaBuilder = new ArenaBuilder();
            arenaBuilder.BuildArena(ctx);

            // Player/ally/boss components are added interleaved, in the original BuildWorld order.
            new ActorsBuilder().Build(ctx);

            arenaBuilder.BuildSun(ctx);

            var cameraBuilder = new CameraBuilder();
            cameraBuilder.Build(ctx);
            cameraBuilder.ApplyAtmosphere(ctx);

            new HexagonInputBuilder().Build(ctx);
            new HudBuilder().Build(ctx);
            new SkillSystemBuilder().Build(ctx);
            new DebugToolsBuilder().Build(ctx);
        }

        internal int ScaledPlayerHp(AssetCatalog assets)
        {
            if (Data.ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                float hp = BossCombatProfile.FromDocument(design.Document).PlayerMaxHp;
                return Mathf.Max(1, Mathf.RoundToInt(hp));
            }
            return Mathf.Max(1, CombatScale.MagnitudeInt(_tuning.Player.PlayerMaxHp));
        }

        internal float ScaledBossHp(AssetCatalog assets, float tuningMaxHp)
        {
            if (Data.ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
                return Mathf.Max(1f, BossCombatProfile.FromDocument(design.Document).BossMaxHp);
            return Mathf.Max(1f, CombatScale.Magnitude(tuningMaxHp));
        }
    }
}
