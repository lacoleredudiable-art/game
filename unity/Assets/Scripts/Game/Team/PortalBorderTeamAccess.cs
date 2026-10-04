using Dovus.Core.Portal;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Saklanan <see cref="PortalBorderTeamHost"/> referansı; devre dışı host'ta nötr çarpanlar.
    /// </summary>
    public sealed class PortalBorderTeamAccess
    {
        static readonly PortalSystem LegacyPortal = new();

        PortalBorderTeamHost _host;

        public void Bind(PortalBorderTeamHost host) => _host = host;

        public TeamModifierHub Hub =>
            _host != null && _host.isActiveAndEnabled ? _host.Modifiers : TeamModifierHub.Neutral;

        public PortalSystem Portal =>
            _host != null && _host.isActiveAndEnabled ? _host.Portal : LegacyPortal;

        public PortalBorderTeamHost Host => _host;
    }
}
