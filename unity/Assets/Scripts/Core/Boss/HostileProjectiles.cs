using System;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// Düşman mermileri: havuzlu, deterministik, Unity'siz (Photon için yeniden oynatılabilir).
    /// Fizik yok; çarpışma düz daire/şerit sorgusuyla. Tavan <see cref="MaxAlive"/> (mobil).
    /// </summary>
    public sealed class HostileProjectiles
    {
        public const int DefaultMaxAlive = 40;

        readonly Projectile[] _pool;
        int _nextId;

        public HostileProjectiles(int maxAlive = DefaultMaxAlive)
        {
            MaxAlive = Math.Max(1, maxAlive);
            _pool = new Projectile[MaxAlive];
        }

        public int MaxAlive { get; }
        public int AliveCount { get; private set; }
        /// <summary>Bu oturumda aynı anda yaşayan en çok mermi (tarama: tavan aşılmadı mı).</summary>
        public int PeakAlive { get; private set; }

        public int SpawnedTotal { get; private set; }
        public int ErasedTotal { get; private set; }
        public int AbsorbedTotal { get; private set; }
        public int LinkErasedTotal { get; private set; }
        public int ReflectedTotal { get; private set; }
        public int ShroudedTotal { get; private set; }
        public int HitFriendlyTotal { get; private set; }
        public int HitHostileTotal { get; private set; }
        public int RejectedTotal { get; private set; }

        public event Action<ProjectileEvent> Events;

        /// <summary>Havuz yuvası (0..MaxAlive-1). Görünüm katmanı yuvaya göre eşler.</summary>
        public ref readonly Projectile Slot(int slot) => ref _pool[slot];

        /// <summary>Yeni mermi; tavan doluysa −1 (en eskisi silinmez, yeni reddedilir).</summary>
        public int Spawn(
            int ownerId, byte team,
            float x, float z, float vx, float vz,
            float radiusM, float damage,
            double nowMs, double lifeMs,
            int targetId = -1, bool homing = false, bool harmless = false)
        {
            int slot = FreeSlot();
            if (slot < 0)
            {
                RejectedTotal++;
                return -1;
            }
            int id = ++_nextId;
            _pool[slot] = new Projectile
            {
                Id = id,
                OwnerId = ownerId,
                Team = team,
                X = x,
                Z = z,
                VX = vx,
                VZ = vz,
                RadiusM = Math.Max(BossDefaults.MinDistM, radiusM),
                Damage = Math.Max(0f, damage),
                SpawnMs = nowMs,
                DieMs = nowMs + Math.Max(1.0, lifeMs),
                TargetId = targetId,
                Homing = homing,
                Harmless = harmless,
                Alive = true
            };
            AliveCount++;
            SpawnedTotal++;
            if (AliveCount > PeakAlive)
                PeakAlive = AliveCount;
            Raise(ProjectileEventKind.Spawned, slot);
            return id;
        }

        /// <summary>Hareket ve süre dolumu. dtMs dünya saati farkı (duraklamada 0).</summary>
        public void Tick(double nowMs, double dtMs)
        {
            float dt = (float)(Math.Max(0.0, dtMs) / BossDefaults.SecToMs);
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].Alive)
                    continue;
                if (nowMs >= _pool[i].DieMs)
                {
                    Kill(i, ProjectileEventKind.Expired);
                    continue;
                }
                _pool[i].X += _pool[i].VX * dt;
                _pool[i].Z += _pool[i].VZ * dt;
            }
        }

        /// <summary>Daire içindeki (mermi yarıçapı dahil) canlı mermiler; team &lt; 0 hepsi.</summary>
        public int QueryCircle(float x, float z, float r, Span<int> ids, int team = -1)
        {
            int n = 0;
            for (int i = 0; i < _pool.Length && n < ids.Length; i++)
            {
                ref Projectile p = ref _pool[i];
                if (!p.Alive || (team >= 0 && p.Team != team))
                    continue;
                float dx = p.X - x;
                float dz = p.Z - z;
                float rr = r + p.RadiusM;
                if (dx * dx + dz * dz <= rr * rr)
                    ids[n++] = p.Id;
            }
            return n;
        }

        /// <summary>A→B şeridi (yarı genişlik + mermi yarıçapı) içindeki canlı mermiler.</summary>
        public int QuerySegment(float ax, float az, float bx, float bz, float halfWidth, Span<int> ids, int team = -1)
        {
            int n = 0;
            for (int i = 0; i < _pool.Length && n < ids.Length; i++)
            {
                ref Projectile p = ref _pool[i];
                if (!p.Alive || (team >= 0 && p.Team != team))
                    continue;
                float d = SegmentDistance(p.X, p.Z, ax, az, bx, bz);
                if (d <= halfWidth + p.RadiusM)
                    ids[n++] = p.Id;
            }
            return n;
        }

        public bool TryGet(int id, out Projectile projectile)
        {
            int slot = SlotOf(id);
            if (slot < 0)
            {
                projectile = default;
                return false;
            }
            projectile = _pool[slot];
            return true;
        }

        /// <summary>Silme. reason Erased/Absorbed/LinkErased/HitFriendly/HitHostile/Cleared.</summary>
        public bool Delete(int id, ProjectileEventKind reason)
        {
            int slot = SlotOf(id);
            if (slot < 0)
                return false;
            Kill(slot, reason);
            return true;
        }

        /// <summary>
        /// Geri gönderme: takım değişir, hız speedMult ile çarpılır. (dirX, dirZ) sıfır değilse
        /// mermi o yöne döner, yoksa hızı tersine çevrilir. Hedef/güdüm düşer.
        /// </summary>
        public bool Reflect(int id, byte newTeam, float speedMult, float dirX = 0f, float dirZ = 0f)
        {
            int slot = SlotOf(id);
            if (slot < 0)
                return false;
            ref Projectile p = ref _pool[slot];
            float speed = MathF.Sqrt(p.VX * p.VX + p.VZ * p.VZ) * Math.Max(0f, speedMult);
            float len = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len > 0.0001f)
            {
                p.VX = dirX / len * speed;
                p.VZ = dirZ / len * speed;
            }
            else
            {
                float cur = MathF.Sqrt(p.VX * p.VX + p.VZ * p.VZ);
                float k = cur > 0.0001f ? speed / cur : 0f;
                p.VX = -p.VX * k;
                p.VZ = -p.VZ * k;
            }
            p.Team = newTeam;
            p.Reflected = true;
            p.TargetId = -1;
            p.Homing = false;
            ReflectedTotal++;
            Raise(ProjectileEventKind.Reflected, slot);
            return true;
        }

        public void SetSpeedMult(int id, float mult)
        {
            int slot = SlotOf(id);
            if (slot < 0)
                return;
            float m = Math.Max(0f, mult);
            _pool[slot].VX *= m;
            _pool[slot].VZ *= m;
        }

        /// <summary>Sis perdesi: hedef ve güdüm düşer. Mermi başına bir kez sayılır.</summary>
        public bool DropTarget(int id)
        {
            int slot = SlotOf(id);
            if (slot < 0)
                return false;
            ref Projectile p = ref _pool[slot];
            if (p.TargetId < 0 && !p.Homing)
                return false;
            p.TargetId = -1;
            p.Homing = false;
            ShroudedTotal++;
            Raise(ProjectileEventKind.Shrouded, slot);
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _pool.Length; i++)
                if (_pool[i].Alive)
                    Kill(i, ProjectileEventKind.Cleared);
        }

        /// <summary>Sayaçlar (tarama vaka başında sıfırlar). Canlı mermilere dokunmaz.</summary>
        public void ResetCounters()
        {
            SpawnedTotal = ErasedTotal = AbsorbedTotal = LinkErasedTotal = 0;
            ReflectedTotal = ShroudedTotal = HitFriendlyTotal = HitHostileTotal = RejectedTotal = 0;
            PeakAlive = AliveCount;
        }

        /// <summary>A'dan hedefe çarpma süresi (ms). Yaklaşmıyorsa +∞.</summary>
        public static double TimeToImpactMs(in Projectile p, float tx, float tz, float targetRadius)
        {
            float dx = tx - p.X;
            float dz = tz - p.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz) - targetRadius - p.RadiusM;
            if (dist <= 0f)
                return 0;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 0.0001f)
                return 0;
            float closing = (p.VX * dx + p.VZ * dz) / len;
            if (closing <= 0.0001f)
                return double.PositiveInfinity;
            return dist / closing * BossDefaults.SecToMs;
        }

        public static float SegmentDistance(float px, float pz, float ax, float az, float bx, float bz)
        {
            float abx = bx - ax;
            float abz = bz - az;
            float len2 = abx * abx + abz * abz;
            float t = len2 > BossDefaults.SegmentLen2EpsilonSqr ? ((px - ax) * abx + (pz - az) * abz) / len2 : 0f;
            t = Math.Clamp(t, 0f, 1f);
            float cx = ax + abx * t - px;
            float cz = az + abz * t - pz;
            return MathF.Sqrt(cx * cx + cz * cz);
        }

        int FreeSlot()
        {
            for (int i = 0; i < _pool.Length; i++)
                if (!_pool[i].Alive)
                    return i;
            return -1;
        }

        int SlotOf(int id)
        {
            if (id <= 0)
                return -1;
            for (int i = 0; i < _pool.Length; i++)
                if (_pool[i].Alive && _pool[i].Id == id)
                    return i;
            return -1;
        }

        void Kill(int slot, ProjectileEventKind reason)
        {
            _pool[slot].Alive = false;
            AliveCount--;
            switch (reason)
            {
                case ProjectileEventKind.Erased: ErasedTotal++; break;
                case ProjectileEventKind.Absorbed: AbsorbedTotal++; break;
                case ProjectileEventKind.LinkErased: LinkErasedTotal++; break;
                case ProjectileEventKind.HitFriendly: HitFriendlyTotal++; break;
                case ProjectileEventKind.HitHostile: HitHostileTotal++; break;
            }
            Raise(reason, slot);
        }

        void Raise(ProjectileEventKind kind, int slot)
        {
            Action<ProjectileEvent> handler = Events;
            if (handler == null)
                return;
            ref Projectile p = ref _pool[slot];
            handler(new ProjectileEvent(kind, p.Id, p.X, p.Z, p.Damage));
        }
    }
}
