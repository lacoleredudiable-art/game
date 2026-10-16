using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Launch;
using Dovus.Game.Skills.Presentation;
using System.Collections.Generic;

namespace Dovus.Game.Skills
{
    public sealed partial class RuleEngineV4SentenceHost
    {
        internal RuleEngineV4SentenceLaunchHost _launchHost;
        internal CastSideEffects _castSideEffects;
        internal SkillPresentation _skillPresentation;

        internal void EnsureLaunchServices()
        {
            if (_launchHost != null)
                return;
            _launchHost = new RuleEngineV4SentenceLaunchHost(this);
            _castSideEffects = new CastSideEffects(_launchHost);
            _skillPresentation = new SkillPresentation(_launchHost);
        }

        internal PresentationCatalog PresentationCatalog
        {
            get
            {
                EnsureLaunchServices();
                _skillPresentation.EnsureCatalog();
                return _skillPresentation.Catalog;
            }
        }

        internal void EnsurePresentationCatalog()
        {
            EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
        }

        internal void ApplyCastMobility(SkillResolution skill, float durationSec)
        {
            EnsureLaunchServices();
            _castSideEffects.ApplyCastMobility(skill, durationSec);
        }

        internal void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words)
        {
            EnsureLaunchServices();
            _castSideEffects.RefreshBuildingMobility(words);
        }
    }
}
