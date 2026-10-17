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
using Dovus.Core.RuleEngineV4;
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
        [Tooltip("RuleEngineV4.SliceScene açıkken oyuncu görseli; yerel PlayerVisualOverride'ın önüne geçer.")]
        [SerializeField] GameObject _slicePlayerVisualPrefab;

        [Header("v6 build (ana_classes_80 id + 0-2 pasif rün id)")]
        [SerializeField, Min(1)] int _prototypeMainClassId = 1;
        [SerializeField] int[] _prototypePassiveRuneIds = new int[0];

        internal GameTuning SceneTuning => _tuning;
        internal GameObject PlayerVisualPrefab => _playerVisualPrefab;
        internal GameObject BossVisualPrefab => _bossVisualPrefab;
        internal GameObject SlicePlayerVisualPrefab
        {
            get => _slicePlayerVisualPrefab;
            set => _slicePlayerVisualPrefab = value;
        }
        internal int PrototypeMainClassId => _prototypeMainClassId;
        internal int[] PrototypePassiveRuneIds => _prototypePassiveRuneIds;

        void Awake()
        {
            _tuning ??= new GameTuning();
            _tuning.EnsureRuntimeDefaults();
            _tuning.RuleEngineV4.ApplySliceBuild();
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
            TuningSourceLog.Write(tuningConfig, RuleEngineV4Catalog, tuningConfig.LastLoadReadFile);

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

        RuleEngineV4Catalog _ruleEngineV4Catalog;

        /// <summary>kural-motoru-v4.json; can ve dilim ayarlarının tek kaynağı.</summary>
        internal RuleEngineV4Catalog RuleEngineV4Catalog
        {
            get
            {
                if (_ruleEngineV4Catalog != null)
                    return _ruleEngineV4Catalog;
                _ruleEngineV4Catalog = new Skills.RuleEngineV4.RuleEngineV4CatalogAccess().Catalog;
                foreach (string key in _ruleEngineV4Catalog.Scale.MissingKeys)
                    Debug.LogError($"[V4-FALLBACK] GameBootstrapHost: kural-motoru-v4.json scale.{key} yok; kod varsayılanı kullanıldı");
                return _ruleEngineV4Catalog;
            }
        }

        /// <summary>O-13.1, O-6.11: dilimde oyuncu, dost ve boss canının tek kaynağı (scale bloğu).</summary>
        internal RuleEngineV4Scale RuleEngineV4Scale => RuleEngineV4Catalog.Scale;

        internal int AllyDummyHp(AssetCatalog assets) =>
            Mathf.Max(1, Mathf.RoundToInt(RuleEngineV4Scale.AllyDummyMaxHp));

        internal int ScaledPlayerHp(AssetCatalog assets) =>
            Mathf.Max(1, Mathf.RoundToInt(RuleEngineV4Scale.PlayerMaxHp));

        internal float ScaledBossHp(AssetCatalog assets, float tuningMaxHp) =>
            Mathf.Max(1f, RuleEngineV4Scale.BossMaxHp);
    }
}
