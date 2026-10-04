using System;

namespace Dovus.Core.Boss
{
    public readonly struct ProjectileEvent
    {
        public ProjectileEvent(ProjectileEventKind kind, int id, float x, float z, float damage)
        {
            Kind = kind;
            Id = id;
            X = x;
            Z = z;
            Damage = damage;
        }

        public ProjectileEventKind Kind { get; }
        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public float Damage { get; }
    }
}
