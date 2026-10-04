using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Skills.Flow;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Closing
{
    public interface IClosingQueueHost
    {
        PrototypeTuning Colors { get; }
        SkillMotor Skills { get; }
        SkillFactory SkillFactory { get; }
        EquipmentItem EquippedWeapon { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        CombatTuning Combat { get; }
        Transform DirectorTransform { get; }
        GameClock Clock { get; }

        Skill LastFactorySkill { get; set; }

        void EnsureSkillServices();
        SentenceManifestationBridge SentenceBridge { get; }
        void EnsurePresentationCatalog();
        PresentationCatalog PresentationCatalog { get; }

        void StampScar(LivingEffectView view, ClosingHit closing);
        void EnsureCastPort();
        void RunBasicClosing(PendingClosing p);
        void RunSkillClosing(PendingClosing p, LivingEffect logic);
        void DestroyUnityObjectAfter(Object obj, float delaySeconds);
    }
}
