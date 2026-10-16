using Dovus.Core.Motion;
using Dovus.Game.Boss;

namespace Dovus.Game.Skills
{
    public sealed partial class RuleEngineV4SentenceHost
    {
        internal bool RecoilInTemplate { get; set; }

        internal MotionTemplateCatalog MotionCatalog { get; } = MotionTemplateCatalog.Empty;

        internal void EnsureMotionServices()
        {
        }

        internal float BossBodyRadius() =>
            _boss != null ? _boss.BodyRadiusM : 0f;

        internal void TryBeginBasicStrikeStep()
        {
        }

        internal void SetRecoilInTemplate(bool value) => RecoilInTemplate = value;

        internal void EnsureMotionReady()
        {
        }

        void CancelActiveSkillForDodge()
        {
        }

        public void BindHostileTargets(Dovus.Game.Boss.HostileTargetsHost targets)
        {
        }
    }
}
