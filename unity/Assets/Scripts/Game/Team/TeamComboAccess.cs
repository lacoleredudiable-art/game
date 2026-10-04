using Dovus.Core.Portal;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Enjekte edilen takım erişimi. Host Composition'da kurulur; host yoksa nötr çarpanlar ve Legacy portal tablosu.
    /// </summary>
    public sealed class TeamComboAccess
    {
        static readonly PortalSystem LegacyPortal = new();

        TeamComboHost _host;

        public void Configure(TeamComboHost host) => _host = host;

        public TeamModifierHub Hub => _host != null ? _host.Modifiers : TeamModifierHub.Neutral;

        public PortalSystem Portal => _host != null ? _host.Portal : LegacyPortal;

        public TeamComboHost Host => _host;
    }
}
