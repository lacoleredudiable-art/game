using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UI;

namespace Dovus.Game.Actors
{
    /// <summary>Bir dünya aktörünü oyuncu başına seçilebilir hedef yapar.</summary>
    public sealed class Targetable : MonoBehaviour
    {
        int _teamId;
        string _displayName = string.Empty;
        Func<bool> _available;
        Collider _collider;

        public int TeamId => _teamId;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public bool IsAvailable => _available == null || _available();

        // O11: sahne taraması yerine etkin hedef kaydı (hedefleme, sekme, top sıçraması).
        static readonly List<Targetable> s_live = new List<Targetable>();
        public static IReadOnlyList<Targetable> Live => s_live;

        void OnEnable()
        {
            if (!s_live.Contains(this))
                s_live.Add(this);
        }

        void OnDisable() => s_live.Remove(this);

        public void Configure(int teamId, string displayName, Func<bool> available = null)
        {
            _teamId = teamId;
            _displayName = displayName ?? string.Empty;
            _available = available;
            _collider = GetComponent<Collider>();
        }

        public float DistanceFrom(Vector3 origin)
        {
            _collider ??= GetComponent<Collider>();
            // ClosestPoint tetikleyici collider'da güvenilir değil (Unity noktayı geri verir,
            // mesafe 0 olur ve her düşman menzilde sanılır). Bounds tetikten etkilenmez.
            Vector3 point = _collider != null
                ? _collider.bounds.ClosestPoint(origin)
                : transform.position;
            point.y = origin.y;
            return Vector3.Distance(origin, point);
        }

        public float MarkerRadius(float padding)
        {
            _collider ??= GetComponent<Collider>();
            if (_collider == null)
                return Mathf.Max(0.1f, padding);
            Bounds b = _collider.bounds;
            return Mathf.Max(b.extents.x, b.extents.z) + padding;
        }

        public float GroundY
        {
            get
            {
                _collider ??= GetComponent<Collider>();
                return _collider != null ? _collider.bounds.min.y : transform.position.y;
            }
        }
    }

    /// <summary>Hedef işaretinin ayarlanabilir sunum verisi; sahne runtime kurulduğu için bileşende yaşar.</summary>
    [Serializable]
    public sealed class TargetingPresentationTuning
    {
        public float RingPaddingM = PlayerTargetingDefaults.RingPaddingM;
        public float RingWidthM = PlayerTargetingDefaults.RingWidthM;
        public float RingGroundOffsetM = PlayerTargetingDefaults.RingGroundOffsetM;
        public int RingSegments = PlayerTargetingDefaults.RingSegments;
        public Color EnemyColor = new(1f, 0.28f, 0.16f, 0.95f);
        public Color AllyColor = new(0.25f, 1f, 0.58f, 0.95f);
        public Vector2 FrameSizePx = new(PlayerTargetingDefaults.FrameWidthPx, PlayerTargetingDefaults.FrameHeightPx);
        public Vector2 FrameOffsetPx = new(0f, -PlayerTargetingDefaults.FrameOffsetYPx);
        public int FrameFontPx = PlayerTargetingDefaults.FrameFontPx;
    }

    /// <summary>
    /// Oyuncuya özel seçim + otomatik hedef çözümü. Static hedef durumu tutmaz; gelecekte her
    /// co-op oyuncusu kendi örneğini ve takım kimliğini taşıyabilir.
    /// </summary>
    public sealed class PlayerTargeting : MonoBehaviour
    {
        [SerializeField] TargetingPresentationTuning _presentation = new();

        readonly List<TargetCandidate> _candidateData = new();
        readonly Dictionary<int, Targetable> _candidateMap = new();

        Transform _owner;
        int _ownerTeamId;
        Camera _camera;
        FollowCamera _follow;
        HexagonInput _hexagon;
        float _tapMaxMoveDp;
        int? _fingerId;
        Vector2 _fingerStart;
        bool _fingerBlocked;
        bool _mouseHeld;
        Vector2 _mouseStart;
        bool _mouseBlocked;
        Targetable _selected;
        LineRenderer _ring;
        GameObject _ringObject;
        RectTransform _frame;
        Text _frameText;
        Image _frameBackground;
        ElementRadialMenu _elementMenu;

        public void BindElementMenu(ElementRadialMenu menu) => _elementMenu = menu;

        public Targetable Selected => _selected;
        public Transform SelectedTransform => _selected != null && _selected.IsAvailable
            ? _selected.transform
            : null;

        public void Bind(
            Transform owner,
            int ownerTeamId,
            Camera camera,
            HexagonInput hexagon,
            float tapMaxMoveDp,
            Transform canvasRoot,
            FollowCamera follow = null)
        {
            _owner = owner;
            _ownerTeamId = ownerTeamId;
            _camera = camera;
            _follow = follow;
            _hexagon = hexagon;
            _tapMaxMoveDp = Mathf.Max(0f, tapMaxMoveDp);
            BuildMarker();
            BuildFrame(canvasRoot);
            RefreshPresentation();
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
            _fingerId = null;
            _mouseHeld = false;
        }

        void Update()
        {
            if (_selected != null && !_selected.IsAvailable)
                Select(null);
            HandleMouse();
        }

        void LateUpdate()
        {
            if (_selected == null || _ring == null)
                return;
            DrawRing(_selected);
        }

        public bool TryResolve(
            in SkillResolution skill,
            float rangeM,
            out Transform target,
            out TargetFailure failure)
        {
            SkillAimMode aimMode = TargetingRules.AimMode(skill);
            BuildCandidates();
            int? selectedId = _selected != null ? _selected.GetInstanceID() : null;
            TargetResolution result = TargetingRules.Resolve(
                skill.TargetMode, aimMode, rangeM, selectedId, _candidateData, skill.Action);
            failure = result.Failure;
            if (!result.Allowed)
            {
                target = result.Failure == TargetFailure.OutOfRange
                    && _candidateMap.TryGetValue(result.TargetId, out Targetable far)
                    ? far.transform
                    : null;
                return false;
            }

            if (result.UseSelf)
            {
                target = _owner;
                return true;
            }

            target = _candidateMap.TryGetValue(result.TargetId, out Targetable found)
                ? found.transform
                : null;
            if (target != null)
                return true;
            failure = TargetFailure.NoTarget;
            return false;
        }

        /// <summary>
        /// Düz vuruş seçili düşman menzildeyse onu, değilse en yakın menzil içi düşmanı alır.
        /// Hiç düşman yoksa false: saldırı yine ileri oynar fakat kimseye kilitlenmez.
        /// </summary>
        public bool TryResolveBasicEnemy(float rangeM, out Transform target)
        {
            BuildCandidates();
            int? selectedId = null;
            if (_owner != null && _selected != null && _selected.TeamId != _ownerTeamId
                && _selected.IsAvailable && _selected.DistanceFrom(_owner.position) <= rangeM)
                selectedId = _selected.GetInstanceID();

            TargetResolution result = TargetingRules.Resolve(
                "enemy_only", SkillAimMode.Targeted, rangeM, selectedId, _candidateData);
            target = result.Allowed
                && _candidateMap.TryGetValue(result.TargetId, out Targetable found)
                    ? found.transform
                    : null;
            return target != null;
        }

        void BuildCandidates()
        {
            _candidateData.Clear();
            _candidateMap.Clear();
            if (_owner == null)
                return;

            IReadOnlyList<Targetable> targets = Targetable.Live;
            for (int i = 0; i < targets.Count; i++)
            {
                Targetable candidate = targets[i];
                if (candidate == null || candidate.transform == _owner)
                    continue;
                int id = candidate.GetInstanceID();
                TargetRelation relation = candidate.TeamId == _ownerTeamId
                    ? TargetRelation.Ally
                    : TargetRelation.Enemy;
                _candidateData.Add(new TargetCandidate(
                    id, relation, candidate.DistanceFrom(_owner.position), candidate.IsAvailable));
                _candidateMap[id] = candidate;
            }
        }

        void OnFingerDown(Finger finger)
        {
            if (_fingerId.HasValue || InputBlocked())
                return;
            _fingerId = finger.index;
            _fingerStart = finger.screenPosition;
            _fingerBlocked = _hexagon != null
                && (_hexagon.ClaimedFingerId == finger.index
                    || _hexagon.ClaimedDodgeFingerId == finger.index
                    || _hexagon.IsStickHalf(finger.screenPosition)
                    || _hexagon.IsCombatControlAt(finger.screenPosition));
        }

        void OnFingerUp(Finger finger)
        {
            if (!_fingerId.HasValue || _fingerId.Value != finger.index)
                return;
            Vector2 end = finger.screenPosition;
            bool blocked = _fingerBlocked || InputBlocked();
            _fingerId = null;
            _fingerBlocked = false;
            if (blocked || PixelsToDp(Vector2.Distance(_fingerStart, end)) > _tapMaxMoveDp)
                return;
            SelectAt(end);
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;
            Vector2 pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _mouseHeld = true;
                _mouseStart = pos;
                _mouseBlocked = InputBlocked()
                    || (_hexagon != null && _hexagon.IsCombatControlAt(pos));
            }
            else if (_mouseHeld && mouse.leftButton.wasReleasedThisFrame)
            {
                _mouseHeld = false;
                if (!_mouseBlocked
                    && !InputBlocked()
                    && PixelsToDp(Vector2.Distance(_mouseStart, pos)) <= _tapMaxMoveDp)
                    SelectAt(pos);
            }
        }

        void SelectAt(Vector2 screenPosition)
        {
            if (_camera == null && _follow != null)
                _camera = _follow.ViewCamera;
            if (_camera == null)
                return;

            Ray ray = _camera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray, _camera.farClipPlane, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Targetable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                Targetable candidate = hits[i].collider != null
                    ? hits[i].collider.GetComponentInParent<Targetable>()
                    : null;
                if (candidate == null || !candidate.IsAvailable || hits[i].distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = hits[i].distance;
            }
            Select(best);
        }

        void Select(Targetable target)
        {
            if (_selected == target)
                return;
            _selected = target;
            RefreshPresentation();
        }

        bool InputBlocked() =>
            TuningPanel.IsOpen || GrammarDebugPanel.IsOpen || BuildSelectScreen.IsOpen
            || (_elementMenu != null && _elementMenu.IsMenuOpen);

        void BuildMarker()
        {
            if (_ring != null)
                return;
            _ringObject = new GameObject("SelectedTargetRing");
            _ringObject.transform.SetParent(transform, false);
            _ring = _ringObject.AddComponent<LineRenderer>();
            _ring.loop = true;
            _ring.useWorldSpace = true;
            _ring.textureMode = LineTextureMode.Stretch;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            Shader shader = AssetLoader.FindShader("Universal Render Pipeline/Unlit", null) ?? AssetLoader.FindShader("Unlit/Color", null);
            if (shader != null)
                _ring.material = new Material(shader);
        }

        void BuildFrame(Transform canvasRoot)
        {
            if (_frame != null || canvasRoot == null)
                return;
            var go = new GameObject("SelectedTargetFrame");
            go.transform.SetParent(canvasRoot, false);
            go.layer = canvasRoot.gameObject.layer;
            _frame = go.AddComponent<RectTransform>();
            _frame.anchorMin = new Vector2(0.5f, 1f);
            _frame.anchorMax = new Vector2(0.5f, 1f);
            _frame.pivot = new Vector2(0.5f, 1f);
            _frame.sizeDelta = _presentation.FrameSizePx;
            _frame.anchoredPosition = _presentation.FrameOffsetPx;
            _frameBackground = go.AddComponent<Image>();
            _frameBackground.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            textGo.layer = go.layer;
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            _frameText = textGo.AddComponent<Text>();
            _frameText.font = HudTheme.LegacyFont;
            if (_frameText.font == null)
                _frameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _frameText.fontSize = _presentation.FrameFontPx;
            _frameText.fontStyle = FontStyle.Bold;
            _frameText.alignment = TextAnchor.MiddleCenter;
            _frameText.raycastTarget = false;
        }

        void RefreshPresentation()
        {
            bool visible = _selected != null;
            if (_ringObject != null)
                _ringObject.SetActive(visible);
            if (_frame != null)
                _frame.gameObject.SetActive(visible);
            if (!visible)
                return;

            bool ally = _selected.TeamId == _ownerTeamId;
            Color color = ally ? _presentation.AllyColor : _presentation.EnemyColor;
            if (_ring != null)
            {
                _ring.startColor = color;
                _ring.endColor = color;
                _ring.startWidth = _presentation.RingWidthM;
                _ring.endWidth = _presentation.RingWidthM;
            }
            if (_frameBackground != null)
                _frameBackground.color = new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 0.88f);
            if (_frameText != null)
            {
                _frameText.color = color;
                _frameText.text = (ally ? "DOST • " : "HEDEF • ") + _selected.DisplayName;
            }
        }

        void DrawRing(Targetable target)
        {
            int segments = Mathf.Max(12, _presentation.RingSegments);
            _ring.positionCount = segments;
            float radius = target.MarkerRadius(_presentation.RingPaddingM);
            float y = target.GroundY + _presentation.RingGroundOffsetM;
            Vector3 center = target.transform.position;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                _ring.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    y,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }

        static float PixelsToDp(float px)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : PlayerTargetingDefaults.FallbackDpi;
            return px * (PlayerTargetingDefaults.FallbackDpi / dpi);
        }
    }
}
