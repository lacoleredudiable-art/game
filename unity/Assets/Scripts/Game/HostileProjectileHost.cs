using System;
using Dovus.Core.Combat;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Düşman mermilerinin dünyadaki tek sahibi: saf <see cref="HostileProjectiles"/>'ı GameClock dünya
    /// saatiyle ilerletir, havuzlu collider'sız küre çizer, dostlarla düz daire çarpışması yapar.
    /// Oyuncu: dodge ya da skill i-frame'i açıkken mermi geçer; yoksa ActorStatus.ApplyDamage(dodgeable:false).
    /// Dost: HostileTargets hasar geri çağrısı (ally_damage_mult orada). Yem: mermiyi emer ve ölür.
    /// Geri gönderilmiş (takım 0) mermi boss'a çarpar, hasar ReflectSink'ten (MD.ApplyReflectedDamage).
    /// </summary>
    public sealed class HostileProjectileHost : MonoBehaviour
    {
        public const int BossOwnerId = 1;

        GameClock _clock;
        HostileTargets _targets;
        Transform _player;
        ActorStatus _playerStatus;
        PlayerVitals _playerVitals;
        Transform _boss;
        float _bossRadiusM = 1f;
        GameObject[] _views;
        double _lastMs = -1;

        public HostileProjectiles Sim { get; } = new HostileProjectiles();

        /// <summary>Takım 0 mermisi boss'a çarptı: ham hasar (MD yansıma çarpanını uygular).</summary>
        public Action<float> ReflectSink { get; set; }

        /// <summary>Sis perdesi: bu noktadaki dost mermiyle vurulamaz (MD.Projectiles doldurur).</summary>
        public Func<Vector3, bool> InShroud { get; set; }

        public void Bind(
            GameClock clock,
            HostileTargets targets,
            Transform player,
            ActorStatus playerStatus,
            PlayerVitals playerVitals,
            Transform boss,
            float bossRadiusM)
        {
            _clock = clock;
            _targets = targets;
            _player = player;
            _playerStatus = playerStatus;
            _playerVitals = playerVitals;
            _boss = boss;
            _bossRadiusM = Mathf.Max(0.1f, bossRadiusM);
            Sim.Events -= OnEvent;
            Sim.Events += OnEvent;
        }

        public double NowMs => _clock != null ? _clock.Director.WorldTimeMs : 0;

        public int Spawn(Vector3 from, Vector3 velocity, float radiusM, float damage, float lifeSec,
            int targetId = -1, bool harmless = false)
        {
            return Sim.Spawn(BossOwnerId, 1, from.x, from.z, velocity.x, velocity.z,
                radiusM, damage, NowMs, Math.Max(0.05, lifeSec) * 1000.0, targetId, false, harmless);
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
                    HostileTargets.Entry entry = entries[e];
                    if (!entry.IsAlive)
                        continue;
                    Vector3 at = entry.Transform.position;
                    if (Flat(p.X, p.Z, at) > p.RadiusM + entry.RadiusM)
                        continue;
                    if (InShroud != null && InShroud(at))
                        continue;
                    if (entry.Kind == TargetKind.Player)
                    {
                        if (PlayerDodgeRig.IsInvulnerableNow(_player))
                            continue;
                        Sim.Delete(p.Id, ProjectileEventKind.HitFriendly);
                        if (_playerStatus != null)
                            _playerStatus.ApplyDamage(p.Damage, dodgeable: false);
                        else
                            _playerVitals?.ApplyDamage(Mathf.CeilToInt(p.Damage));
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
                _views = new GameObject[Sim.MaxAlive];
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
                {
                    v = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(v.GetComponent<Collider>());
                    v.name = "HostileProjectile";
                    v.transform.SetParent(transform, true);
                    _views[i] = v;
                }
                if (!v.activeSelf)
                    v.SetActive(true);
                float d = p.RadiusM * 2f;
                v.transform.localScale = new Vector3(d, d, d);
                v.transform.position = new Vector3(p.X, 1.1f, p.Z);
                Renderer r = v.GetComponent<Renderer>();
                if (r != null)
                    r.material.color = p.Team == 0
                        ? new Color(0.4f, 0.9f, 1f, 0.9f)
                        : p.Harmless ? new Color(0.9f, 0.9f, 0.3f, 0.7f) : new Color(0.45f, 0.95f, 0.2f, 0.9f);
            }
        }

        static float Flat(float x, float z, Vector3 b)
        {
            float dx = x - b.x;
            float dz = z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
