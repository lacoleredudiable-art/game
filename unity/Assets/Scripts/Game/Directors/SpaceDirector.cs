using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Layers;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// space_layer InvisibleLink + Tear görselleri ve trigger.
    /// Core <see cref="SpaceDirector"/> yaşam döngüsü; bu sınıf LineRenderer / kapsül.
    /// </summary>
    public sealed class SpaceDirectorHost : MonoBehaviour
    {
        public const string ActorPlayer = "player";
        public const string ActorBoss = "boss";
        public const string ActorAlly = "ally";

        SpaceDirector _logic;
        SpaceLayerTuning _tuning;
        Transform _root;
        Transform _player;
        Transform _boss;
        Transform _ally;
        BossVitals _bossVitals;
        PlayerVitals _playerVitals;
        AllyDummy _allyDummy;

        readonly Dictionary<int, LinkVisual> _links = new();
        readonly Dictionary<int, TearVisual> _tears = new();
        readonly List<SpaceLinkTick> _tickBuf = new();
        readonly HashSet<int> _closingTears = new();
        float _linkHealCarry;

        Material _linkMat;
        Material _tearMat;

        struct LinkVisual
        {
            public LineRenderer Line;
            public float Phase;
        }

        struct TearVisual
        {
            public GameObject Root;
            public TearTrigger Trigger;
            public float CloseAtUnscaled;
            public bool Closing;
        }

        public SpaceDirector Logic => _logic;

        public void Bind(
            SpaceDirector logic,
            SpaceLayerTuning tuning,
            Transform player,
            Transform boss,
            BossVitals bossVitals,
            PlayerVitals playerVitals,
            AllyDummy ally = null)
        {
            _logic = logic;
            _tuning = tuning ?? new SpaceLayerTuning();
            _player = player;
            _boss = boss;
            _bossVitals = bossVitals;
            _playerVitals = playerVitals;
            _allyDummy = ally;
            _ally = ally != null ? ally.transform : null;

            if (_root == null)
            {
                var go = new GameObject("SpaceFieldVisuals");
                go.transform.SetParent(transform, false);
                _root = go.transform;
            }

            // Oyuncu efekti: mor-siyah (kırmızı-turuncu yasak).
            _linkMat ??= MakeUnlit(new Color(0.45f, 0.12f, 0.55f, 0.95f));
            _tearMat ??= MakeUnlit(new Color(0.12f, 0.04f, 0.14f, 0.92f));
        }

        /// <summary>Oyuncu hasar alınca Karabasan hattını kopar.</summary>
        public void NotifyOwnerDamaged(string ownerId)
        {
            if (_logic == null)
                return;
            int n = _logic.BreakLinksOwnedBy(ownerId);
            if (n > 0)
                SyncVisuals();
        }

        public void Tick(float dtSec)
        {
            if (_logic == null)
                return;

            Vector3 o = _player != null ? _player.position : Vector3.zero;
            Vector3 t = _boss != null ? _boss.position : Vector3.zero;

            _tickBuf.Clear();
            _logic.Tick(dtSec, o.x, o.y, o.z, t.x, t.y, t.z, _tickBuf);

            for (int i = 0; i < _tickBuf.Count; i++)
            {
                SpaceLinkTick tick = _tickBuf[i];
                if (tick.Drain > 0f && _bossVitals != null && !_bossVitals.IsDown)
                    _bossVitals.ApplyDamage(tick.Drain);
                if (tick.Heal > 0f && _playerVitals != null && !_playerVitals.IsDown)
                {
                    _linkHealCarry += tick.Heal;
                    int whole = Mathf.FloorToInt(_linkHealCarry);
                    if (whole > 0)
                    {
                        _linkHealCarry -= whole;
                        _playerVitals.ApplyHeal(whole);
                    }
                }
            }

            TickTearClose();
            SyncVisuals();
            UpdateLinkWaves(dtSec);
        }

        public void SyncVisuals()
        {
            if (_logic == null || _root == null)
                return;

            var live = new HashSet<int>();
            IReadOnlyList<SpaceEffect> effects = _logic.ActiveEffects;
            for (int i = 0; i < effects.Count; i++)
            {
                SpaceEffect e = effects[i];
                live.Add(e.RuntimeId);
                if (e.Kind == SpaceEffectKind.InvisibleLink)
                    HandleInvisibleLink(e);
                else if (e.Kind == SpaceEffectKind.Tear)
                    HandleTear(e);
            }

            PruneLinks(live);
            PruneTears(live);
        }

        void HandleInvisibleLink(SpaceEffect effect)
        {
            if (!_links.TryGetValue(effect.RuntimeId, out LinkVisual vis) || vis.Line == null)
            {
                var go = new GameObject($"Link_{effect.RuntimeId}_{effect.Id}");
                go.transform.SetParent(_root, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = _linkMat;
                lr.widthMultiplier = _tuning.LinkLineWidthM;
                lr.positionCount = 8;
                lr.numCapVertices = 4;
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // Gradient: mor → siyah-mor
                var grad = new Gradient();
                grad.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.55f, 0.2f, 0.75f), 0f),
                        new GradientColorKey(new Color(0.08f, 0.02f, 0.12f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.95f, 0f),
                        new GradientAlphaKey(0.7f, 1f)
                    });
                lr.colorGradient = grad;
                vis = new LinkVisual { Line = lr, Phase = Random.value * Mathf.PI * 2f };
                _links[effect.RuntimeId] = vis;
            }

            UpdateLinkLine(effect.RuntimeId, vis);
        }

        void HandleTear(SpaceEffect effect)
        {
            if (_tears.TryGetValue(effect.RuntimeId, out TearVisual existing) && existing.Root != null)
            {
                if (!existing.Closing)
                    existing.Root.transform.position = new Vector3(effect.X, effect.Y, effect.Z);
                return;
            }

            float h = _tuning.TearHeightM;
            float w = _tuning.TearWidthM;
            float d = _tuning.TearDepthM;

            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = $"Tear_{effect.RuntimeId}_{effect.Id}";
            root.transform.SetParent(_root, false);
            // Capsule default: yükseklik 2, radius 0.5 → scale ile 3×0.5×0.2
            root.transform.position = new Vector3(effect.X, h * 0.5f, effect.Z);
            root.transform.localScale = new Vector3(w, h * 0.5f, d);

            var col = root.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            var rend = root.GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = _tearMat;

            var trigger = root.AddComponent<TearTrigger>();
            trigger.Bind(this, effect.RuntimeId);

            // Kenar jitter: ince ikinci kapsül (mor kenar) — özel shader yok, pulse Scale.
            var rim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rim.name = "TearRim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localScale = new Vector3(1.15f, 1.02f, 1.15f);
            Object.Destroy(rim.GetComponent<Collider>());
            var rimR = rim.GetComponent<Renderer>();
            if (rimR != null)
                rimR.sharedMaterial = MakeUnlit(new Color(0.55f, 0.18f, 0.32f, 0.55f)); // mercan-mor

            _tears[effect.RuntimeId] = new TearVisual
            {
                Root = root,
                Trigger = trigger,
                CloseAtUnscaled = -1f,
                Closing = false
            };
        }

        public float OnTearTrigger(int runtimeId, string actorId)
        {
            if (_logic == null)
                return 0f;
            float dmg = _logic.TryCrossTear(runtimeId, actorId);
            if (dmg <= 0f)
                return 0f;

            ApplyCrossDamage(actorId, dmg);
            return dmg;
        }

        void ApplyCrossDamage(string actorId, float dmg)
        {
            if (string.Equals(actorId, ActorBoss, System.StringComparison.Ordinal))
            {
                if (_bossVitals != null && !_bossVitals.IsDown)
                    _bossVitals.ApplyDamage(dmg);
                return;
            }

            if (string.Equals(actorId, ActorPlayer, System.StringComparison.Ordinal))
            {
                if (_playerVitals != null && !_playerVitals.IsDown)
                    _playerVitals.ApplyDamage(Mathf.CeilToInt(dmg));
                return;
            }

            if (string.Equals(actorId, ActorAlly, System.StringComparison.Ordinal))
            {
                if (_allyDummy != null)
                    _allyDummy.ApplyDamage(Mathf.CeilToInt(dmg));
            }
        }

        void UpdateLinkWaves(float dtSec)
        {
            if (_player == null || _boss == null)
                return;

            var keys = new List<int>(_links.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int id = keys[i];
                if (!_links.TryGetValue(id, out LinkVisual vis) || vis.Line == null)
                    continue;
                vis.Phase += dtSec * 3.2f;
                _links[id] = vis;
                // effect hâlâ aktif mi — Sync zaten çizgiyi günceller; dalga için yeniden çiz
                UpdateLinkLine(id, vis);
            }

            // Tear rim jitter
            foreach (KeyValuePair<int, TearVisual> kv in _tears)
            {
                TearVisual tv = kv.Value;
                if (tv.Root == null || tv.Closing)
                    continue;
                Transform rim = tv.Root.transform.childCount > 0 ? tv.Root.transform.GetChild(0) : null;
                if (rim == null)
                    continue;
                float j = 1.12f + 0.06f * Mathf.Sin(Time.time * 11f + kv.Key);
                rim.localScale = new Vector3(j, 1.02f, j);
            }
        }

        void UpdateLinkLine(int runtimeId, LinkVisual vis)
        {
            if (vis.Line == null || _player == null || _boss == null)
                return;

            Vector3 a = _player.position + Vector3.up * 0.9f;
            Vector3 b = _boss.position + Vector3.up * 1.2f;
            int n = vis.Line.positionCount;
            for (int i = 0; i < n; i++)
            {
                float u = n <= 1 ? 0f : i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(a, b, u);
                // Hafif sinüs dalgalanma (oyuncu-mor hat).
                Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up);
                if (side.sqrMagnitude < 0.001f)
                    side = Vector3.right;
                side.Normalize();
                p += side * (Mathf.Sin(vis.Phase + u * Mathf.PI * 2f) * 0.12f);
                p.y += Mathf.Sin(vis.Phase * 1.3f + u * 4f) * 0.08f;
                vis.Line.SetPosition(i, p);
            }
        }

        void TickTearClose()
        {
            // Süre bitince Core listeden düşer → PruneTears kapan animasyonu başlatır.
            float closeSec = _tuning.TearCloseAnimSec;
            var done = new List<int>();
            foreach (KeyValuePair<int, TearVisual> kv in _tears)
            {
                TearVisual tv = kv.Value;
                if (!tv.Closing || tv.Root == null)
                    continue;
                float t = 1f - Mathf.Clamp01((tv.CloseAtUnscaled - Time.unscaledTime) / Mathf.Max(0.01f, closeSec));
                float s = Mathf.Max(0.01f, 1f - t);
                tv.Root.transform.localScale = Vector3.Scale(
                    new Vector3(_tuning.TearWidthM, _tuning.TearHeightM * 0.5f, _tuning.TearDepthM),
                    new Vector3(s, s, s));
                if (Time.unscaledTime >= tv.CloseAtUnscaled)
                    done.Add(kv.Key);
            }

            for (int i = 0; i < done.Count; i++)
            {
                int id = done[i];
                if (_tears.TryGetValue(id, out TearVisual tv) && tv.Root != null)
                    Destroy(tv.Root);
                _tears.Remove(id);
                _closingTears.Remove(id);
            }
        }

        void PruneLinks(HashSet<int> live)
        {
            var stale = new List<int>();
            foreach (int id in _links.Keys)
            {
                if (!live.Contains(id))
                    stale.Add(id);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                int id = stale[i];
                if (_links.TryGetValue(id, out LinkVisual vis) && vis.Line != null)
                    Destroy(vis.Line.gameObject);
                _links.Remove(id);
            }
        }

        void PruneTears(HashSet<int> live)
        {
            var stale = new List<int>();
            foreach (int id in _tears.Keys)
            {
                if (!live.Contains(id) && !_closingTears.Contains(id))
                    stale.Add(id);
            }

            float closeSec = _tuning.TearCloseAnimSec;
            for (int i = 0; i < stale.Count; i++)
            {
                int id = stale[i];
                if (!_tears.TryGetValue(id, out TearVisual tv) || tv.Root == null)
                {
                    _tears.Remove(id);
                    continue;
                }

                tv.Closing = true;
                tv.CloseAtUnscaled = Time.unscaledTime + closeSec;
                _tears[id] = tv;
                _closingTears.Add(id);
                if (tv.Trigger != null)
                    tv.Trigger.enabled = false;
            }
        }

        static Material MakeUnlit(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            else
                mat.color = c;
            return mat;
        }
    }

    /// <summary>Tear trigger — boss / oyuncu / ally (dost da kesilir).</summary>
    public sealed class TearTrigger : MonoBehaviour
    {
        SpaceDirectorHost _host;
        int _runtimeId;

        public void Bind(SpaceDirectorHost host, int runtimeId)
        {
            _host = host;
            _runtimeId = runtimeId;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_host == null || other == null)
                return;

            string actor = ResolveActor(other);
            if (actor == null)
                return;
            _host.OnTearTrigger(_runtimeId, actor);
        }

        static string ResolveActor(Collider other)
        {
            if (other.GetComponentInParent<BossReactor>() != null ||
                other.CompareTag("Boss"))
                return SpaceDirectorHost.ActorBoss;

            // Ally önce — player child değilse.
            if (other.GetComponentInParent<AllyDummy>() != null)
                return SpaceDirectorHost.ActorAlly;

            if (other.GetComponentInParent<PlayerVitals>() != null ||
                other.GetComponentInParent<MoveInput>() != null)
                return SpaceDirectorHost.ActorPlayer;

            return null;
        }
    }
}
