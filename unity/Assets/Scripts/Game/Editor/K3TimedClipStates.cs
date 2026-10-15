using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Game.Actors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// k3 controller: süreye oturtulan state'ler. State hızı = klip boyu (sn), çarpan parametresi
    /// = 1 / istenen süre (<see cref="DodgeClipPlayback"/>), yani klip oyunun verdiği sürede tam biter:
    /// takla = kayma, kalkış = toparlanma, geri sıçrama = sıçrama hareketi.
    /// </summary>
    static class K3TimedClipStates
    {
        static readonly (string State, string Clip, string SpeedParam)[] TimedStates =
        {
            (DodgeClipPlayback.RollState, DodgeClipPlayback.RollClip, DodgeClipPlayback.RollSpeedParam),
            (DodgeClipPlayback.RiseState, DodgeClipPlayback.RiseClip, DodgeClipPlayback.RiseSpeedParam),
            (DodgeClipPlayback.BackstepClip, DodgeClipPlayback.BackstepClip, DodgeClipPlayback.RollSpeedParam),
        };

        /// <summary>
        /// Var olan controller'ı günceller: her klibe aynı adlı state (skill klibi adla oynatılır),
        /// süreli state'ler ve Dodge → DodgeRise (kesintisiz) → Locomotion zinciri.
        /// </summary>
        internal static void Ensure(AnimatorController ctrl, Dictionary<string, AnimationClip> clips, string loopPrefix)
        {
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState loco = sm.defaultState;
            foreach (AnimationClip clip in clips.Values.Where(c => !c.name.StartsWith(loopPrefix, StringComparison.Ordinal)))
            {
                if (Find(sm, clip.name) == null)
                    K3AssetSetup.AddAction(sm, clip.name, clip, loco);
            }

            foreach (string p in new[] { DodgeClipPlayback.RollSpeedParam, DodgeClipPlayback.RiseSpeedParam })
            {
                if (!ctrl.parameters.Any(x => x.name == p))
                    ctrl.AddParameter(new AnimatorControllerParameter
                    {
                        name = p,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 1f,
                    });
            }

            foreach ((string stateName, string clipName, string param) in TimedStates)
            {
                AnimationClip clip = clips[clipName];
                AnimatorState s = Find(sm, stateName) ?? K3AssetSetup.AddAction(sm, stateName, clip, loco);
                s.motion = clip;
                s.speed = clip.length;
                s.speedParameter = param;
                s.speedParameterActive = true;
            }

            AnimatorState roll = Find(sm, DodgeClipPlayback.RollState);
            foreach (AnimatorStateTransition t in roll.transitions.ToArray())
                roll.RemoveTransition(t);
            AnimatorStateTransition toRise = roll.AddTransition(Find(sm, DodgeClipPlayback.RiseState));
            toRise.hasExitTime = true;
            toRise.exitTime = 1f;
            toRise.duration = 0f;
            EditorUtility.SetDirty(ctrl);
        }

        static AnimatorState Find(AnimatorStateMachine sm, string name) =>
            sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
    }
}
