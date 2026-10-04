using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Config;
using Dovus.Game.Skills;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.Editor
{
    public static partial class MixamoAnimatorBind
    {
        static void BuildBossController(ClipSource c)
        {
            AnimationClip idle = c.Pick("idle") ?? c.Any();
            AnimationClip walk = c.Pick("walk") ?? idle;
            AnimationClip slam = c.Pick("slam", "jump attack", "melee thrust", "downward", "attack") ?? idle;
            AnimationClip breath = c.Pick("breath", "cone", "spell cast", "2h magic", "roar") ?? slam;
            AnimationClip roar = c.Pick("roar", "flex", "spell cast") ?? breath;
            AnimationClip stagger = c.Pick("stagger", "hit", "impact", "reaction") ?? idle;
            AnimationClip death = c.Pick("death", "dying") ?? stagger;

            var ac = LoadOrCreate(BossCtrl);
            EnsureParam(ac, "Speed", AnimatorControllerParameterType.Float);
            EnsureParam(ac, BossView.ParamLocoSpeed, AnimatorControllerParameterType.Float, 1f);
            EnsureParam(ac, BossView.ParamActionSpeed, AnimatorControllerParameterType.Float, 1f);

            var sm = ResetBaseLayer(ac);
            var loco = sm.AddState(BossView.StateLocomotion, new Vector3(300, 0, 0));
            // Boss koşmaz: Speed 0 idle, 1 walk. Adım hızı LocoSpeed ile yaklaşma hızına eşlenir.
            float idleScale = c.PickPrimary("idle") != null ? 1f : 0.02f;
            loco.motion = MakeLocomotionTree(ac, "BossLocomotionBT", idle, walk, null, idleScale);
            loco.speedParameterActive = true;
            loco.speedParameter = BossView.ParamLocoSpeed;
            sm.defaultState = loco;

            AddBossAction(sm, BossView.StateSlam, slam, 520, 0, speedParam: true);
            AddBossAction(sm, BossView.StateBreath, breath, 520, 80, speedParam: true);
            AddBossAction(sm, BossView.StateRoar, roar, 520, 160, speedParam: false);
            AddBossAction(sm, BossView.StateStagger, stagger, 520, 240, speedParam: false);
            AddBossAction(sm, BossView.StateDeath, death, 300, 240, speedParam: false, returns: false);

            ClearBaseLayerMask(ac);
            EditorUtility.SetDirty(ac);
        }

        static void AddBossAction(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y,
            bool speedParam, bool returns = true)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            if (speedParam)
            {
                st.speedParameterActive = true;
                st.speedParameter = BossView.ParamActionSpeed;
            }
            if (!returns)
                return;
            var back = st.AddTransition(sm.defaultState);
            back.hasExitTime = true;
            back.exitTime = 0.92f;
            back.duration = 0.2f;
            back.hasFixedDuration = true;
        }

        // --- Ortak yardımcılar ---------------------------------------------------------------
    }
}
