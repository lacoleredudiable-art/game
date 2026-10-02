using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// Assets/Art/Mixamo/**.fbx → Humanoid AnimatorController → Synty visuals.
    /// Menu: Dovus/Synty/Bind Mixamo Animator
    ///
    /// Klip klasörleri: <c>Mixamo/Player/</c> ve <c>Mixamo/Boss/</c> önce aranır, bulunamayan rol
    /// ortak <c>Mixamo/*.fbx</c> kliplerine düşer. İndirme listeleri: <c>tools/mixamo-jobs/*.json</c>.
    /// Oyuncu state isimleri <see cref="ActorVisual"/> ve AnimationBridge ile, boss state
    /// isimleri <see cref="BossVisual"/> ile birebir.
    /// </summary>
    public static class MixamoAnimatorBind
    {
        const string MixamoDir = "Assets/Art/Mixamo";
        const string PlayerDir = MixamoDir + "/Player";
        const string BossDir = MixamoDir + "/Boss";
        const string OutDir = "Assets/Art/Mixamo/Animators";
        internal const string PlayerCtrl = OutDir + "/Player_Synty.controller";
        const string BossCtrl = OutDir + "/Boss_Synty.controller";
        const string UpperBodyMask = OutDir + "/UpperBody.mask";
        const string PlayerVisual = "Assets/Art/Synty/Prefabs/PlayerVisual_Synty.prefab";
        const string BossVisualPrefab = "Assets/Art/Synty/Prefabs/BossVisual_Synty.prefab";

        // O-anim(c): "sakin" his — aksiyondan dönüş geçişi. Hold durumlarında (CastChannel/
        // CastGuard) ayrıca HoldReturnSec kullanılır ("~0,2 sn döngüden lokomosyona").
        const float ActionReturnSec = 0.15f;
        const float HoldReturnSec = 0.2f;

        [MenuItem("Dovus/Synty/Bind Mixamo Animator")]
        public static void Bind()
        {
            EnsureFolder(OutDir);
            ForceHumanoidOnMixamoFbxs();

            var shared = CollectClips(MixamoDir, recursive: false);
            var player = CollectClips(PlayerDir, recursive: true);
            var boss = CollectClips(BossDir, recursive: true);
            if (shared.Count == 0 && player.Count == 0 && boss.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Mixamo yok",
                    "Assets/Art/Mixamo/ altına FBX koy (tools/mixamo-jobs listesi).\n" +
                    "Format: FBX for Unity, Without Skin, 30 fps.",
                    "Tamam");
                return;
            }

            BuildPlayerController(new ClipSource(player, shared));
            AlignPlayerLocoFeet();
            BuildBossController(new ClipSource(boss, shared));
            string archetypeLog = MixamoArchetypeBind.Build();

            AssignController(PlayerVisual, PlayerCtrl);
            AssignController(BossVisualPrefab, BossCtrl);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MixamoBind] OK — shared={shared.Count} player={player.Count} boss={boss.Count} clip.");
            if (!string.IsNullOrEmpty(archetypeLog))
                Debug.Log("[MixamoBind] arketip override:\n" + archetypeLog);
        }

        /// <summary>Rol klasörü önce, ortak klasör sonra.</summary>
        sealed class ClipSource
        {
            readonly Dictionary<string, AnimationClip> _primary;
            readonly Dictionary<string, AnimationClip> _fallback;

            public ClipSource(Dictionary<string, AnimationClip> primary, Dictionary<string, AnimationClip> fallback)
            {
                _primary = primary;
                _fallback = fallback;
            }

            public AnimationClip Pick(params string[] needles) =>
                MixamoAnimatorBind.Pick(_primary, needles) ?? MixamoAnimatorBind.Pick(_fallback, needles);

            public AnimationClip PickPrimary(params string[] needles) => MixamoAnimatorBind.Pick(_primary, needles);

            public AnimationClip Any() =>
                _primary.Values.FirstOrDefault() ?? _fallback.Values.FirstOrDefault();
        }

        // --- Oyuncu --------------------------------------------------------------------------

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
            EnsureParam(ac, ActorVisual.ParamChannelHold, AnimatorControllerParameterType.Bool);
            EnsureParam(ac, ActorVisual.ParamGuardHold, AnimatorControllerParameterType.Bool);
            EnsureParam(ac, ActorVisual.ParamStrikeSpeed, AnimatorControllerParameterType.Float, 1f);

            var sm = ResetBaseLayer(ac);
            var loco = sm.AddState("Locomotion", new Vector3(300, 0, 0));
            // 17 Eyl sahip kararı: ortak Fighting Idle fazla oynak → donuk. Oyuncuya özel idle normal hızda.
            float idleScale = c.PickPrimary("idle") != null ? 1f : 0.02f;
            // Eşikler kliplerin ölçülmüş zemin hızı: ActorVisual.SetLocomotion gerçek hızı model birimiyle
            // verir, ayak yere bastığı yerde kalır. Ölçüm olmazsa eski normalize eşiklere düşer.
            float walkSpeed = MeasureGroundSpeed(walk);
            float runSpeed = run != walk ? MeasureGroundSpeed(run) : 0f;
            bool measured = walkSpeed > 0.01f && runSpeed > walkSpeed;
            loco.motion = measured
                ? MakeLocomotionTree(ac, "LocomotionBT", idle, walk, run, idleScale, walkSpeed, runSpeed)
                : MakeLocomotionTree(ac, "LocomotionBT", idle, walk, run, idleScale);
            if (measured)
            {
                EnsureParam(ac, ActorVisual.ParamLocoRunSpeed, AnimatorControllerParameterType.Float, runSpeed);
                EnsureParam(ac, ActorVisual.ParamLocoPlayback, AnimatorControllerParameterType.Float, 1f);
                loco.speedParameterActive = true;
                loco.speedParameter = ActorVisual.ParamLocoPlayback;
            }
            else
            {
                RemoveParam(ac, ActorVisual.ParamLocoRunSpeed);
                RemoveParam(ac, ActorVisual.ParamLocoPlayback);
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
            AddActionState(sm, "CastChannel", channel, 520, 240, 0.8f, HoldReturnSec, ActorVisual.ParamChannelHold);
            AddActionState(sm, "CastGuard", guard, 520, 320, 0.8f, HoldReturnSec, ActorVisual.ParamGuardHold);
            AddActionState(sm, "CastShoot", shoot, 520, 400, 0.8f, ActionReturnSec);
            AddActionState(sm, "Death", death, 300, 240, -1f, 0f);
            AddActionState(sm, "BasicStrike", strikeA, 740, 0, 0.78f, ActionReturnSec,
                speedParam: ActorVisual.ParamStrikeSpeed);
            AddActionState(sm, "BasicStrikeB", strikeB, 740, 80, 0.78f, ActionReturnSec,
                speedParam: ActorVisual.ParamStrikeSpeed);
            AddActionState(sm, "BasicStrikeC", strikeC, 740, 160, 0.78f, ActionReturnSec,
                speedParam: ActorVisual.ParamStrikeSpeed);

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
        /// ActorVisual yürürken aksiyonu bu katmana yönlendirir (state adı "Upper" + ad).
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
                    st.speedParameter = ActorVisual.ParamStrikeSpeed;
                }
                var back = st.AddTransition(empty);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                // Hold karşılığı üst gövde: CastChannel/CastGuard taşırken de aynı kilit uygulanır.
                string holdParam = names[i] == "UpperCastChannel" ? ActorVisual.ParamChannelHold
                    : names[i] == "UpperCastGuard" ? ActorVisual.ParamGuardHold
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
            EnsureParam(ac, BossVisual.ParamLocoSpeed, AnimatorControllerParameterType.Float, 1f);
            EnsureParam(ac, BossVisual.ParamActionSpeed, AnimatorControllerParameterType.Float, 1f);

            var sm = ResetBaseLayer(ac);
            var loco = sm.AddState(BossVisual.StateLocomotion, new Vector3(300, 0, 0));
            // Boss koşmaz: Speed 0 idle, 1 walk. Adım hızı LocoSpeed ile yaklaşma hızına eşlenir.
            float idleScale = c.PickPrimary("idle") != null ? 1f : 0.02f;
            loco.motion = MakeLocomotionTree(ac, "BossLocomotionBT", idle, walk, null, idleScale);
            loco.speedParameterActive = true;
            loco.speedParameter = BossVisual.ParamLocoSpeed;
            sm.defaultState = loco;

            AddBossAction(sm, BossVisual.StateSlam, slam, 520, 0, speedParam: true);
            AddBossAction(sm, BossVisual.StateBreath, breath, 520, 80, speedParam: true);
            AddBossAction(sm, BossVisual.StateRoar, roar, 520, 160, speedParam: false);
            AddBossAction(sm, BossVisual.StateStagger, stagger, 520, 240, speedParam: false);
            AddBossAction(sm, BossVisual.StateDeath, death, 300, 240, speedParam: false, returns: false);

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
                st.speedParameter = BossVisual.ParamActionSpeed;
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

        static AnimatorController LoadOrCreate(string path)
        {
            // GUID koru — DeleteAsset+Create referansları kırıp T-pose'a düşürüyordu.
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            return ac != null ? ac : AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static AnimatorStateMachine ResetBaseLayer(AnimatorController ac)
        {
            var sm = ac.layers[0].stateMachine;
            foreach (var s in sm.states)
                sm.RemoveState(s.state);
            if (ac.layers.Length > 1)
                ac.layers = new[] { ac.layers[0] };
            // Yeniden kurulumda eski blend tree / üst gövde state machine alt-asset'leri birikmesin.
            foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(ac)))
            {
                if (sub is BlendTree bt)
                    Object.DestroyImmediate(bt, true);
                else if (sub is AnimatorStateMachine other && other != sm && other.name == "UpperBody")
                {
                    foreach (var s in other.states)
                        other.RemoveState(s.state);
                    Object.DestroyImmediate(other, true);
                }
            }
            return sm;
        }

        static void ClearBaseLayerMask(AnimatorController ac)
        {
            var layers = ac.layers;
            layers[0].avatarMask = null;
            ac.layers = layers;
        }

        static BlendTree MakeLocomotionTree(AnimatorController ac, string name, AnimationClip idle, AnimationClip walk,
            AnimationClip run, float idleTimeScale, float walkThreshold, float runThreshold)
        {
            var tree = new BlendTree
            {
                name = name,
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, walkThreshold);
            tree.AddChild(run, runThreshold);
            var children = tree.children;
            children[0].timeScale = idleTimeScale;
            tree.children = children;
            return tree;
        }

        /// <summary>
        /// Klibin zemin hızı (model birimi/sn): ayağın yere bastığı karelerde gövdeye göre yatay kayma
        /// hızının medyanı. In-place Mixamo kliplerinde kök hızı olmadığından tek güvenilir kaynak bu.
        /// Yön bağımsız: SampleAnimation kök dönüş ofsetini uygulamaz.
        /// </summary>
        static float MeasureGroundSpeed(AnimationClip clip)
        {
            const int samples = 120;
            const float groundBand = 0.025f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisual);
            if (clip == null || prefab == null)
                return 0f;
            var go = (GameObject)Object.Instantiate(prefab);
            try
            {
                var an = go.GetComponentInChildren<Animator>(true);
                if (an == null || !an.isHuman)
                    return 0f;
                Transform root = an.transform;
                Transform[] feet =
                {
                    an.GetBoneTransform(HumanBodyBones.LeftToes) ?? an.GetBoneTransform(HumanBodyBones.LeftFoot),
                    an.GetBoneTransform(HumanBodyBones.RightToes) ?? an.GetBoneTransform(HumanBodyBones.RightFoot),
                };
                if (feet[0] == null || feet[1] == null)
                    return 0f;
                float dt = clip.length / samples;
                var y = new float[2, samples + 1];
                var xz = new Vector2[2, samples + 1];
                for (int i = 0; i <= samples; i++)
                {
                    clip.SampleAnimation(an.gameObject, dt * i);
                    for (int f = 0; f < 2; f++)
                    {
                        Vector3 p = root.InverseTransformPoint(feet[f].position);
                        y[f, i] = p.y;
                        xz[f, i] = new Vector2(p.x, p.z);
                    }
                }
                var speeds = new List<float>();
                for (int f = 0; f < 2; f++)
                {
                    float min = float.MaxValue;
                    for (int i = 0; i <= samples; i++)
                        min = Mathf.Min(min, y[f, i]);
                    for (int i = 0; i < samples; i++)
                    {
                        if (y[f, i] < min + groundBand)
                            speeds.Add((xz[f, i + 1] - xz[f, i]).magnitude / dt);
                    }
                }
                if (speeds.Count == 0)
                    return 0f;
                speeds.Sort();
                return Mathf.Max(0f, speeds[speeds.Count / 2]);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Yürüme/koşu/duruş kliplerinde ayak uçlarının ortalama yönünü hareket yönüne (kök ileri)
        /// hizalar: gerçek Animator'la ölçülen sapma klibin rotationOffset'ine eklenir. Burulmuş
        /// gövdeli balta klibinde koşarken sol ayak 60-70° yana dönüyordu (29 Eyl sahip bildirimi).
        /// </summary>
        static void AlignPlayerLocoFeet()
        {
            const float toleranceDeg = 1f;
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerCtrl);
            var tree = ac != null ? ac.layers[0].stateMachine.defaultState?.motion as BlendTree : null;
            if (tree == null)
                return;
            string log = "";
            foreach (ChildMotion child in tree.children)
            {
                var clip = child.motion as AnimationClip;
                string path = clip != null ? AssetDatabase.GetAssetPath(clip) : null;
                if (path == null || !path.StartsWith(PlayerDir + "/", System.StringComparison.Ordinal))
                    continue;
                float yaw = MeasureMeanFootYaw(ac, child.threshold, clip.length);
                if (float.IsNaN(yaw))
                    continue;
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                var clips = imp != null ? imp.clipAnimations : null;
                if (clips == null || clips.Length != 1)
                    continue;
                if (Mathf.Abs(yaw) > toleranceDeg)
                {
                    clips[0].rotationOffset = Mathf.DeltaAngle(0f, clips[0].rotationOffset + yaw);
                    imp.clipAnimations = clips;
                    imp.SaveAndReimport();
                }
                log += $" {clip.name}: sapma={yaw:F0}° ofset={clips[0].rotationOffset:F0}°";
            }
            Debug.Log("[MixamoBind] ayak hizası:" + log);
        }

        /// <summary>İki ayağın (parmak − bilek) yatay yönünün kök ileriye göre ortalama açısı; NaN = ölçülemedi.</summary>
        static float MeasureMeanFootYaw(AnimatorController ac, float speedParam, float clipLength)
        {
            const float stepSec = 1f / 60f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisual);
            if (prefab == null)
                return float.NaN;
            var go = (GameObject)Object.Instantiate(prefab);
            try
            {
                var an = go.GetComponentInChildren<Animator>(true);
                if (an == null || !an.isHuman)
                    return float.NaN;
                an.runtimeAnimatorController = ac;
                Transform root = an.transform;
                Transform lf = an.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rf = an.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform lt = an.GetBoneTransform(HumanBodyBones.LeftToes);
                Transform rt = an.GetBoneTransform(HumanBodyBones.RightToes);
                if (lf == null || rf == null || lt == null || rt == null)
                    return float.NaN;
                an.Rebind();
                an.SetFloat("Speed", speedParam);
                an.Update(0f);
                int steps = Mathf.Max(30, Mathf.CeilToInt(clipLength / stepSec));
                float sum = 0f;
                for (int i = 0; i < steps; i++)
                {
                    an.Update(stepSec);
                    Vector3 l = lt.position - lf.position;
                    Vector3 r = rt.position - rf.position;
                    l.y = 0f;
                    r.y = 0f;
                    sum += Vector3.SignedAngle(root.forward, l, Vector3.up)
                        + Vector3.SignedAngle(root.forward, r, Vector3.up);
                }
                return sum / (steps * 2f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static void RemoveParam(AnimatorController ac, string name)
        {
            var ps = ac.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name == name)
                {
                    ac.RemoveParameter(i);
                    return;
                }
            }
        }

        static BlendTree MakeLocomotionTree(AnimatorController ac, string name, AnimationClip idle, AnimationClip walk, AnimationClip run,
            float idleTimeScale)
        {
            var tree = new BlendTree
            {
                name = name,
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(idle, 0f);
            if (run != null)
            {
                // Tam çubuk = koşu; ölü bölge üstü ~0.4 = yürüme (PrototypeTuning.MinStickSpeedFrac).
                tree.AddChild(walk, 0.4f);
                tree.AddChild(run, 1f);
            }
            else
            {
                tree.AddChild(walk, 1f);
            }
            var children = tree.children;
            children[0].timeScale = idleTimeScale;
            tree.children = children;
            return tree;
        }

        static void AddActionState(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y,
            float exitTime, float blendSec, string holdParam = null, string speedParam = null)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            if (!string.IsNullOrEmpty(speedParam))
            {
                st.speedParameterActive = true;
                st.speedParameter = speedParam;
            }
            if (exitTime <= 0f)
                return;
            var toLoco = st.AddTransition(sm.defaultState);
            toLoco.hasExitTime = true;
            toLoco.exitTime = exitTime;
            toLoco.duration = blendSec;
            toLoco.hasFixedDuration = true;
            // Hold sinyali true iken dönüşü engeller: klip Loop Time olduğundan her turda tekrar
            // dener, sinyal bitince (false) ilk uygun turda ~blendSec'le lokomosyona döner.
            if (!string.IsNullOrEmpty(holdParam))
                toLoco.AddCondition(AnimatorConditionMode.IfNot, 0, holdParam);
        }

        /// <summary>
        /// O-anim(c): sağ yön klibi yok — humanoid mirror ile aynı sol klip ters oynar
        /// (<see cref="ActorVisual.DriveMotion"/> blend.Strafe &gt; 0'da bu state'i seçer).
        /// </summary>
        static void AddMirroredState(AnimatorStateMachine sm, string name, AnimationClip clip, float x, float y,
            float exitTime, float blendSec)
        {
            var st = sm.AddState(name, new Vector3(x, y, 0));
            st.motion = clip;
            st.mirrorParameterActive = false;
            st.mirror = true;
            var toLoco = st.AddTransition(sm.defaultState);
            toLoco.hasExitTime = true;
            toLoco.exitTime = exitTime;
            toLoco.duration = blendSec;
            toLoco.hasFixedDuration = true;
        }

        static void EnsureParam(AnimatorController ac, string name, AnimatorControllerParameterType type, float defaultFloat = 0f)
        {
            var ps = ac.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name != name)
                    continue;
                if (ps[i].type == type)
                {
                    ps[i].defaultFloat = defaultFloat;
                    ac.parameters = ps;
                    return;
                }
                ac.RemoveParameter(i);
                break;
            }
            ac.AddParameter(new AnimatorControllerParameter { name = name, type = type, defaultFloat = defaultFloat });
        }

        /// <summary>O-anim(c): CastChannel/CastGuard hold klipleri — döngü isteyen tek seferlik olmayan klipler.</summary>
        static bool IsHoldClipName(string fileName) =>
            fileName is "Player_Block_Hold" or "SS_Block" or "Fist_Block"
                or "Player_Spell_Cast" or "Caster_2H_Cast" or "Bow_Draw";

        static void ForceHumanoidOnMixamoFbxs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { MixamoDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null)
                    continue;
                bool dirty = false;
                if (imp.animationType != ModelImporterAnimationType.Human)
                {
                    imp.animationType = ModelImporterAnimationType.Human;
                    dirty = true;
                }
                if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    dirty = true;
                }
                if (!imp.importAnimation)
                {
                    imp.importAnimation = true;
                    dirty = true;
                }
                // Döngü klipleri (idle/walk/run) loop. Tek klipli Mixamo FBX'te klip adı "mixamo.com"
                // gelir; runtime (BossVisual/ActorVisual) klibi adıyla bulabilsin diye dosya adı verilir.
                string fileName = Path.GetFileNameWithoutExtension(path);
                string lower = fileName.ToLowerInvariant();
                // O-anim(c): basılı/kanal tutuş klipleri de döngü — sürdürülen cast/kalkan sinyali
                // kesilene kadar Animator'da döner (bkz. ActorVisual.SetHoldFlags).
                bool loop = lower.Contains("idle") || lower.Contains("walk") || lower.Contains("run")
                    || IsHoldClipName(fileName);
                // Oyuncu locomotion'ı yerinde oynar: kök dönüşü (gövde yönüne göre), yüksekliği ve XZ'si
                // poza gömülür; yön farkını AlignPlayerLocoFeet ölçüp rotationOffset'e yazar. Bu Mixamo
                // dosyalarında "Original" kök gövdeye göre ~42° dönük (duruşta bile ayaklar yana bakıyor).
                // Boss hariç: BossVisual walk.averageSpeed okur.
                bool playerLoco = loop && path.StartsWith(PlayerDir + "/", System.StringComparison.Ordinal);
                var clips = imp.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = imp.defaultClipAnimations;
                bool clipsDirty = false;
                for (int i = 0; i < clips.Length; i++)
                {
                    if (loop && !clips[i].loopTime)
                    {
                        clips[i].loopTime = true;
                        clipsDirty = true;
                    }
                    if (playerLoco && !(clips[i].lockRootRotation && !clips[i].keepOriginalOrientation
                            && clips[i].lockRootHeightY && clips[i].keepOriginalPositionY
                            && clips[i].lockRootPositionXZ && clips[i].keepOriginalPositionXZ))
                    {
                        clips[i].lockRootRotation = true;
                        clips[i].keepOriginalOrientation = false;
                        clips[i].lockRootHeightY = true;
                        clips[i].keepOriginalPositionY = true;
                        clips[i].heightFromFeet = false;
                        clips[i].lockRootPositionXZ = true;
                        clips[i].keepOriginalPositionXZ = true;
                        clipsDirty = true;
                    }
                    if (clips.Length == 1 && clips[i].name != fileName)
                    {
                        clips[i].name = fileName;
                        clipsDirty = true;
                    }
                }
                if (clipsDirty)
                {
                    imp.clipAnimations = clips;
                    dirty = true;
                }
                if (dirty)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static Dictionary<string, AnimationClip> CollectClips(string dir, bool recursive)
        {
            var map = new Dictionary<string, AnimationClip>();
            if (!AssetDatabase.IsValidFolder(dir))
                return map;
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Animators/"))
                    continue;
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!recursive && folder != dir)
                    continue;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (o is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                        continue;
                    string key = Normalize(Path.GetFileNameWithoutExtension(path) + " " + clip.name);
                    if (!map.ContainsKey(key))
                        map[key] = clip;
                }
            }
            return map;
        }

        static AnimationClip Pick(Dictionary<string, AnimationClip> clips, params string[] needles)
        {
            // İğne sırası önceliktir: ilk iğneye uyan klip, sonrakilere uyanlardan önce gelir.
            foreach (string n in needles)
            {
                foreach (var kv in clips)
                {
                    if (kv.Key.Contains(n))
                        return kv.Value;
                }
            }
            return null;
        }

        static string Normalize(string s) =>
            (s ?? "").ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');

        static void AssignController(string prefabPath, string ctrlPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning("[MixamoBind] prefab yok: " + prefabPath);
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            // Nested Synty chr: property override Instantiate'te düşebiliyor → unpack.
            var nested = new List<GameObject>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject != root && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    nested.Add(t.gameObject);
            }
            foreach (var go in nested)
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var anim = root.GetComponentInChildren<Animator>(true);
            if (anim == null)
            {
                Debug.LogWarning("[MixamoBind] Animator yok: " + prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }
            anim.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
