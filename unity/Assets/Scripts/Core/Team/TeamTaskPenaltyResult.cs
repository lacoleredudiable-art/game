using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Başarısız görevin cezası (KE-9.3, A2 kararı: ceza zorluğa göre).</summary>
    public readonly struct TeamTaskPenaltyResult
    {
        public TeamTaskPenaltyResult(bool wipe, float teamDamageFractionOfMax, float bossHpResetRatio, bool exposedDie, string message)
        {
            Wipe = wipe;
            TeamDamageFractionOfMax = teamDamageFractionOfMax;
            BossHpResetRatio = bossHpResetRatio;
            ExposedDie = exposedDie;
            Message = message;
        }

        /// <summary>Tur yanar: bütün takım ölür (Cehennem).</summary>
        public bool Wipe { get; }

        /// <summary>Bütün takımın azami canından yiyeceği hasar oranı (0 = yok).</summary>
        public float TeamDamageFractionOfMax { get; }

        /// <summary>&gt;0 ise boss canı bu orana döner (Zor, Kor Mührü: 0,25). 0 = dokunma.</summary>
        public float BossHpResetRatio { get; }

        /// <summary>Kül Yağmuru'nda şerit dışındakiler ölür mü (Antrenman'da can 1'de durur, KE-6.13).</summary>
        public bool ExposedDie { get; }

        public string Message { get; }
    }
}
