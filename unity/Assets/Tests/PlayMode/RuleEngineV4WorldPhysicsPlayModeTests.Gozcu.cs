using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dovus.Core.Element;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Görsel duman testi: ~60 sn gerçek girdi (Kılıç ve Yay düz vuruş, dilimin 36 kombosu, 4 dodge,
    /// boss'a yaklaşma); 0,25 sn'de bir oyun kamerasından kare → tek kontakt sayfası PNG.
    /// Senaryo sonunda [GOZCU] satırları tür başına tools/gozcu-esik.txt ile karşılaştırılır (cırcır).
    /// Çıktı: <c>-gozcuOut &lt;klasör&gt;</c> ya da repo kökünde tools/verify-out/gozcu
    /// (kontakt.png, gozcu.txt, senaryo.txt, cirlik.txt).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string GozcuOutArg = "-gozcuOut";
        const string GozcuHostName = "GozcuHost";
        const float SmokeScenarioSec = 60f;
        const float SmokeFrameSec = 0.25f;
        const int SmokeSheetColumns = 12;
        const int SmokeThumbWidthPx = 256;
        const int SmokeSheetPadPx = 4;
        const int SmokeMarkBorderPx = 4;
        const float SmokeBasicWaitSec = 1.2f;
        const float SmokeSwapWaitSec = 2f;
        const float SmokeSettleSec = 2f;
        const float SmokeDodgeWaitSec = 2f;
        /// <summary>Dodge hakkı dolumu 6 sn (DodgeTuning); bir tam dolum + pay.</summary>
        const float SmokeDodgeRetrySec = 7f;
        const float SmokeApproachStopM = 1f;
        const float SmokeApproachMinSec = 3f;
        const float SmokeOffDotFrac = 0.5f;
        const int SmokeCombosPerWeapon = 3;
        const float SmokeMinionBaseM = 3f;
        const float SmokeMinionFanDeg = 40f;
        const float GozcuSelfCheckWaitSec = 0.5f;

        readonly List<string> _gozcuLines = new();
        readonly List<string> _smokeLog = new();
        float _smokeStart;
        GozcuContactRecorder _recorder;

        /// <summary>
        /// Gözcü LogError basar ve Unity bunu beklenmeyen hata sayar; Gözcü testleri dışındaki senaryolar
        /// eski konsol kapısıyla koşsun diye açılıştan hemen sonra kapatılır.
        /// </summary>
        internal static void DisableGozcu()
        {
            GameObject host = GameObject.Find(GozcuHostName);
            if (host != null)
                host.SetActive(false);
        }

        static bool IsGozcuTest() =>
            TestContext.CurrentContext.Test.Name.StartsWith("Gozcu", StringComparison.Ordinal);

        void OnGozcuLog(string msg, string stack, LogType type)
        {
            if (msg == null || !msg.StartsWith(GozcuReporter.Tag, StringComparison.Ordinal))
                return;
            float t = _smokeStart > 0f ? Time.realtimeSinceStartup - _smokeStart : 0f;
            _gozcuLines.Add($"t={t.ToString("0.00", CultureInfo.InvariantCulture)}s [{_label}] {msg}");
            if (_recorder != null)
                _recorder.MarkGozcu();
        }

        [UnityTest]
        public IEnumerator Gozcu_FlagsFallbackLogAndWallInsideBody()
        {
            // Gözcü LogError basar; kapı _gozcuLines, Unity'nin "beklenmeyen hata" iddiası değil.
            LogAssert.ignoreFailingMessages = true;
            FreezeBoss();
            Assert.That(GameObject.Find(GozcuHostName), Is.Not.Null, "Gözcü kurulmadı (DebugToolsBuilder)");
            _gozcuLines.Clear();
            Application.logMessageReceived += OnGozcuLog;
            GameObject wall = null;
            try
            {
                Debug.Log("[V4-FALLBACK] gozcu-oz-denetim: sahte fallback satırı");
                yield return new WaitForSecondsRealtime(GozcuSelfCheckWaitSec);
                Assert.That(_gozcuLines.Any(l => l.Contains(GozcuRule.Fallback)), Is.True,
                    "fallback satırı Gözcü'ye düşmedi:\n" + string.Join("\n", _gozcuLines));

                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "GozcuSelfCheckWall";
                wall.transform.position = _player.position + Vector3.up;
                wall.transform.localScale = new Vector3(0.6f, 2f, 0.6f);
                Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(GozcuSelfCheckWaitSec);
                Assert.That(_gozcuLines.Any(l => l.Contains(GozcuRule.BodyInCollider) && l.Contains(wall.name)), Is.True,
                    "gövdeye gömülü kutu Gözcü'ye düşmedi:\n" + string.Join("\n", _gozcuLines));
            }
            finally
            {
                Application.logMessageReceived -= OnGozcuLog;
                if (wall != null)
                    Object.Destroy(wall);
            }
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator GozcuSmoke_SixtySecondSlice_WithinRatchet()
        {
            LogAssert.ignoreFailingMessages = true;
            FreezeBoss();
            Assert.That(GameObject.Find(GozcuHostName), Is.Not.Null, "Gözcü kurulmadı (DebugToolsBuilder)");
            HexagonInputController input = InputController();
            yield return WaitSentenceIdle(input);
            RuneLoadout savedRunes = input.Engine.Loadout;
            Assert.That(input.Engine.TrySetLoadout(new RuneLoadout(RuleEngineV4Slice.Runes.ToArray())), Is.True, "build seçilemedi");
            SetSmokeLoadout(Kilic, Yay);

            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "oyun kamerası yok");
            _gozcuLines.Clear();
            _smokeLog.Clear();
            var recGo = new GameObject("GozcuContactRecorder");
            _recorder = recGo.AddComponent<GozcuContactRecorder>();
            _recorder.Begin(cam, SmokeFrameSec, SmokeThumbWidthPx);
            _smokeStart = Time.realtimeSinceStartup;
            Application.logMessageReceived += OnGozcuLog;
            var move = _player.GetComponent<MoveInputController>();
            try
            {
                // Dodge hakkı 2, dolum 6 sn: ikisi kombolardan önce, ikisi sonra.
                yield return SmokeBasicStrikes(input);
                yield return SmokeDodges(input, move, 0, 2);
                yield return SmokeCombos(input);
                yield return SmokeDodges(input, move, 2, 2);
                yield return SmokeApproachBoss(move);
            }
            finally
            {
                Application.logMessageReceived -= OnGozcuLog;
                move?.SetScriptedDirection(null);
                input.Engine.TrySetLoadout(savedRunes);
                _recorder.enabled = false;
            }

            string dir = GozcuOutDir();
            Directory.CreateDirectory(dir);
            int frames = _recorder.WriteSheet(Path.Combine(dir, "kontakt.png"), SmokeSheetColumns, SmokeSheetPadPx, SmokeMarkBorderPx);
            File.WriteAllText(Path.Combine(dir, "gozcu.txt"), string.Join("\n", _gozcuLines) + "\n");
            File.WriteAllText(Path.Combine(dir, "senaryo.txt"), string.Join("\n", _smokeLog) + "\n");
            float total = Time.realtimeSinceStartup - _smokeStart;
            Debug.Log($"[gozcu-duman] {total:0.0} sn, {frames} kare, {_gozcuLines.Count} Gözcü satırı → {dir}");
            _recorder = null;
            _smokeStart = 0f;

            Assert.That(frames, Is.GreaterThan(0), "kontakt sayfası boş");
            List<string> red = ApplyGozcuRatchet(_gozcuLines, dir);
            Assert.That(red, Is.Empty, "Gözcü cırcırı (tools/gozcu-esik.txt):\n" + string.Join("\n", red)
                + $"\n\n{_gozcuLines.Count} Gözcü satırı:\n" + string.Join("\n", _gozcuLines));
        }

        IEnumerator SmokeBasicStrikes(HexagonInputController input)
        {
            foreach (int weapon in new[] { Kilic, Yay })
            {
                yield return SmokeSwapTo(input, weapon);
                for (int i = 0; i < 2; i++)
                {
                    _label = $"düz vuruş w{weapon} #{i + 1}";
                    yield return SmokeReady(input);
                    SmokePlaceMinions();
                    input.Pointer.BeginPointer(CenterPos(input));
                    input.Pointer.EndPointer(cancelled: false);
                    var clips = new HashSet<string>();
                    float end = Time.realtimeSinceStartup + SmokeBasicWaitSec;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        foreach (AnimatorClipInfo ci in PlayerAnimator().GetCurrentAnimatorClipInfo(0))
                        {
                            if (ci.weight > ClipActiveWeight)
                                clips.Add(ci.clip.name);
                        }
                    }
                    SmokeNote("klipler=" + string.Join(",", clips));
                }
            }
        }

        IEnumerator SmokeCombos(HexagonInputController input)
        {
            int index = 0;
            foreach (int verb in RuleEngineV4Slice.Runes)
            foreach (int adj in RuleEngineV4Slice.Runes)
            {
                int weapon = (index / SmokeCombosPerWeapon) % 2 == 0 ? Kilic : Yay;
                index++;
                yield return SmokeSwapTo(input, weapon);
                _label = $"kombo {verb}-{adj} w{weapon}";
                yield return SmokeReady(input);
                SmokePlaceMinions();
                int verbDot = DotForRune(input, verb);
                int adjDot = DotForRune(input, adj);
                input.Pointer.BeginPointer(DotPos(input, verbDot));
                yield return null;
                if (adjDot == verbDot)
                {
                    // Aynı noktaya ikinci dokunuş: noktadan çıkıp geri girilir.
                    input.Pointer.MovePointer(Vector2.Lerp(DotPos(input, verbDot), CenterPos(input), SmokeOffDotFrac));
                    yield return null;
                }
                input.Pointer.MovePointer(DotPos(input, adjDot));
                yield return null;
                string drawn = $"faz={input.Engine.State.Phase} kelime={input.Engine.State.Words.Count}";
                input.Pointer.EndPointer(cancelled: false);
                SmokeNote((_cast.IsRunning ? "atıldı" : $"atılmadı red={_cast.LastDenial} {SmokeDiag()}") + " " + drawn);
                yield return WaitUntilIdle();
            }
            SmokeNote($"{index} kombo");
        }

        IEnumerator SmokeDodges(HexagonInputController input, MoveInputController move, int first, int count)
        {
            Vector2[] sticks = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
            Vector2 dodgePx = HexagonLayoutScreen.DodgeButtonPx(input.Tuning, Screen.width, Screen.height);
            for (int i = first; i < first + count && i < sticks.Length; i++)
            {
                _label = $"dodge #{i + 1} çubuk={sticks[i]}";
                move.SetScriptedDirection(sticks[i]);
                yield return null;
                // Hak dolana dek basmayı yineler (oyuncunun yapacağı gibi).
                int presses = 0;
                bool started = false;
                float retryEnd = Time.realtimeSinceStartup + SmokeDodgeRetrySec;
                while (!started && Time.realtimeSinceStartup < retryEnd)
                {
                    input.Pointer.BeginPointer(dodgePx);
                    input.Pointer.EndPointer(cancelled: false);
                    presses++;
                    started = input.Dodge.IsActive(NowWorldMs());
                    if (!started)
                        yield return new WaitForSecondsRealtime(SmokeFrameSec);
                }
                SmokeNote((started ? "dodge başladı" : "dodge başlamadı") + $" basış={presses} {SmokeDiag()}");
                float end = Time.realtimeSinceStartup + SmokeDodgeWaitSec;
                yield return null;
                while (input.Dodge.IsActive(NowWorldMs()) && Time.realtimeSinceStartup < end)
                    yield return null;
                move.SetScriptedDirection(null);
                yield return new WaitForSecondsRealtime(SmokeFrameSec);
            }
        }

        IEnumerator SmokeApproachBoss(MoveInputController move)
        {
            _label = "boss'a yaklaşma";
            var follow = Object.FindAnyObjectByType<FollowCameraController>();
            float bossR = _physics.BodyRadius(_boss);
            float start = Time.realtimeSinceStartup;
            bool reached = false;
            while (Time.realtimeSinceStartup - _smokeStart < SmokeScenarioSec
                   || Time.realtimeSinceStartup - start < SmokeApproachMinSec)
            {
                Vector3 to = Flat(_boss.position - _player.position);
                if (to.magnitude <= bossR + SmokeApproachStopM)
                {
                    if (!reached)
                        SmokeNote($"boss'a vardı ({to.magnitude:0.0} m)");
                    reached = true;
                    move.SetScriptedDirection(null);
                }
                else
                {
                    float yaw = follow != null ? follow.MovementYawDeg : 0f;
                    Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * to.normalized;
                    move.SetScriptedDirection(new Vector2(local.x, local.z));
                }
                yield return null;
            }
            move.SetScriptedDirection(null);
            if (!reached)
                SmokeNote($"boss'a varamadı (kalan {Flat(_boss.position - _player.position).magnitude:0.0} m)");
        }

        IEnumerator SmokeSwapTo(HexagonInputController input, int weapon)
        {
            if (_md.EquippedWeaponNumber() == weapon)
                yield break;
            _label = $"silah takası → w{weapon}";
            yield return SmokeReady(input);
            Vector2 swapPx = HexagonLayoutScreen.WeaponSwapButtonPx(input.Tuning, Screen.width, Screen.height);
            input.Pointer.BeginPointer(swapPx);
            input.Pointer.EndPointer(cancelled: false);
            float end = Time.realtimeSinceStartup + SmokeSwapWaitSec;
            while (_md.EquippedWeaponNumber() != weapon && Time.realtimeSinceStartup < end)
                yield return null;
            if (_md.EquippedWeaponNumber() == weapon)
            {
                SmokeNote("takas düğmesiyle");
                yield break;
            }
            SmokeNote($"takas düğmesi değiştirmedi ({SmokeDiag()}); yükleme ile takıldı");
            SetSmokeLoadout(weapon, weapon == Kilic ? Yay : Kilic);
        }

        void SetSmokeLoadout(int primary, int secondary)
        {
            IReadOnlyList<EquipmentItem> all = _md.AvailableWeapons;
            EquipmentItem p = all.FirstOrDefault(w => WeaponNumber(w) == primary);
            EquipmentItem s = all.FirstOrDefault(w => WeaponNumber(w) == secondary);
            Assert.That(p, Is.Not.Null, $"silah {primary} yok");
            Assert.That(s, Is.Not.Null, $"silah {secondary} yok");
            _md.SetWeaponLoadout(p, s);
            Assert.That(_md.EquippedWeaponNumber(), Is.EqualTo(primary), "takılı silah");
        }

        /// <summary>Oyuncu ışınlanmaz (kamera sıçraması Gözcü'ye yanlış satır yazdırır); hedefler önüne dizilir.</summary>
        void SmokePlaceMinions()
        {
            Vector3 fwd = Flat(_player.forward);
            fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            for (int i = 0; i < _minions.Length; i++)
            {
                _minions[i].Configure(_minions[i].ActorId, _minions[i].MaxHp);
                Vector3 dir = Quaternion.Euler(0f, SmokeMinionFanDeg * (i - 1), 0f) * fwd;
                Place(_minions[i].transform, _player.position + dir * (SmokeMinionBaseM + i));
            }
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Oyuncunun bekleyeceği kadar bekler (cümle boş, skill bitti, genel bekleme ve takas bitti, durum çizime
        /// izin veriyor), sonra iki manayı da doldurur: 36 kombo gerçek bekleme sürelerini 60 sn'ye sığdıramaz.
        /// </summary>
        IEnumerator SmokeReady(HexagonInputController input)
        {
            PlayerCooldownHost cooldown = SceneHost<PlayerCooldownHost>();
            float end = Time.realtimeSinceStartup + SmokeSettleSec;
            while (Time.realtimeSinceStartup < end
                   && (input.Engine.State.Phase != SentencePhase.Idle
                       || _cast.IsRunning
                       || (cooldown != null && !cooldown.CanStartGlobalGate(NowWorldMs()))
                       || (_md.WeaponSwap != null && _md.WeaponSwap.IsSwapping)
                       || (_md._playerStates != null && !_md._playerStates.AllowsDraw)))
                yield return null;
            PlayerResourceHost res = SceneHost<PlayerResourceHost>();
            if (res != null)
                res.SweepRefillMana();
            _cast.ResetEconomy();
        }

        /// <summary>Oyuncu kaynağı/bekleme host'u oyuncu nesnesinde ya da sahnede tektir.</summary>
        T SceneHost<T>() where T : Component
        {
            T onPlayer = _player.GetComponent<T>();
            return onPlayer != null ? onPlayer : Object.FindAnyObjectByType<T>();
        }

        string SmokeDiag()
        {
            PlayerResourceHost res = SceneHost<PlayerResourceHost>();
            PlayerCooldownHost cooldown = SceneHost<PlayerCooldownHost>();
            double ms = _md.MechanicsClock.Director.WorldTimeMs;
            return $"durum={_md._playerStates?.CurrentId ?? "-"} " +
                   $"mana={(res != null ? res.Mana : -1f):0}/{(res != null ? res.MaxMana : -1f):0} " +
                   $"gcd={(cooldown != null ? cooldown.GlobalRemainingSec(ms) : -1f):0.00} " +
                   $"takas={_md.WeaponSwap?.IsSwapping} v4mana={_cast.Economy.Mana(ms):0}";
        }

        int NowWorldMs() => (int)_md.MechanicsClock.Director.WorldTimeMs;

        void SmokeNote(string what)
        {
            float t = Time.realtimeSinceStartup - _smokeStart;
            int frame = _recorder != null ? _recorder.FrameCount : 0;
            _smokeLog.Add($"t={t.ToString("0.00", CultureInfo.InvariantCulture)}s kare#{frame} [{_label}] {what}");
        }

        static string GozcuOutDir()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, GozcuOutArg);
            if (i >= 0 && i + 1 < args.Length)
                return args[i + 1];
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", "verify-out", "gozcu"));
        }

        /// <summary>
        /// Oyun kamerasını küçük RT'ye çizer; HUD/altıgen katmanı ayrı kamerada, karede yok.
        /// Gözcü'den sonra koşar: ölçüm, kayıt çiziminden önce alınır.
        /// </summary>
        [DefaultExecutionOrder(20000)]
        sealed class GozcuContactRecorder : MonoBehaviour
        {
            readonly List<Color32[]> _frames = new();
            readonly List<bool> _marked = new();
            Camera _cam;
            RenderTexture _rt;
            Texture2D _read;
            float _intervalSec;
            float _next;
            bool _pendingMark;
            int _w;
            int _h;

            public int FrameCount => _frames.Count;

            public void Begin(Camera cam, float intervalSec, int widthPx)
            {
                _cam = cam;
                _intervalSec = intervalSec;
                _w = widthPx;
                _h = Mathf.Max(1, Mathf.RoundToInt(widthPx / Mathf.Max(0.1f, cam.aspect)));
                _rt = new RenderTexture(_w, _h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _read = new Texture2D(_w, _h, TextureFormat.RGB24, false);
                _next = Time.realtimeSinceStartup;
            }

            public void MarkGozcu() => _pendingMark = true;

            void LateUpdate()
            {
                if (_cam == null || Time.realtimeSinceStartup < _next)
                    return;
                _next += _intervalSec;
                // SupportsRenderRequest URP'de ilgisiz shader graph hatası basıyor; istek doğrudan gönderilir.
                RenderPipeline.SubmitRenderRequest(_cam, new RenderPipeline.StandardRequest { destination = _rt });
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = _rt;
                _read.ReadPixels(new Rect(0, 0, _w, _h), 0, 0);
                RenderTexture.active = prev;
                _frames.Add(_read.GetPixels32());
                _marked.Add(_pendingMark);
                _pendingMark = false;
            }

            /// <summary>Soldan sağa, yukarıdan aşağı; Gözcü satırı düşen kare kırmızı çerçeveli.</summary>
            public int WriteSheet(string path, int columns, int pad, int border)
            {
                int n = _frames.Count;
                if (n == 0)
                    return 0;
                int rows = (n + columns - 1) / columns;
                int sw = columns * (_w + pad) + pad;
                int sh = rows * (_h + pad) + pad;
                var sheet = new Texture2D(sw, sh, TextureFormat.RGB24, false);
                var bg = new Color32[sw * sh];
                var dark = new Color32(24, 24, 24, 255);
                for (int i = 0; i < bg.Length; i++)
                    bg[i] = dark;
                sheet.SetPixels32(bg);
                var red = new Color32(255, 0, 0, 255);
                for (int f = 0; f < n; f++)
                {
                    int col = f % columns;
                    int row = f / columns;
                    int x = pad + col * (_w + pad);
                    int y = sh - (row + 1) * (_h + pad);
                    Color32[] px = _frames[f];
                    if (_marked[f])
                    {
                        px = (Color32[])px.Clone();
                        for (int yy = 0; yy < _h; yy++)
                        for (int xx = 0; xx < _w; xx++)
                        {
                            if (xx < border || yy < border || xx >= _w - border || yy >= _h - border)
                                px[yy * _w + xx] = red;
                        }
                    }
                    sheet.SetPixels32(x, y, _w, _h, px);
                }
                sheet.Apply(false);
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Destroy(sheet);
                return n;
            }

            void OnDestroy()
            {
                if (_rt != null)
                    _rt.Release();
                if (_read != null)
                    Destroy(_read);
            }
        }
    }
}
