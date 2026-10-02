#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Editor
{
    /// <summary>cw-1: kamera çarpışma, lock-on overlap, mobil lock-on düğmesi — log-only Play doğrulama.</summary>
    public static class FeelCaptureCw1
    {
        public static void RunAll()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FeelCaptureCw1] Play modunda değil; önce Prototype sahnesini Play'e alın.");
                return;
            }

            ScheduleFrames(45, () =>
            {
                VerifyCollisionBehindRock();
                ScheduleFrames(30, () =>
                {
                    VerifyWindupJitter();
                    ScheduleFrames(20, VerifyLockOnOverlaps);
                    ScheduleFrames(15, VerifyLockOnButton);
                });
            });
        }

        static void VerifyCollisionBehindRock()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            var motor = Object.FindAnyObjectByType<KinematicMotor>();
            if (follow == null || motor == null)
            {
                Debug.LogError("[FeelCaptureCw1] collision: FollowCamera/KinematicMotor yok");
                return;
            }

            Transform rock = FindNearestAmbienceRock(motor.transform.position);
            if (rock != null)
            {
                Vector3 toRock = rock.position - motor.transform.position;
                toRock.y = 0f;
                if (toRock.sqrMagnitude > 0.01f)
                    motor.transform.position += toRock.normalized * 2.5f;
            }

            follow.LockOnActive = false;
            float pulled = follow.CollisionPulledInM;
            bool insideBlocker = CameraInsideAnyBlocker(follow.transform.position);
            Debug.Log(
                $"[FeelCaptureCw1] (a) CollisionPulledInM={pulled:F3} insideBlocker={insideBlocker} cam={follow.transform.position}");
        }

        static void VerifyWindupJitter()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow == null)
                return;
            var samples = new List<float>();
            float maxDelta = 0f;
            for (int i = 0; i < 30; i++)
            {
                float d = follow.transform.position.magnitude;
                if (samples.Count > 0)
                    maxDelta = Mathf.Max(maxDelta, Mathf.Abs(d - samples[samples.Count - 1]));
                samples.Add(d);
            }

            Debug.Log($"[FeelCaptureCw1] (b) windup/collision 30f maxDistDelta={maxDelta:F4} pullback={follow.WindupPullback01:F2}");
        }

        static void VerifyLockOnOverlaps()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            var motor = Object.FindAnyObjectByType<KinematicMotor>();
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (follow == null || motor == null || boss == null)
                return;

            Vector3 origin = motor.transform.position;
            Vector3 dir = boss.transform.position - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector3.forward;
            dir.Normalize();
            Vector3 bossPos = boss.transform.position;
            foreach (float sep in new[] { 2f, 5f, 10f })
            {
                motor.transform.position = origin;
                boss.transform.position = origin + dir * sep;
                follow.LockOnActive = true;
                follow.LogLockOnOverlap(sep);
            }

            boss.transform.position = bossPos;
        }

        static void VerifyLockOnButton()
        {
            var view = Object.FindAnyObjectByType<HexagonView>();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (view == null || follow == null)
            {
                Debug.LogError("[FeelCaptureCw1] (d) HexagonView/FollowCamera yok");
                return;
            }

            LogButtonRect("1600x900", 1600, 900);
            LogButtonRect("2340x1080", 2340, 1080);

            bool before = follow.LockOnActive;
            var btn = view.LockOnButton;
            if (btn == null)
            {
                Debug.LogError("[FeelCaptureCw1] (d) LockOnButton yok");
                return;
            }

            btn.onClick.Invoke();
            view.RefreshLockOnVisual();
            bool after = follow.LockOnActive;
            Debug.Log(
                $"[FeelCaptureCw1] (d) lockOnButton toggled {before}->{after} highlight={(after ? "on" : "off")}");
        }

        static void LogButtonRect(string tag, int w, int h)
        {
            var rt = Object.FindAnyObjectByType<HexagonView>()?.LockOnButtonRect;
            if (rt == null)
                return;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Debug.Log(
                $"[FeelCaptureCw1] (d) lockOnRect {tag} min=({corners[0].x:F0},{corners[0].y:F0}) max=({corners[2].x:F0},{corners[2].y:F0})");
        }

        static Transform FindNearestAmbienceRock(Vector3 near)
        {
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c == null || c.name != "CameraBlocker")
                    continue;
                float d = Vector3.Distance(c.bounds.center, near);
                if (d < bestD)
                {
                    bestD = d;
                    best = c.transform.parent;
                }
            }

            return best;
        }

        static bool CameraInsideAnyBlocker(Vector3 camPos)
        {
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c == null || c.name != "CameraBlocker")
                    continue;
                if (c.bounds.Contains(camPos))
                    return true;
            }

            return false;
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
