using System.Globalization;
using Dovus.Core.Shared;

namespace Dovus.Core.Actors
{
    /// <summary>TargetingRules int kimliği — ActorId sayısal dizesi (davranış: eski sıra/eşitlik korunur).</summary>
    public static class ActorTargetKey
    {
        public static int FromActorId(ActorId id)
        {
            if (id.IsEmpty)
                return 0;
            return int.TryParse(id.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                ? n
                : 0;
        }
    }
}
