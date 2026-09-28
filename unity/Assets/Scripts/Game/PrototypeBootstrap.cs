using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game
{
    /// <summary>
    /// Tek sahne kökü: arena, oyuncu, boss, kamera ve ışığı çalışma anında kurar.
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        const float PlayerRadiusM = 0.5f;
        const float PlayerHeightM = 2f;
        const float BossRadiusM = 0.85f;
        const float BossHeightM = 2.6f;
        const int HexagonInkLayer = 5; // Unity built-in UI layer

        [SerializeField] PrototypeTuning _tuning = new();

        [Header("Görsel prefab (Asset Store — boşsa kapsül)")]
        [SerializeField] GameObject _playerVisualPrefab;
        [SerializeField] GameObject _bossVisualPrefab;
        [SerializeField] GameObject _arenaVisualPrefab;

        [Header("v6 build (ana_classes_80 id + 0-2 pasif rün id)")]
        [SerializeField, Min(1)] int _prototypeMainClassId = 1;
        [SerializeField] int[] _prototypePassiveRuneIds = new int[0];

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
            _tuning.EnsureRuntimeDefaults();
#if !UNITY_EDITOR
            // Development APK konsolu CapsuleCollider spam'i ile HUD'u örtüyordu.
            Debug.developerConsoleVisible = false;
#endif
            ApplyFrameRateTarget();
            BuildWorld();
        }

        /// <summary>
        /// T11 hedefi sabit 60 fps. Android'de varsayılan tavan cihazın ekran tazeleme hızıdır
        /// (120 Hz bir telefonda oyun 120'ye tırmanmaya çalışır ve kare süresi dalgalanır), o
        /// yüzden tavan açıkça yazılır. vSync sayacı sıfırlanmazsa targetFrameRate yok sayılır.
        /// </summary>
        void ApplyFrameRateTarget()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(1, _tuning.TargetFrameRateHz);
        }

        void BuildWorld()
        {
            var combat = new CombatTuning();

            // T10: kayıtlı ayar, DÜNYA kurulmadan önce combat/_tuning'in İÇİNE kopyalanır
            // (CopyFrom yolu — referans kimliği korunur). Böylece arena/oyuncu/boss ilk kareden
            // kaydedilmiş değerlerle doğar, sonradan "sıçrayan" bir düzeltme karesi olmaz.
            var tuningConfig = TuningConfig.Create(combat, _tuning);
            tuningConfig.TryLoad();
            // His denemesi: boss vurmasın (kayıtlı ayar ezmesin).
            combat.Boss.Damage = 0;
            combat.Boss.FireConeDamage = 0; // 16 Eylül: ikinci saldırı da bu deneyin kapsamında.
            var clock = gameObject.AddComponent<GameClock>();

            var arena = CreateArena();
            // Daire salonda duvarlar CircularArena'da collider'lı; eski mesh fit yok.
            float walkHalf = _tuning.ArenaHalfSizeM;
            combat.SkillMotion.ArenaHalfSizeM = walkHalf;
            LavaDecor.Build(arena.transform, walkHalf);
            ArenaHorizon.Build(arena.transform, walkHalf);
            Debug.Log($"[Arena] circle r={walkHalf:0.##}m wallH={_tuning.ArenaWallHeightM:0.#}m");

            var player = CreateCapsule(
                "Player",
                new Vector3(0f, PlayerHeightM * 0.5f, -2f),
                PlayerRadiusM,
                PlayerHeightM,
                _tuning.PlayerColor);
            AttachVisual(
                player,
                _playerVisualPrefab,
                _tuning.PlayerVisualHeightM,
                player.transform.position.y - PlayerHeightM * 0.5f,
                _tuning.CharacterAnimSpeed,
                out var playerAnim);

            var ally = CreateCapsule(
                "AllyDummy",
                new Vector3(-3.2f, PlayerHeightM * 0.5f, -1.2f),
                PlayerRadiusM * 0.95f,
                PlayerHeightM,
                new Color(0.35f, 0.85f, 0.55f));
            AttachVisual(
                ally,
                _playerVisualPrefab,
                _tuning.PlayerVisualHeightM,
                ally.transform.position.y - PlayerHeightM * 0.5f,
                _tuning.CharacterAnimSpeed,
                out _);
            var allyDummy = ally.AddComponent<AllyDummy>();
            allyDummy.Bind(_tuning.PlayerMaxHp, startRatio: 0.5f);

            var boss = CreateCapsule(
                "Boss",
                new Vector3(0f, BossHeightM * 0.5f, 5f * Mathf.Max(1f, _tuning.ArenaVisualScale * 0.55f)),
                BossRadiusM,
                BossHeightM,
                _tuning.BossColor);
            // SkillExecutor overlap/projectile yolu için gerçek fizik hedefi. Primitive mesh
            // bilinçli collider'sız kurulur; yalnız aktör hedef kapsülü burada eklenir.
            var bossHitCollider = boss.AddComponent<CapsuleCollider>();
            bossHitCollider.isTrigger = true;
            AttachVisual(
                boss,
                _bossVisualPrefab,
                _tuning.BossVisualHeightM,
                boss.transform.position.y - BossHeightM * 0.5f,
                _tuning.CharacterAnimSpeed,
                out var bossAnim);

            player.AddComponent<MoveInput>().Tuning = _tuning;

            var motor = player.AddComponent<KinematicMotor>();
            motor.Tuning = _tuning;
            motor.BodyRadiusM = PlayerRadiusM;

            var pose = player.AddComponent<ActorPose>();
            pose.Tuning = _tuning;
            pose.CaptureBase();

            var visual = player.AddComponent<ActorVisual>();
            if (playerAnim != null)
                visual.Bind(playerAnim, player.GetComponent<Renderer>());
            visual.CrossFadeSec = _tuning.AnimCrossFadeSec;
            visual.StrikeComboResetSec = _tuning.BasicStrikeComboResetSec;
            visual.UpperBodyMinSpeed = _tuning.UpperBodyCastMinSpeed;

            var bossVisual = boss.AddComponent<BossVisual>();
            if (bossAnim != null)
                bossVisual.Bind(bossAnim, boss.GetComponent<Renderer>());
            bossVisual.Configure(_tuning);

            player.AddComponent<HitFlash>().Bind(combat.Feel);
            boss.AddComponent<HitFlash>().Bind(combat.Feel);

            var vitals = player.AddComponent<PlayerVitals>();
            // His: heal denemesi — oyuncu da %50 (full iken mend boş döner).
            vitals.Bind(combat.Boss, _tuning.PlayerMaxHp, startRatio: 0.5f);

            var resource = player.AddComponent<PlayerResource>();
            // docs/element-sistemi.json resource_system: 100 / 8 / 1.5
            resource.Bind();

            var cooldown = player.AddComponent<PlayerCooldown>();
            // docs/element-sistemi.json cooldown_rules: 0.3 / 1
            cooldown.Bind();

            var playerStatus = player.AddComponent<ActorStatus>();
            var bossStatus = boss.AddComponent<ActorStatus>();

            var afterimage = player.AddComponent<AfterimageTrail>();
            afterimage.Bind(combat.Feel, _tuning);

            var dodgeMotion = player.AddComponent<DodgeMotion>();
            player.AddComponent<SkillMotionDriver>();

            var reactor = boss.AddComponent<BossReactor>();
            reactor.Tuning = _tuning;
            reactor.BodyRadiusM = BossRadiusM;
            reactor.CaptureHome();

            var bossVitals = new BossVitals(combat.Boss.MaxHp);
            playerStatus.Bind(null, combat.Status, vitals, null, null);
            bossStatus.Bind(null, combat.Status, null, bossVitals, reactor);

            var telegraph = boss.AddComponent<BossTelegraph>();
            telegraph.Bind(_tuning, combat.Boss, boss.transform);

            var sun = CreateSun();
            FollowCamera follow = CreateCamera(player.transform, boss.transform);
            SceneAtmosphere.Apply(sun, Camera.main, _tuning);
            // LavDecor.Build — eski arena-wide kırmızı ember noktaları kalktı.
            BillboardVfx.CreateEmberField(boss.transform, new Color(1f, 0.45f, 0.12f), rate: 14f);
            CreateHexagon(clock, combat, player.transform, pose, reactor, bossVitals, dodgeMotion, afterimage, vitals, telegraph, follow, tuningConfig, allyDummy, resource, cooldown);
        }

        void CreateHexagon(
            GameClock clock,
            CombatTuning combat,
            Transform player,
            ActorPose pose,
            BossReactor boss,
            BossVitals bossVitals,
            DodgeMotion dodgeMotion,
            AfterimageTrail afterimage,
            PlayerVitals vitals,
            BossTelegraph telegraph,
            FollowCamera follow,
            TuningConfig tuningConfig,
            AllyDummy allyDummy = null,
            PlayerResource resource = null,
            PlayerCooldown cooldown = null)
        {
            var root = new GameObject("Hexagon");
            root.transform.SetParent(transform, false);

            var mainCam = Camera.main;
            if (mainCam != null)
                mainCam.cullingMask &= ~(1 << HexagonInkLayer);

            var overlayGo = new GameObject("InkOverlayCam");
            overlayGo.transform.SetParent(root.transform, false);
            var overlay = overlayGo.AddComponent<HexagonOverlayCamera>();
            overlay.Build(HexagonInkLayer);
            AttachOverlayToMain(mainCam, overlay.Cam);

            ElementSystemDesign design = null;
            ElementSystemAssetCatalog assetCatalog = null;
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign loaded))
            {
                design = loaded;
                assetCatalog = ElementSystemAssetCatalog.CreateRuntime(design);
            }
            SkillMotor skills = design?.SkillMotor ?? SkillMotorLoader.LoadOrDefault();
            var runeManager = new RuneManager(skills);
            if (!runeManager.TrySelectMainClass(
                    _prototypeMainClassId,
                    _prototypePassiveRuneIds,
                    out string buildError))
            {
                Debug.LogWarning($"[RuneManager] {buildError}; varsayılan build kullanıldı.");
            }
            RuneLoadout loadout = runeManager.Current;
            Debug.Log(
                $"[ElementSystem] mainClass={_prototypeMainClassId} build=[{string.Join(",", loadout.RuneIds)}] "
                + $"passives={loadout.PassiveCount}; SO={assetCatalog?.Runes.Count ?? 0}/"
                + $"{assetCatalog?.Weapons.Count ?? 0}/{assetCatalog?.Elements.Count ?? 0}");
            var view = root.AddComponent<HexagonView>();
            view.Build(_tuning, overlay.Cam, skills, loadout);

            // 16 Eylül: sol yarıdaki sanal çubuk fonksiyonel olarak zaten çalışıyordu, hiç
            // görseli yoktu (bug raporu). MoveInput'un mantığına dokunmuyor, sadece çiziyor.
            var moveInput = player.GetComponent<MoveInput>();
            if (moveInput != null)
            {
                var joystickGo = new GameObject("JoystickView");
                joystickGo.transform.SetParent(root.transform, false);
                var joystick = joystickGo.AddComponent<JoystickView>();
                joystick.Build(moveInput, _tuning, overlay.Cam);
            }

            var inkGo = new GameObject("InkTrail");
            inkGo.transform.SetParent(root.transform, false);
            inkGo.layer = HexagonInkLayer;
            var ink = inkGo.AddComponent<InkTrail>();
            ink.Configure(_tuning, overlay, HexagonInkLayer);

            var syllable = root.AddComponent<SyllableFeedback>();
            syllable.Configure(_tuning);
            var debug = root.AddComponent<SentenceDebugHud>();

            var input = root.AddComponent<HexagonInput>();
            input.Tuning = _tuning;
            input.Combat = combat;
            input.Bind(clock, ink, syllable, debug, skills, loadout);
            input.DotAccepted += view.NotifyPressed;

            // 16 Eylül: "kamera sabit" bug raporu — MoveInput/HexagonInput'un parmaklarına
            // dokunmadan üçüncü bir parmakla (veya editörde sağ-tık sürükleyerek) 360° orbit.
            if (follow != null)
            {
                var orbit = root.AddComponent<CameraOrbitInput>();
                orbit.Bind(follow, player.GetComponent<MoveInput>(), input, _tuning);
                var motor = player.GetComponent<KinematicMotor>();
                motor?.BindCamera(follow);
            }
            debug.Configure(input.Engine, view.CanvasRoot, skills, _tuning.ShowSentenceDebugHud);
            debug.BindVitals(vitals);
            input.BindVitals(vitals);

            var playerStatus = player.GetComponent<ActorStatus>();
            var bossStatus = boss.GetComponent<ActorStatus>();
            if (playerStatus != null)
            {
                playerStatus.Bind(clock, combat.Status, vitals, null, null);
                input.BindStatus(playerStatus);
            }
            if (bossStatus != null)
                bossStatus.Bind(clock, combat.Status, null, bossVitals, boss);

            var readout = root.AddComponent<ReactionReadout>();
            readout.Configure(combat.Feel, _tuning, view.CanvasRoot);
            input.BindResource(resource, readout, skills);
            input.BindCooldown(cooldown, readout, skills);

            var vitalsHud = root.AddComponent<VitalsHud>();
            vitalsHud.Configure(vitals, bossVitals, _tuning, view.CanvasRoot, allyDummy, resource);

            // Status strips — gerçek StatusBoard
            if (playerStatus != null)
            {
                var playerStrip = root.AddComponent<StatusIconStrip>();
                float stripY = vitalsHud.PlayerStackBottomCanvasY
                    - HexagonLayoutScreen.DpToPixels(_tuning.StatusIconGapDp + 4f);
                float left = HexagonLayoutScreen.SafeLeftInsetPx()
                    + HexagonLayoutScreen.DpToPixels(_tuning.VitalsMarginDp);
                playerStrip.Configure(
                    playerStatus.Board, _tuning, view.CanvasRoot,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(left, stripY), "PlayerStatusStrip");
            }

            if (bossStatus != null)
            {
                var bossStrip = root.AddComponent<StatusIconStrip>();
                float stripY = vitalsHud.BossStackBottomCanvasY
                    - HexagonLayoutScreen.DpToPixels(_tuning.StatusIconGapDp + 2f);
                bossStrip.Configure(
                    bossStatus.Board, _tuning, view.CanvasRoot,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, stripY), "BossStatusStrip");
            }

            if (allyDummy != null)
                allyDummy.EnsureStatusBoard();

            var lockHud = root.AddComponent<RecoveryLockHud>();
            lockHud.Configure(input.Engine, combat, _tuning, view.CanvasRoot, vitalsHud.BarCount);
            lockHud.BindVitalsHud(vitalsHud);

            var frameHud = root.AddComponent<FrameTimeHud>();
            frameHud.Configure(_tuning, view.CanvasRoot);

            var damageHud = root.AddComponent<DamageNumberHud>();
            damageHud.Configure(_tuning, view.CanvasRoot);

            var modeHud = root.AddComponent<ActiveModeHud>();
            modeHud.Configure(view.CanvasRoot);
            modeHud.BindBelowBoss(vitalsHud);

            var passiveHud = root.AddComponent<PassiveHud>();
            passiveHud.Configure(view.CanvasRoot, _tuning);
            passiveHud.BindRunes(runeManager, skills);
            passiveHud.BindBelowPlayer(vitalsHud);

            dodgeMotion.Bind(clock, input, boss.transform, afterimage, follow);

            var feelGo = new GameObject("CombatFeel");
            feelGo.transform.SetParent(transform, false);
            var feel = feelGo.AddComponent<CombatFeel>();
            feel.Bind(clock, follow, combat, _tuning, overlay.Cam, debug, readout);
            feel.BindActors(player.GetComponent<HitFlash>(), boss.GetComponent<HitFlash>());
            var overlayHud = feelGo.AddComponent<CombatOverlayHud>();
            overlayHud.Configure(vitals, bossVitals, player, boss.transform, overlay.Cam);

            var directorGo = boss.gameObject;
            var bossDir = directorGo.AddComponent<BossDirector>();
            bossDir.Bind(clock, combat, _tuning, boss, input, player, vitals, bossVitals, telegraph, feel);
            if (bossStatus != null)
                bossDir.BindStatus(bossStatus);
            if (playerStatus != null)
                bossDir.BindPlayerStatus(playerStatus);
            bossDir.BindVisual(boss.GetComponent<BossVisual>());
            vitalsHud.BindBoss(bossDir);

            feelGo.AddComponent<SfxDirector>();
            feelGo.AddComponent<PresentationFx>().Bind(bossDir, dodgeMotion, feel, input);
            var playerSteps = player.gameObject.AddComponent<FootstepEmitter>();
            playerSteps.StrideM = _tuning.FootstepStrideM;
            var bossSteps = boss.gameObject.AddComponent<FootstepEmitter>();
            bossSteps.StrideM = _tuning.BossFootstepStrideM;
            bossSteps.IsBoss = true;

            var scarsGo = new GameObject("GroundScars");
            scarsGo.transform.SetParent(transform, false);
            var scars = scarsGo.AddComponent<GroundScarField>();
            scars.Configure(_tuning);

            EquipmentBonusResolver equipmentBonus;
            EquipmentItem equippedWeapon;
            if (design != null && assetCatalog != null)
            {
                equipmentBonus = new EquipmentBonusResolver(design.Equipment);
                equippedWeapon = assetCatalog.FindWeapon(4)?.ToEquipmentItem();
            }
            else
            {
                equipmentBonus = LoadPrototypeEquipment(out equippedWeapon);
            }
            if (equippedWeapon != null)
                Debug.Log($"[Equipment] prototip silah={equippedWeapon.Name}; v6 fiil uyumu etkin.");
            var skillFactory = new SkillFactory(skills, equipmentBonus);
            VerifyBindingPipeline(design, assetCatalog, runeManager, skillFactory, equippedWeapon);

            var manGo = new GameObject("Manifestation");
            manGo.transform.SetParent(transform, false);
            var director = manGo.AddComponent<ManifestationDirector>();
            director.Bind(clock, input, player, pose, boss, bossVitals, scars, _tuning, damageHud, bossDir, playerStatus, bossStatus, debug, readout, follow, allyDummy, modeHud, view, passiveHud, equippedWeapon, equipmentBonus, skills, skillFactory, design?.Animations);
            director.ConfigureWeaponCycle(design?.Equipment.Items);
            if (design != null)
            {
                director.ConfigureWeaponSwap(WeaponSwapRules.FromJson(design.Json));
                director.ConfigureVerbExecution(VerbExecutionData.FromJson(design.Json));
                MobilityCcData mobilityCc = MobilityCcData.FromJson(design.Json);
                director.ConfigureMobilityCc(mobilityCc);
                input.BindMobilityCc(mobilityCc);
            }
            view.BindWeaponSwap(director, clock);
            input.WeaponSwapRequested += () => director.TryRequestWeaponSwap();

            var preview = root.AddComponent<SkillPreviewHud>();
            preview.Configure(input.Engine, skills, skillFactory, director, _tuning, view.CanvasRoot);

            var buildSelect = root.AddComponent<BuildSelectScreen>();
            buildSelect.Configure(skills, runeManager, input, view, clock, director, !_tuning.SkipBuildSelectOnStart);

            int elementTransitionMs = 300;
            if (design != null)
                elementTransitionMs = MiniJson.Parse(design.Json)["element_system"]["selection"]
                    ["transition_time_ms"].AsInt(300);
            var elementMenu = root.AddComponent<ElementRadialMenu>();
            elementMenu.Configure(
                director, skills, playerStatus, _tuning, view.CanvasRoot, elementTransitionMs);

            var v6Panel = root.AddComponent<V611DebugPanel>();
            v6Panel.Configure(input, director, buildSelect, view.CanvasRoot);

            CreateTuningPanel(tuningConfig, vitals);
        }

        static void VerifyBindingPipeline(
            ElementSystemDesign design,
            ElementSystemAssetCatalog assets,
            RuneManager runes,
            SkillFactory factory,
            EquipmentItem weapon)
        {
            if (design == null || assets == null || runes == null || factory == null || weapon == null)
            {
                Debug.LogWarning("[BindingReady] v6.1.1 preflight atlandı: bağımlılık eksik.");
                return;
            }

            int elementId = assets.Elements.Count > 0 ? assets.Elements[0].Id : 0;
            var buildSkills = runes.BuildSkills(factory, weapon, elementId);
            Skill smoke = factory.Create(1, 1, weapon, elementId);
            if (assets.Runes.Count != 12 || assets.Weapons.Count != 10
                || assets.Elements.Count != 6 || buildSkills.Count != 36
                || smoke.Id != "1-1")
            {
                throw new System.InvalidOperationException(
                    "v6 binding preflight 12/10/6 SO, 36 build skill ve 1-1 smoke bekler.");
            }

            Debug.Log(
                $"[BindingReady] JSON {design.Version} → SO 12/10/6 → "
                + $"buildSkills={buildSkills.Count} → smoke={smoke.DisplayName} → "
                + $"weapon={weapon.Name} → element={assets.Elements[0].DisplayName}");
        }

        /// <summary>
        /// Resources element-sistemi v6.1.1 → prototip Kılıç. Seçim UI ayrı sunum işi.
        /// </summary>
        static EquipmentBonusResolver LoadPrototypeEquipment(out EquipmentItem weapon)
        {
            weapon = null;
            if (!ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
                return new EquipmentBonusResolver(string.Empty);

            try
            {
                EquipmentCatalog catalog = design.Equipment;
                weapon = catalog.FindWeapon(4);
                return new EquipmentBonusResolver(catalog);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Equipment] katalog okunamadı: {e.Message}");
                return new EquipmentBonusResolver(string.Empty);
            }
        }

        /// <summary>
        /// T10: uGUI Slider/Button ilk kez sahneye giriyor — proje şimdiye kadar hep elle
        /// hit-test eden EnhancedTouch kullanıyordu (HexagonInput/MoveInput). Standart Slider
        /// bir EventSystem + bir input modülü ister; InputSystemUIInputModule seçildi çünkü
        /// proje zaten Yeni Input System üstünde (Unity.InputSystem asmdef referansı).
        /// </summary>
        static void CreateTuningPanel(TuningConfig tuningConfig, PlayerVitals vitals)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var panelGo = new GameObject("TuningPanel");
            var panel = panelGo.AddComponent<TuningPanel>();
            panel.Configure(tuningConfig, vitals);
        }

        static void AttachOverlayToMain(Camera main, Camera overlay)
        {
            if (main == null || overlay == null)
                return;

            var mainData = main.GetComponent<UniversalAdditionalCameraData>();
            if (mainData == null)
                mainData = main.gameObject.AddComponent<UniversalAdditionalCameraData>();

            var overlayData = overlay.GetComponent<UniversalAdditionalCameraData>();
            if (overlayData == null)
                overlayData = overlay.gameObject.AddComponent<UniversalAdditionalCameraData>();

            overlayData.renderType = CameraRenderType.Overlay;
            if (!mainData.cameraStack.Contains(overlay))
                mainData.cameraStack.Add(overlay);
        }

        GameObject CreateArena()
        {
            // Sahip: 100 m çap daire, yüksek duvar, tavansız — Long_Hall avize/sütun görüşü kesiyordu.
            Color wall = new Color(0.28f, 0.26f, 0.24f);
            return CircularArena.Build(
                _tuning.ArenaHalfSizeM,
                _tuning.ArenaWallHeightM,
                _tuning.ArenaWallThicknessM,
                _tuning.GroundColor,
                wall);
        }

        /// <summary>
        /// Asset Store prefab'ı kökün child'ı olur; mantık kökte kalır (motor/pose/reactor).
        /// Ayak pivot'u varsayılır — local Y ofseti prefab'a göre sonra ayarlanır.
        /// </summary>
        static void AttachVisual(
            GameObject root,
            GameObject prefab,
            float targetHeightM,
            float groundY,
            float animSpeed,
            out Animator animator)
        {
            animator = null;
            if (root == null || prefab == null)
                return;

            var visual = Instantiate(prefab, root.transform, false);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // Gameplay kökü kapsül mesh'ini ölçekliyor (boss'ta XZ ve Y farklı). Görsel bu
            // ölçeği miras alırsa karakter ezilip genişliyordu; önce dünya ölçeğini 1'e çeker.
            Vector3 parentScale = root.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, parentScale.x),
                1f / Mathf.Max(0.0001f, parentScale.y),
                1f / Mathf.Max(0.0001f, parentScale.z));

            if (TryGetRendererBounds(visual, out Bounds initial) && initial.size.y > 0.01f)
            {
                float fit = Mathf.Max(0.1f, targetHeightM) / initial.size.y;
                visual.transform.localScale *= fit;
                if (TryGetRendererBounds(visual, out Bounds fitted))
                    visual.transform.position += Vector3.up * (groundY - fitted.min.y);
                Debug.Log(
                    $"[VisualScale] {root.name} target={targetHeightM:0.00}m "
                    + $"source={initial.size.y:0.00}m fit={fit:0.000}");
            }

            animator = visual.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.speed = Mathf.Clamp(animSpeed, 0.25f, 3f);

            var capsuleRend = root.GetComponent<Renderer>();
            if (capsuleRend != null)
                capsuleRend.enabled = false;
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        static GameObject CreateCapsule(string name, Vector3 position, float radius, float height, Color color)
        {
            var capsule = CreateMeshObject(name, PrimitiveType.Capsule);
            capsule.transform.position = position;
            capsule.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyColor(capsule, color);
            return capsule;
        }

        /// <summary>
        /// Mesh'i doğrudan ata (MeshFilter+MeshRenderer) — CreatePrimitive'in otomatik
        /// Collider'ı hiç oluşmaz (teknoloji-kararlari §4).
        /// </summary>
        static GameObject CreateMeshObject(string name, PrimitiveType type)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(type);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        Light CreateSun()
        {
            var sunGo = new GameObject("Sun");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.55f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = light;
            // Synty Generic_Basic atlas karanlıkta flat görünür — fill ambient.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.48f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.26f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.11f, 0.10f);
            return light;
        }

        FollowCamera CreateCamera(Transform target, Transform boss)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _tuning.BackgroundColor;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 250f;
            camera.fieldOfView = _tuning.CameraFovDeg;

            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var follow = camGo.AddComponent<FollowCamera>();
            follow.Tuning = _tuning;
            follow.Target = target;
            follow.BossTarget = boss;
            Vector3 startOffset = _tuning.CameraShoulderOffset
                + Vector3.back * _tuning.CameraDistanceM;
            camGo.transform.position = target.position + startOffset;
            return follow;
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return;

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            renderer.sharedMaterial = material;
        }
    }
}
