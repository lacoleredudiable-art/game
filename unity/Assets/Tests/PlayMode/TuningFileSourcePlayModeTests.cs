using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Adım 2.4 (envanter A3, O-17.8, Y4, Y10): telefondaki ayar dosyası (tuning.json). Dosyadaki değer yalnız panelden
    /// KAYDET'e basılarak yazılmışsa geçerli; sürüm değişince sıfırlanır; açılışta her ayarın kaynağı log'a yazılır;
    /// dilimde JSON'dan gelen ayarın kaydırıcısı gri ve etkisiz. Her test oyunu gerçek GameBootstrapHost ile (dilim
    /// bayrakları açık) kurar; panel EventSystem işaretçi olaylarıyla (dokunuşun UI yolu) sürülür; "sonraki açılış"
    /// dünyayı yıkıp yeniden kurmaktır.
    /// </summary>
    public sealed class TuningFileSourcePlayModeTests
    {
        const float BootSettleSec = 0.5f;
        /// <summary>Panelin eski kendiliğinden kaydı 0,35 s sonra yazıyordu; bekleme onu kapsar.</summary>
        const float AutoSaveWaitSec = 1f;
        const string KnownDevToolsNoise = "FeelPlayVerifyController";
        const string FallbackTag = "[V4-FALLBACK]";
        const string SourceTag = "[AYAR-KAYNAK]";
        const string FollowSmoothRow = "Row_Takip yumuşatma";
        const string FollowSmoothPath = "prototype.FollowSmoothTimeSec";
        const string DodgeDistanceRow = "Row_Mesafe";
        const string CameraDistanceRow = "Row_Mesafe (varsayılan)";
        const string CameraLookHeightRow = "Row_Bakış yüksekliği";
        const string CameraPitchRow = "Row_Pitch (varsayılan)";
        const string PlayerHpRow = "Row_Oyuncu can tavanı";
        const string BossDamageRow = "Row_Hasar";

        static readonly Regex SourceLine = new(@"^\[AYAR-KAYNAK\] (\S+) = (.+?) \(kaynak: (JSON|ayar dosyası|kod)\b");

        readonly HashSet<GameObject> _preexisting = new();
        readonly List<string> _errors = new();
        readonly List<string> _logs = new();
        readonly Dictionary<string, byte[]> _guarded = new();
        GameBootstrapHost _host;
        ManifestationDirector _md;

        static string TuningPath => TuningConfig.FilePath;
        static string StaleBackup => TuningConfig.StaleBackupPath(TuningSchema.Version - 1);
        static float CodeFollowSmooth => new GameTuning().Camera.FollowSmoothTimeSec;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _errors.Clear();
            _logs.Clear();
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += OnLog;
            _preexisting.Clear();
            foreach (GameObject root in AllRoots())
                _preexisting.Add(root);
            Guard(TuningPath);
            Guard(StaleBackup);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return Shutdown();
            DebugPanelsChrome.SetVisible(false);
            foreach (KeyValuePair<string, byte[]> kv in _guarded)
            {
                if (kv.Value != null)
                    File.WriteAllBytes(kv.Key, kv.Value);
                else if (File.Exists(kv.Key))
                    File.Delete(kv.Key);
            }
            _guarded.Clear();
            Application.logMessageReceived -= OnLog;
            Assert.That(_errors, Is.Empty, "konsol hatası:\n" + string.Join("\n", _errors));
        }

        /// <summary>O-17.8: eski sürümlü dosya (panelden kaydedilmiş alan olsa bile) uygulanmaz; yedeklenir, yeniden yazılır.</summary>
        [UnityTest]
        public IEnumerator O_17_8_SurumDegisinceSifirlanir()
        {
            GameTuning.PanelFields proto = new GameTuning().ToPanelFields();
            proto.FollowSmoothTimeSec = 0.47f;
            var combat = new CombatTuning();
            combat.Dodge.DistanceM = 7.1f;
            File.WriteAllText(TuningPath,
                FileJson(TuningSchema.Version - 1, proto, combat, FollowSmoothPath, "combat.Dodge.DistanceM"));

            yield return Boot();
            Assert.That(LiveFollowSmooth(), Is.EqualTo(CodeFollowSmooth).Within(0.0001f),
                "eski sürüm dosyasındaki değer uygulandı");
            yield return OpenPanel();
            Assert.That(RowLabel(DodgeDistanceRow).text, Does.Contain(new CombatTuning().Dodge.DistanceM.ToString("0.00", CultureInfo.InvariantCulture)),
                "eski sürüm dosyasındaki dodge mesafesi uygulandı");
            yield return ClosePanel();
            Assert.That(File.Exists(StaleBackup), Is.True, "eski dosya yedeklenmedi");
            Assert.That(JsonUtility.FromJson<VersionProbe>(File.ReadAllText(TuningPath)).version,
                Is.EqualTo(TuningSchema.Version), "dosya güncel sürümle yeniden yazılmadı");

            yield return Shutdown();
            yield return Boot();
            Assert.That(LiveFollowSmooth(), Is.EqualTo(CodeFollowSmooth).Within(0.0001f),
                "yeniden yazılan dosya sonraki açılışta eski değeri taşıdı");
        }

        /// <summary>
        /// O-17.8: dosyada olup panelden KAYDET ile yazılmamış değer (eski panelin kendiliğinden kaydı, TryLoad'ın
        /// yeniden yazması) uygulanmaz; kod değeri kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator O_17_8_PaneldenKaydedilmeyenDegerUygulanmaz()
        {
            GameTuning.PanelFields proto = new GameTuning().ToPanelFields();
            proto.FollowSmoothTimeSec = 0.47f;
            File.WriteAllText(TuningPath, FileJson(TuningSchema.Version, proto, new CombatTuning()));

            yield return Boot();
            Assert.That(LiveFollowSmooth(), Is.EqualTo(CodeFollowSmooth).Within(0.0001f),
                "işaretsiz (kendiliğinden kaydedilmiş) dosya değeri uygulandı");
            yield return Shutdown();

            File.Delete(TuningPath);
            yield return Boot();
            yield return OpenPanel();
            Drag(RowSlider(FollowSmoothRow), 0.85f);
            Assert.That(LiveFollowSmooth(), Is.Not.EqualTo(CodeFollowSmooth).Within(0.01f), "kaydırıcı canlı değeri değiştirmedi");
            yield return new WaitForSecondsRealtime(AutoSaveWaitSec);
            yield return ClosePanel();
            yield return Shutdown();

            yield return Boot();
            Assert.That(LiveFollowSmooth(), Is.EqualTo(CodeFollowSmooth).Within(0.0001f),
                "KAYDET'e basılmadan değiştirilen değer sonraki açılışta uygulandı");
        }

        /// <summary>O-17.8: panelden KAYDET'e basılarak yazılan değer sonraki açılışta geçerli ve kaynağı "ayar dosyası".</summary>
        [UnityTest]
        public IEnumerator O_17_8_PaneldenKaydedilenDegerSonrakiAcilistaGecerli()
        {
            yield return Boot();
            yield return OpenPanel();
            Drag(RowSlider(FollowSmoothRow), 0.85f);
            float saved = LiveFollowSmooth();
            Assert.That(saved, Is.Not.EqualTo(CodeFollowSmooth).Within(0.01f), "kaydırıcı canlı değeri değiştirmedi");
            Click(PanelButton("KAYDET"));
            yield return ClosePanel();
            yield return Shutdown();

            _logs.Clear();
            yield return Boot();
            Assert.That(LiveFollowSmooth(), Is.EqualTo(saved).Within(0.0001f), "KAYDET'le yazılan değer sonraki açılışta uygulanmadı");
            Dictionary<string, (string Value, string Source)> sources = SourceLines();
            Assert.That(sources.ContainsKey(FollowSmoothPath), Is.True, "kaynak log'unda " + FollowSmoothPath + " yok");
            Assert.That(sources[FollowSmoothPath].Source, Is.EqualTo("ayar dosyası"), FollowSmoothPath + " kaynağı");
            Assert.That(sources["prototype.LookAheadM"].Source, Is.EqualTo("kod"), "dokunulmayan alan kod değerinde değil");
        }

        /// <summary>
        /// O-17.8: açılışta tek log bloğu; dosyadaki her ayar alanı "ad = değer (kaynak: JSON / ayar dosyası / kod)".
        /// Dilimde JSON'dan gelenler (slice_camera, scale, slice_boss_attacks, slice_arena) "JSON" yazar.
        /// </summary>
        [UnityTest]
        public IEnumerator O_17_8_AcilistaKaynakLogu()
        {
            GameTuning.PanelFields proto = new GameTuning().ToPanelFields();
            proto.FollowSmoothTimeSec = 0.31f;
            proto.LookAheadM = 2.2f;
            File.WriteAllText(TuningPath, FileJson(TuningSchema.Version, proto, new CombatTuning(), FollowSmoothPath));

            yield return Boot();
            Dictionary<string, (string Value, string Source)> s = SourceLines();
            Debug.Log("[O-17.8] kaynak log örneği:\n" + string.Join("\n", _logs
                .SelectMany(l => l.Split('\n')).Where(l => l.StartsWith(SourceTag, StringComparison.Ordinal)).Take(12)));

            var expected = new List<string>();
            CollectLeaves(new CombatTuning(), "combat.", expected);
            CollectLeaves(new GameTuning().ToPanelFields(), "prototype.", expected);
            List<string> missing = expected.Where(p => !s.ContainsKey(p)).ToList();
            Assert.That(missing, Is.Empty, "kaynak log'unda olmayan ayar alanları: " + string.Join(", ", missing));

            Assert.That(s[FollowSmoothPath], Is.EqualTo(("0.31", "ayar dosyası")), "panelden kaydedilmiş alan");
            Assert.That(s["prototype.LookAheadM"].Source, Is.EqualTo("kod"), "işaretsiz dosya alanı kod değerinde değil");
            Assert.That(Num(s["prototype.LookAheadM"].Value), Is.EqualTo(new GameTuning().Camera.LookAheadM).Within(0.0001f));
            Assert.That(s["combat.Dodge.IframeMs"].Source, Is.EqualTo("kod"));

            RuleEngineV4Catalog cat = new RuleEngineV4CatalogAccess().Catalog;
            AssertJson(s, "prototype.CameraDistanceM", cat.SliceCamera.DistanceM);
            AssertJson(s, "prototype.CameraDefaultPitchDeg", cat.SliceCamera.PitchDeg);
            AssertJson(s, "prototype.CameraLookHeightM", cat.SliceCamera.LookHeightM);
            AssertJson(s, "GameTuning.Camera.CameraFovDeg", cat.SliceCamera.FovDeg);
            AssertJson(s, "prototype.PlayerMaxHp", cat.Scale.PlayerMaxHp);
            AssertJson(s, "combat.Boss.MaxHp", cat.Scale.BossMaxHp);
            AssertJson(s, "combat.Boss.Damage", cat.SliceBossAttacks.LegSlamDamage);
            AssertJson(s, "combat.Boss.PounceDamage", cat.SliceBossAttacks.PounceDamage);
            AssertJson(s, "combat.SkillMotion.ArenaHalfSizeM", cat.SliceArena.OuterWallCollisionRadiusM);
            AssertJson(s, "GameTuning.Arena.ArenaHalfSizeM", cat.SliceArena.OuterWallCollisionRadiusM);
        }

        /// <summary>
        /// O-17.8, Y4, Y10: dilimde JSON'dan gelen ayarın kaydırıcısı gri ve "JSON" etiketli; dokunuş değeri değiştiremez,
        /// oyundaki değer JSON'da kalır ve [V4-FALLBACK] yazılmaz. "Oyuncu can tavanı" gerçek canı (1000) gösterir.
        /// </summary>
        [UnityTest]
        public IEnumerator O_17_8_DilimdeJsonKaydiricisi_GriVeEtkisiz()
        {
            yield return Boot();
            RuleEngineV4Catalog cat = new RuleEngineV4CatalogAccess().Catalog;
            FollowCameraController follow = Object.FindAnyObjectByType<FollowCameraController>();
            PlayerVitalsHost vitals = _md.CachedPlayerVitals();
            // Editörde DevHp havuzu yazılan canı örter; test yazılan canı okur.
            vitals.SetDevHp(false);
            float sceneDistance = _host.SceneTuning.Camera.CameraDistanceM;
            int sceneHp = _host.SceneTuning.Player.PlayerMaxHp;

            yield return OpenPanel();
            foreach (string row in new[] { CameraDistanceRow, CameraLookHeightRow, CameraPitchRow, PlayerHpRow, BossDamageRow })
            {
                Slider slider = RowSlider(row);
                Assert.That(slider.interactable, Is.False, row + " dilimde gri (etkileşimsiz) değil");
                Assert.That(RowLabel(row).text, Does.Contain("JSON"), row + " JSON etiketi taşımıyor");
                float before = slider.value;
                Drag(slider, 0.1f);
                Drag(slider, 0.9f);
                yield return null;
                Assert.That(slider.value, Is.EqualTo(before), row + " dokunuşla değişti");
            }
            Assert.That(RowLabel(PlayerHpRow).text, Does.Contain(Mathf.RoundToInt(cat.Scale.PlayerMaxHp).ToString(CultureInfo.InvariantCulture)),
                "Oyuncu can tavanı gerçek canı göstermiyor");
            Assert.That(RowLabel(BossDamageRow).text, Does.Contain(Mathf.RoundToInt(cat.SliceBossAttacks.LegSlamDamage).ToString(CultureInfo.InvariantCulture)),
                "Hasar JSON değerini göstermiyor");
            yield return ClosePanel();

            Assert.That(follow.BaseFrame.DistanceM, Is.EqualTo(cat.SliceCamera.DistanceM).Within(0.0001f), "O-5.1 kamera uzaklığı");
            Assert.That(follow.BaseFrame.PitchDeg, Is.EqualTo(cat.SliceCamera.PitchDeg).Within(0.0001f), "O-5.2 kamera eğimi");
            Assert.That(follow.BaseFrame.LookHeightM, Is.EqualTo(cat.SliceCamera.LookHeightM).Within(0.0001f), "O-5.3 bakış yüksekliği");
            Assert.That(vitals.MaxHp, Is.EqualTo(Mathf.RoundToInt(cat.Scale.PlayerMaxHp)), "O-13.1 oyuncu canı");
            Assert.That(_host.SceneTuning.Camera.CameraDistanceM, Is.EqualTo(sceneDistance), "gri kaydırıcı ayar değerini yazdı");
            Assert.That(_host.SceneTuning.Player.PlayerMaxHp, Is.EqualTo(sceneHp), "gri kaydırıcı can ayarını yazdı");
            List<string> fallbacks = _logs.Where(l => l.Contains(FallbackTag)).ToList();
            Assert.That(fallbacks, Is.Empty, "gri kaydırıcı " + FallbackTag + " yazdırdı:\n" + string.Join("\n", fallbacks));
        }

        // ---- kurulum ------------------------------------------------------------------------

        IEnumerator Boot()
        {
            var boot = new GameObject("TuningFileSourceBoot");
            boot.SetActive(false);
            _host = boot.AddComponent<GameBootstrapHost>();
            _host.SceneTuning.RuleEngineV4.Enabled = true;
            _host.SceneTuning.RuleEngineV4.SliceScene = true;
            boot.SetActive(true);
            RuleEngineV4WorldPhysicsPlayModeTests.DisableGozcu();
            yield return new WaitForSecondsRealtime(BootSettleSec);

            Assert.That(RuleEngineV4Feature.Enabled, Is.True, "RuleEngineV4Feature.Enabled");
            _md = Object.FindAnyObjectByType<ManifestationDirector>();
            Assert.That(_md, Is.Not.Null, "ManifestationDirector");
            if (BuildSelectHud.IsOpen)
                Object.FindAnyObjectByType<BuildSelectHud>().SweepClose();
            if (_md.MechanicsBossDirector != null)
                _md.MechanicsBossDirector.enabled = false;
        }

        IEnumerator Shutdown()
        {
            if (TuningPanelHud.IsOpen && Panel() != null)
                yield return ClosePanel();
            foreach (GameObject root in AllRoots())
            {
                if (root != null && !_preexisting.Contains(root))
                    Object.Destroy(root);
            }
            RuleEngineV4Feature.Enabled = false;
            _host = null;
            _md = null;
            yield return null;
        }

        void Guard(string path)
        {
            if (_guarded.ContainsKey(path))
                return;
            _guarded[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            if (File.Exists(path))
                File.Delete(path);
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (msg != null)
                _logs.Add(msg);
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            if (stack != null && stack.Contains(KnownDevToolsNoise))
                return;
            string first = string.IsNullOrEmpty(stack) ? string.Empty : stack.Split('\n')[0];
            _errors.Add($"[{type}] {msg} @ {first}");
        }

        float LiveFollowSmooth() => _host.SceneTuning.Camera.FollowSmoothTimeSec;

        // ---- panel (işaretçi olaylarıyla) ------------------------------------------------------

        static TuningPanelHud Panel() => Object.FindAnyObjectByType<TuningPanelHud>(FindObjectsInactive.Include);

        static IEnumerator OpenPanel()
        {
            DebugPanelsChrome.SetVisible(true);
            yield return null;
            if (!TuningPanelHud.IsOpen)
                Click(PanelChild<Button>("ToggleButton"));
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(TuningPanelHud.IsOpen, Is.True, "ayar paneli açılmadı");
        }

        static IEnumerator ClosePanel()
        {
            if (TuningPanelHud.IsOpen)
                Click(PanelChild<Button>("ToggleButton"));
            yield return null;
        }

        static T PanelChild<T>(string name) where T : Component
        {
            TuningPanelHud panel = Panel();
            Assert.That(panel, Is.Not.Null, "TuningPanelHud");
            Transform t = panel.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
            Assert.That(t, Is.Not.Null, "panelde " + name + " yok");
            T c = t.GetComponent<T>();
            Assert.That(c, Is.Not.Null, name + " üstünde " + typeof(T).Name + " yok");
            return c;
        }

        static Button PanelButton(string label) => PanelChild<Button>("Button_" + label);

        static Slider RowSlider(string row)
        {
            Slider s = PanelChild<Transform>(row).GetComponentInChildren<Slider>(true);
            Assert.That(s, Is.Not.Null, row + " kaydırıcısı");
            return s;
        }

        static Text RowLabel(string row)
        {
            Transform label = PanelChild<Transform>(row).Find("Label");
            Assert.That(label, Is.Not.Null, row + " etiketi");
            return label.GetComponent<Text>();
        }

        static void Click(Button button)
        {
            var e = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, e, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>Kaydırıcının <paramref name="frac"/> noktasına basıp sürükler (ekran overlay: dünya köşesi = piksel).</summary>
        static void Drag(Slider slider, float frac)
        {
            var corners = new Vector3[4];
            ((RectTransform)slider.transform).GetWorldCorners(corners);
            var pos = new Vector2(Mathf.Lerp(corners[0].x, corners[2].x, frac), (corners[0].y + corners[2].y) * 0.5f);
            var e = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = pos,
                pressPosition = pos,
                pointerPress = slider.gameObject,
                pointerDrag = slider.gameObject,
                useDragThreshold = false,
            };
            ExecuteEvents.Execute(slider.gameObject, e, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(slider.gameObject, e, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slider.gameObject, e, ExecuteEvents.pointerUpHandler);
        }

        // ---- dosya ve log ------------------------------------------------------------------

        [Serializable]
        sealed class VersionProbe
        {
            public int version;
        }

        /// <summary>tuning.json biçimi: sürüm, panelden kaydedilmiş alanlar, combat, prototype.</summary>
        static string FileJson(int version, GameTuning.PanelFields proto, CombatTuning combat, params string[] panelSaved)
        {
            string marks = string.Join(",", panelSaved.Select(p => "\"" + p + "\""));
            return "{\"version\":" + version.ToString(CultureInfo.InvariantCulture)
                + ",\"panelSaved\":[" + marks + "]"
                + ",\"combat\":" + JsonUtility.ToJson(combat)
                + ",\"prototype\":" + JsonUtility.ToJson(proto) + "}";
        }

        Dictionary<string, (string Value, string Source)> SourceLines()
        {
            var map = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            var dupes = new List<string>();
            foreach (string msg in _logs)
            foreach (string line in msg.Split('\n'))
            {
                Match m = SourceLine.Match(line.TrimEnd('\r'));
                if (!m.Success)
                    continue;
                if (map.ContainsKey(m.Groups[1].Value))
                    dupes.Add(m.Groups[1].Value);
                map[m.Groups[1].Value] = (m.Groups[2].Value, m.Groups[3].Value);
            }
            Assert.That(map, Is.Not.Empty, "açılışta " + SourceTag + " kaynak log bloğu yok");
            Assert.That(dupes, Is.Empty, "kaynak log'u tek blok değil; tekrar eden alanlar: " + string.Join(", ", dupes.Distinct()));
            return map;
        }

        static void AssertJson(Dictionary<string, (string Value, string Source)> s, string path, float jsonValue)
        {
            Assert.That(s.ContainsKey(path), Is.True, "kaynak log'unda " + path + " yok");
            Assert.That(s[path].Source, Is.EqualTo("JSON"), path + " kaynağı");
            Assert.That(Num(s[path].Value), Is.EqualTo(jsonValue).Within(0.0001f), path + " değeri");
        }

        static float Num(string v) => float.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>Ayar dosyasının yaprak alanları: "combat.Dodge.IframeMs", "combat.Sentence.Steps[0].RecoverySec".</summary>
        static void CollectLeaves(object node, string prefix, List<string> into)
        {
            foreach (FieldInfo f in node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsNotSerialized)
                    continue;
                object v = f.GetValue(node);
                Type t = f.FieldType;
                if (t.IsArray)
                {
                    var arr = (Array)v;
                    for (int i = 0; arr != null && i < arr.Length; i++)
                    {
                        object e = arr.GetValue(i);
                        if (IsLeaf(t.GetElementType()))
                            into.Add($"{prefix}{f.Name}[{i}]");
                        else if (e != null)
                            CollectLeaves(e, $"{prefix}{f.Name}[{i}].", into);
                    }
                }
                else if (IsLeaf(t))
                    into.Add(prefix + f.Name);
                else if (v != null)
                    CollectLeaves(v, prefix + f.Name + ".", into);
            }
        }

        static bool IsLeaf(Type t) => t.IsPrimitive || t.IsEnum || t == typeof(string) || t.IsValueType;

        static List<GameObject> AllRoots()
        {
            var roots = new List<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            var probe = new GameObject("TuningFileSourceProbe");
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
