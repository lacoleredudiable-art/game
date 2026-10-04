using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    /// <summary>Build değişince ilk merkez vuruşunun kuralları. Game katmanı bunu uygular.</summary>
    public static class BasicStrikeInput
    {
        /// <summary>
        /// Çizim kapalı görünse bile loadout yeni uygulandıysa ve motor vuruşu kabul ediyorsa
        /// merkez vuruş başlar. Bayat "casting" bayrağı ilk dokunuşu yutmasın.
        /// </summary>
        public static bool AllowsCenterStrike(bool drawAllowed, bool loadoutJustApplied, bool engineAcceptsStrike) =>
            engineAcceptsStrike && (drawAllowed || loadoutJustApplied);

        /// <summary>Vuruş anında düşman kenar menzilindeyse, cast başında hedef boş olsa da hasar iner.</summary>
        public static bool DealsDamage(bool capsuleHit, bool enemyInEdgeReachAtImpact) =>
            capsuleHit || enemyInEdgeReachAtImpact;

        /// <summary>Tek kelimelik saldırı cümlesi, basic olmayan eski bir görünüme bağlanmaz.</summary>
        public static bool ReplaceStaleView(bool sentenceIsBasic, bool viewIsBasic) =>
            sentenceIsBasic && !viewIsBasic;
    }
}
