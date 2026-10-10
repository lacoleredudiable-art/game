using System;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Animasyon klibi olayları → efekt motoru (#149) köprüsü.</summary>
    [Flags]
    public enum RuleEngineV4SkillAnimEvent : byte
    {
        None = 0,
        TrailOn = 1 << 0,
        TrailOff = 1 << 1,
        Impact = 1 << 2,
        Ejder = 1 << 3,
    }
}
