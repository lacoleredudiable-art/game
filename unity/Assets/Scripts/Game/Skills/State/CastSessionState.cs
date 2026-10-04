using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;

namespace Dovus.Game.Skills.State
{
    /// <summary>Cast / kapanış oturumu — son skill, zincir, teslim kuyruğu (tek kopya).</summary>
    public sealed class CastSessionState
    {
        public SkillResolution JsonCastSkill { get; set; } = SkillResolution.Empty;
        public ClosingHit JsonCastClosing { get; set; }
        public float ClosingChainBonus { get; set; } = 1f;
        public int SlotQueryCastId { get; set; }
        public bool CasterRecoilSuppressed { get; set; }
        public SkillResolution DeliverySkill { get; set; } = SkillResolution.Empty;
        public PendingClosing DeliveryPending { get; set; }
        public float LastHitX { get; set; }
        public float LastHitZ { get; set; }
    }
}
