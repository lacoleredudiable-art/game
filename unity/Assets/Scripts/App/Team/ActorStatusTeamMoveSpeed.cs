namespace Dovus.App.Team
{
    /// <summary>
    /// ActorStatus hız çarpanı: takım/portal tablosu yalnız bağlı aktör kimliğine uygulanır.
    /// Oyuncu dışı (boss vb.) tabloda yoksa <see cref="MoveSpeedFor"/> 1 döner.
    /// </summary>
    public static class ActorStatusTeamMoveSpeed
    {
        public static int TableActorId(bool hasPlayerVitals, int playerActorId) =>
            hasPlayerVitals ? playerActorId : 0;

        public static float TeamMoveSpeedMult(TeamModifierTable table, bool hasPlayerVitals, int playerActorId) =>
            table.MoveSpeedFor(TableActorId(hasPlayerVitals, playerActorId));
    }
}
