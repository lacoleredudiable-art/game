using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dovus.Core.Equipment;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Dilim sahnesi dünya fiziği senaryoları: her test GameBootstrapHost'u RuleEngineV4.Enabled +
    /// SliceScene açık kurar, gerçek oyuncu/boss/hafif düşmanlar üzerinde PhysX ve yürütücüyü koşar.
    /// Fiil 5 (itme) ve alan teslimatı dilim silahlarında yok; o iki kural planlayıcının ürettiği
    /// komutlarla yürütücüye doğrudan verilir.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float BootSettleSec = 0.5f;
        const float CastTimeoutSec = 8f;
        const float PosTolM = 0.05f;
        const int Kilic = 4;
        const int Yay = 2;
        static readonly Vector3 Origin = new(-6f, 0f, -8f);
        static readonly Vector3 Park = new(12f, 0f, -14f);

        /// <summary>v4 dışı, önceden var olan DevTools hatası (arena shader'larında _Color yok).</summary>
        const string KnownDevToolsNoise = "FeelPlayVerifyController";

        readonly HashSet<GameObject> _preexisting = new();
        readonly List<string> _errors = new();
        string _label = "kurulum";
        ManifestationDirector _md;
        RuleEngineV4CastHost _cast;
        RuleEngineV4PhysicsServices _physics;
        RuleEngineV4CatalogAccess _catalog;
        Transform _player;
        Transform _boss;
        SliceLightMinionHost[] _minions;
        Transform[] _allies;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _errors.Clear();
            _label = "kurulum";
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += OnLog;
            _preexisting.Clear();
            foreach (GameObject root in AllRoots())
                _preexisting.Add(root);

            var boot = new GameObject("RuleEngineV4SliceBoot");
            boot.SetActive(false);
            var host = boot.AddComponent<GameBootstrapHost>();
            host.SceneTuning.RuleEngineV4.Enabled = true;
            host.SceneTuning.RuleEngineV4.SliceScene = true;
            host.SlicePlayerVisualPrefab = LoadSlicePlayerVisual();
            boot.SetActive(true);
            yield return new WaitForSecondsRealtime(BootSettleSec);

            _md = Object.FindAnyObjectByType<ManifestationDirector>();
            Assert.That(_md, Is.Not.Null, "ManifestationDirector");
            Assert.That(RuleEngineV4Feature.Enabled, Is.True, "RuleEngineV4Feature.Enabled");
            _player = _md.MechanicsPlayer;
            Assert.That(_player, Is.Not.Null, "oyuncu");
            // Açılışta build seçimi açık ve dünya saati duraklı; varsayılan build ile kapatılır.
            if (BuildSelectHud.IsOpen)
                Object.FindAnyObjectByType<BuildSelectHud>().SweepClose();
            Assert.That(_md.MechanicsClock.Paused, Is.False, "dünya saati duraklı");
            _boss = Object.FindAnyObjectByType<BossReactorController>().transform;
            _md._castPort ??= new MdCastPort(_md);
            _cast = _md._castPort.RuleEngineV4Bridge.CastHost;
            _physics = new RuleEngineV4PhysicsServices();
            _catalog = _cast.CatalogAccess;
            _minions = new[] { Minion("SliceMinion1"), Minion("SliceMinion2"), Minion("SliceMinion3") };
            _allies = new[] { GameObject.Find("AllyDummy").transform, GameObject.Find("AllyDummy2").transform };
            _label = TestContext.CurrentContext.Test.Name;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject root in AllRoots())
            {
                if (root != null && !_preexisting.Contains(root))
                    Object.Destroy(root);
            }
            RuleEngineV4Feature.Enabled = false;
            yield return null;
            Application.logMessageReceived -= OnLog;
            Assert.That(_errors, Is.Empty, "konsol hatası:\n" + string.Join("\n", _errors));
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (msg != null && msg.Contains(MissingEventReceiver))
            {
                _errors.Add($"{_label} [{type}] {msg}");
                return;
            }
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            if (stack != null && stack.Contains(KnownDevToolsNoise))
                return;
            string first = string.IsNullOrEmpty(stack) ? string.Empty : stack.Split('\n')[0];
            _errors.Add($"{_label} [{type}] {msg} @ {first}");
        }

        [UnityTest]
        public IEnumerator SliceScene_FlagsOn_NoConsoleErrors()
        {
            yield return new WaitForSecondsRealtime(5f);
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Dash_StopsAtWall()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 5f);
            const float wallX = 2.5f;
            const float wallHalfDepth = 0.2f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestWall";
            wall.transform.position = new Vector3(Origin.x + wallX, _player.position.y, Origin.z);
            wall.transform.localScale = new Vector3(wallHalfDepth * 2f, 4f, 4f);
            Physics.SyncTransforms();

            float startX = _player.position.x;
            yield return Cast(3, 1);

            float radius = _physics.BodyRadius(_player);
            float wallNear = Origin.x + wallX - wallHalfDepth;
            Assert.That(_player.position.x - startX, Is.GreaterThan(1f), "dash başlamadı");
            Assert.That(_player.position.x + radius, Is.LessThanOrEqualTo(wallNear + PosTolM),
                $"oyuncu duvarın içine/arkasına geçti (x={_player.position.x:0.00}, duvar={wallNear:0.00})");
        }

        [UnityTest]
        public IEnumerator BossPush_AppliesPoiseNotDisplacement()
        {
            FreezeBoss();
            BossDirector bossDir = _md.MechanicsBossDirector;
            Assert.That(bossDir, Is.Not.Null, "BossDirector");
            Vector3 bossPos = _boss.position;
            Vector3 toPlayer = Flat(Origin - bossPos).normalized;
            float bossR = _physics.BodyRadius(_boss);
            ParkAll();
            Place(_player, bossPos + toPlayer * (bossR + 1f));
            _player.rotation = Quaternion.LookRotation(-toPlayer);
            Physics.SyncTransforms();

            CommandPlan push = PlannedFor(5, 2, Kilic);
            Assert.That(push.Commands.OfType<ItCommand>().Any(), "planda itme yok");
            float poiseBefore = bossDir.PoiseRatio;
            yield return Run(push, _boss);

            Assert.That(Flat(_boss.position - bossPos).magnitude, Is.LessThan(0.01f), "boss itildi");
            Assert.That(bossDir.PoiseRatio, Is.LessThan(poiseBefore), "boss denge hasarı almadı");
        }

        [UnityTest]
        public IEnumerator Missile_FirstHitOnly()
        {
            FreezeBoss();
            Equip(Yay);
            Arrange(Vector3.right);
            SliceLightMinionHost front = _minions[0];
            SliceLightMinionHost back = _minions[1];
            Place(front.transform, Origin + Vector3.right * 4f);
            Place(back.transform, Origin + Vector3.right * 8f);
            Physics.SyncTransforms();

            CommandPlan plan = PlannedFor(1, 2, Yay);
            Assert.That(plan.Commands.OfType<MermiFirlatCommand>().Any(), "planda mermi yok");
            yield return Run(plan, back.transform);

            Assert.That(front.Hp, Is.LessThan(front.MaxHp), "yoldaki ilk düşman vurulmadı");
            Assert.That(back.Hp, Is.EqualTo(back.MaxHp), "mermi ilk değeni geçip arkadakine vurdu");
        }

        [UnityTest]
        public IEnumerator Area_ForwardHemisphereOnly()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            SliceLightMinionHost front = _minions[0];
            SliceLightMinionHost back = _minions[1];
            Place(front.transform, Origin + Vector3.right * 2f);
            Place(back.transform, Origin - Vector3.right * 2f);
            Physics.SyncTransforms();

            CommandPlan melee = PlannedFor(1, 2, Kilic);
            var commands = new List<PhysicsCommand>
            {
                melee.Commands.OfType<OnSureCommand>().First(),
                new AlanAcCommand
                {
                    RadiusM = melee.Commands.OfType<YakinVurusCommand>().First().RangeM,
                    MaxTargets = _minions.Length,
                    PowerMult = 1f,
                },
                melee.Commands.OfType<HasarVerCommand>().First(),
            };
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            yield return Run(new CommandPlan(1, 2, Kilic, commands, melee.Target), front.transform);

            Assert.That(front.Hp, Is.LessThan(front.MaxHp), "öndeki düşman alan hasarı almadı");
            Assert.That(back.Hp, Is.EqualTo(back.MaxHp), "arkadaki düşman alan hasarı aldı");
        }

        [UnityTest]
        public IEnumerator Odakli_PicksLowestHpTarget()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            SliceLightMinionHost near = _minions[0];
            SliceLightMinionHost hurt = _minions[1];
            Place(near.transform, Origin + Vector3.right * 2f);
            Place(hurt.transform, Origin + Vector3.forward * 4f);
            Physics.SyncTransforms();
            hurt.ApplyDamage(hurt.MaxHp * 0.5f);
            int hurtBefore = hurt.Hp;
            float bossBefore = _md.MechanicsBossVitals.Hp;

            yield return Cast(1, 2);

            Assert.That(hurt.Hp, Is.LessThan(hurtBefore), "Odaklı en düşük canlı düşmanı vurmadı");
            Assert.That(near.Hp, Is.EqualTo(near.MaxHp), "Odaklı en yakın (tam canlı) düşmanı vurdu");
            Assert.That(_md.MechanicsBossVitals.Hp, Is.EqualTo(bossBefore), "Odaklı boss'u vurdu");
        }

        [UnityTest]
        public IEnumerator Structure_LimitsFivePerPlayerTwentyFiveGlobal()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();

            int perPlayer = RuleEngineV4WorldPhysicsRuntime.Active.StructureMaxPerPlayer;
            int global = RuleEngineV4WorldPhysicsRuntime.Active.StructureMaxGlobal;
            for (int i = 0; i < perPlayer + 1; i++)
                yield return Cast(1, 4);
            Assert.That(CountStructures(), Is.EqualTo(perPlayer), "oyuncu başına sabit yapı sınırı");

            RuleEngineV4StructureLimits limits = _md.RuleEngineV4Session.Structures;
            int placed = perPlayer;
            for (int slot = 1; placed < global; slot++)
            {
                for (int k = 0; k < perPlayer && placed < global; k++, placed++)
                    Assert.That(limits.TryPlace(slot), Is.True, $"slot {slot} yapı {k + 1}");
            }
            Assert.That(limits.TryPlace(global / perPlayer), Is.False, "küresel sabit yapı sınırı");

            float life = _catalog.Catalog.Globals.SabitStructureLifeSec;
            yield return WaitWorld(life + 0.5f);
            Assert.That(CountStructures(), Is.EqualTo(0), "yapılar ömrü dolunca kalkmadı");
            Assert.That(limits.TryPlace(0), Is.True, "süresi dolan yapılar sınırdan düşmedi");
        }

        [UnityTest]
        public IEnumerator Diminish_AppliesToPushNotDamage()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            SliceLightMinionHost target = _minions[0];

            CommandPlan push = PlannedFor(5, 2, Kilic);
            CommandPlan hit = PlannedFor(1, 2, Kilic);
            var commands = push.Commands.Where(c => c is not DengeVerCommand).ToList();
            commands.Add(hit.Commands.OfType<HasarVerCommand>().First());
            var plan = new CommandPlan(5, 2, Kilic, commands, push.Target);
            float pushM = commands.OfType<ItCommand>().First().DistanceM;
            RuleEngineV4WorldPhysics wp = RuleEngineV4WorldPhysicsRuntime.Active;
            float[] expected = { pushM, pushM * wp.DiminishMultSecond, pushM * wp.DiminishMultThird };

            var damage = new List<int>();
            for (int i = 0; i < expected.Length; i++)
            {
                Place(target.transform, Origin + Vector3.right * 2f);
                Physics.SyncTransforms();
                Vector3 before = target.transform.position;
                int hpBefore = target.Hp;
                yield return Run(plan, target.transform);
                float moved = Flat(target.transform.position - before).magnitude;
                Assert.That(moved, Is.EqualTo(expected[i]).Within(PosTolM), $"itme #{i + 1}");
                damage.Add(hpBefore - target.Hp);
                target.Configure(target.ActorId, target.MaxHp);
            }

            Assert.That(damage[0], Is.GreaterThan(0), "hasar uygulanmadı");
            Assert.That(damage, Is.All.EqualTo(damage[0]), "hasar azaldı: " + string.Join(", ", damage));
        }

        [UnityTest]
        public IEnumerator MotionHandoff_NewMotionCutsOld()
        {
            FreezeBoss();
            Equip(Kilic);
            Arrange(Vector3.right);
            Transform side = _minions[0].transform;
            Place(side, Origin + Vector3.forward * 6f);
            Physics.SyncTransforms();

            CommandPlan move = PlannedFor(3, 2, Kilic);
            var dashOnly = new CommandPlan(3, 2, Kilic,
                move.Commands.OfType<KendiniTasiCommand>().ToList(), move.Target);
            Assert.That(dashOnly.IsValid, "planda dash yok");

            bool firstDone = false;
            bool secondDone = false;
            _cast.Runner.Start(dashOnly, null, _ => firstDone = true);
            yield return WaitWorld(0.1f);
            Assert.That(_player.position.x - Origin.x, Is.GreaterThan(0.1f), "ilk dash başlamadı");

            Vector3 cutAt = _player.position;
            double startMs = _md.MechanicsClock.Director.WorldTimeMs;
            _cast.Runner.Start(dashOnly, side, _ => secondDone = true);
            yield return WaitUntilIdle();
            double sec = (_md.MechanicsClock.Director.WorldTimeMs - startMs) / 1000.0;

            Assert.That(firstDone, Is.False, "eski hareket kesilmedi (onComplete çağrıldı)");
            Assert.That(secondDone, Is.True, "yeni hareket bitmedi");
            Vector3 travel = Flat(_player.position - cutAt);
            Vector3 wanted = Flat(side.position - cutAt);
            Assert.That(travel.magnitude, Is.GreaterThan(1f), "yeni hareket başlamadı");
            Assert.That(Vector3.Angle(travel, wanted), Is.LessThan(10f),
                "eski hareket kesildikten sonra sürdü (yön yeni hedefe değil)");
            float cap = RuleEngineV4WorldPhysicsDefaults.DashSpeedMps * RuleEngineV4WorldPhysicsDefaults.MotionSpeedMaxMult;
            Assert.That(travel.magnitude / sec, Is.LessThanOrEqualTo(cap * 1.05f), "hız tavanı aşıldı");
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Slice108_AllCombosCastWithoutError()
        {
            FreezeBoss();
            var failures = new StringBuilder();
            int count = 0;
            foreach (int weapon in RuleEngineV4Slice.Weapons)
            {
                Equip(weapon);
                foreach (int verb in RuleEngineV4Slice.Runes)
                foreach (int adj in RuleEngineV4Slice.Runes)
                {
                    _label = $"{verb}-{adj} w{weapon}";
                    count++;
                    Arrange(Vector3.right);
                    for (int i = 0; i < _minions.Length; i++)
                    {
                        _minions[i].Configure(_minions[i].ActorId, _minions[i].MaxHp);
                        Place(_minions[i].transform, Origin + Quaternion.Euler(0f, 40f * (i - 1), 0f) * Vector3.right * (3f + i));
                    }
                    Place(_allies[0], Origin + Vector3.forward * 2f);
                    Physics.SyncTransforms();

                    bool launched;
                    try
                    {
                        launched = _cast.TryLaunchCombo(verb, adj, out _);
                    }
                    catch (Exception e)
                    {
                        failures.AppendLine($"{_label}: {e.GetType().Name}: {e.Message}");
                        continue;
                    }
                    if (!launched)
                        failures.AppendLine($"{_label}: TryLaunch false");
                    yield return WaitUntilIdle();
                    if (_cast.IsRunning)
                        failures.AppendLine($"{_label}: {CastTimeoutSec} sn içinde bitmedi");
                }
            }

            Assert.That(count, Is.EqualTo(108), "kombo sayısı");
            foreach (string e in _errors)
                failures.AppendLine(e);
            _errors.Clear();
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
        }

        IEnumerator Cast(int verb, int adj)
        {
            Assert.That(_cast.TryLaunchCombo(verb, adj, out _), Is.True, $"{verb}-{adj} cast edilmedi");
            yield return WaitUntilIdle();
            Assert.That(_cast.IsRunning, Is.False, $"{verb}-{adj} {CastTimeoutSec} sn içinde bitmedi; {Diag()}");
        }

        string Diag()
        {
            var clock = _md.MechanicsClock;
            var body = _player.GetComponent<Dovus.Game.Actors.MotionTemplateBodyHost>();
            return $"clock={(clock != null ? clock.name : "null")} paused={clock?.Paused} " +
                   $"worldMs={clock?.Director.WorldTimeMs:0} worldDt={clock?.WorldDeltaMs:0.0} " +
                   $"clockEnabled={clock?.isActiveAndEnabled} frame={Time.frameCount} " +
                   $"bodyDisplacing={body?.IsDisplacing} mdActive={_md.isActiveAndEnabled} " +
                   $"player={_player.position}";
        }

        IEnumerator Run(CommandPlan plan, Transform target)
        {
            bool done = false;
            _cast.Runner.Start(plan, target, _ => done = true);
            yield return WaitUntilIdle();
            Assert.That(done, Is.True, $"komut planı bitmedi; {Diag()}");
        }

        IEnumerator WaitUntilIdle()
        {
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (_cast.IsRunning && Time.realtimeSinceStartup < end)
                yield return null;
            yield return null;
        }

        IEnumerator WaitWorld(float sec)
        {
            double end = _md.MechanicsClock.Director.WorldTimeMs + sec * 1000.0;
            float guard = Time.realtimeSinceStartup + sec * 4f + 2f;
            while (_md.MechanicsClock.Director.WorldTimeMs < end && Time.realtimeSinceStartup < guard)
                yield return null;
        }

        void FreezeBoss()
        {
            if (_md.MechanicsBossDirector != null)
                _md.MechanicsBossDirector.enabled = false;
            var reactor = _boss.GetComponent<BossReactorController>();
            if (reactor != null)
                reactor.enabled = false;
        }

        void Equip(int weaponId)
        {
            IReadOnlyList<EquipmentItem> all = _md.AvailableWeapons;
            EquipmentItem primary = all.FirstOrDefault(w => WeaponNumber(w) == weaponId);
            Assert.That(primary, Is.Not.Null, $"silah {weaponId} yok");
            EquipmentItem secondary = all.FirstOrDefault(w => w != null && w != primary);
            _md.SetWeaponLoadout(primary, secondary);
            Assert.That(_md.EquippedWeaponNumber(), Is.EqualTo(weaponId), "takılı silah");
        }

        static int WeaponNumber(EquipmentItem item)
        {
            string id = item?.Id ?? string.Empty;
            int colon = id.LastIndexOf(':');
            int.TryParse(colon >= 0 ? id.Substring(colon + 1) : id, out int n);
            return n;
        }

        void Arrange(Vector3 facing)
        {
            ParkAll();
            Place(_player, Origin);
            _player.rotation = Quaternion.LookRotation(facing);
            Physics.SyncTransforms();
        }

        void ParkAll()
        {
            for (int i = 0; i < _minions.Length; i++)
                Place(_minions[i].transform, Park + Vector3.right * (2f * i));
            for (int i = 0; i < _allies.Length; i++)
                Place(_allies[i], Park + Vector3.forward * (2f + 2f * i));
        }

        static void Place(Transform t, Vector3 xz)
        {
            Vector3 p = t.position;
            t.position = new Vector3(xz.x, p.y, xz.z);
        }

        static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);

        CommandPlan PlannedFor(int verb, int adj, int weapon)
        {
            CommandPlan plan = _catalog.Planner.Plan(
                verb, adj, weapon, new RuleEngineV4CastContext(hasValidTarget: true, targetInWeaponRange: true));
            Assert.That(plan.IsValid, $"{verb}-{adj} w{weapon} boş plan");
            return plan;
        }

        static int CountStructures() =>
            Object.FindObjectsByType<RuleEngineV4PhysicsBodyHost>()
                .Count(b => b.name == "RuleEngineV4Structure");

        static SliceLightMinionHost Minion(string name)
        {
            GameObject go = GameObject.Find(name);
            Assert.That(go, Is.Not.Null, name);
            return go.GetComponent<SliceLightMinionHost>();
        }

        static List<GameObject> AllRoots()
        {
            var roots = new List<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            var probe = new GameObject("RuleEngineV4RootProbe");
            Object.DontDestroyOnLoad(probe);
            foreach (GameObject go in probe.scene.GetRootGameObjects())
            {
                if (go != probe)
                    roots.Add(go);
            }
            Object.DestroyImmediate(probe);
            return roots;
        }
    }
}
