using System;

namespace Dovus.Core.Boss
{
    /// <summary>Tek mermi. Team 1 = düşman (boss), 0 = dost (geri gönderilmiş).</summary>
    public struct Projectile
    {
        public int Id;
        public int OwnerId;
        public byte Team;
        public float X, Z, VX, VZ, RadiusM, Damage;
        public double SpawnMs, DieMs;
        public bool Reflected;
        public int TargetId;
        public bool Homing;
        /// <summary>Tarama düzeneği: dostlara çarpmaz, yalnız silme/yansıtma kurallarını sınar.</summary>
        public bool Harmless;
        public bool Alive;
    }
}
