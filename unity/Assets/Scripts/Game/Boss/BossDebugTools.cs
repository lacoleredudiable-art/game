using Dovus.App.Boss;
using Dovus.Core.Boss;
using Dovus.Game.Diagnostics;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Debug: boss faz 2 atlatma. BossDirector'a dokunulmaz (sınıf satır tavanı, ClassSizeRatchetTests).
    /// Kayıp <c>BossVitals</c> hasar yolundan geçer, HUD normal hasarla aynı görür; faz 2 roar'ı bir
    /// sonraki boşta <c>TickIdle</c> üzerinden gelir. Release APK'da (<see cref="DebugConfig.Enabled"/>
    /// false) hiçbir şey yapmaz. Değişim olduysa true.
    /// </summary>
    public static class BossDebugTools
    {
        public static bool SkipToPhase2(BossVitals vitals)
        {
            if (!DebugConfig.Enabled || vitals == null)
                return false;
            if (!vitals.SkipToHpRatio(BossDirectorRules.EnrageHpRatio))
                return false;
            DebugConfig.DevLog($"[DEBUG] boss faz 2 atlatıldı: can {vitals.Hp:0.#}/{vitals.MaxHp:0.#}");
            return true;
        }
    }
}
