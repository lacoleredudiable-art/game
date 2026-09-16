using Dovus.Core.Combat;
using Dovus.Core.Equipment;
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
        const int PentagonInkLayer = 5; // Unity built-in UI layer

        [SerializeField] PrototypeTuning _tuning = new();

        [Header("Görsel prefab (Asset Store — boşsa kapsül)")]
        [SerializeField] GameObject _playerVisualPrefab;
        [SerializeField] GameObject _bossVisualPrefab;
        [SerializeField] GameObject _arenaVisualPrefab;

        /// <summary>
        /// Alfa: sabit tek silah (seçim UI yok). Katalogdan Alev Kılıcı / Ateş.
        /// </summary>
        EquipmentItem _equippedWeapon;

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
            _tuning.EnsureRuntimeDefaults();
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
            clock.Bind(combat.Slowmo);

            var arena = CreateArena();
            // 16 Eylül: "duvarların içine giriliyor" bug raporu — dungeon mesh'leri collider'sız
            // geliyordu (KinematicMotor sadece dış kare clamp yapıyordu, iç duvar/sütun yoktu).
            int wallColliders = WallColliderFit.AddCollidersToObstacles(arena);
            Debug.Log($"[Arena] {wallColliders} engel collider eklendi (duvar/sütun/kemer).");
            // Görsel ölçek büyüyünce floor extents de büyür; FitHalf iç dekoru ezmesin.
            float walkHalf = ArenaWalkFit.FitHalfSizeM(arena, _tuning.ArenaHalfSizeM, insetM: 0.35f);
            float minWalk = Mathf.Max(10f, 11f * Mathf.Max(1f, _tuning.ArenaVisualScale) * 0.9f);
            walkHalf = Mathf.Max(walkHalf, minWalk);
            _tuning.ArenaHalfSizeM = walkHalf;
            combat.SkillMotion.ArenaHalfSizeM = walkHalf;
            LavaDecor.Build(arena.transform, walkHalf);
            Debug.Log($"[Arena] visualScale={_tuning.ArenaVisualScale:0.##} walkHalf={walkHalf:0.##}m");

            var player = CreateCapsule(
                "Player",
                new Vector3(0f, PlayerHeightM * 0.5f, -2f),
                PlayerRadiusM,
                PlayerHeightM,
                _tuning.PlayerColor);
            AttachVisual(player, _playerVisualPrefab, out var playerAnim);

            var ally = CreateCapsule(
                "AllyDummy",
                new Vector3(-3.2f, PlayerHeightM * 0.5f, -1.2f),
                PlayerRadiusM * 0.95f,
                PlayerHeightM,
                new Color(0.35f, 0.85f, 0.55f));
            AttachVisual(ally, _playerVisualPrefab, out _);
            var allyDummy = ally.AddComponent<AllyDummy>();
            allyDummy.Bind(_tuning.PlayerMaxHp, startRatio: 0.5f);

            var boss = CreateCapsule(
                "Boss",
                new Vector3(0f, BossHeightM * 0.5f, 5f * Mathf.Max(1f, _tuning.ArenaVisualScale * 0.55f)),
                BossRadiusM,
                BossHeightM,
                _tuning.BossColor);
            AttachVisual(boss, _bossVisualPrefab, out var bossAnim);

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

            var bossVisual = boss.AddComponent<BossVisual>();
            if (bossAnim != null)
                bossVisual.Bind(bossAnim, boss.GetComponent<Renderer>());

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
            FollowCamera follow = CreateCamera(player.transform);
            SceneAtmosphere.Apply(sun, Camera.main, _tuning);
            // LavDecor.Build — eski arena-wide kırmızı ember noktaları kalktı.
            BillboardVfx.CreateEmberField(boss.transform, new Color(1f, 0.45f, 0.12f), rate: 14f);
            CreatePentagon(clock, combat, player.transform, pose, reactor, bossVitals, dodgeMotion, afterimage, vitals, telegraph, follow, tuningConfig, allyDummy, resource, cooldown);
        }

        void CreatePentagon(
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
            var root = new GameObject("Pentagon");
            root.transform.SetParent(transform, false);

            var mainCam = Camera.main;
            if (mainCam != null)
                mainCam.cullingMask &= ~(1 << PentagonInkLayer);

            var overlayGo = new GameObject("InkOverlayCam");
            overlayGo.transform.SetParent(root.transform, false);
            var overlay = overlayGo.AddComponent<PentagonOverlayCamera>();
            overlay.Build(PentagonInkLayer);
            AttachOverlayToMain(mainCam, overlay.Cam);

            var view = root.AddComponent<PentagonView>();
            view.Build(_tuning, overlay.Cam);

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
            inkGo.layer = PentagonInkLayer;
            var ink = inkGo.AddComponent<InkTrail>();
            ink.Configure(_tuning, overlay, PentagonInkLayer);

            var syllable = root.AddComponent<SyllableFeedback>();
            syllable.Configure(_tuning);
            var debug = root.AddComponent<SentenceDebugHud>();
            var skills = SkillMotorLoader.LoadOrDefault();

            var input = root.AddComponent<PentagonInput>();
            input.Tuning = _tuning;
            input.Combat = combat;
            input.Bind(clock, ink, syllable, debug);

            // 16 Eylül: "kamera sabit" bug raporu — MoveInput/PentagonInput'un parmaklarına
            // dokunmadan üçüncü bir parmakla (veya editörde sağ-tık sürükleyerek) 360° orbit.
            if (follow != null)
            {
                var orbit = root.AddComponent<CameraOrbitInput>();
                orbit.Bind(follow, player.GetComponent<MoveInput>(), input);
            }
            debug.Configure(input.Engine, view.CanvasRoot, skills);
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

            var lockHud = root.AddComponent<RecoveryLockHud>();
            lockHud.Configure(input.Engine, combat, _tuning, view.CanvasRoot, vitalsHud.BarCount);

            var frameHud = root.AddComponent<FrameTimeHud>();
            frameHud.Configure(_tuning, view.CanvasRoot);

            var damageHud = root.AddComponent<DamageNumberHud>();
            damageHud.Configure(_tuning, view.CanvasRoot);

            var modeHud = root.AddComponent<ActiveModeHud>();
            modeHud.Configure(view.CanvasRoot);

            var passiveHud = root.AddComponent<PassiveHud>();
            passiveHud.Configure(view.CanvasRoot);

            dodgeMotion.Bind(clock, input, boss.transform, afterimage);

            var feelGo = new GameObject("CombatFeel");
            feelGo.transform.SetParent(transform, false);
            var feel = feelGo.AddComponent<CombatFeel>();
            feel.Bind(clock, follow, combat, _tuning, overlay.Cam, debug, readout);

            var directorGo = boss.gameObject;
            var bossDir = directorGo.AddComponent<BossDirector>();
            bossDir.Bind(clock, combat, _tuning, boss, input, player, vitals, bossVitals, telegraph, feel);
            if (bossStatus != null)
                bossDir.BindStatus(bossStatus);
            if (playerStatus != null)
                bossDir.BindPlayerStatus(playerStatus);
            bossDir.BindVisual(boss.GetComponent<BossVisual>());

            var scarsGo = new GameObject("GroundScars");
            scarsGo.transform.SetParent(transform, false);
            var scars = scarsGo.AddComponent<GroundScarField>();
            scars.Configure(_tuning);

            EquipmentBonusResolver equipmentBonus = LoadPrototypeEquipment(out _equippedWeapon);
            if (_equippedWeapon != null)
                Debug.Log($"[Equipment] sabit silah={_equippedWeapon.Name} ({_equippedWeapon.Element}) matchMult={equipmentBonus.MatchBonusMult:0.##}");

            var manGo = new GameObject("Manifestation");
            manGo.transform.SetParent(transform, false);
            var director = manGo.AddComponent<ManifestationDirector>();
            director.Bind(clock, input, player, pose, boss, bossVitals, scars, _tuning, damageHud, bossDir, playerStatus, bossStatus, debug, readout, follow, allyDummy, modeHud, view, passiveHud, _equippedWeapon, equipmentBonus);

            CreateTuningPanel(tuningConfig, vitals);
        }

        /// <summary>
        /// Resources element-sistemi → Alev Kılıcı (Ateş). Katalog yoksa null / çarpan 1.
        /// </summary>
        static EquipmentBonusResolver LoadPrototypeEquipment(out EquipmentItem weapon)
        {
            weapon = null;
            const string resourcePath = "ElementSystem/element-sistemi";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return new EquipmentBonusResolver(string.Empty);

            try
            {
                var catalog = EquipmentCatalog.FromJson(asset.text);
                weapon = catalog.Find(EquipmentSlot.Weapon, "Ateş");
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
        /// hit-test eden EnhancedTouch kullanıyordu (PentagonInput/MoveInput). Standart Slider
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
            if (_arenaVisualPrefab != null)
            {
                var instance = Instantiate(_arenaVisualPrefab);
                instance.name = "Arena";
                float visualScale = Mathf.Max(0.5f, _tuning.ArenaVisualScale);
                instance.transform.position = Vector3.zero;
                instance.transform.localScale = Vector3.one * visualScale;
                return instance;
            }

            var ground = CreateMeshObject("Arena", PrimitiveType.Plane);
            // Plane mesh 10x10 m; ArenaHalfSizeM yarım kenar uzunluğu.
            float planeScale = _tuning.ArenaHalfSizeM / 5f;
            ground.transform.localScale = new Vector3(planeScale, 1f, planeScale);
            ApplyColor(ground, _tuning.GroundColor);
            return ground;
        }

        /// <summary>
        /// Asset Store prefab'ı kökün child'ı olur; mantık kökte kalır (motor/pose/reactor).
        /// Ayak pivot'u varsayılır — local Y ofseti prefab'a göre sonra ayarlanır.
        /// </summary>
        static void AttachVisual(GameObject root, GameObject prefab, out Animator animator)
        {
            animator = null;
            if (root == null || prefab == null)
                return;

            var visual = Instantiate(prefab, root.transform, false);
            visual.name = "Visual";
            visual.transform.localPosition = new Vector3(0f, -1f, 0f); // kapsül merkezi → ayak
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            animator = visual.GetComponentInChildren<Animator>();

            var capsuleRend = root.GetComponent<Renderer>();
            if (capsuleRend != null)
                capsuleRend.enabled = false;
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
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = light;
            return light;
        }

        FollowCamera CreateCamera(Transform target)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _tuning.BackgroundColor;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 120f;

            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var follow = camGo.AddComponent<FollowCamera>();
            follow.Tuning = _tuning;
            follow.Target = target;
            camGo.transform.position = target.position + _tuning.CameraOffset;
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
