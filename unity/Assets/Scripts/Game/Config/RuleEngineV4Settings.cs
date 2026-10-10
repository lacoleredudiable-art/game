using System;
using UnityEngine;

namespace Dovus.Game.Config
{
    /// <summary>kural_motoru_v4 dilim ayarları (varsayılan kapalı).</summary>
    [Serializable]
    public sealed class RuleEngineV4Settings
    {
        [Tooltip("Açıkken skill yürütme RuleEngineV4 komut planına gider; kapalıda eski yol.")]
        public bool Enabled;

        [Tooltip("Boss + 2-3 hafif yaratık + 2 AllyDummy dilim sahnesi.")]
        public bool SliceScene;

        public float SliceMinionRadiusM = 0.5f;
        public float SliceMinionHeightM = 1f;
        public int SliceMinionMaxHp = 200;
    }
}
