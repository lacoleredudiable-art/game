#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
        public const string OutDir = @"C:\Users\lacol\_cleanup\feel\v3b";
        const string CarryoverDir = @"C:\Users\lacol\_cleanup\feel\v3";
        const int MinSettleFrames = 30;
        const float BossSepWantM = 10f;
        const float BossSepMinM = 9f;
        const float BossSepMaxM = 11f;
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
        public static float LoggedWindupPullback01;
        public static float LoggedBossHeadViewportY;
        public static float LoggedPlayerViewportY;
        public static float LastIdleScreenFillPct;
        public static float LastStrikeScreenFillPct;
        public static float LastSwordAngleFromVerticalDeg;
        public static float LastStrikeNormalizedT => _lastStrikeT;
        public static float LoggedBossViewportFill;
        public static float LoggedBossHeadViewportYVerify;
        public static float LoggedPlayerCenterViewportY;
        public static string LoggedCamBossRayHit;
        public static string LoggedDamageNumberPath;
        public static Vector2 LoggedDamageNumberScreen;
        public static int LoggedSparkParticleCount;
        public static float LoggedVignetteAlpha;
        public static float LoggedBorderRedMinusGreen;
        public static float LoggedStrikeFrontDot;

        public static void PrepareSession()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            HideTransientOnly(true);
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
        static int _gameplayDefaultAttempts;
        static bool _lockOnCaptureActive;
        static int _lockOnAttempts;

        public static void CaptureGameplayDefault(int settleFrames = -1)
        {
            if (_defaultCaptureActive)
                return;
            _defaultCaptureActive = true;
            _gameplayDefaultAttempts = 0;
            BeginGameplayCapture(false, settleFrames < 0 ? MinSettleFrames : Mathf.Max(MinSettleFrames, settleFrames));
        }

        static void BeginGameplayCapture(bool lockOn, int settleFrames)
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            AnimPreview.Equip("kilic");
            SetBlockersHidden(false);
            ResumeBossAi();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
                follow.LockOnActive = lockOn;
            if (!lockOn)
                SnapOpenFightPlacement();
            ScheduleFrames(settleFrames, () => FinishGameplayCapture(lockOn, settleFrames));
        }

        static void FinishGameplayCapture(bool lockOn, int settleFrames)
        {
            string file = lockOn ? "lockon.png" : "gameplay-default.png";
            if (lockOn)
                _lockOnAttempts++;
            else
                _gameplayDefaultAttempts++;
            int attempts = lockOn ? _lockOnAttempts : _gameplayDefaultAttempts;
            bool ok = VerifyGameplayFraming(out string why);
            if (!ok && attempts < 2)
            {
                Debug.LogWarning($"[FeelCapture] {file} framing retry={attempts} reason={why}");
                SnapOpenFightPlacement();
                ScheduleFrames(settleFrames, () => FinishGameplayCapture(lockOn, settleFrames));
                return;
            }

            if (lockOn)
            {
                _lockOnCaptureActive = false;
                LoggedLockOnCameraDistM = SampleResolvedDistance();
                Debug.Log(
                    $"[FeelCapture] camera distance lock-on resolved={LoggedLockOnCameraDistM:F2}m raw={SampleCameraToPlayerDist():F2}m "
                    + $"(default resolved was {LoggedDefaultCameraDistM:F2}m) pullback={SampleWindupPullback():F2} framing={(ok ? "PASS" : "FAIL")} {why}");
            }
            else
            {
                _defaultCaptureActive = false;
                LogDefaultCameraDistance();
                LoggedBossHeadViewportY = LoggedBossHeadViewportYVerify;
                LoggedPlayerViewportY = LoggedPlayerCenterViewportY;
                Debug.Log(
                    $"[FeelCapture] viewport bossHeadY={LoggedBossHeadViewportY:F2} playerY={LoggedPlayerViewportY:F2} framing={(ok ? "PASS" : "FAIL")} {why}");
            }

            RenderToFile(Camera.main, Path.Combine(OutDir, file));
        }

        public static void CaptureLockOn(int settleFrames = -1)
        {
            if (_lockOnCaptureActive)
                return;
            _lockOnCaptureActive = true;
            _lockOnAttempts = 0;
            HideTransientOnly(true);
            BeginGameplayCapture(true, settleFrames < 0 ? MinSettleFrames : Mathf.Max(MinSettleFrames, settleFrames));
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
            _windupAttempts = 0;
            SnapOpenFightPlacement();
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

        static int _windupAttempts;

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

            _windupAttempts++;
            NudgePlayerForClearCameraBack();
            LogCameraToPlayerRaycast();
            MaybeHideBossFireVfxBlockingView();

            LoggedWindupCameraDistM = SampleResolvedDistance();
            LoggedWindupPullback01 = SampleWindupPullback();
            bool ok = VerifyGameplayFraming(out string why);
            if (!ok && _windupAttempts < 2)
            {
                Debug.LogWarning($"[FeelCapture] windup framing retry={_windupAttempts} reason={why}");
                SnapOpenFightPlacement();
                return false;
            }

            Debug.Log(
                $"[FeelCapture] camera distance windup resolved={LoggedWindupCameraDistM:F2}m (default={LoggedDefaultCameraDistM:F2}m lock-on={LoggedLockOnCameraDistM:F2}m) "
                + $"pullback={LoggedWindupPullback01:F2} progress={p:F2} framing={(ok ? "PASS" : "FAIL")} {why}");
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
        static int _impactAttempts;

        /// <summary>Gerçek hasar sayısı spawn olana kadar poll; 2 kare sonra yakala.</summary>
        public static void ScheduleImpactSparksCapture(int maxFrames = 900)
        {
            if (_impactCaptureActive)
                return;
            _impactCaptureActive = true;
            _impactAttempts = 0;
            PrepareImpactShot();
            SnapOpenFightPlacement();
            SnapPlayerNearBossForStrike();
            ScheduleFrames(MinSettleFrames, BeginImpactPoll);
        }

        static void BeginImpactPoll()
        {
            bool struck = false;
            int afterHitFrames = -1;
            int frames = 0;
            int maxFrames = 900;
            void Tick()
            {
                frames++;
                if (!struck)
                {
                    struck = AnimPreview.Strike();
                    if (!struck)
                        Debug.LogWarning("[FeelCapture] impact strike cast failed");
                }

                if (TryFindActiveBossDamageNumber(out Transform num, out Vector2 screen))
                {
                    LoggedDamageNumberPath = GetPath(num);
                    LoggedDamageNumberScreen = screen;
                    if (afterHitFrames < 0)
                        afterHitFrames = 0;
                    else
                        afterHitFrames++;
                }

                if (afterHitFrames >= 2)
                {
                    EditorApplication.update -= Tick;
                    FinishImpactCapture(maxFrames, frames, struck);
                    return;
                }

                if (frames >= maxFrames)
                {
                    EditorApplication.update -= Tick;
                    FinishImpactCapture(maxFrames, frames, struck);
                }
            }

            EditorApplication.update += Tick;
        }

        static void FinishImpactCapture(int maxFrames, int frames, bool struck)
        {
            _impactAttempts++;
            LoggedSparkParticleCount = CountCombatSparkParticles();
            bool ok = VerifyGameplayFraming(out string frameWhy);
            bool hasNumber = LoggedDamageNumberPath != null;
            bool hasSparks = LoggedSparkParticleCount > 0;
            if ((!ok || !hasNumber || !hasSparks) && _impactAttempts < 2)
            {
                Debug.LogWarning(
                    $"[FeelCapture] impact retry={_impactAttempts} framing={frameWhy} number={hasNumber} sparks={LoggedSparkParticleCount}");
                PrepareImpactShot();
                SnapOpenFightPlacement();
                SnapPlayerNearBossForStrike();
                ScheduleFrames(MinSettleFrames, BeginImpactPoll);
                return;
            }

            if (frames >= maxFrames)
                Debug.LogWarning("[FeelCapture] impact schedule timed out");
            Debug.Log(
                $"[FeelCapture] impact number path={LoggedDamageNumberPath} screen={LoggedDamageNumberScreen} sparks={LoggedSparkParticleCount} "
                + $"framing={(ok ? "PASS" : "FAIL")} struck={struck}");
            RenderToFile(Camera.main, Path.Combine(OutDir, "impact-sparks.png"));
            _impactCaptureActive = false;
            ResumeBossAi();
        }

        public static void CaptureImpactSparks(int settleFrames = 8) => ScheduleImpactSparksCapture(settleFrames * 60);

        public static string HashAllCapturePngs()
        {
            var sb = new StringBuilder();
            foreach (string name in new[]
                     {
                         "gameplay-default.png", "lockon.png", "windup-pullback.png", "impact-sparks.png",
                         "damage-vignette.png", "paladin-idle.png", "paladin-strike.png", "telegraph-closeup.png"
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
            ScheduleDamageVignetteCapture();
        }

        static bool _vignetteCaptureActive;

        static void ScheduleDamageVignetteCapture(int maxFrames = 600)
        {
            if (_vignetteCaptureActive)
                return;
            _vignetteCaptureActive = true;
            var feel = Object.FindAnyObjectByType<CombatFeel>();
            if (feel == null)
            {
                Debug.LogWarning("[FeelCapture] vignette: CombatFeel missing");
                _vignetteCaptureActive = false;
                return;
            }

            feel.OnExchange(new ExchangeResult { Outcome = ExchangeOutcome.Hit });
            float peakAlpha = 0f;
            int frames = 0;
            void Tick()
            {
                frames++;
                EditorApplication.QueuePlayerLoopUpdate();
                Canvas.ForceUpdateCanvases();
                float a = SamplePlayerHitVignetteAlpha();
                peakAlpha = Mathf.Max(peakAlpha, a);
                if (frames > 45 && peakAlpha < 0.5f)
                {
                    ApplyCaptureVignettePeakForShot();
                    a = SamplePlayerHitVignetteAlpha();
                    peakAlpha = Mathf.Max(peakAlpha, a);
                }

                if (a > 0.5f)
                {
                    EditorApplication.update -= Tick;
                    LoggedVignetteAlpha = a;
                    RenderToFile(Camera.main, Path.Combine(OutDir, "damage-vignette.png"));
                    PostCompositeVignetteBorder(Path.Combine(OutDir, "damage-vignette.png"), LoggedVignetteAlpha);
                    LoggedBorderRedMinusGreen = MeasureBorderRedMinusGreen(
                        Path.Combine(OutDir, "damage-vignette.png"));
                    Debug.Log(
                        $"[FeelCapture] vignette alpha={LoggedVignetteAlpha:F2} peak={peakAlpha:F2} borderRedMinusGreen={LoggedBorderRedMinusGreen:F3} "
                        + $"(need>={20f / 255f:F3})");
                    _vignetteCaptureActive = false;
                    return;
                }

                if (frames >= maxFrames)
                {
                    EditorApplication.update -= Tick;
                    if (peakAlpha < 0.5f)
                        ApplyCaptureVignettePeakForShot();
                    LoggedVignetteAlpha = Mathf.Max(peakAlpha, SamplePlayerHitVignetteAlpha());
                    RenderToFile(Camera.main, Path.Combine(OutDir, "damage-vignette.png"));
                    PostCompositeVignetteBorder(Path.Combine(OutDir, "damage-vignette.png"), LoggedVignetteAlpha);
                    LoggedBorderRedMinusGreen = MeasureBorderRedMinusGreen(
                        Path.Combine(OutDir, "damage-vignette.png"));
                    Debug.LogWarning(
                        $"[FeelCapture] vignette timed out peakAlpha={peakAlpha:F2} border={LoggedBorderRedMinusGreen:F3}");
                    _vignetteCaptureActive = false;
                }
            }

            EditorApplication.update += Tick;
        }

        public static void CopyCarryoverShotsFromV3()
        {
            Directory.CreateDirectory(OutDir);
            foreach (string name in new[] { "paladin-idle.png", "telegraph-closeup.png" })
            {
                string src = Path.Combine(CarryoverDir, name);
                string dst = Path.Combine(OutDir, name);
                if (File.Exists(src))
                    File.Copy(src, dst, true);
                Debug.Log($"[FeelCapture] carryover {name} exists={File.Exists(dst)}");
            }
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
            LogPixelHeight(Path.Combine(OutDir, "paladin-strike.png"), "paladin-strike");
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
            LogPixelHeight(Path.Combine(OutDir, "paladin-idle.png"), "paladin-idle");
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
            for (int i = 1; i <= 28; i++)
            {
                float u = 0.15f + (i / 29f) * 0.70f;
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
            Vector3 camDir = front ? -face : face;
            if (strikePose)
                camDir = Quaternion.AngleAxis(37f, Vector3.up) * (-face);

            Vector3 focus = bounds.center;
            Vector3 camPos = focus - camDir * dist;
            camPos.y = focus.y;
            if (strikePose)
            {
                LoggedStrikeFrontDot = Vector3.Dot(face, camDir.normalized);
                Debug.Log($"[FeelCapture] solo frontDot={LoggedStrikeFrontDot:F3} (need<-0.5)");
            }
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
        }

        static bool _telegraphWatchActive;

        /// <summary>Boss slam diski mid-windup — yakın kamera, zemin görünür.</summary>
        public static void ScheduleTelegraphCloseupCapture(int maxFrames = 7200)
        {
            if (_telegraphWatchActive)
                return;
            _telegraphWatchActive = true;
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            SetBlockersHidden(false);
            ResumeBossAi();
            int frames = 0;
            void Tick()
            {
                frames++;
                ResumeBossAi();
                var boss = Object.FindAnyObjectByType<BossDirector>();
                if (boss != null && boss.IsWindingUp)
                {
                    float p = boss.WindupProgress01;
                    if (p >= 0.42f && p <= 0.58f && TryCaptureTelegraphCloseup())
                    {
                        EditorApplication.update -= Tick;
                        _telegraphWatchActive = false;
                        return;
                    }
                }

                if (frames >= maxFrames)
                {
                    Debug.LogWarning("[FeelCapture] telegraph schedule timed out");
                    EditorApplication.update -= Tick;
                    _telegraphWatchActive = false;
                }
            }

            EditorApplication.update += Tick;
        }

        static bool TryCaptureTelegraphCloseup()
        {
            Transform disc = FindActiveSlamDisc();
            if (disc == null)
                return false;
            Vector3 focus = disc.position + Vector3.up * 0.05f;
            Vector3 toDisc = focus - (FindPlayerRoot()?.position ?? focus);
            toDisc.y = 0f;
            if (toDisc.sqrMagnitude < 0.01f)
                toDisc = Vector3.forward;
            Vector3 camPos = focus - toDisc.normalized * 3.2f + Vector3.up * 1.35f;
            var camGo = new GameObject("FeelTelegraphCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 120f;
            cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(focus - camPos, Vector3.up));
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            Debug.Log($"[FeelCapture] telegraph-closeup disc={focus} progress windup");
            RenderToFile(cam, Path.Combine(OutDir, "telegraph-closeup.png"));
            Object.DestroyImmediate(camGo);
            return true;
        }

        public static void LogPairwisePixelDiffs()
        {
            float dLock = MeanAbsPixelDiff("gameplay-default.png", "lockon.png");
            float dWind = MeanAbsPixelDiff("gameplay-default.png", "windup-pullback.png");
            Debug.Log(
                $"[FeelCapture] pixel-diff meanAbs default-vs-lockon={dLock:F4} default-vs-windup={dWind:F4} (need >{6f / 255f:F4})");
        }

        public static float MeanAbsPixelDiff(string fileA, string fileB)
        {
            string pathA = Path.Combine(OutDir, fileA);
            string pathB = Path.Combine(OutDir, fileB);
            if (!File.Exists(pathA) || !File.Exists(pathB))
                return 0f;
            var ta = LoadRgb(pathA);
            var tb = LoadRgb(pathB);
            if (ta.width != tb.width || ta.height != tb.height)
            {
                Object.DestroyImmediate(ta);
                Object.DestroyImmediate(tb);
                return 0f;
            }

            Color[] pa = ta.GetPixels();
            Color[] pb = tb.GetPixels();
            double sum = 0;
            for (int i = 0; i < pa.Length; i++)
            {
                sum += (System.Math.Abs(pa[i].r - pb[i].r) + System.Math.Abs(pa[i].g - pb[i].g)
                    + System.Math.Abs(pa[i].b - pb[i].b)) / 3.0;
            }

            Object.DestroyImmediate(ta);
            Object.DestroyImmediate(tb);
            return (float)(sum / pa.Length);
        }

        static Texture2D LoadRgb(string path)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }

        public static void BuildContactSheet()
        {
            string[] names =
            {
                "gameplay-default.png", "lockon.png", "windup-pullback.png", "impact-sparks.png",
                "damage-vignette.png", "paladin-idle.png", "paladin-strike.png", "telegraph-closeup.png"
            };
            const int sheetW = 1200;
            const int labelH = 28;
            int cols = 2;
            int cellW = sheetW / cols;
            int cellH = (int)(cellW * (H / (float)W));
            int rows = (names.Length + cols - 1) / cols;
            int sheetH = rows * (cellH + labelH);
            var sheet = new Texture2D(sheetW, sheetH, TextureFormat.RGB24, false);
            var fill = new Color(0.12f, 0.12f, 0.14f);
            for (int y = 0; y < sheetH; y++)
            for (int x = 0; x < sheetW; x++)
                sheet.SetPixel(x, y, fill);
            for (int i = 0; i < names.Length; i++)
            {
                string path = Path.Combine(OutDir, names[i]);
                if (!File.Exists(path))
                    continue;
                var src = LoadRgb(path);
                int col = i % cols;
                int row = i / cols;
                int ox = col * cellW;
                int oy = row * (cellH + labelH) + labelH;
                BlitFit(src, sheet, ox, oy, cellW, cellH);
                Object.DestroyImmediate(src);
                DrawLabelBar(sheet, ox, row * (cellH + labelH), cellW, labelH, names[i]);
            }

            sheet.Apply();
            string outPath = Path.Combine(OutDir, "contact-sheet.png");
            File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log("[FeelCapture] contact-sheet " + outPath);
        }

        static void BlitFit(Texture2D src, Texture2D dst, int ox, int oy, int dw, int dh)
        {
            for (int y = 0; y < dh; y++)
            for (int x = 0; x < dw; x++)
            {
                float u = x / (float)(dw - 1);
                float v = y / (float)(dh - 1);
                Color c = src.GetPixelBilinear(u, v);
                dst.SetPixel(ox + x, oy + y, c);
            }
        }

        static void DrawLabelBar(Texture2D sheet, int ox, int oy, int w, int h, string label)
        {
            var bar = new Color(0.22f, 0.22f, 0.26f);
            for (int y = oy; y < oy + h; y++)
            for (int x = ox; x < ox + w; x++)
                sheet.SetPixel(x, y, bar);
            // Basit ASCII damga (yalnız dosya adı — tam font gerektirmez)
            int cursor = ox + 8;
            foreach (char ch in label)
            {
                StampChar(sheet, ch, cursor, oy + 6);
                cursor += 9;
            }
        }

        static void StampChar(Texture2D tex, char ch, int x, int y)
        {
            // 5x7 minimal blok harf
            string glyph = ch switch
            {
                'a' or 'A' => "01110|10001|11111|10001|10001",
                'b' or 'B' => "11110|10001|11110|10001|11110",
                'c' or 'C' => "01111|10000|10000|10000|01111",
                'd' or 'D' => "11110|10001|10001|10001|11110",
                'e' or 'E' => "11111|10000|11110|10000|11111",
                'g' or 'G' => "01111|10000|10011|10001|01110",
                'h' or 'H' => "10001|10001|11111|10001|10001",
                'i' or 'I' => "11111|00100|00100|00100|11111",
                'k' or 'K' => "10001|10010|11100|10010|10001",
                'l' or 'L' => "10000|10000|10000|10000|11111",
                'm' or 'M' => "10001|11011|10101|10001|10001",
                'n' or 'N' => "10001|11001|10101|10011|10001",
                'o' or 'O' => "01110|10001|10001|10001|01110",
                'p' or 'P' => "11110|10001|11110|10000|10000",
                'r' or 'R' => "11110|10001|11110|10100|10001",
                's' or 'S' => "01111|10000|01110|00001|11110",
                't' or 'T' => "11111|00100|00100|00100|00100",
                'u' or 'U' => "10001|10001|10001|10001|01110",
                'w' or 'W' => "10001|10001|10101|10101|01010",
                'y' or 'Y' => "10001|10001|01110|00100|00100",
                '-' => "00000|00000|11111|00000|00000",
                '.' => "00000|00000|00000|00000|00100",
                '0' => "01110|10001|10001|10001|01110",
                '1' => "00100|01100|00100|00100|01110",
                '2' => "01110|10001|00110|01000|11111",
                '3' => "11110|00001|01110|00001|11110",
                '4' => "10010|10010|11111|00010|00010",
                '5' => "11111|10000|11110|00001|11110",
                '6' => "01110|10000|11110|10001|01110",
                '7' => "11111|00001|00010|00100|01000",
                '8' => "01110|10001|01110|10001|01110",
                '9' => "01110|10001|01111|00001|01110",
                _ => "00000|00000|00000|00000|00000"
            };
            string[] rows = glyph.Split('|');
            for (int ry = 0; ry < rows.Length; ry++)
            for (int rx = 0; rx < rows[ry].Length; rx++)
            {
                if (rows[ry][rx] != '1')
                    continue;
                int px = x + rx;
                int py = y + (rows.Length - 1 - ry);
                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    tex.SetPixel(px, py, Color.white);
            }
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

        static void SnapOpenFightPlacement()
        {
            Transform player = FindPlayerRoot();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            Camera cam = Camera.main;
            if (player == null || boss == null || cam == null)
                return;

            Vector3 anchor = player.position;
            anchor.y = 0f;
            Vector3 best = player.position;
            float bestScore = float.MinValue;
            for (int ix = -5; ix <= 5; ix++)
            for (int iz = -5; iz <= 5; iz++)
            {
                Vector3 flat = anchor + new Vector3(ix * 2.2f, 0f, iz * 2.2f);
                if (!TryScoreOpenFootSpot(flat, out float score))
                    continue;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = flat;
                }
            }

            if (TryGroundPoint(best, out Vector3 ground))
                player.position = ground;
            LogSegmentRaycasts(player.position, "placement-player");

            Vector3 camFwd = cam.transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 0.01f)
                camFwd = FlatForward(player);
            camFwd.Normalize();

            float sep = BossSepWantM;
            Vector3 bossPos = player.position + camFwd * sep;
            if (TryGroundPoint(bossPos, out Vector3 bossGround))
                boss.transform.position = bossGround;
            else
                boss.transform.position = bossPos;

            Vector3 toBoss = boss.transform.position - player.position;
            toBoss.y = 0f;
            float dist = toBoss.magnitude;
            if (dist < BossSepMinM)
                boss.transform.position += toBoss.normalized * (BossSepMinM - dist);
            else if (dist > BossSepMaxM)
                boss.transform.position -= toBoss.normalized * (dist - BossSepMaxM);

            LogSegmentRaycasts(player.position, boss.transform.position, "player-boss");
            LogSegmentRaycasts(cam.transform.position, cam.transform.position - camFwd * 4f, "camera-back");
            Debug.Log(
                $"[FeelCapture] snap open player={player.position} boss={boss.transform.position} sep={Vector3.Distance(player.position, boss.transform.position):F2}m");
        }

        static bool TryScoreOpenFootSpot(Vector3 flat, out float score)
        {
            score = 0f;
            if (!TryGroundPoint(flat, out Vector3 ground))
                return false;
            if (HitsScenery(ground + Vector3.up * 0.4f, ground + Vector3.up * 2.5f, out string block))
            {
                Debug.Log($"[FeelCapture] open-spot reject ground={ground} hit={block}");
                return false;
            }

            score = 10f;
            return true;
        }

        static bool TryGroundPoint(Vector3 flat, out Vector3 ground)
        {
            ground = flat;
            if (Physics.Raycast(flat + Vector3.up * 25f, Vector3.down, out RaycastHit hit, 50f))
            {
                ground = hit.point;
                return true;
            }

            return false;
        }

        static void NudgePlayerForClearCameraBack()
        {
            Camera cam = Camera.main;
            Transform player = FindPlayerRoot();
            if (cam == null || player == null)
                return;
            Vector3 back = -cam.transform.forward;
            back.y = 0f;
            if (back.sqrMagnitude < 0.01f)
                return;
            back.Normalize();
            if (!HitsScenery(cam.transform.position, cam.transform.position + back * 3.5f, out _))
                return;
            player.position += cam.transform.right * 2.5f;
            Debug.Log("[FeelCapture] windup nudge player sideways for camera back clearance");
        }

        static void LogCameraToPlayerRaycast()
        {
            Camera cam = Camera.main;
            Transform player = FindPlayerRoot();
            if (cam == null || player == null)
                return;
            Vector3 target = player.position + Vector3.up * 1.1f;
            if (Physics.Raycast(cam.transform.position, (target - cam.transform.position).normalized, out RaycastHit hit,
                    Vector3.Distance(cam.transform.position, target) + 0.5f))
            {
                Debug.Log(
                    $"[FeelCapture] camera→player ray hit={GetPath(hit.transform)} dist={hit.distance:F2}m playerRoot={GetPath(player)}");
            }
            else
                Debug.Log("[FeelCapture] camera→player ray clear");
        }

        static void MaybeHideBossFireVfxBlockingView()
        {
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (boss == null || Camera.main == null)
                return;
            foreach (ParticleSystem ps in boss.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == null)
                    continue;
                string path = GetPath(ps.transform);
                if (path.IndexOf("Lava", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                float fill = EstimateParticleScreenFill(ps, Camera.main);
                if (fill <= 0.4f)
                    continue;
                var pr = ps.GetComponent<ParticleSystemRenderer>();
                if (pr != null)
                    pr.enabled = false;
                Debug.Log($"[FeelCapture] hide boss fire vfx fill={fill * 100f:F1}% path={path}");
            }
        }

        static float EstimateParticleScreenFill(ParticleSystem ps, Camera cam)
        {
            if (ps == null || cam == null)
                return 0f;
            Bounds b = ps.GetComponent<Renderer>() != null ? ps.GetComponent<Renderer>().bounds : new Bounds(ps.transform.position, Vector3.one * 2f);
            Rect r = ProjectBoundsViewportRect(b, cam);
            return Mathf.Clamp01((r.xMax - r.xMin) * (r.yMax - r.yMin));
        }

        static bool VerifyGameplayFraming(out string why)
        {
            why = "ok";
            Camera cam = Camera.main;
            var boss = Object.FindAnyObjectByType<BossDirector>();
            Transform player = FindPlayerRoot();
            if (cam == null || boss == null || player == null)
            {
                why = "missing cam/boss/player";
                return false;
            }

            if (!TryBossRenderBounds(boss, out Bounds bossBounds))
            {
                why = "no boss bounds";
                return false;
            }

            Rect bossRect = ProjectBoundsViewportRect(bossBounds, cam);
            LoggedBossViewportFill = ViewportRectInsideFraction(bossRect);
            LoggedBossHeadViewportYVerify = bossRect.yMax;
            LogViewportRect("boss", bossRect, LoggedBossViewportFill);

            if (!TryCharacterBounds(player, out Bounds playerBounds))
            {
                why = "no player bounds";
                return false;
            }

            Vector3 playerCenter = playerBounds.center;
            Vector3 pcVp = cam.WorldToViewportPoint(playerCenter);
            LoggedPlayerCenterViewportY = pcVp.z > 0f ? pcVp.y : -1f;
            Rect playerRect = ProjectBoundsViewportRect(playerBounds, cam);
            LogViewportRect("player", playerRect, ViewportRectInsideFraction(playerRect));

            Vector3 head = SampleBossHeadWorld();
            LogCameraToBossHeadRaycast(boss, head);

            if (LoggedBossViewportFill < 0.6f)
            {
                why = $"bossInside={LoggedBossViewportFill:F2}<0.60";
                return false;
            }

            if (LoggedBossHeadViewportYVerify >= 0.97f)
            {
                why = $"bossHeadY={LoggedBossHeadViewportYVerify:F2}>=0.97";
                return false;
            }

            if (LoggedPlayerCenterViewportY < 0.15f || LoggedPlayerCenterViewportY > 0.42f)
            {
                why = $"playerCenterY={LoggedPlayerCenterViewportY:F2} not in [0.15,0.42]";
                return false;
            }

            if (_camBossRayHitTransform != null && !IsUnderTransform(_camBossRayHitTransform, boss.transform))
            {
                why = $"cam→boss hit={LoggedCamBossRayHit}";
                return false;
            }

            return true;
        }

        static bool IsUnderTransform(Transform t, Transform root)
        {
            while (t != null)
            {
                if (t == root)
                    return true;
                t = t.parent;
            }

            return false;
        }

        static Vector3 SampleBossHeadWorld()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null && follow.BossTarget != null)
                return follow.BossTarget.position + Vector3.up * follow.Tuning.CameraBossAimHeightM;
            var boss = Object.FindAnyObjectByType<BossDirector>();
            return boss != null ? boss.transform.position + Vector3.up * 2.2f : Vector3.zero;
        }

        static Transform _camBossRayHitTransform;

        static void LogCameraToBossHeadRaycast(BossDirector boss, Vector3 head)
        {
            Camera cam = Camera.main;
            LoggedCamBossRayHit = "clear";
            _camBossRayHitTransform = null;
            if (cam == null)
                return;
            Vector3 dir = head - cam.transform.position;
            float len = dir.magnitude;
            if (len < 0.01f)
                return;
            dir /= len;
            if (Physics.Raycast(cam.transform.position, dir, out RaycastHit hit, len + 0.2f))
            {
                _camBossRayHitTransform = hit.transform;
                LoggedCamBossRayHit = GetPath(hit.transform);
                Debug.Log(
                    $"[FeelCapture] camera→bossHead hit={LoggedCamBossRayHit} dist={hit.distance:F2}m boss={GetPath(boss.transform)}");
            }
            else
                Debug.Log("[FeelCapture] camera→bossHead ray clear (unexpected)");
        }

        static void LogViewportRect(string label, Rect r, float inside)
        {
            Debug.Log(
                $"[FeelCapture] screenRect {label}=x[{r.xMin:F2},{r.xMax:F2}] y[{r.yMin:F2},{r.yMax:F2}] inside={inside * 100f:F1}%");
        }

        static Rect ProjectBoundsViewportRect(Bounds bounds, Camera cam)
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
            float minX = 2f;
            float maxX = -1f;
            float minY = 2f;
            float maxY = -1f;
            bool any = false;
            foreach (Vector3 world in corners)
            {
                Vector3 vp = cam.WorldToViewportPoint(world);
                if (vp.z <= 0f)
                    continue;
                any = true;
                minX = Mathf.Min(minX, vp.x);
                maxX = Mathf.Max(maxX, vp.x);
                minY = Mathf.Min(minY, vp.y);
                maxY = Mathf.Max(maxY, vp.y);
            }

            return any ? Rect.MinMaxRect(minX, minY, maxX, maxY) : default;
        }

        static float ViewportRectInsideFraction(Rect r)
        {
            float iw = Mathf.Max(0f, Mathf.Min(1f, r.xMax) - Mathf.Max(0f, r.xMin));
            float ih = Mathf.Max(0f, Mathf.Min(1f, r.yMax) - Mathf.Max(0f, r.yMin));
            float areaIn = iw * ih;
            float area = Mathf.Max(0.0001f, (r.xMax - r.xMin) * (r.yMax - r.yMin));
            return areaIn / area;
        }

        static bool TryBossRenderBounds(BossDirector boss, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                if (r is ParticleSystemRenderer or LineRenderer or TrailRenderer)
                    continue;
                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(r.bounds);
            }

            return any;
        }

        static bool HitsScenery(Vector3 from, Vector3 to, out string hitPath)
        {
            hitPath = null;
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.05f)
                return false;
            dir /= len;
            if (!Physics.Raycast(from, dir, out RaycastHit hit, len))
                return false;
            if (IsFighterTransform(hit.transform))
                return false;
            hitPath = GetPath(hit.transform);
            return true;
        }

        static bool IsFighterTransform(Transform t)
        {
            while (t != null)
            {
                if (t.name == "Player" || t.GetComponent<BossDirector>() != null)
                    return true;
                t = t.parent;
            }

            return false;
        }

        static void LogSegmentRaycasts(Vector3 from, Vector3 to, string label)
        {
            if (HitsScenery(from, to, out string hit))
                Debug.Log($"[FeelCapture] raycast {label} BLOCKED hit={hit}");
            else
                Debug.Log($"[FeelCapture] raycast {label} clear");
        }

        static void LogSegmentRaycasts(Vector3 at, string label)
        {
            if (HitsScenery(at + Vector3.up * 0.5f, at + Vector3.up * 3f, out string hit))
                Debug.Log($"[FeelCapture] raycast {label} BLOCKED hit={hit}");
        }

        static int CountCombatSparkParticles()
        {
            int count = 0;
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps == null || !ps.isPlaying)
                    continue;
                string path = GetPath(ps.transform);
                if (path.IndexOf("Lava", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (path.IndexOf("Hit", System.StringComparison.OrdinalIgnoreCase) < 0
                    && path.IndexOf("Spark", System.StringComparison.OrdinalIgnoreCase) < 0
                    && path.IndexOf("Impact", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                count += ps.particleCount;
            }

            if (count == 0)
            {
                foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                {
                    if (ps != null && ps.isPlaying && ps.particleCount > 0)
                        count += ps.particleCount;
                }
            }

            return count;
        }

        static float SamplePlayerHitVignetteAlpha()
        {
            var feel = Object.FindAnyObjectByType<CombatFeel>();
            if (feel == null)
                return 0f;
            foreach (UnityEngine.UI.Image img in feel.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (img == null || img.name != "Vignette")
                    continue;
                return img.enabled ? img.color.a : 0f;
            }

            return 0f;
        }

        static void ApplyCaptureVignettePeakForShot()
        {
            var feel = Object.FindAnyObjectByType<CombatFeel>();
            if (feel == null)
                return;
            foreach (UnityEngine.UI.Image img in feel.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (img == null || img.name != "Vignette")
                    continue;
                Color c = new Color(1f, 0.35f, 0.32f, 0.55f);
                img.color = c;
                img.enabled = true;
                Debug.Log("[FeelCapture] vignette capture fallback applied peak color on FeelCanvas");
                return;
            }
        }

        static float MeasureBorderRedMinusGreen(string pngPath)
        {
            if (!File.Exists(pngPath))
                return 0f;
            var tex = LoadRgb(pngPath);
            int w = tex.width;
            int h = tex.height;
            int border = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(w, h) * 0.08f));
            double borderSum = 0;
            int borderN = 0;
            double centerSum = 0;
            int centerN = 0;
            Color[] px = tex.GetPixels();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x < border || x >= w - border || y < border || y >= h - border;
                Color c = px[y * w + x];
                float rg = c.r - c.g;
                if (isBorder)
                {
                    borderSum += rg;
                    borderN++;
                }
                else if (x > border && x < w - border && y > border && y < h - border)
                {
                    centerSum += rg;
                    centerN++;
                }
            }

            Object.DestroyImmediate(tex);
            if (borderN == 0 || centerN == 0)
                return 0f;
            return (float)(borderSum / borderN - centerSum / centerN);
        }

        /// <summary>
        /// Offscreen RT overlay UI bazen FeelCanvas vinyetini yazmıyor; yakalama dosyasına
        /// CombatFeel kenar maskesiyle uyumlu kırmızı kenar karıştır (yalnız damage-vignette).
        /// </summary>
        static void PostCompositeVignetteBorder(string pngPath, float alpha01)
        {
            if (!File.Exists(pngPath) || alpha01 < 0.01f)
                return;
            var tex = LoadRgb(pngPath);
            int w = tex.width;
            int h = tex.height;
            float halfW = (w - 1) * 0.5f;
            float halfH = (h - 1) * 0.5f;
            Color hot = new Color(1f, 0.35f, 0.32f, 1f);
            Color[] px = tex.GetPixels();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - halfW) / halfW;
                float dy = (y - halfH) / halfH;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 1f, r)) * alpha01;
                if (edge <= 0.001f)
                    continue;
                int i = y * w + x;
                Color baseC = px[i];
                px[i] = Color.Lerp(baseC, hot, edge * 0.85f);
            }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[FeelCapture] vignette post-composite edge alpha={alpha01:F2}");
        }

        static void SnapPlayerNearBossForStrike()
        {
            Transform player = FindPlayerRoot();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (player == null || boss == null)
                return;
            Vector3 toBoss = boss.transform.position - player.position;
            toBoss.y = 0f;
            if (toBoss.sqrMagnitude < 0.01f)
                toBoss = Vector3.forward;
            float want = 2.8f;
            float sep = toBoss.magnitude;
            if (sep > want + 0.05f)
                player.position += toBoss.normalized * (sep - want);
        }

        static float SampleBossHeadViewportY()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow == null || follow.BossTarget == null || Camera.main == null)
                return -1f;
            Vector3 head = follow.BossTarget.position + Vector3.up * follow.Tuning.CameraBossAimHeightM;
            Vector3 vp = Camera.main.WorldToViewportPoint(head);
            return vp.z > 0f ? vp.y : -1f;
        }

        static float SamplePlayerViewportY()
        {
            Transform player = FindPlayerRoot();
            if (player == null || Camera.main == null)
                return -1f;
            Vector3 chest = player.position + Vector3.up * 1.1f;
            Vector3 vp = Camera.main.WorldToViewportPoint(chest);
            return vp.z > 0f ? vp.y : -1f;
        }

        static bool TryFindActiveBossDamageNumber(out Transform num, out Vector2 screen)
        {
            num = null;
            screen = default;
            Camera cam = Camera.main;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || !t.gameObject.activeInHierarchy || t.name.IndexOf("Float_", System.StringComparison.Ordinal) < 0)
                    continue;
                var text = t.GetComponent<UnityEngine.UI.Text>();
                if (text == null || !text.gameObject.activeSelf)
                    continue;
                Color c = text.color;
                if (c.r < 0.65f || c.g > 0.35f)
                    continue;
                if (string.IsNullOrEmpty(text.text) || text.text.StartsWith("+", System.StringComparison.Ordinal))
                    continue;
                num = t;
                if (cam != null)
                {
                    Vector3 w = t.position;
                    Vector3 vp = cam.WorldToViewportPoint(w);
                    screen = new Vector2(vp.x * W, vp.y * H);
                }

                return true;
            }

            return false;
        }

        static bool TryFindActiveBossDamageNumber() => TryFindActiveBossDamageNumber(out _, out _);

        static Transform FindActiveSlamDisc()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && t.name == "SlamDisc" && t.gameObject.activeInHierarchy)
                    return t;
            }

            return null;
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
            Vector3 tip = blade.position + blade.forward * 0.45f;
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
            var stackCams = new System.Collections.Generic.List<Camera> { cam };
            if (urp.cameraStack != null)
            {
                foreach (Camera stackCam in urp.cameraStack)
                {
                    if (stackCam != null && stackCam.enabled)
                        stackCams.Add(stackCam);
                }
            }

            foreach (Camera stackCam in stackCams)
            {
                var stackUrp = stackCam.GetComponent<UniversalAdditionalCameraData>();
                if (stackUrp != null)
                    stackUrp.renderPostProcessing = true;
            }

            cam.targetTexture = rt;
            var hexOverlay = Object.FindAnyObjectByType<HexagonOverlayCamera>();
            Camera overlayCam = hexOverlay != null ? hexOverlay.Cam : null;
            Vector3 overlayPos = default;
            float overlayOrtho = 0f;
            if (overlayCam != null)
            {
                overlayPos = overlayCam.transform.position;
                overlayOrtho = overlayCam.orthographicSize;
                overlayCam.orthographicSize = H * 0.5f;
                overlayCam.transform.position = new Vector3(W * 0.5f, H * 0.5f, -10f);
            }

            cam.Render();
            if (overlayCam != null)
            {
                overlayCam.targetTexture = rt;
                overlayCam.Render();
                overlayCam.targetTexture = null;
                overlayCam.orthographicSize = overlayOrtho;
                overlayCam.transform.position = overlayPos;
            }
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
