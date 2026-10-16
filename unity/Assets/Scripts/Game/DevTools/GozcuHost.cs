using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// Gözcü (bariz hata dedektörü), yalnız editör / DOVUS_DEBUG: her kare oyuncu, kamera ve efektleri
    /// yoklar; ihlalde sol üstte kırmızı satır + <c>LogError("[GOZCU] &lt;kural&gt; &lt;değer&gt; kare=&lt;n&gt;")</c>.
    /// Aynı kural saniyede en çok bir kez yazılır. Oyun durumuna yazmaz; eşikler kural-motoru-v4.json "gozcu".
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class GozcuHost : MonoBehaviour
    {
        const int MaxShownLines = 8;
        const int OverlapCapacity = 32;
        const float LineHeightScreenFrac = 0.025f;
        const float LineMarginPx = 8f;

        readonly HumanBodyBones[] _footBones =
            { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes };
        readonly Collider[] _overlap = new Collider[OverlapCapacity];
        readonly List<AnimatorClipInfo> _clipInfos = new();
        readonly GozcuScreenCover _cover = new();
        readonly List<(string line, float until)> _shown = new();

        RuleEngineV4Gozcu _rules;
        GozcuReporter _reporter;
        Transform _player;
        Camera _camera;
        System.Func<int> _equippedWeaponNumber;
        KinematicMotorController _motor;
        Animator _animator;
        CapsuleCollider _probe;
        HashSet<int> _startupRenderers;
        Transform _bossBody;
        float _bossBodyRadiusM;
        float _bossBodyTopAboveRootM;
        Quaternion _lastCameraRotation;
        bool _hasCameraRotation;
        bool _noCameraWarned;
        int _fallbackLogs;
        GUIStyle _style;

        public int ReportedCount => _reporter?.Reported ?? 0;

        public void Configure(RuleEngineV4Gozcu rules, Transform player, Camera camera, System.Func<int> equippedWeaponNumber)
        {
            _rules = rules;
            _reporter = new GozcuReporter(rules.LogIntervalSec);
            _player = player;
            _camera = camera;
            _equippedWeaponNumber = equippedWeaponNumber;
            _motor = player != null ? player.GetComponent<KinematicMotorController>() : null;

            // Gövde yoklaması: oyuncunun collider'ı yok. Yalnız ComputePenetration anında açılır,
            // böylece oyunun hiçbir sorgusu onu görmez.
            var probeGo = new GameObject("GozcuBodyProbe");
            probeGo.transform.SetParent(transform, false);
            _probe = probeGo.AddComponent<CapsuleCollider>();
            _probe.direction = 1;
            _probe.enabled = false;
        }

        /// <summary>O-5.15: boss gövdesi, kökün altından tepeye dikey silindir (yarıçap world_physics, tepe görsel boy).</summary>
        public void WatchBossBody(Transform root, float radiusM, float topAboveRootM)
        {
            _bossBody = root;
            _bossBodyRadiusM = radiusM;
            _bossBodyTopAboveRootM = topAboveRootM;
        }

        void OnEnable() => Application.logMessageReceived += OnLog;

        void OnDisable() => Application.logMessageReceived -= OnLog;

        void OnLog(string message, string stack, LogType type)
        {
            if (_rules != null && message != null && message.Contains(_rules.FallbackLogTag))
                _fallbackLogs++;
        }

        void LateUpdate()
        {
            if (_rules == null || _player == null)
                return;
            if (_startupRenderers == null)
            {
                // Açılış sahnesi efekt sayılmaz; ilk kare anlık görüntüsü.
                _startupRenderers = new HashSet<int>();
                foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    _startupRenderers.Add(r.GetInstanceID());
            }
            CheckFeet();
            CheckBody();
            CheckClip();
            if (_fallbackLogs > _rules.FallbackLogMax)
                Report(GozcuRule.Fallback, _fallbackLogs.ToString(CultureInfo.InvariantCulture));
            Camera cam = GameCamera();
            if (cam == null)
            {
                WarnNoCameraOnce();
                return;
            }
            CheckCameraSpeed(cam);
            CheckCameraInCollider(cam);
            CheckCameraInBossBody(cam);
            // Oyunun ana kamerası FOV'u eşiğin altına düşmemeli; düşerse ihlal, izdüşüm anlamsız olduğundan
            // ekran ölçümleri (oyuncu ekranda mı, efekt kaplaması) o kare yapılmaz.
            if (cam.fieldOfView < _rules.CameraFovMinDeg)
            {
                Report(GozcuRule.CameraFovInvalid,
                    "fov" + cam.fieldOfView.ToString("0.0", CultureInfo.InvariantCulture));
                return;
            }
            CheckPlayerOnScreen(cam);
            CheckEffectCover(cam);
        }

        /// <summary>Yalnız oyunun ana kamerası (MainCamera etiketli takip kamerası).</summary>
        Camera GameCamera()
        {
            Camera cam = Camera.main != null ? Camera.main : _camera;
            return cam != null && cam.isActiveAndEnabled ? cam : null;
        }

        void WarnNoCameraOnce()
        {
            if (_noCameraWarned)
                return;
            _noCameraWarned = true;
            Debug.LogWarning("Gözcü: ana kamera yok, kamera ve ekran ölçümleri atlandı. kare="
                + Time.frameCount.ToString(CultureInfo.InvariantCulture));
        }

        void CheckFeet()
        {
            Animator anim = PlayerAnimator();
            if (anim == null || !anim.isHuman)
                return;
            float lowest = float.MaxValue;
            for (int i = 0; i < _footBones.Length; i++)
            {
                Transform bone = anim.GetBoneTransform(_footBones[i]);
                if (bone != null)
                    lowest = Mathf.Min(lowest, bone.position.y);
            }
            if (lowest == float.MaxValue)
                return;
            // Zemin yalnız arena zemini: sabit yapı, duvar ve koza üstü zemin sayılmaz.
            float floorY = RuleEngineV4GozcuDefaults.ArenaFloorYM;
            float depth = floorY - lowest;
            if (depth > _rules.FootBelowGroundMaxM)
                Report(GozcuRule.FootBelowGround, Cm(depth) + ",kok" + Cm(_player.position.y - floorY));
        }

        /// <summary>Hareket motorunun gövde kapsülüyle aynı biçim (KinematicMotorController).</summary>
        void CheckBody()
        {
            if (_motor == null)
                return;
            _motor.BodyCapsule(_player.position, out Vector3 p1, out Vector3 p2, out float r);
            int n = Physics.OverlapCapsuleNonAlloc(p1, p2, r, _overlap, ~0, QueryTriggerInteraction.Ignore);
            if (n == 0)
                return;

            Vector3 center = (p1 + p2) * 0.5f;
            _probe.radius = r;
            _probe.height = (p2.y - p1.y) + 2f * r;
            float worst = 0f;
            Collider worstCol = null;
            _probe.enabled = true;
            for (int i = 0; i < n; i++)
            {
                Collider col = _overlap[i];
                // Statik: fizikle sürülmeyen (gövdesiz ya da kinematik) katı collider.
                if (col == null || col == _probe || col.transform.IsChildOf(_player)
                    || (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic))
                    continue;
                if (Physics.ComputePenetration(_probe, center, Quaternion.identity,
                        col, col.transform.position, col.transform.rotation, out _, out float depth) && depth > worst)
                {
                    worst = depth;
                    worstCol = col;
                }
            }
            _probe.enabled = false;
            if (worst > _rules.BodyPenetrationMaxM)
                Report(GozcuRule.BodyInCollider, Cm(worst) + "@" + worstCol.name);
        }

        void CheckClip()
        {
            Animator anim = PlayerAnimator();
            if (anim == null || anim.runtimeAnimatorController == null || _equippedWeaponNumber == null)
                return;
            int weapon = _equippedWeaponNumber();
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                float layerWeight = layer == 0 ? 1f : anim.GetLayerWeight(layer);
                if (layerWeight <= 0f)
                    continue;
                anim.GetCurrentAnimatorClipInfo(layer, _clipInfos);
                foreach (AnimatorClipInfo ci in _clipInfos)
                {
                    if (ci.clip == null || ci.weight * layerWeight <= _rules.ClipWeightMin)
                        continue;
                    if (!_rules.IsClipAllowed(ci.clip.name, weapon))
                        Report(GozcuRule.WrongClip, ci.clip.name + "@w" + weapon.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        void CheckCameraSpeed(Camera cam)
        {
            Quaternion rot = cam.transform.rotation;
            float dt = Time.unscaledDeltaTime;
            if (_hasCameraRotation && dt > 0f)
            {
                float dps = Quaternion.Angle(_lastCameraRotation, rot) / dt;
                if (dps > _rules.CameraAngularSpeedMaxDps)
                    Report(GozcuRule.CameraAngularSpeed, dps.ToString("0", CultureInfo.InvariantCulture) + "dps");
            }
            _lastCameraRotation = rot;
            _hasCameraRotation = true;
        }

        void CheckCameraInCollider(Camera cam)
        {
            int n = Physics.OverlapSphereNonAlloc(cam.transform.position, _rules.CameraProbeRadiusM, _overlap,
                ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider col = _overlap[i];
                if (col == null || col == _probe || col.transform.IsChildOf(_player))
                    continue;
                Report(GozcuRule.CameraInCollider, col.name);
                return;
            }
        }

        /// <summary>Boss gövdesi tetik çarpıştırıcı; collider yoklaması onu görmez. Değer: kenardan içeri derinlik.</summary>
        void CheckCameraInBossBody(Camera cam)
        {
            if (_bossBody == null)
                return;
            Vector3 c = _bossBody.position;
            Vector3 p = cam.transform.position;
            float depth = _bossBodyRadiusM - new Vector2(p.x - c.x, p.z - c.z).magnitude;
            if (depth > 0f && p.y < c.y + _bossBodyTopAboveRootM)
                Report(GozcuRule.CameraInBossBody, Cm(depth));
        }

        void CheckPlayerOnScreen(Camera cam)
        {
            Vector3 body = _player.position + Vector3.up * (KinematicMotorControllerDefaults.CapsuleTopLiftM * 0.5f);
            Vector3 vp = cam.WorldToViewportPoint(body);
            if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
                return;
            Report(GozcuRule.PlayerOffScreen, string.Format(CultureInfo.InvariantCulture,
                "vp={0:0.00},{1:0.00},z{2:0.0},fov{3:0.0},asp{4:0.00}", vp.x, vp.y, vp.z, cam.fieldOfView, cam.aspect));
        }

        /// <summary>
        /// Efekt: açılışta olmayan, ana kameranın gördüğü parçacık / iz / çizgi ya da saydam kuyruktaki renderer.
        /// Kaplama <see cref="GozcuScreenCover"/>.
        /// </summary>
        void CheckEffectCover(Camera cam)
        {
            Renderer[] all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            float worst = 0f;
            Renderer worstRenderer = null;
            foreach (Renderer r in all)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || !r.isVisible
                    || (cam.cullingMask & (1 << r.gameObject.layer)) == 0
                    || _startupRenderers.Contains(r.GetInstanceID()) || !IsEffect(r))
                    continue;
                float cover = _cover.Measure(cam, r);
                if (cover > worst)
                {
                    worst = cover;
                    worstRenderer = r;
                }
            }
            if (worst > _rules.EffectScreenCoverMaxFrac)
                Report(GozcuRule.EffectScreenCover,
                    (worst * 100f).ToString("0", CultureInfo.InvariantCulture) + "%@" + worstRenderer.name);
        }

        static bool IsEffect(Renderer r)
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                return true;
            Material m = r.sharedMaterial;
            return m != null && m.renderQueue >= (int)RenderQueue.Transparent;
        }

        Animator PlayerAnimator()
        {
            if (_animator == null && _player != null)
                _animator = _player.GetComponentInChildren<Animator>();
            return _animator;
        }

        void Report(string rule, string value)
        {
            if (!_reporter.TryReport(rule, value, Time.frameCount, Time.realtimeSinceStartupAsDouble, out string line))
                return;
            Debug.LogError(line);
            _shown.Add((line, Time.realtimeSinceStartup + _rules.LineShowSec));
            if (_shown.Count > MaxShownLines)
                _shown.RemoveAt(0);
        }

        static string Cm(float m) => (m * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "cm";

        void OnGUI()
        {
            float now = Time.realtimeSinceStartup;
            _shown.RemoveAll(s => s.until < now);
            if (_shown.Count == 0)
                return;
            float lineH = Mathf.Max(1f, Screen.height * LineHeightScreenFrac);
            _style ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
            _style.fontSize = Mathf.RoundToInt(lineH * 0.8f);
            for (int i = 0; i < _shown.Count; i++)
                GUI.Label(new Rect(LineMarginPx, LineMarginPx + i * lineH, Screen.width, lineH), _shown[i].line, _style);
        }
    }
}
