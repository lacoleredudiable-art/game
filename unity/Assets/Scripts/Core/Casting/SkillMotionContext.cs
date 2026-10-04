using System;
using Dovus.Core.Grammar;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Casting
{
    public readonly struct SkillMotionContext
    {
        public SkillMotionContext(
            float casterX, float casterZ,
            float faceX, float faceZ,
            float bossX, float bossZ,
            bool bossAlive,
            float arenaHalfSizeM)
        {
            CasterX = casterX;
            CasterZ = casterZ;
            FaceX = faceX;
            FaceZ = faceZ;
            BossX = bossX;
            BossZ = bossZ;
            BossAlive = bossAlive;
            ArenaHalfSizeM = arenaHalfSizeM;
        }

        public float CasterX { get; }
        public float CasterZ { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public float BossX { get; }
        public float BossZ { get; }
        public bool BossAlive { get; }
        public float ArenaHalfSizeM { get; }
    }
}
