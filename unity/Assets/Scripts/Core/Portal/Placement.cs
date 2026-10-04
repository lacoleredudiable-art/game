using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public readonly struct Placement
    {
        public Placement(int actorId, float x, float y, float z, SkillId skillId, bool transferDebuffs, bool teleport = false)
        {
            ActorId = actorId;
            X = x;
            Y = y;
            Z = z;
            SkillId = skillId;
            TransferDebuffs = transferDebuffs;
            Teleport = teleport;
        }

        public int ActorId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public SkillId SkillId { get; }
        public bool TransferDebuffs { get; }
        /// <summary>Yer değiştirme, çapa dönüşü veya kapı geçişi. Tarama yalnız bu kareyi ışın sayar.</summary>
        public bool Teleport { get; }
    }
}
