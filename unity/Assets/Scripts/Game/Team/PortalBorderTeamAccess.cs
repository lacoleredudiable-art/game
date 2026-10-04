using Dovus.Core.Portal;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Enjekte edilen takım erişimi. Host yaşam döngüsü <see cref="PortalBorderTeamHost"/> (AfterSceneLoad Boot,
    /// DontDestroyOnLoad) sahibinde kalır; host yoksa / devre dışıysa nötr çarpanlar ve Legacy portal tablosu.
    /// </summary>
    public sealed class PortalBorderTeamAccess
    {
        static readonly PortalSystem LegacyPortal = new();

        public TeamModifierHub Hub => PortalBorderTeamHost.Hub;

        public PortalSystem Portal =>
            PortalBorderTeamHost.Instance != null ? PortalBorderTeamHost.Instance.Portal : LegacyPortal;

        public PortalBorderTeamHost Host => PortalBorderTeamHost.Instance;
    }
}
