using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Tuning;
using Dovus.Game.Composition;
using System;
using UnityEngine;
namespace Dovus.Game.Skills.Execution
{
    public abstract class SkillExecutorController : MonoBehaviour, ISkillExecutor
        {
            protected SkillExecutionContext Context { get; private set; }
            protected bool HasContext { get; private set; }
            float _activationDelayRemainingSec;
    
            public abstract SkillExecutorKind Kind { get; }
    
            /// <summary>Sunum katmanı (HitboxVfxRegistry) bu cast'in gramer planını ve sahibini okur.</summary>
            public MechanicPlan Plan => HasContext ? Context.MechanicPlan : null;
            public Transform CastOwner => HasContext ? Context.Owner : null;
    
            /// <summary>Dünya saati: build menüsü açıkken 0, TimeDirector ölçeğini izler.</summary>
            protected float WorldDeltaSec => Context.Clock != null
                ? (float)(Context.Clock.WorldDeltaMs / SkillsTimeDefaults.SecToMs)
                : Time.deltaTime;
    
            public virtual void Execute(in SkillExecutionContext context)
            {
                Context = context;
                HasContext = true;
                _activationDelayRemainingSec = context.ActivationDelaySec;
            }
    
            /// <summary>Yükselen/gecikmeli-an: görsel dünyada durur, gameplay bu kapıdan sonra başlar.</summary>
            protected bool WaitingForActivation()
            {
                if (_activationDelayRemainingSec <= 0f)
                    return false;
                _activationDelayRemainingSec -= WorldDeltaSec;
                return _activationDelayRemainingSec > 0f;
            }
    
            protected bool IsTarget(Collider collider)
            {
                if (collider == null || Context.Target == null)
                    return false;
                Transform hit = collider.transform;
                return hit == Context.Target || hit.IsChildOf(Context.Target);
            }
    
            protected void Apply(float effectFraction)
            {
                if (effectFraction > 0f)
                    Context.ApplyEffect?.Invoke(effectFraction);
            }
    
            protected void Finish()
            {
                HasContext = false;
                Destroy(gameObject);
            }
        }
    }
