using System.Collections.Generic;

using Dovus.Core.Shared;
namespace Dovus.Core.Data
{
    public sealed class VfxBindingData
    {
        public Dictionary<string, string> ElementPrimaryHex { get; } = new();
        public List<(string Id, string VfxStyle)> TrailEntries { get; } = new();
        public List<string> ImpactStyleIds { get; } = new();
    }
}
