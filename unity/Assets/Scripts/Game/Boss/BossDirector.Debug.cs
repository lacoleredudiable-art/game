using Dovus.App.Boss;
using Dovus.Game.Diagnostics;

namespace Dovus.Game.Boss
{
    public sealed partial class BossDirector
    {
        /// <summary>
        /// Debug: boss canını öfke eşiğine (<see cref="BossDirectorRules.EnrageHpRatio"/>) indirir; faz 2
        /// roar'ı bir sonraki boşta anda <c>TickIdle</c> üzerinden gelir. Kayıp <c>BossVitals</c> hasar
        /// yolundan geçer, HUD normal hasarla aynı görür. Release APK'da (<see cref="DebugConfig.Enabled"/>
        /// false) hiçbir şey yapmaz. Değişim olduysa true.
        /// </summary>
        public bool DebugSkipToPhase2()
        {
            if (!DebugConfig.Enabled || _bossVitals == null)
                return false;
            if (!_bossVitals.SkipToHpRatio(BossDirectorRules.EnrageHpRatio))
                return false;
            DebugConfig.DevLog($"[DEBUG] boss faz 2 atlatıldı: can {_bossVitals.Hp:0.#}/{_bossVitals.MaxHp:0.#}");
            return true;
        }
    }
}
