using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    /// <summary>Minyon ve klon boss'un ve oyuncunun gövdesinin dışında durur.</summary>
    public static class ActorSpacing
    {
        public static void PushOutside(ref float x, ref float z, float ox, float oz, float minSep)
        {
            if (minSep <= 0f)
                return;
            float dx = x - ox;
            float dz = z - oz;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist >= minSep)
                return;
            if (dist < 0.0001f)
            {
                x = ox + minSep;
                z = oz;
                return;
            }

            float scale = minSep / dist;
            x = ox + dx * scale;
            z = oz + dz * scale;
        }
    }
}
