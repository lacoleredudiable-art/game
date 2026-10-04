using Dovus.Core.Status;

namespace Dovus.Core.Boss
{
    public readonly struct BossAttackGate
    {
        public BossAttackGate(bool canStart, bool cancelWindup, float phaseSpeed)
        {
            CanStart = canStart;
            CancelWindup = cancelWindup;
            PhaseSpeed = phaseSpeed;
        }

        public bool CanStart { get; }
        /// <summary>Sersemlik hazırlıktaki vuruşu keser. Vuruş anı ve toparlanma kesilmez.</summary>
        public bool CancelWindup { get; }
        /// <summary>1 = normal. Yavaşlatma hareketle aynı çarpan; hazırlık ve toparlanma bu hızda akar.</summary>
        public float PhaseSpeed { get; }
    }
}
