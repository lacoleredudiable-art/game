using System.Collections.Generic;
using Dovus.Game.Config.Sections;
using Dovus.Game.Diagnostics;
using UnityEngine;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Takip kamerasının çarpışması: bakış noktasından istenen konuma küre taraması (O-5.10) ve boss gövdesi
    /// (O-5.11, O-5.15). Karakter/VFX kökleri ve tetikler engel sayılmaz; boss gövdesi silindir olarak ayrıca sayılır
    /// ve kamerayı öne çekmez, gövdenin arkasına iter.
    /// </summary>
    public sealed class CameraCollisionSolver
    {
        static readonly RaycastHit[] Hits = new RaycastHit[8];

        Transform[] _ignoreRoots;
        int _layerMask;
        bool _filterLogged;
        float _smoothedAlongM;
        float _alongVelocity;
        CameraBossBody _bossBody;
        CameraBossBody[] _columns = new CameraBossBody[0];

        /// <summary>Son karede pivot→istenen mesafe ekseninde çekiş (m).</summary>
        public float PulledInM { get; private set; }

        public CameraBossBody BossBody => _bossBody;

        public void BindBossBody(CameraBossBody body) => _bossBody = body;

        /// <summary>
        /// O-5.10 [K 2026-10-15]: sütunlar kamerayı öne çekmez (taramadan çıkar, saydamlaşır); kamera sütunun içine
        /// de giremez (<see cref="KeepOutOfColumns"/>). <see cref="BindFiltering"/>'den sonra çağrılır.
        /// </summary>
        public void BindColumns(CameraBossBody[] columns)
        {
            _columns = columns ?? new CameraBossBody[0];
            var roots = new List<Transform>(_ignoreRoots ?? new Transform[0]);
            foreach (CameraBossBody c in _columns)
            {
                if (c?.Root != null)
                    roots.Add(c.Root);
            }
            _ignoreRoots = roots.ToArray();
        }

        /// <summary>Karakter/VFX kökleri spherecast'ten çıkar; layer mask burada kurulur.</summary>
        public void BindFiltering(Transform playerRoot, Transform bossRoot, Transform allyRoot)
        {
            var roots = new List<Transform>(3);
            if (playerRoot != null)
                roots.Add(playerRoot);
            if (bossRoot != null)
                roots.Add(bossRoot);
            if (allyRoot != null)
                roots.Add(allyRoot);
            _ignoreRoots = roots.ToArray();

            int blocker = LayerMask.NameToLayer("CameraBlocker");
            _layerMask = blocker >= 0
                ? LayerMask.GetMask("Default", "CameraBlocker")
                : LayerMask.GetMask("Default");
            if (!_filterLogged)
            {
                DebugConfig.DevLog(
                    $"[FollowCamera] collision mask layers={(blocker >= 0 ? "Default+CameraBlocker" : "Default only")} "
                    + $"ignoreRoots={_ignoreRoots.Length} (player/boss/ally, triggers skipped)");
                _filterLogged = true;
            }
        }

        public Vector3 Resolve(Vector3 pivot, Vector3 desiredWorld, float dt, CameraSettings cam)
        {
            Vector3 delta = desiredWorld - pivot;
            float targetAlong = delta.magnitude;
            if (targetAlong < FollowCameraControllerDefaults.MinTargetAlongM)
            {
                PulledInM = 0f;
                return desiredWorld;
            }

            Vector3 dir = delta / targetAlong;
            float radius = SphereRadiusM(cam);
            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                radius,
                dir,
                Hits,
                targetAlong,
                _layerMask,
                QueryTriggerInteraction.Ignore);
            float best = targetAlong;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = Hits[i];
                if (!IsBlocking(h.collider))
                    continue;
                float along = h.distance - cam.CameraCollisionMarginM;
                if (along < best)
                    best = along;
            }

            // O-5.11: boss gövdesi kamerayı öne çekmez (boss saydamlaşır); gövdeye girmeyi KeepOutOfBossBody önler.
            float blockedAlong = Mathf.Max(cam.CameraCollisionMinDistanceM, best);
            bool pullingIn = blockedAlong < _smoothedAlongM - 0.001f;
            float smooth = pullingIn
                ? cam.CameraCollisionPullInSmoothSec
                : cam.CameraCollisionPullOutSmoothSec;
            if (_smoothedAlongM <= FollowCameraControllerDefaults.MinSmoothedAlongDistM)
                _smoothedAlongM = targetAlong;
            _smoothedAlongM = Mathf.SmoothDamp(
                _smoothedAlongM,
                Mathf.Min(targetAlong, blockedAlong),
                ref _alongVelocity,
                Mathf.Max(0.001f, smooth),
                Mathf.Infinity,
                dt);
            PulledInM = Mathf.Max(0f, targetAlong - _smoothedAlongM);
            return pivot + dir * _smoothedAlongM;
        }

        /// <summary>
        /// O-5.15 sert sınır: kamera boss gövdesinin (yarıçap + kamera küresi) içindeyse bakış noktasından kameraya
        /// giden ışında gövdenin çıkışının arkasına alınır (O-5.11 öne gelmez, Y3). Yumuşatma bunu aşamaz.
        /// </summary>
        public Vector3 KeepOutOfBossBody(Vector3 pivot, Vector3 camPos, CameraSettings cam)
        {
            if (_bossBody == null || !_bossBody.TryKeepOut(pivot, camPos, SphereRadiusM(cam), cam.CameraCollisionMarginM, out Vector3 safe))
                return camPos;
            _smoothedAlongM = Mathf.Min(_smoothedAlongM, (safe - pivot).magnitude);
            return safe;
        }

        /// <summary>O-5.10 sert sınır: boss gövdesindeki gibi kamera sütunun içindeyse çıkışının arkasına alınır (öne gelmez).</summary>
        public Vector3 KeepOutOfColumns(Vector3 pivot, Vector3 camPos, CameraSettings cam)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (_columns[i] == null
                    || !_columns[i].TryKeepOut(pivot, camPos, SphereRadiusM(cam), cam.CameraCollisionMarginM, out Vector3 safe))
                    continue;
                _smoothedAlongM = Mathf.Min(_smoothedAlongM, (safe - pivot).magnitude);
                camPos = safe;
            }
            return camPos;
        }

        static float SphereRadiusM(CameraSettings cam) =>
            Mathf.Max(FollowCameraControllerDefaults.MinCollisionSphereRadiusM, cam.CameraCollisionSphereRadiusM);

        bool IsBlocking(Collider col)
        {
            if (col == null || col.isTrigger)
                return false;
            if (_ignoreRoots != null)
            {
                Transform t = col.transform;
                for (int r = 0; r < _ignoreRoots.Length; r++)
                {
                    Transform root = _ignoreRoots[r];
                    if (root != null && t.IsChildOf(root))
                        return false;
                }
            }

            return true;
        }
    }
}
