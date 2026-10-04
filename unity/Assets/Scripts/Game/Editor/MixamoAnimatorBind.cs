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
    /// <summary>
    /// Assets/Art/Mixamo/**.fbx → Humanoid AnimatorController → Synty visuals.
    /// Menu: Dovus/Synty/Bind Mixamo Animator
    ///
    /// Klip klasörleri: <c>Mixamo/Player/</c> ve <c>Mixamo/Boss/</c> önce aranır, bulunamayan rol
    /// ortak <c>Mixamo/*.fbx</c> kliplerine düşer. İndirme listeleri: <c>tools/mixamo-jobs/*.json</c>.
    /// Oyuncu state isimleri <see cref="ActorView"/> ve AnimationBridge ile, boss state
    /// isimleri <see cref="BossView"/> ile birebir.
    /// </summary>
    public static partial class MixamoAnimatorBind
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


    }
}
