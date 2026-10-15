using System;

namespace Dovus.Core.Cancel
{
    /// <summary>Gelen eylemin faz süreleri: çağıran (skill kataloğu, kılıç kombosu, silah değişimi) üretir.</summary>
    public delegate ActionPlan ActionPlanResolver(ActionKind kind, int token, int nowMs);
}
