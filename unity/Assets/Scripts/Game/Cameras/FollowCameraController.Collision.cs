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
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Cameras
{
    public sealed partial class FollowCameraController : MonoBehaviour
    {
        void UpdateWindupPullback(float dt)
        {
            float target = 0f;
            if (_bossDirector != null && _bossDirector.WindupProgress01 > 0f
                && _bossDirector.CurrentAttackKind.HasValue
                && IsBigWindupTelegraph(_bossDirector.CurrentAttackKind.Value))
            {
                target = 1f;
            }

            _windupPullback = Mathf.SmoothDamp(
                _windupPullback,
                target,
                ref _windupVelocity,
                Mathf.Max(FollowCameraControllerDefaults.MinPositiveSmoothSec, _tuning.Camera.CameraWindupSmoothSec),
                Mathf.Infinity,
                dt);
        }

        void ResolveYaw(float dt)
        {
            float desired = OrbitYawDeg;
            if (_bossTarget != null)
            {
                Vector3 toBoss = _bossTarget.position - _target.position;
                toBoss.y = 0f;
                if (LockOnActive && toBoss.sqrMagnitude > 0.001f)
                {
                    // Lock-on: menzilden bağımsız tam yaw kenetleme (konum da boss'a döner).
                    desired = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;
                }
                else
                {
                    float range = Mathf.Max(FollowCameraControllerDefaults.MinPositiveSmoothSec, _tuning.Camera.CameraSoftLockRangeM);
                    if (toBoss.sqrMagnitude <= range * range && toBoss.sqrMagnitude > 0.001f)
                    {
                        float bossYaw = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;
                        desired = Mathf.LerpAngle(
                            OrbitYawDeg,
                            bossYaw,
                            Mathf.Clamp01(_tuning.Camera.CameraSoftLockStrength));
                    }
                }
            }

            _resolvedYawDeg = Mathf.SmoothDampAngle(
                _resolvedYawDeg,
                desired,
                ref _yawVelocity,
                Mathf.Max(FollowCameraControllerDefaults.MinPositiveSmoothSec, _tuning.Camera.FollowSmoothTimeSec),
                Mathf.Infinity,
                dt);
        }

        float BossFramingWeight()
        {
            if (_bossTarget == null)
                return 0f;
            Vector3 toBoss = _bossTarget.position - _target.position;
            toBoss.y = 0f;
            float range = Mathf.Max(FollowCameraControllerDefaults.MinPositiveSmoothSec, _tuning.Camera.CameraSoftLockRangeM);
            float distanceWeight = 1f - Mathf.SmoothStep(FollowCameraControllerDefaults.SoftLockDistanceWeightStart, 1f, toBoss.magnitude / range);
            return Mathf.Clamp01(distanceWeight);
        }

        Vector3 ApplyCameraCollision(Vector3 pivot, Vector3 desiredWorld, float dt)
        {
            Vector3 delta = desiredWorld - pivot;
            float targetAlong = delta.magnitude;
            if (targetAlong < FollowCameraControllerDefaults.MinTargetAlongM)
            {
                _collisionPulledInM = 0f;
                return desiredWorld;
            }

            Vector3 dir = delta / targetAlong;
            float blockedAlong = targetAlong;
            float radius = Mathf.Max(FollowCameraControllerDefaults.MinCollisionSphereRadiusM, _tuning.Camera.CameraCollisionSphereRadiusM);
            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                radius,
                dir,
                CollisionHits,
                targetAlong,
                _collisionLayerMask,
                QueryTriggerInteraction.Ignore);
            float best = targetAlong;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = CollisionHits[i];
                if (!IsBlockingCollider(h.collider))
                    continue;
                float along = h.distance - _tuning.Camera.CameraCollisionMarginM;
                if (along < best)
                    best = along;
            }

            blockedAlong = Mathf.Max(_tuning.Camera.CameraCollisionMinDistanceM, best);
            bool pullingIn = blockedAlong < _smoothedAlongDistM - 0.001f;
            float smooth = pullingIn
                ? _tuning.Camera.CameraCollisionPullInSmoothSec
                : _tuning.Camera.CameraCollisionPullOutSmoothSec;
            if (_smoothedAlongDistM <= FollowCameraControllerDefaults.MinSmoothedAlongDistM)
                _smoothedAlongDistM = targetAlong;
            _smoothedAlongDistM = Mathf.SmoothDamp(
                _smoothedAlongDistM,
                Mathf.Min(targetAlong, blockedAlong),
                ref _alongDistVelocity,
                Mathf.Max(0.001f, smooth),
                Mathf.Infinity,
                dt);
            _collisionPulledInM = Mathf.Max(0f, targetAlong - _smoothedAlongDistM);
            return pivot + dir * _smoothedAlongDistM;
        }
    }
}
