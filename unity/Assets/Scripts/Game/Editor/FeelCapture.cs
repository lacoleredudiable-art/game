#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Dovus.Core.Combat;
using Dovus.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.EditorTools
{
    /// <summary>feel-pack (3/3) yakalama: lava görünür, gerçek vuruş karesi, %60 kadraj.</summary>
    public static class FeelCapture
    {
        public const string OutDir = @"C:\Users\lacol\_cleanup\feel\v2";
        const int W = 1600;
        const int H = 900;
        const float TargetFill = 0.60f;

        static Renderer[] _hiddenTransient;
        static BossDirector _pausedBoss;
        static float _lastStrikeT;
        static float _lastStrikeMetric;
        public static float LoggedDefaultCameraDistM;
        public static float LoggedLockOnCameraDistM;
        public static float LoggedWindupCameraDistM;
        public static float LastIdleScreenFillPct;
        public static float LastStrikeScreenFillPct;
        public static float LastSwordAngleFromVerticalDeg;
        public static float LastStrikeNormalizedT => _lastStrikeT;

        public static void PrepareSession()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            HideTransientOnly(true);
            Thread.Sleep(400);
        }

        public static void EndSession() => HideTransientOnly(false);

        public static void LogPropWorldSizes()
        {
            Transform player = FindPlayerRoot();
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            Animator pAnim = player != null ? player.GetComponentInChildren<Animator>() : null;
            Animator aAnim = ally != null ? ally.GetComponentInChildren<Animator>() : null;
            if (pAnim != null)
                WeaponHandProps.LogPropDiagnostics(pAnim, "Paladin");
            if (aAnim != null)
                WeaponHandProps.LogPropDiagnostics(aAnim, "SyntyAlly");
        }

        public static void LogGreenEllipsoidFindings()
        {
            Transform player = FindPlayerRoot();
            if (player == null)
                return;
            Vector3 p = player.position;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                if (r is ParticleSystemRenderer or LineRenderer or TrailRenderer)
                    continue;
                Color c = ReadColor(r);
                if (c.g < 0.7f || c.r > 0.55f || c.b > 0.55f)
                    continue;
                if (r.bounds.size.magnitude < 0.35f || r.bounds.size.magnitude > 2.5f)
                    continue;
                float d = Vector3.Distance(r.bounds.center, p);
                if (d > 3f)
                    continue;
                Debug.Log(
                    $"[FeelCapture] green-prop path={GetPath(r.transform)} size={r.bounds.size} dist={d:F2}m mat={r.sharedMaterial?.name}");
            }
        }

        public static void EnsureFightReady()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            AnimPreview.EnterFight();
            AnimPreview.Equip("kilic");
            HideTransientOnly(true);
            SetBlockersHidden(false);
            ResumeBossAi();
        }

        public static void LogDefaultCameraDistance()
        {
            LoggedDefaultCameraDistM = SampleResolvedDistance();
            Debug.Log(
                $"[FeelCapture] camera distance default resolved={LoggedDefaultCameraDistM:F2}m raw={SampleCameraToPlayerDist():F2}m pullback={SampleWindupPullback():F2}");
        }

        static bool _defaultCaptureActive;

        /// <summary>
        /// ff-4: Thread.Sleep ana iş parçacığını durdurduğu için hiç kare çalışmıyordu
        /// (FollowCamera.LateUpdate hiç tetiklenmiyordu) → varsayılan/lock-on kareleri birebir
        /// aynıydı. Gerçek Play karesi bekleyen EditorApplication.update zamanlayıcısı (bkz.
        /// ScheduleWindupCapture) ile değiştirildi.
        /// </summary>
        public static void CaptureGameplayDefault(int settleFrames = 24)
        {
            if (_defaultCaptureActive)
                return;
            _defaultCaptureActive = true;
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            AnimPreview.Equip("kilic");
            SetBlockersHidden(false);
            ResumeBossAi();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
                follow.LockOnActive = false;
            ScheduleFrames(settleFrames, () =>
            {
                _defaultCaptureActive = false;
                LogDefaultCameraDistance();
                RenderToFile(Camera.main, Path.Combine(OutDir, "gameplay-default.png"));
            });
        }

        static bool _lockOnCaptureActive;

        public static void CaptureLockOn(int settleFrames = 28)
        {
            if (_lockOnCaptureActive)
                return;
            _lockOnCaptureActive = true;
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            HideTransientOnly(true);
            SetBlockersHidden(false);
            ResumeBossAi();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
                follow.LockOnActive = true;
            SnapPlayerForLockOnFraming();
            ScheduleFrames(settleFrames, () =>
            {
                _lockOnCaptureActive = false;
                LoggedLockOnCameraDistM = SampleResolvedDistance();
                Debug.Log(
                    $"[FeelCapture] camera distance lock-on resolved={LoggedLockOnCameraDistM:F2}m raw={SampleCameraToPlayerDist():F2}m "
                    + $"(default resolved was {LoggedDefaultCameraDistM:F2}m) pullback={SampleWindupPullback():F2}");
                RenderToFile(Camera.main, Path.Combine(OutDir, "lockon.png"));
            });
        }

        /// <summary>Gerçek Play karesi bekleyen genel zamanlayıcı (Thread.Sleep YASAK — bkz. yukarı not).</summary>
        static void ScheduleFrames(int frames, System.Action onDone)
        {
            int left = Mathf.Max(1, frames);
            void Tick()
            {
                ResumeBossAi();
                left--;
                if (left > 0)
                    return;
                EditorApplication.update -= Tick;
                onDone();
            }

            EditorApplication.update += Tick;
        }

        static float SampleWindupPullback()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            return follow != null ? follow.WindupPullback01 : 0f;
        }

        static bool _windupWatchActive;

        /// <summary>Play modunda gerçek karelerle ~%50 windup yakalayıcı (RunCommand sonrası devam eder).</summary>
        public static void ScheduleWindupCapture(int maxFrames = 7200)
        {
            if (_windupWatchActive)
                return;
            _windupWatchActive = true;
            int frames = 0;
            void Tick()
            {
                frames++;
                ResumeBossAi();
                if (TryCaptureWindupPullback())
                {
                    EditorApplication.update -= Tick;
                    _windupWatchActive = false;
                    return;
                }

                if (frames >= maxFrames)
                {
                    Debug.LogWarning("[FeelCapture] windup schedule timed out");
                    EditorApplication.update -= Tick;
                    _windupWatchActive = false;
                }
            }

            EditorApplication.update += Tick;
        }

        /// <summary>Boss AI açıkken ~%50 windup'ta yakalar; hazır değilse false.</summary>
        public static bool TryCaptureWindupPullback()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            SetBlockersHidden(false);
            ResumeBossAi();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (boss == null || !boss.IsWindingUp)
                return false;
            float p = boss.WindupProgress01;
            if (p < 0.42f || p > 0.58f)
            {
                Debug.Log($"[FeelCapture] windup wait progress={p:F2}");
                return false;
            }

            LoggedWindupCameraDistM = SampleResolvedDistance();
            Debug.Log(
                $"[FeelCapture] camera distance windup resolved={LoggedWindupCameraDistM:F2}m (default={LoggedDefaultCameraDistM:F2}m lock-on={LoggedLockOnCameraDistM:F2}m) "
                + $"pullback={SampleWindupPullback():F2} progress={p:F2}");
            RenderToFile(Camera.main, Path.Combine(OutDir, "windup-pullback.png"));
            return true;
        }

        public static void PrepareImpactShot()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            HideTransientOnly(true);
            SetBlockersHidden(false);
            PauseBossAi();
            ClearCombatParticlesOnly();
            var feel = Object.FindAnyObjectByType<CombatFeel>();
            feel?.ClearThreat();
            AnimPreview.Equip("kilic");
        }

        static bool _impactCaptureActive;

        public static void CaptureImpactSparks(int settleFrames = 8)
        {
            if (_impactCaptureActive)
                return;
            _impactCaptureActive = true;
            AnimPreview.Strike();
            ScheduleFrames(settleFrames, () =>
            {
                _impactCaptureActive = false;
                RenderToFile(Camera.main, Path.Combine(OutDir, "impact-sparks.png"));
                ResumeBossAi();
            });
        }

        public static string HashAllCapturePngs()
        {
            var sb = new StringBuilder();
            foreach (string name in new[]
                     {
                         "gameplay-default.png", "lockon.png", "windup-pullback.png", "impact-sparks.png",
                         "damage-vignette.png", "paladin-idle.png", "paladin-strike.png"
                     })
            {
                string path = Path.Combine(OutDir, name);
                sb.Append(name).Append('=').Append(File.Exists(path) ? Sha256File(path) : "missing").Append(' ');
            }

            Debug.Log("[FeelCapture] sha256 " + sb);
            return sb.ToString().TrimEnd();
        }

        public static void CaptureDamageVignette()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            var feel = Object.FindAnyObjectByType<CombatFeel>();
            feel?.OnExchange(new ExchangeResult { Outcome = ExchangeOutcome.Hit });

            Thread.Sleep(200);
            RenderToFile(Camera.main, Path.Combine(OutDir, "damage-vignette.png"));
        }

        public static void CapturePaladinStrike()
        {
            PrepareSoloShot();
            Transform root = FindPlayerRoot();
            Animator anim = root != null ? root.GetComponentInChildren<Animator>() : null;
            float t = SampleStrikeTime(anim, out float metric);
            _lastStrikeT = t;
            _lastStrikeMetric = metric;
            Debug.Log($"[FeelCapture] strike t={t:F3} metric={metric:F3}");
            if (anim != null)
                ApplyStrikeSample(anim, t);
            CaptureSolo(root, true, "paladin-strike.png", strikePose: true);
            LastStrikeScreenFillPct = LogPixelHeight(Path.Combine(OutDir, "paladin-strike.png"), "paladin-strike");
        }

        public static void CapturePaladinIdle()
        {
            PrepareSoloShot();
            Transform root = FindPlayerRoot();
            Animator anim = root != null ? root.GetComponentInChildren<Animator>() : null;
            if (anim != null)
            {
                anim.Play("Locomotion", 0, 0f);
                anim.Update(0f);
                ResnapProps();
                WeaponHandProps.LogMixamoSwordAngle(anim, "feel-idle");
                LastSwordAngleFromVerticalDeg = ReadLastSwordAngleFromLog(anim);
            }

            CaptureSolo(root, true, "paladin-idle.png", strikePose: false);
            LastIdleScreenFillPct = LogPixelHeight(Path.Combine(OutDir, "paladin-idle.png"), "paladin-idle");
        }

        public static void CaptureSyntyAlly()
        {
            PrepareSession();
            Transform player = FindPlayerRoot();
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            if (player != null)
                player.gameObject.SetActive(false);
            if (ally != null)
            {
                ally.gameObject.SetActive(true);
                var av = ally.GetComponent<ActorVisual>();
                av?.SetWeapon("kilic");
                ResnapProps();
                CaptureSolo(ally.transform, true, "synty-ally.png", strikePose: false);
                LogPixelHeight(Path.Combine(OutDir, "synty-ally.png"), "synty-ally");
            }

            if (player != null)
                player.gameObject.SetActive(true);
        }

        static float SampleStrikeTime(Animator anim, out float bestMetric)
        {
            bestMetric = -1f;
            if (anim == null)
                return 0.42f;
            AnimationClip clip = FindStrikeClip(anim);
            if (clip == null)
                return 0.42f;
            Transform chest = anim.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                ?? anim.transform;
            Transform root = anim.transform;
            Vector3 fwd = FlatForward(root);
            bool was = anim.enabled;
            anim.enabled = false;
            float bestT = 0.42f;
            float clipLen = clip.length;
            for (int i = 1; i <= 24; i++)
            {
                float u = 0.05f + (i / 25f) * 0.90f;
                float tSec = u * clipLen;
                clip.SampleAnimation(anim.gameObject, tSec);
                ResnapProps();
                Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null)
                    continue;
                float dot = Vector3.Dot(hand.position - chest.position, fwd);
                float metric = dot + hand.position.y * 0.01f;
                if (metric > bestMetric)
                {
                    bestMetric = metric;
                    bestT = u;
                }
            }

            anim.enabled = was;
            return bestT;
        }

        static void ApplyStrikeSample(Animator anim, float normalizedT)
        {
            AnimationClip clip = FindStrikeClip(anim);
            if (clip == null)
                return;
            bool was = anim.enabled;
            anim.enabled = false;
            clip.SampleAnimation(anim.gameObject, normalizedT * clip.length);
            anim.enabled = was;
            ResnapProps();
        }

        static AnimationClip FindStrikeClip(Animator anim)
        {
            if (anim.runtimeAnimatorController is AnimatorOverrideController aoc)
            {
                var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                aoc.GetOverrides(list);
                foreach (var kv in list)
                {
                    if (kv.Key != null && kv.Key.name.IndexOf("Strike", System.StringComparison.OrdinalIgnoreCase) >= 0 && kv.Value != null)
                        return kv.Value;
                }
            }

            foreach (AnimationClip c in anim.runtimeAnimatorController.animationClips)
            {
                if (c.name.IndexOf("Strike", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return c;
            }

            return null;
        }

        static void CaptureSolo(Transform root, bool front, string fileName, bool strikePose)
        {
            if (root == null)
                return;
            string path = Path.Combine(OutDir, fileName);
            Vector3 face = FlatForward(root);
            Bounds bounds;
            TryCharacterBounds(root, out bounds);
            float h = Mathf.Max(0.5f, bounds.size.y);
            float fov = 38f;
            float margin = 1.06f;
            float dist = (h / TargetFill) / (2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad)) * margin;
            Vector3 camDir = front ? face : -face;
            if (strikePose)
                camDir = Quaternion.AngleAxis(35f, Vector3.up) * face;
            Vector3 focus = bounds.center;
            Vector3 camPos = focus - camDir * dist;
            camPos.y = focus.y;
            var camGo = new GameObject("FeelCaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(focus - camPos, Vector3.up));
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);
            float screenFill = ComputeScreenHeightFill(bounds, cam);
            Debug.Log($"[FeelCapture] screenFill {fileName}={screenFill * 100f:F1}%");
            if (!strikePose && fileName.Contains("idle"))
                LastIdleScreenFillPct = screenFill * 100f;
            if (strikePose)
                LastStrikeScreenFillPct = screenFill * 100f;
            RenderToFile(cam, path);
            Object.DestroyImmediate(camGo);
        }

        static float ComputeScreenHeightFill(Bounds bounds, Camera cam)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            Vector3[] corners =
            {
                c + new Vector3(e.x, e.y, e.z), c + new Vector3(e.x, e.y, -e.z),
                c + new Vector3(e.x, -e.y, e.z), c + new Vector3(e.x, -e.y, -e.z),
                c + new Vector3(-e.x, e.y, e.z), c + new Vector3(-e.x, e.y, -e.z),
                c + new Vector3(-e.x, -e.y, e.z), c + new Vector3(-e.x, -e.y, -e.z)
            };
            float minY = 1f;
            float maxY = 0f;
            bool any = false;
            foreach (Vector3 world in corners)
            {
                Vector3 vp = cam.WorldToViewportPoint(world);
                if (vp.z <= 0f)
                    continue;
                any = true;
                minY = Mathf.Min(minY, vp.y);
                maxY = Mathf.Max(maxY, vp.y);
            }

            return any ? Mathf.Clamp01(maxY - minY) : 0f;
        }

        static void HideTransientOnly(bool hide)
        {
            if (hide)
            {
                var list = new System.Collections.Generic.List<Renderer>();
                foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if (t == null || t.name != "FireConeTelegraph")
                        continue;
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                        list.Add(r);
                }

                _hiddenTransient = list.ToArray();
                foreach (Renderer r in _hiddenTransient)
                {
                    if (r != null)
                    {
                        r.enabled = false;
                        Debug.Log($"[FeelCapture] hidden transient path={GetPath(r.transform)} type=FireConeTelegraph");
                    }
                }
            }
            else if (_hiddenTransient != null)
            {
                foreach (Renderer r in _hiddenTransient)
                {
                    if (r != null)
                        r.enabled = true;
                }

                _hiddenTransient = null;
            }
        }

        static void SetBlockersHidden(bool hide)
        {
            var allies = new System.Collections.Generic.List<AllyDummy>(AllyDummy.Live);
            foreach (AllyDummy ally in allies)
            {
                if (ally != null)
                    ally.gameObject.SetActive(!hide);
            }

            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (boss != null)
                boss.gameObject.SetActive(!hide);
        }

        static void PrepareSoloShot()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            HideTransientOnly(true);
            SetBlockersHidden(true);
            PauseBossAi();
            ClearCombatParticlesOnly();
            AnimPreview.Equip("kilic");
            Thread.Sleep(200);
        }

        static void PauseBossAi()
        {
            if (_pausedBoss == null)
                _pausedBoss = Object.FindAnyObjectByType<BossDirector>();
            if (_pausedBoss != null)
                _pausedBoss.enabled = false;
        }

        static void ResumeBossAi()
        {
            if (_pausedBoss != null)
            {
                _pausedBoss.enabled = true;
                _pausedBoss = null;
            }
        }

        static void ClearCombatParticlesOnly()
        {
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps == null)
                    continue;
                string path = GetPath(ps.transform);
                if (path.IndexOf("Lava", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        static void SnapPlayerForLockOnFraming()
        {
            Transform player = FindPlayerRoot();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (player == null || boss == null)
                return;
            Vector3 toBoss = boss.transform.position - player.position;
            toBoss.y = 0f;
            float want = 7.5f;
            if (toBoss.sqrMagnitude < 0.01f)
                toBoss = Vector3.forward;
            float extra = Mathf.Max(0f, toBoss.magnitude - want);
            if (extra > 0.05f)
                player.position += toBoss.normalized * extra;
        }

        /// <summary>Gerçek kamera↔oyuncu metre mesafesi (ham, FollowCamera'nın yumuşatılmış hedefinden ayrı).</summary>
        static float SampleCameraToPlayerDist()
        {
            Transform player = FindPlayerRoot();
            if (player == null || Camera.main == null)
                return 0f;
            return Vector3.Distance(Camera.main.transform.position, player.position);
        }

        /// <summary>FollowCamera.ResolvedDistanceM — yumuşatılmış hedef mesafe (ff-4 doğrulama).</summary>
        static float SampleResolvedDistance()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            return follow != null ? follow.ResolvedDistanceM : SampleCameraToPlayerDist();
        }

        static float ReadLastSwordAngleFromLog(Animator anim)
        {
            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null)
                return 0f;
            Transform blade = null;
            foreach (Transform t in hand.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.IndexOf("Sword", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    blade = t;
                    break;
                }
            }

            if (blade == null)
                return 0f;
            Vector3 tip = blade.position + blade.up * 0.45f;
            Vector3 dir = (tip - hand.position).normalized;
            return Vector3.Angle(dir, Vector3.up);
        }

        static string Sha256File(string path)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(path));
            var hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.Append(b.ToString("x2"));
            return hex.ToString();
        }

        static void ResnapProps()
        {
            foreach (WeaponHandProps whp in Object.FindObjectsByType<WeaponHandProps>(FindObjectsSortMode.None))
                whp?.Apply("kilic");
        }

        static bool TryCharacterBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr == null)
                    continue;
                if (!any)
                {
                    bounds = smr.bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(smr.bounds);
            }

            return any;
        }

        static float LogPixelHeight(string pngPath, string label)
        {
            if (!File.Exists(pngPath))
                return 0f;
            byte[] bytes = File.ReadAllBytes(pngPath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            int minY = H, maxY = 0;
            Color[] px = tex.GetPixels();
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color c = px[y * tex.width + x];
                    if (c.grayscale > 0.08f && c.a > 0.5f)
                    {
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            Object.DestroyImmediate(tex);
            int height = maxY >= minY ? maxY - minY + 1 : 0;
            float pct = 100f * height / H;
            Debug.Log($"[FeelCapture] pixelHeight {label}={height} ({pct:F1}% of {H})");
            return pct;
        }

        static Transform FindPlayerRoot()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t != null && t.name == "Player" && t.gameObject.scene.isLoaded)
                    return t;
            }

            return null;
        }

        static Vector3 FlatForward(Transform root)
        {
            Vector3 f = root.forward;
            f.y = 0f;
            return f.sqrMagnitude < 0.01f ? Vector3.forward : f.normalized;
        }

        static Color ReadColor(Renderer r)
        {
            if (r.sharedMaterial == null)
                return Color.black;
            if (r.sharedMaterial.HasProperty("_BaseColor"))
                return r.sharedMaterial.GetColor("_BaseColor");
            if (r.sharedMaterial.HasProperty("_Color"))
                return r.sharedMaterial.GetColor("_Color");
            return Color.black;
        }

        static string GetPath(Transform t)
        {
            if (t == null)
                return "";
            var sb = new StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, '/');
                sb.Insert(0, t.name);
            }

            return sb.ToString();
        }

        static void RenderToFile(Camera cam, string outputPath)
        {
            if (cam == null)
                return;
            var urp = cam.GetComponent<UniversalAdditionalCameraData>()
                ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            rt.Release();
            Object.DestroyImmediate(rt);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? OutDir);
            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
#endif
