using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Manifestation;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Core;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.Mechanics;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Motion;
using Dovus.Game.Skills.Weapons;
using Dovus.Core.Equipment;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

using Dovus.Core.Shared;
namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        IMotionTemplateRepository _motionTemplateRepository;
        internal MdMotionServicesHost _motionHost;
        internal MotionTemplateDriver _motionDriver;
        internal MotionHitResolver _motionHitResolver;
        internal TemplateDeliveryRuntime _templateDelivery;
        internal Transform _templateAim;

        bool _templateOwnsPosition
        {
            get
            {
              EnsureMotionServices();
                return _motionDriver.TemplateOwnsPosition;
            }
        }

        SkillResolution _templateSkill
        {
            get
            {
              EnsureMotionServices();
                return _motionDriver.TemplateSkill;
            }
        }

        float _templateChain
        {
            get
            {
              EnsureMotionServices();
                return _motionDriver.TemplateChain;
            }
        }

        internal void EnsureMotionServices()
        {
            if (_motionHost != null)
                return;
            _motionHost = new MdMotionServicesHost(this);
            _motionDriver = new MotionTemplateDriver(_motionHost);
            if (_motionTemplateRepository != null)
                _motionDriver.BindRepository(_motionTemplateRepository);
            _motionHitResolver = new MotionHitResolver(_motionHost);
            _templateDelivery = new TemplateDeliveryRuntime(_motionHost);
        }

        void EnsureMotionReady()
        {
          EnsureMotionServices();
            _motionDriver.EnsureMotionReady();
        }

        internal bool TryBeginMotionTemplate(SkillResolution skill, PendingClosing pending)
        {
          EnsureMotionServices();
            return _motionDriver.TryBeginMotionTemplate(skill, pending);
        }

        PositionPlayback PreparePositionPlayback(SkillResolution skill, MotionTemplate template)
        {
          EnsureMotionServices();
            return _motionDriver.PreparePositionPlayback(skill, template);
        }

        float ColliderRadius(Transform body)
        {
          EnsureMotionServices();
            return _motionDriver.ColliderRadius(body);
        }

        internal bool TryBeginBasicStrikeStep()
        {
          EnsureMotionServices();
            return _motionDriver.TryBeginBasicStrikeStep();
        }

        void CancelActiveSkillForDodge()
        {
          EnsureMotionServices();
            _motionDriver.CancelActiveSkillForDodge();
        }

        internal void ArmTemplateDelivery(SkillResolution skill, PendingClosing pending, SkillMotionPlan motion)
        {
          EnsureMotionServices();
            CastSession.DeliverySkill = skill;
            CastSession.DeliveryPending = pending;
            _templateDelivery.ArmTemplateDelivery(skill, pending, motion);
        }

        void TickTemplateDelivery(double worldMs)
        {
          EnsureMotionServices();
            _templateDelivery.TickTemplateDelivery(worldMs);
        }

        internal MotionTemplateCatalog MotionCatalog
        {
            get
            {
              EnsureMotionServices();
                return _motionDriver.Catalog;
            }
        }

        internal bool IsEnemyBody(Transform body)
        {
            if (body == null || body == _player)
                return false;
            if (_ally != null && (body == _ally.transform || body.IsChildOf(_ally.transform)))
                return false;
            return true;
        }

        static bool IsSustained(in SkillResolution skill) => WeaponPassiveRuntime.IsSustained(skill);

        internal float BossBodyRadius()
        {
            if (_boss == null)
                return CombatFallbacks.MotionBossBodyRadiusFallbackM;
            Collider col = _boss.GetComponentInChildren<Collider>();
            if (col == null)
            {
                DesignWarnings.Once(
                    "motion.boss_radius",
                    "Boss gövdesi okunamadı. Vuruş payı yedek 0.6 m.");
                return CombatFallbacks.MotionBossBodyRadiusFallbackM;
            }
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }
    }
}
