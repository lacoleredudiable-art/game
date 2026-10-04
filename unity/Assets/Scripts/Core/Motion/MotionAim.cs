using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    public static class MotionAim
    {
        public const string Enemy = "enemy";
        public const string Effect = "effect";

        public static bool IsEnemy(string aim) =>
            string.Equals(aim, Enemy, StringComparison.Ordinal);

        /// <summary>
        /// Düşmana göre hareket: seçili düşman, yoksa menzildeki otomatik düşman.
        /// Fiilin etki hedefi (kendin / dost) yok sayılır.
        /// </summary>
        public static bool TryResolveEnemy(
            bool hasSelectedEnemy,
            float selectedX,
            float selectedZ,
            bool hasAutoEnemy,
            float autoX,
            float autoZ,
            out float x,
            out float z)
        {
            if (hasSelectedEnemy)
            {
                x = selectedX;
                z = selectedZ;
                return true;
            }

            if (hasAutoEnemy)
            {
                x = autoX;
                z = autoZ;
                return true;
            }

            x = 0f;
            z = 0f;
            return false;
        }
    }
}
