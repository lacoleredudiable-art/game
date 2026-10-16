using System;

namespace Dovus.Core.Cancel
{
    public enum CancelVerdict : byte
    {
        /// <summary>Çalışan eylem yok; gelen eylem doğrudan başlar (başka kapılar — mana, CD, CC — başka yerde).</summary>
        Start = 0,
        /// <summary>Gelen eylem çalışanı keser ve başlar.</summary>
        Cancel = 1,
        /// <summary>Reddedildi; <see cref="CancelDecision.BufferMs"/> > 0 ise saklanır.</summary>
        Deny = 2,
    }
}
