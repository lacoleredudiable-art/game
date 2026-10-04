using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Cameras
{
    public sealed partial class FollowCameraController
    {
        void UpdateLockOnScreenOverlap()
        {
            if (_cam == null || _bossTarget == null)
                return;
            Rect player = ProjectActorRect(_target, FollowCameraControllerDefaults.ActorRectHeightM, FollowCameraControllerDefaults.ActorRectHalfWidthM);
            Rect boss = ProjectActorRect(_bossTarget, _tuning.Camera.CameraBossAimHeightM, FollowCameraControllerDefaults.BossRectHalfWidthM);
            float playerArea = player.width * player.height;
            if (playerArea < 1e-5f)
            {
                _lastLockOnOverlapPct = 1f;
                return;
            }

            float overlap = RectIntersectionArea(player, boss);
            _lastLockOnOverlapPct = Mathf.Clamp01(1f - overlap / playerArea);
        }

        Rect ProjectActorRect(Transform actor, float centerUpM, float halfHeightM)
        {
            Vector3 c = actor.position + Vector3.up * centerUpM;
            Vector3 top = c + Vector3.up * halfHeightM;
            Vector3 bottom = c - Vector3.up * halfHeightM;
            Vector3 left = c - transform.right * FollowCameraControllerDefaults.DebugOffsetM;
            Vector3 right = c + transform.right * FollowCameraControllerDefaults.DebugOffsetM;
            Vector3[] pts =
            {
                _cam.WorldToViewportPoint(top),
                _cam.WorldToViewportPoint(bottom),
                _cam.WorldToViewportPoint(left),
                _cam.WorldToViewportPoint(right)
            };
            float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].z <= 0f)
                    continue;
                minX = Mathf.Min(minX, pts[i].x);
                maxX = Mathf.Max(maxX, pts[i].x);
                minY = Mathf.Min(minY, pts[i].y);
                maxY = Mathf.Max(maxY, pts[i].y);
            }

            if (maxX < minX)
                return Rect.zero;
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        static float RectIntersectionArea(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float yMax = Mathf.Min(a.yMax, b.yMax);
            if (xMax <= xMin || yMax <= yMin)
                return 0f;
            return (xMax - xMin) * (yMax - yMin);
        }

        void AdvancePunch()
        {
            if (_punchT <= 0f)
            {
                _punchT = 0f;
                return;
            }

            _punchT = Mathf.Max(0f, _punchT - Time.unscaledDeltaTime * _punchDecay);
        }
    }
}
