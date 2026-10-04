using Dovus.Core.Portal;

namespace Dovus.Game.Team
{
    /// <summary>
    /// Enjekte edilen takım erişimi. Host yaşam döngüsü <see cref="TeamComboHost"/> (AfterSceneLoad Boot,
    /// DontDestroyOnLoad) sahibinde kalır; host yoksa / devre dışıysa nötr çarpanlar ve Legacy portal tablosu.
    /// </summary>
    public sealed class TeamComboAccess
    {
        static readonly PortalSystem LegacyPortal = new();

        public TeamModifierHub Hub => TeamComboHost.Hub;

        public PortalSystem Portal =>
            TeamComboHost.Instance != null ? TeamComboHost.Instance.Portal : LegacyPortal;

        public TeamComboHost Host => TeamComboHost.Instance;
    }
}
