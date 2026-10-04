using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Core.Shared;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Effects;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Flow;
using Dovus.Game.Skills.Targeting;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal MdSkillServicesHost _skillServicesHost;
        internal SkillAim _skillAim;
        internal LivingEffectSpawner _effectSpawner;
        internal WeaponLoadoutCycle _weaponLoadout;
        internal SentenceManifestationBridge _sentenceBridge;

        internal void EnsureSkillServices()
        {
            if (_skillServicesHost != null)
                return;
            _skillServicesHost = new MdSkillServicesHost(this);
            _skillAim = new SkillAim(_skillServicesHost);
            _effectSpawner = new LivingEffectSpawner(_skillServicesHost);
            _weaponLoadout = new WeaponLoadoutCycle(_skillServicesHost);
            _sentenceBridge = new SentenceManifestationBridge(_skillServicesHost);
        }

        internal bool TryArmSkillTarget(SkillResolution skill)
        {
          EnsureSkillServices();
            return _skillAim.TryArmSkillTarget(skill);
        }

        internal void FaceTarget(Transform target)
        {
          EnsureSkillServices();
            _skillAim.FaceTarget(target);
        }

        internal void CaptureBasicFacing()
        {
          EnsureSkillServices();
            _skillAim.CaptureBasicFacing();
        }

        internal Vector3 FlatBodyForward()
        {
          EnsureSkillServices();
            return _skillAim.FlatBodyForward();
        }

        internal Transform CastFacingTarget
        {
            get
            {
              EnsureSkillServices();
                return _skillAim.CastFacingTarget;
            }
        }
    }
}
