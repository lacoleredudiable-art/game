using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Platform;
using Dovus.Game.Diagnostics;
using Dovus.Game.Vfx;
using System;
using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.Rendering;

using Dovus.Core.Shared;
namespace Dovus.Game.Boss
{
    /// <summary>
    /// Düşman mermilerinin dünyadaki tek sahibi: saf <see cref="HostileProjectiles"/>'ı GameClockHost dünya
    /// saatiyle ilerletir, havuzlu collider'sız küre çizer, dostlarla düz daire çarpışması yapar.
    /// Oyuncu: dodge ya da skill i-frame'i açıkken mermi geçer; yoksa ActorStatusHost.ApplyDamage(dodgeable:false).
    /// Dost: HostileTargetsHost hasar geri çağrısı (ally_damage_mult orada). Yem: mermiyi emer ve ölür.
    /// Geri gönderilmiş (takım 0) mermi boss'a çarpar, hasar ReflectSink'ten (MD.ApplyReflectedDamage).
    /// </summary>
    public sealed class HostileProjectileHost : MonoBehaviour
    {
        public const int BossOwnerId = 1;

        GameClockHost _clock;
        HostileTargetsHost _targets;
        Transform _player;
        ActorStatusHost _playerStatus;
        PlayerVitalsHost _playerVitals;
        Transform _boss;
        float _bossRadiusM = 1f;
        Camera _mainCamera;
        FollowCameraController _follow;
        GameObject[] _views;
        Transform[] _coreViews;
        Transform[] _glowViews;
        Transform[] _shadowViews;
        double _lastMs = -1;
        static Material _coreMat;

        // ff-4: "oyuncunun yanındaki solid yeşil top'lar" — bu mermiler (boss volley). Düz
        // doygun küreler çok yüksek sesliydi; artık koyu kor çekirdek + sıcak yumuşak hale +
        // yerde soluk gölge/halka (gri dünya, sıcak vurgu kuralı — bkz. AGENTS.md "Art rule").
        const float CoreScale = 0.45f;
        const float GlowScale = 1.3f;
        const float ShadowScale = 1f;
        const float ShadowGroundY = 0.03f;
        const float ProjectileFlightY = 1.1f;

        public HostileProjectiles Sim { get; } = new HostileProjectiles();

        /// <summary>Takım 0 mermisi boss'a çarptı: ham hasar (MD yansıma çarpanını uygular).</summary>
        public Action<float> ReflectSink { get; set; }

        /// <summary>Sis perdesi: bu noktadaki dost mermiyle vurulamaz (MD.Projectiles doldurur).</summary>
        public Func<Vector3, bool> InShroud { get; set; }

        /// <summary>Boss volley mermisi oyuncuya isabet ettikten sonra (hasar uygulandıktan sonra).</summary>
        public Action OnPlayerProjectileHit { get; set; }

        public void Bind(
            GameClockHost clock,
            HostileTargetsHost targets,
            Transform player,
            ActorStatusHost playerStatus,
            PlayerVitalsHost playerVitals,
            Transform boss,
            float bossRadiusM)
        {
            _clock = clock;
            _targets = targets;
            _player = player;
            _playerStatus = playerStatus;
            _playerVitals = playerVitals;
            _boss = boss;
            _bossRadiusM = Mathf.Max(HostileProjectileDefaults.MinBossRadiusM, bossRadiusM);
            Sim.Events -= OnEvent;
            Sim.Events += OnEvent;
        }

        public void BindMainCamera(Camera camera, FollowCameraController follow = null)
        {
            _mainCamera = camera;
            _follow = follow;
        }

        public double NowMs => _clock != null ? _clock.Director.WorldTimeMs : 0;

        public int Spawn(Vector3 from, Vector3 velocity, float radiusM, float damage, float lifeSec,
            int targetId = -1, bool harmless = false)
        {
            return Sim.Spawn(BossOwnerId, 1, from.x, from.z, velocity.x, velocity.z,
                radiusM, damage, NowMs, Math.Max(HostileProjectileDefaults.MinLifeSec, lifeSec) * Units.SecToMs, targetId, false, harmless);
        }

        public void ClearAll()
        {
            Sim.Clear();
            SyncViews();
        }

        void Update()
        {
            if (_clock == null)
                return;
            double now = _clock.Director.WorldTimeMs;
            double dt = _lastMs < 0 ? 0 : Math.Max(0, now - _lastMs);
            _lastMs = now;
            if (Sim.AliveCount > 0)
            {
                Sim.Tick(now, dt);
                Collide();
            }
            SyncViews();
        }

        void Collide()
        {
            for (int i = 0; i < Sim.MaxAlive; i++)
            {
                Projectile p = Sim.Slot(i);
                if (!p.Alive)
                    continue;
                if (p.Team == 0)
                {
                    if (_boss != null && Flat(p.X, p.Z, _boss.position) <= p.RadiusM + _bossRadiusM)
                    {
                        Sim.Delete(p.Id, ProjectileEventKind.HitHostile);
                        ReflectSink?.Invoke(p.Damage);
                    }
                    continue;
                }
                if (p.Harmless || _targets == null)
                    continue;
                var entries = _targets.Entries;
                for (int e = entries.Count - 1; e >= 0; e--)
                {
                    HostileTargetsHost.Entry entry = entries[e];
                    if (!entry.IsAlive)
                        continue;
                    Vector3 at = entry.Transform.position;
                    if (Flat(p.X, p.Z, at) > p.RadiusM + entry.RadiusM)
                        continue;
                    if (InShroud != null && InShroud(at))
                        continue;
                    if (entry.Kind == TargetKind.Player)
                    {
                        if (PlayerDodgeController.IsInvulnerableNow(_player))
                            continue;
                        Sim.Delete(p.Id, ProjectileEventKind.HitFriendly);
                        if (_playerStatus != null)
                            _playerStatus.ApplyDamage(p.Damage, dodgeable: false);
                        else
                            _playerVitals?.ApplyDamage(Mathf.CeilToInt(p.Damage));
                        OnPlayerProjectileHit?.Invoke();
                        break;
                    }
                    Sim.Delete(p.Id, ProjectileEventKind.HitFriendly);
                    if (entry.Kind == TargetKind.Decoy)
                        entry.Kill?.Invoke();
                    else
                        entry.Damage?.Invoke(p.Damage);
                    break;
                }
            }
        }

        void OnEvent(ProjectileEvent e)
        {
            if (e.Kind is ProjectileEventKind.Spawned or ProjectileEventKind.Expired or ProjectileEventKind.Cleared)
                return;
            DebugConfig.DevLog($"[Mechanic] json mermi {e.Kind} #{e.Id} ({e.X:0.#},{e.Z:0.#}) canlı={Sim.AliveCount}");
        }

        void SyncViews()
        {
            if (_views == null)
            {
                _views = new GameObject[Sim.MaxAlive];
                _coreViews = new Transform[Sim.MaxAlive];
                _glowViews = new Transform[Sim.MaxAlive];
                _shadowViews = new Transform[Sim.MaxAlive];
            }

            Camera live = _mainCamera;
            if (live == null && _follow != null)
                live = _follow.ViewCamera;
            Transform cam = live != null ? live.transform : null;
            for (int i = 0; i < Sim.MaxAlive; i++)
            {
                Projectile p = Sim.Slot(i);
                GameObject v = _views[i];
                if (!p.Alive)
                {
                    if (v != null && v.activeSelf)
                        v.SetActive(false);
                    continue;
                }
                if (v == null)
                    v = BuildView(i);
                if (!v.activeSelf)
                    v.SetActive(true);

                float d = p.RadiusM * 2f;
                v.transform.position = new Vector3(p.X, ProjectileFlightY, p.Z);

                Transform core = _coreViews[i];
                core.localScale = new Vector3(d, d, d) * CoreScale;

                Transform glow = _glowViews[i];
                glow.localScale = new Vector3(d, d, d) * GlowScale;
                if (cam != null)
                    glow.rotation = Quaternion.LookRotation(glow.position - cam.position, Vector3.up);

                Transform shadow = _shadowViews[i];
                shadow.localScale = new Vector3(d, d, d) * ShadowScale;

                (Color coreColor, Color glowColor) = TintFor(p.Team, p.Harmless);
                SharedTint.Apply(core.GetComponent<Renderer>(), coreColor);
                SharedTint.Apply(glow.GetComponent<Renderer>(), glowColor);
            }
        }

        GameObject BuildView(int slot)
        {
            var root = new GameObject("HostileProjectile");
            root.transform.SetParent(transform, true);
            _views[slot] = root;

            var core = new GameObject("Core");
            core.transform.SetParent(root.transform, false);
            core.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Sphere);
            var coreR = core.AddComponent<MeshRenderer>();
            coreR.shadowCastingMode = ShadowCastingMode.Off;
            coreR.receiveShadows = false;
            coreR.sharedMaterial = CoreMat();
            _coreViews[slot] = core.transform;

            var glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            glow.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
            var glowR = glow.AddComponent<MeshRenderer>();
            glowR.shadowCastingMode = ShadowCastingMode.Off;
            glowR.receiveShadows = false;
            glowR.sharedMaterial = PresentationParticleMaterials.AdditiveTextured;
            _glowViews[slot] = glow.transform;

            // Yerde soluk gölge/halka — kamera açılı olduğundan yüzen çekirdek/hale her zaman
            // tam isabet yarıçapını okutmaz; bu sabit yere yakın disk okunabilirliği garantiler.
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = Vector3.down * (ProjectileFlightY - ShadowGroundY);
            shadow.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            shadow.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
            var shadowR = shadow.AddComponent<MeshRenderer>();
            shadowR.shadowCastingMode = ShadowCastingMode.Off;
            shadowR.receiveShadows = false;
            shadowR.sharedMaterial = PresentationParticleMaterials.AlphaTextured;
            SharedTint.Apply(shadowR, new Color(0.03f, 0.03f, 0.03f, 0.4f));
            _shadowViews[slot] = shadow.transform;

            return root;
        }

        static Material CoreMat()
        {
            if (_coreMat != null)
                return _coreMat;
            Shader shader = AssetLoader.FindShader("Universal Render Pipeline/Simple Lit", null)
                ?? AssetLoader.FindShader("Universal Render Pipeline/Lit", null)
                ?? AssetLoader.FindShader("Standard", null);
            _coreMat = new Material(shader) { name = "HostileProjectileCore" };
            return _coreMat;
        }

        /// <summary>
        /// Desatüre edilmiş uçuş rengi: düşman (gerçek hasar) sıcak kor vurgusu alır (grey dünya,
        /// sıcak vurgu kuralı — lav/VFX); dost (yansıtılmış, takım 0) soğuk/soluk, zararsız nötr gri.
        /// </summary>
        static (Color core, Color glow) TintFor(byte team, bool harmless)
        {
            if (team == 0)
                return (new Color(0.20f, 0.24f, 0.30f, 0.95f), new Color(0.45f, 0.62f, 0.75f, 0.40f));
            if (harmless)
                return (new Color(0.34f, 0.33f, 0.30f, 0.9f), new Color(0.6f, 0.58f, 0.52f, 0.28f));
            return (new Color(0.24f, 0.10f, 0.06f, 0.95f), new Color(0.95f, 0.48f, 0.16f, 0.5f));
        }

        static float Flat(float x, float z, Vector3 b)
        {
            float dx = x - b.x;
            float dz = z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
