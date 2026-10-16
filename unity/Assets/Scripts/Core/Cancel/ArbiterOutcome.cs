namespace Dovus.Core.Cancel
{
    public enum ArbiterOutcome : byte
    {
        /// <summary>Bir şey olmadı (Tick'te başlatılacak tampon girdisi yok).</summary>
        None = 0,
        /// <summary>Boşta başladı.</summary>
        Started = 1,
        /// <summary>Çalışanı kesip başladı.</summary>
        Cancelled = 2,
        /// <summary>Reddedildi, tamponda bekliyor.</summary>
        Buffered = 3,
        /// <summary>Reddedildi, saklanmadı.</summary>
        Rejected = 4,
    }
}
