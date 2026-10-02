#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dovus.Game;
using Dovus.Game.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Editor
{
    /// <summary>cw-3: kamera + 10 silah + contact sheet yakalama (gerçek Play kareleri).</summary>
    public static class FeelCaptureCw3
    {
        public const string OutDir = FeelCapture.CamWeaponOutDir;
        const int W = 1600;
        const int H = 900;

        static readonly string[] WeaponKeys =
        {
            "kilic", "kalkan", "cekic", "yumruk", "yay", "asa", "kitap", "kure", "tilsim", "top",
        };

        static readonly string[] WeaponFiles =
        {
            "w01-kilic.png", "w02-kalkan.png", "w03-cekic.png", "w04-yumruk.png", "w05-yay.png",
            "w06-asa.png", "w07-kitap.png", "w08-kure.png", "w09-tilsim.png", "w10-top.png",
        };

        static readonly string[] WeaponLabels =
        {
            "Kilic+Kalkan", "Kalkan+Hancer", "Cekic", "Yumruk", "Yay", "Asa", "Buyu Kitabi", "Kure", "Tilsim", "Top",
        };

        static int _phase;
        static bool _running;

        public static void RebuildCameraContactSheet() => BuildCameraContactSheet();

        public static void RunAll()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FeelCaptureCw3] Play modunda değil.");
                return;
            }

            _running = true;
            _phase = 0;
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            if (!AnimPreview.EnterFight())
            {
                ScheduleFrames(30, RunAll);
                _running = false;
                return;
            }

            ScheduleFrames(90, BeginCameraRock);
        }

        static void BeginCameraRock()
        {
            RestoreFight();
            FeelCapture.CamWeaponSnapOpen();
            PlacePlayerBehindLargeRock();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
                follow.LockOnActive = false;
            ScheduleFrames(90, () =>
            {
                LogCameraShot("rock", "cam-default-rock.png");
                RestoreFight();
                FeelCapture.CamWeaponSnapOpen();
                var f = Object.FindAnyObjectByType<FollowCamera>();
                if (f != null)
                    f.LockOnActive = false;
                ScheduleFrames(90, () =>
                {
                    LogCameraShot("open", "cam-default-open.png");
                    ScheduleFrames(30, BeginLockOn);
                });
            });
        }

        static void RestoreFight()
        {
            AnimPreview.EnterFight();
            FeelCapture.CamWeaponRestoreFight();
            LookPresets.Apply('B');
        }

        static void BeginLockOn()
        {
            RestoreFight();
            SnapLockOnPlacement(6.5f);
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            var view = Object.FindAnyObjectByType<HexagonView>();
            if (follow != null)
                follow.LockOnActive = true;
            ScheduleFrames(90, () =>
            {
                if (follow != null)
                {
                    follow.LockOnActive = true;
                    float visible = follow.LockOnPlayerVisibleRatio * 100f;
                    float lateral = SampleLateralOffsetM();
                    Debug.Log(
                        $"[FeelCaptureCw3] lock-on overlap visible={visible:F1}% lateralOffsetM={lateral:F3}");
                }

                FeelCapture.CamWeaponRender("cam-lockon.png");
                if (follow != null)
                    follow.LockOnActive = false;
                ScheduleFrames(45, () =>
                {
                    if (view?.LockOnButton != null)
                    {
                        view.RefreshLockOnVisual();
                        Canvas.ForceUpdateCanvases();
                        Debug.Log("[FeelCaptureCw3] lock-on HUD via main+overlay RenderToFile (HexagonOverlayCamera)");
                    }

                    FeelCapture.CamWeaponRender("cam-lockon-button.png");
                    ScheduleFrames(30, BeginWeapons);
                });
            });
        }

        static void BeginWeapons()
        {
            FeelCapture.CamWeaponPrepareSolo();
            ScheduleWeapon(0);
        }

        static void ScheduleWeapon(int index)
        {
            if (index >= WeaponKeys.Length)
            {
                ScheduleFrames(30, BuildCameraContactSheet);
                ScheduleFrames(45, BuildWeaponsContactSheet);
                ScheduleFrames(60, BeginSyntySheet);
                return;
            }

            string key = WeaponKeys[index];
            string file = WeaponFiles[index];
            AnimPreview.Equip(key);
            ScheduleFrames(65, () =>
            {
                Transform root = FindPlayerRoot();
                Animator anim = root != null ? root.GetComponentInChildren<Animator>() : null;
                if (anim != null)
                {
                    anim.Play("Locomotion", 0, 0f);
                    anim.Update(0f);
                }

                FeelCapture.CamWeaponCaptureIdleFront34(root, file, out float dot, out float fill);
                Camera cam = Camera.main;
                if (anim != null)
                    WeaponHandProps.LogWeaponPropVerification(anim, key, cam, "Cw3-Paladin");
                Debug.Log(
                    $"[FeelCaptureCw3] weapon {key} file={file} frontDot={dot:F3} screenFill={fill * 100f:F1}%");
                ScheduleWeapon(index + 1);
            });
        }

        static void BeginSyntySheet()
        {
            Transform player = FindPlayerRoot();
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            if (player != null)
                player.gameObject.SetActive(false);
            if (ally != null)
                ally.gameObject.SetActive(true);
            ScheduleSyntyWeapon(0, ally, player);
        }

        static void ScheduleSyntyWeapon(int index, AllyDummy ally, Transform player)
        {
            if (index >= WeaponKeys.Length)
            {
                BuildSyntyContactSheet();
                if (player != null)
                    player.gameObject.SetActive(true);
                if (ally != null)
                    ally.gameObject.SetActive(false);
                ScheduleFrames(20, Finish);
                return;
            }

            string key = WeaponKeys[index];
            AnimPreview.Equip(key);
            ScheduleFrames(65, () =>
            {
                Animator anim = ally != null ? ally.GetComponentInChildren<Animator>() : null;
                if (anim != null)
                {
                    anim.Play("Locomotion", 0, 0f);
                    anim.Update(0f);
                    WeaponHandProps.LogWeaponPropVerification(anim, key, Camera.main, "Cw3-Synty");
                    LogSyntyPropRoots(anim, key);
                }

                string tile = Path.Combine(OutDir, $"synty-tile-{index:00}.png");
                FeelCapture.CamWeaponCaptureIdleFront34(ally != null ? ally.transform : null, tile, out _, out _);
                ScheduleSyntyWeapon(index + 1, ally, player);
            });
        }

        static void LogSyntyPropRoots(Animator anim, string key)
        {
            foreach (HumanBodyBones bone in new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand })
            {
                Transform hand = anim.GetBoneTransform(bone);
                if (hand == null)
                    continue;
                Transform prop = null;
                for (int i = 0; i < hand.childCount; i++)
                {
                    Transform c = hand.GetChild(i);
                    if (c.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (c.name.IndexOf("thumb", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    prop = c;
                    break;
                }

                string vis = prop != null && prop.gameObject.activeInHierarchy ? "visible" : "hidden";
                Debug.Log($"[FeelCaptureCw3] Synty {key} {bone} prop={(prop != null ? prop.name : "none")} {vis}");
            }
        }

        static void Finish()
        {
            LogSha256();
            FeelCaptureCw1.RunAll();
            Debug.Log("[FeelCaptureCw3] capture sequence complete");
            _running = false;
        }

        static void LogCameraShot(string tag, string file)
        {
            RestoreFight();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            var player = FindPlayerRoot();
            Camera cam = Camera.main;
            float pulled = follow != null ? follow.CollisionPulledInM : 0f;
            bool inside = cam != null && CameraInsideAnyBlocker(cam.transform.position);
            float bossHeadY = -1f;
            Rect bossRect = default;
            Rect playerRect = default;
            if (cam != null && boss != null && player != null)
            {
                if (TryBounds(boss.transform, out Bounds bb))
                    bossRect = ProjectRect(bb, cam);
                if (TryBounds(player, out Bounds pb))
                    playerRect = ProjectRect(pb, cam);
                bossHeadY = bossRect.yMax;
            }

            Debug.Log(
                $"[FeelCaptureCw3] {tag} CollisionPulledInM={pulled:F3} insideBlocker={inside} "
                + $"bossHeadY={bossHeadY:F2} bossRect=({bossRect.xMin:F2},{bossRect.yMin:F2})-({bossRect.xMax:F2},{bossRect.yMax:F2}) "
                + $"playerRect=({playerRect.xMin:F2},{playerRect.yMin:F2})-({playerRect.xMax:F2},{playerRect.yMax:F2})");
            FeelCapture.CamWeaponRender(file);
        }

        static void PlacePlayerBehindLargeRock()
        {
            var motor = Object.FindAnyObjectByType<KinematicMotor>();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (motor == null || boss == null)
                return;
            Collider best = null;
            float bestVol = 0f;
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c == null || c.name != "CameraBlocker")
                    continue;
                float vol = c.bounds.size.x * c.bounds.size.y * c.bounds.size.z;
                if (vol > bestVol)
                {
                    bestVol = vol;
                    best = c;
                }
            }

            if (best == null)
                return;
            Vector3 rockCenter = best.bounds.center;
            Vector3 toBoss = boss.transform.position - rockCenter;
            toBoss.y = 0f;
            if (toBoss.sqrMagnitude < 0.25f)
                toBoss = Vector3.forward;
            toBoss.Normalize();
            Vector3 playerPos = rockCenter - toBoss * (best.bounds.extents.magnitude + 2.2f);
            if (Physics.Raycast(playerPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
                playerPos = hit.point;
            motor.transform.position = playerPos;
            Vector3 fwd = toBoss;
            motor.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
                follow.LockOnActive = false;
            float sep = 10f;
            Vector3 bossPos = playerPos + fwd * sep;
            if (Physics.Raycast(bossPos + Vector3.up * 20f, Vector3.down, out RaycastHit bh, 40f))
                bossPos = bh.point;
            boss.transform.position = bossPos;
            Debug.Log($"[FeelCaptureCw3] behind-rock player={playerPos} bossSep={Vector3.Distance(playerPos, bossPos):F2}m");
        }

        static void SnapLockOnPlacement(float sepM)
        {
            var motor = Object.FindAnyObjectByType<KinematicMotor>();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            Camera cam = Camera.main;
            if (motor == null || boss == null || cam == null)
                return;
            FeelCapture.CamWeaponSnapOpen();
            Vector3 fwd = motor.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f)
                fwd = Vector3.forward;
            fwd.Normalize();
            boss.transform.position = motor.transform.position + fwd * sepM;
        }

        static float SampleLateralOffsetM()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            var motor = Object.FindAnyObjectByType<KinematicMotor>();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (follow == null || motor == null || boss == null)
                return 0f;
            Vector3 line = boss.transform.position - motor.transform.position;
            line.y = 0f;
            if (line.sqrMagnitude < 0.01f)
                return 0f;
            line.Normalize();
            Vector3 camOff = follow.transform.position - motor.transform.position;
            camOff.y = 0f;
            Vector3 lateral = camOff - line * Vector3.Dot(camOff, line);
            return lateral.magnitude;
        }

        static void BuildCameraContactSheet()
        {
            string[] names = { "cam-default-rock.png", "cam-default-open.png", "cam-lockon.png", "cam-lockon-button.png" };
            BuildSheet(names, "camera-contact-sheet.png", 2);
        }

        static void BuildWeaponsContactSheet()
        {
            BuildSheet(WeaponFiles, "weapons-contact-sheet.png", 5, WeaponLabels);
        }

        static void BuildSyntyContactSheet()
        {
            var tiles = new string[WeaponKeys.Length];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = $"synty-tile-{i:00}.png";
            BuildSheet(tiles, "synty-weapons-contact-sheet.png", 5, WeaponLabels);
        }

        static void BuildSheet(string[] names, string outName, int cols, string[] labels = null)
        {
            const int sheetW = 1600;
            int cellW = sheetW / cols;
            int cellH = (int)(cellW * (H / (float)W));
            int labelH = 24;
            int rows = (names.Length + cols - 1) / cols;
            int sheetH = rows * (cellH + labelH);
            var sheet = new Texture2D(sheetW, sheetH, TextureFormat.RGB24, false);
            var fill = new Color(0.1f, 0.1f, 0.12f);
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
                if (labels != null && i < labels.Length)
                    DrawBar(sheet, ox, row * (cellH + labelH), cellW, labelH, labels[i]);
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, outName), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log("[FeelCaptureCw3] sheet " + outName);
        }

        static void LogSha256()
        {
            var seen = new HashSet<string>();
            var sb = new StringBuilder();
            foreach (string f in Directory.GetFiles(OutDir, "*.png"))
            {
                string h = Sha256File(f);
                string name = Path.GetFileName(f);
                bool distinct = seen.Add(h);
                sb.Append(name).Append('=').Append(h.Substring(0, 12)).Append(distinct ? " " : " DUP! ");
            }

            Debug.Log("[FeelCaptureCw3] sha256 " + sb);
        }

        static string Sha256File(string path)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(path));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        static Texture2D LoadRgb(string path)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }

        static void BlitFit(Texture2D src, Texture2D dst, int ox, int oy, int dw, int dh)
        {
            for (int y = 0; y < dh; y++)
            for (int x = 0; x < dw; x++)
            {
                float u = x / (float)Mathf.Max(1, dw - 1);
                float v = y / (float)Mathf.Max(1, dh - 1);
                dst.SetPixel(ox + x, oy + y, src.GetPixelBilinear(u, v));
            }
        }

        static void DrawBar(Texture2D sheet, int ox, int oy, int w, int h, string label)
        {
            var bar = new Color(0.2f, 0.2f, 0.24f);
            for (int y = oy; y < oy + h; y++)
            for (int x = ox; x < ox + w; x++)
                sheet.SetPixel(x, y, bar);
            int cx = ox + 6;
            foreach (char ch in label)
            {
                Stamp(sheet, ch, cx, oy + 8);
                cx += 8;
            }
        }

        static void Stamp(Texture2D tex, char ch, int x, int y)
        {
            if (ch == ' ')
                return;
            for (int dy = 0; dy < 5; dy++)
            for (int dx = 0; dx < 5; dx++)
                tex.SetPixel(x + dx, y + dy, Color.white);
        }

        static bool CameraInsideAnyBlocker(Vector3 camPos)
        {
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c != null && c.name == "CameraBlocker" && c.bounds.Contains(camPos))
                    return true;
            }

            return false;
        }

        static bool TryBounds(Transform root, out Bounds b)
        {
            b = default;
            var rends = root.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
                return false;
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            return true;
        }

        static Rect ProjectRect(Bounds b, Camera cam)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;
            float xMin = 1f;
            float xMax = 0f;
            float yMin = 1f;
            float yMax = 0f;
            foreach (Vector3 corner in new[]
                     {
                         c + new Vector3(e.x, e.y, e.z), c + new Vector3(-e.x, e.y, e.z),
                         c + new Vector3(e.x, -e.y, e.z), c + new Vector3(-e.x, -e.y, e.z),
                     })
            {
                Vector3 vp = cam.WorldToViewportPoint(corner);
                if (vp.z <= 0f)
                    continue;
                xMin = Mathf.Min(xMin, vp.x);
                xMax = Mathf.Max(xMax, vp.x);
                yMin = Mathf.Min(yMin, vp.y);
                yMax = Mathf.Max(yMax, vp.y);
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
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

        static void ScheduleFrames(int frames, System.Action onDone)
        {
            int left = Mathf.Max(1, frames);
            void Tick()
            {
                left--;
                if (left > 0)
                    return;
                EditorApplication.update -= Tick;
                onDone();
            }

            EditorApplication.update += Tick;
        }
    }
}
#endif
