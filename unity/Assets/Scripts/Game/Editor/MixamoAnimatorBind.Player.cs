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
        static void BuildPlayerController(ClipSource c)
        {
            AnimationClip idle = c.Pick("idle") ?? c.Any();
            AnimationClip walk = c.Pick("walk") ?? idle;
            AnimationClip run = c.Pick("run") ?? walk;
            AnimationClip dodge = c.Pick("roll", "dodge", "dash", "diving") ?? run;
            AnimationClip hit = c.Pick("hit", "impact", "reaction") ?? idle;
            AnimationClip death = c.Pick("death", "dying") ?? hit;
            AnimationClip pierce = c.Pick("melee thrust", "thrust", "downward", "attack") ?? idle;
            AnimationClip sweep = c.Pick("melee slash", "horizontal", "slash") ?? pierce;
            AnimationClip slam = c.Pick("ground slam", "melee punch", "punch", "hook", "kick") ?? sweep;
            AnimationClip channel = c.Pick("spell cast", "2h magic", "casting", "spell", "magic") ?? idle;
            AnimationClip guard = c.Pick("block", "guard", "power up") ?? channel;
            AnimationClip shoot = c.Pick("1h magic", "throw", "shoot") ?? pierce;

            // Düz vuruş görsel döngüsü: 3 farklı kılıç klibi varsa sırayla; yoksa eldeki melee kliplere düşer.
            AnimationClip strikeA = c.PickPrimary("strike a", "slash 1", "slash") ?? pierce;
            AnimationClip strikeB = c.PickPrimary("strike b", "slash 2", "attack 2") ?? sweep;
            AnimationClip strikeC = c.PickPrimary("strike c", "slash 3", "attack 3") ?? slam;

            // O-anim(c): yeni hareket anahtarı state'leri (bkz. tools/build-motion-templates.py
            // anim_bridge) — klipler arketip havuzundan (Mixamo/Archetypes), temelde ortak klip yok.
            // Shared_Dodge_Backward/Shared_Dodge_Left henüz hiçbir tools/mixamo-jobs listesinde değil;
            // bulunamazsa state klipsiz kalır (HasState true, motion null — hata değil).
            Dictionary<string, AnimationClip> archetypeClips = MixamoArchetypeBind.CollectAllArchetypeClips();
            archetypeClips.TryGetValue("Shared_Dodge_Backward", out AnimationClip backstep);
            archetypeClips.TryGetValue("Shared_Dodge_Left", out AnimationClip sidestep);
            archetypeClips.TryGetValue("Hammer_JumpAttack", out AnimationClip jumpAttack);
            archetypeClips.TryGetValue("Hammer_Spin", out AnimationClip spin);
            archetypeClips.TryGetValue("Shared_Throw", out AnimationClip throwClip);

            Debug.Log($"[MixamoBind] CastPierce temel klip={pierce?.name ?? "yok"} CastSweep temel klip={sweep?.name ?? "yok"}");

            var ac = LoadOrCreate(PlayerCtrl);
            EnsureParam(ac, "Speed", AnimatorControllerParameterType.Float);
            // O-anim(c): CastChannel/CastGuard döngü (hold) sinyali — ManifestationDirector.TickCastHold
            // her kare bu bool'ları yazar, binder'daki dönüş geçişi bunlar false olmadan tetiklenmez.
            EnsureParam(ac, ActorView.ParamChannelHold, AnimatorControllerParameterType.Bool);
            EnsureParam(ac, ActorView.ParamGuardHold, AnimatorControllerParameterType.Bool);
            EnsureParam(ac, ActorView.ParamStrikeSpeed, AnimatorControllerParameterType.Float, 1f);

            var sm = ResetBaseLayer(ac);
            var loco = sm.AddState("Locomotion", new Vector3(300, 0, 0));
            // 17 Eyl sahip kararı: ortak Fighting Idle fazla oynak → donuk. Oyuncuya özel idle normal hızda.
            float idleScale = c.PickPrimary("idle") != null ? 1f : 0.02f;
            // Eşikler kliplerin ölçülmüş zemin hızı: ActorView.SetLocomotion gerçek hızı model birimiyle
            // verir, ayak yere bastığı yerde kalır. Ölçüm olmazsa eski normalize eşiklere düşer.
            float walkSpeed = MeasureGroundSpeed(walk);
            float runSpeed = run != walk ? MeasureGroundSpeed(run) : 0f;
            bool measured = walkSpeed > 0.01f && runSpeed > walkSpeed;
            loco.motion = measured
                ? MakeLocomotionTree(ac, "LocomotionBT", idle, walk, run, idleScale, walkSpeed, runSpeed)
                : MakeLocomotionTree(ac, "LocomotionBT", idle, walk, run, idleScale);
            if (measured)
            {
                EnsureParam(ac, ActorView.ParamLocoRunSpeed, AnimatorControllerParameterType.Float, runSpeed);
                EnsureParam(ac, ActorView.ParamLocoPlayback, AnimatorControllerParameterType.Float, 1f);
                loco.speedParameterActive = true;
                loco.speedParameter = ActorView.ParamLocoPlayback;
            }
            else
            {
                RemoveParam(ac, ActorView.ParamLocoRunSpeed);
                RemoveParam(ac, ActorView.ParamLocoPlayback);
            }
            loco.iKOnFeet = true;
            Debug.Log($"[MixamoBind] locomotion zemin hızı (model/sn): yürüme={walkSpeed:F2} koşu={runSpeed:F2} ölçüldü={measured}");
            sm.defaultState = loco;

            AddActionState(sm, "Dodge", dodge, 300, 80, 0.85f, ActionReturnSec);
            AddActionState(sm, "Hit", hit, 300, 160, 0.8f, ActionReturnSec);
            AddActionState(sm, "CastPierce", pierce, 520, 0, 0.8f, ActionReturnSec);
            AddActionState(sm, "CastSweep", sweep, 520, 80, 0.8f, ActionReturnSec);
            AddActionState(sm, "CastSlam", slam, 520, 160, 0.8f, ActionReturnSec);
            // Hold: ChannelHold/GuardHold true iken dönüş geçişi kilitli (bkz. AddActionState holdParam).
            AddActionState(sm, "CastChannel", channel, 520, 240, 0.8f, HoldReturnSec, ActorView.ParamChannelHold);
            AddActionState(sm, "CastGuard", guard, 520, 320, 0.8f, HoldReturnSec, ActorView.ParamGuardHold);
            AddActionState(sm, "CastShoot", shoot, 520, 400, 0.8f, ActionReturnSec);
            AddActionState(sm, "Death", death, 300, 240, -1f, 0f);
            AddActionState(sm, "BasicStrike", strikeA, 740, 0, 0.78f, ActionReturnSec,
                speedParam: ActorView.ParamStrikeSpeed);
            AddActionState(sm, "BasicStrikeB", strikeB, 740, 80, 0.78f, ActionReturnSec,
                speedParam: ActorView.ParamStrikeSpeed);
            AddActionState(sm, "BasicStrikeC", strikeC, 740, 160, 0.78f, ActionReturnSec,
                speedParam: ActorView.ParamStrikeSpeed);

            // O-anim(c): hareket anahtarı state'leri — Dodge gibi tek gövde, Upper kopyası yok
            // (motion template zaten gövdeyi taşır, bkz. AGENTS "tek hareket sistemi").
            AddActionState(sm, "Backstep", backstep, 300, 400, 0.85f, ActionReturnSec);
            AddActionState(sm, "Sidestep", sidestep, 300, 480, 0.85f, ActionReturnSec);
            AddMirroredState(sm, "SidestepRight", sidestep, 300, 560, 0.85f, ActionReturnSec);
            AddActionState(sm, "JumpAttack", jumpAttack, 300, 640, 0.85f, ActionReturnSec);
            AddActionState(sm, "Spin", spin, 300, 720, 0.85f, ActionReturnSec);
            AddActionState(sm, "Throw", throwClip, 300, 800, 0.85f, ActionReturnSec);

            BuildUpperBodyLayer(ac, pierce, sweep, slam, channel, guard, shoot, strikeA, strikeB, strikeC);
            EditorUtility.SetDirty(ac);
        }

        /// <summary>
        /// Üst gövde katmanı: hareket ederken cast edilen skill'de bacaklar koşmaya devam eder.
        /// ActorView yürürken aksiyonu bu katmana yönlendirir (state adı "Upper" + ad).
        /// </summary>
        static void BuildUpperBodyLayer(AnimatorController ac, params AnimationClip[] clips)
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMask);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, UpperBodyMask);
            }

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                var part = (AvatarMaskBodyPart)i;
                bool upper = part is AvatarMaskBodyPart.Body or AvatarMaskBodyPart.Head
                    or AvatarMaskBodyPart.LeftArm or AvatarMaskBodyPart.RightArm
                    or AvatarMaskBodyPart.LeftFingers or AvatarMaskBodyPart.RightFingers
                    or AvatarMaskBodyPart.LeftHandIK or AvatarMaskBodyPart.RightHandIK;
                mask.SetHumanoidBodyPartActive(part, upper);
            }
            EditorUtility.SetDirty(mask);

            var layers = ac.layers.ToList();
            layers.RemoveAll(l => l.name == "UpperBody");
            var sm = new AnimatorStateMachine { name = "UpperBody", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(sm, ac);
            var empty = sm.AddState("Empty", new Vector3(300, 0, 0));
            sm.defaultState = empty;

            string[] names =
            {
                "UpperCastPierce", "UpperCastSweep", "UpperCastSlam", "UpperCastChannel", "UpperCastGuard",
                "UpperCastShoot", "UpperBasicStrike", "UpperBasicStrikeB", "UpperBasicStrikeC",
            };
            for (int i = 0; i < names.Length && i < clips.Length; i++)
            {
                var st = sm.AddState(names[i], new Vector3(520, i * 80, 0));
                st.motion = clips[i];
                if (names[i] is "UpperBasicStrike" or "UpperBasicStrikeB" or "UpperBasicStrikeC")
                {
                    st.speedParameterActive = true;
                    st.speedParameter = ActorView.ParamStrikeSpeed;
                }
                var back = st.AddTransition(empty);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                // Hold karşılığı üst gövde: CastChannel/CastGuard taşırken de aynı kilit uygulanır.
                string holdParam = names[i] == "UpperCastChannel" ? ActorView.ParamChannelHold
                    : names[i] == "UpperCastGuard" ? ActorView.ParamGuardHold
                    : null;
                back.duration = holdParam != null ? HoldReturnSec : ActionReturnSec;
                back.hasFixedDuration = true;
                if (holdParam != null)
                    back.AddCondition(AnimatorConditionMode.IfNot, 0, holdParam);
            }

            layers.Add(new AnimatorControllerLayer
            {
                name = "UpperBody",
                stateMachine = sm,
                avatarMask = mask,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
            });
            ac.layers = layers.ToArray();
        }

        // --- Boss ----------------------------------------------------------------------------

    }
}
