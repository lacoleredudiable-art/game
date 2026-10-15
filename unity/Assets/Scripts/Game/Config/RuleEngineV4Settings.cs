using System;
using UnityEngine;

namespace Dovus.Game.Config
{
    /// <summary>kural_motoru_v4 dilim ayarları (varsayılan kapalı).</summary>
    [Serializable]
    public sealed class RuleEngineV4Settings
    {
        /// <summary>Yalnız v4 dilim APK'sının define'ı (AndroidBuilder -dovusV4Slice); repoda tanımlı değil.</summary>
        public const string SliceBuildDefine = "DOVUS_V4_SLICE";

        public static bool IsSliceBuild =>
#if DOVUS_V4_SLICE
            true;
#else
            false;
#endif

        /// <summary>Dilim build'inde sahnedeki serileşmiş değeri ezer; define yokken hiçbir şey yapmaz.</summary>
        public void ApplySliceBuild()
        {
            if (!IsSliceBuild)
                return;
            Enabled = true;
            SliceScene = true;
        }

        [Tooltip("Açıkken skill yürütme RuleEngineV4 komut planına gider; kapalıda eski yol.")]
        public bool Enabled;

        [Tooltip("Boss + 2-3 hafif yaratık + 2 AllyDummy dilim sahnesi.")]
        public bool SliceScene;

        // Kullanılmıyor: minyon yarıçapı world_physics.body_radius_creature_m'den gelir. Serileştirme için alan kaldı.
        public float SliceMinionRadiusM = 0.5f;
        public float SliceMinionHeightM = 1f;
    }
}
