#if UNITY_EDITOR
using Dovus.Game.Team;

namespace Dovus.Game.Editor
{
    public static partial class PlaySweep
    {
        static TeamComboAccess _teamAccess;

        public static void BindSweepAccess(TeamComboAccess teamAccess) => _teamAccess = teamAccess;

        public static void ResetTeamCase() => _teamAccess?.Host?.ResetCase();

        public static bool ConsumeIntentionalTeleport() =>
            _teamAccess != null && _teamAccess.Hub.ConsumeIntentionalTeleport();
    }
}
#endif
