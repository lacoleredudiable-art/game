using Dovus.App.Casting;

namespace Dovus.App.Boss
{
    public readonly struct ApproachStep
    {
        public ApproachStep(bool stop, float newHomeX, float newHomeZ, float walkMps, float dirX, float dirZ)
        {
            Stop = stop;
            NewHomeX = newHomeX;
            NewHomeZ = newHomeZ;
            WalkMps = walkMps;
            DirX = dirX;
            DirZ = dirZ;
        }

        public bool Stop { get; }
        public float NewHomeX { get; }
        public float NewHomeZ { get; }
        public float WalkMps { get; }
        public float DirX { get; }
        public float DirZ { get; }
    }
}
