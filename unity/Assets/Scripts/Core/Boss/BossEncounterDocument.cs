using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using System.Collections.Generic;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>Parse edilmiş boss encounter JSON kökü; yalnız mapper üretir.</summary>
    public sealed class BossEncounterDocument
    {
        internal BossEncounterDocument(JsonValue root) => Root = root;

        internal JsonValue Root { get; }

        public bool IsValid => Root != null && !Root.IsNull;
    }
}
