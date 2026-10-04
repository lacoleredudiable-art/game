using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    /// <summary>Silah çarpanı hareket kalıbının yazılmış mesafesini kısaltamaz.</summary>
    public static class MotionTravel
    {
        public static float Protect(float authoredM, float weaponScaledM)
        {
            float authored = Math.Max(0f, authoredM);
            float scaled = Math.Max(0f, weaponScaledM);
            return Math.Max(authored, scaled);
        }
    }
}
