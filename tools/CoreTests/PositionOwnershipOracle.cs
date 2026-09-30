namespace Dovus.Core.Motion
{
    /// <summary>
    /// Konum sahipliği kuralının test tarafı kâhini. Runtime'da çağıranı yoktu;
    /// CLEANUP-2 ile PositionOwnership'ten test projesine taşındı.
    /// </summary>
    public static class PositionOwnershipOracle
    {
        /// <summary>Kalıp konumu yönetirken gramer oyuncuyu ışınlamaz / yer değiştirmez.</summary>
        public static bool SuppressesMove(bool templateMovesPlayer, string atom, string stat) =>
            templateMovesPlayer && PositionOwnership.Kind(atom, stat) != PositionStepKind.None;

        /// <summary>
        /// Bir cast'te oyuncuyu oynatan kaynak sayısı. Kalıp yönetiyorsa gramer eklenmez; ikisi birden olmaz.
        /// </summary>
        public static int PositionWriters(bool templateMovesPlayer, bool grammarMovesPlayer)
        {
            if (templateMovesPlayer)
                return 1;
            return grammarMovesPlayer ? 1 : 0;
        }
    }
}
