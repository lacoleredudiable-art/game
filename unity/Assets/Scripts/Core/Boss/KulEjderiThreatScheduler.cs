using System;
using System.Collections.Generic;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// KE-4.1 küçük tehdit ritmi: 3-4 s'de bir (öfkede 2 s), aynı anda 1 + ⌊(oyuncu - 1) / 2⌋ tane; büyük duyurulu
    /// saldırı ya da takım görevi sürerken durur. KE-4.5 (en uzun tehditsiz süre 8 s)
    /// yapıyla sağlanır: ardışık iki tehdit en çok 4 s arayla gelir, ön işaretler bu aralığı doldurur.
    /// </summary>
    public sealed class KulEjderiThreatScheduler
    {
        readonly Random _rng;
        bool _started;
        double _nextAtMs;

        public KulEjderiThreatScheduler(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>Dövüş başlayınca çağrılır (uyanış bitişi).</summary>
        public void Start(double worldMs)
        {
            _started = true;
            _nextAtMs = worldMs + Interval(false);
        }

        public void Stop() => _started = false;

        /// <summary>KE-4.1: oyuncu sayısına göre aynı anda gelen tehdit sayısı (1-2 kişi: 1, 3-4: 2, 5: 3).</summary>
        public static int ConcurrentCount(int playerCount) => 1 + (Math.Max(1, playerCount) - 1) / 2;

        /// <param name="suppressed">Duyurulu büyük saldırı ya da takım görevi sürüyor.</param>
        /// <returns>Bu çağrıda üretilen tehdit sayısı; tehditler <paramref name="output"/>'a eklenir.</returns>
        public int Tick(
            double worldMs,
            int playerCount,
            bool suppressed,
            bool enraged,
            List<KulEjderiSmallThreat> output)
        {
            if (!_started)
                return 0;
            if (suppressed)
            {
                _nextAtMs = worldMs + Interval(enraged);
                return 0;
            }

            if (worldMs < _nextAtMs)
                return 0;

            int count = ConcurrentCount(playerCount);
            for (int i = 0; i < count; i++)
            {
                KulEjderiSmallThreat threat = Create(_rng.Next(KulEjderiDefaults.SmallThreatKindCount), worldMs);
                output.Add(threat);
            }

            _nextAtMs = worldMs + Interval(enraged);
            return count;
        }

        int Interval(bool enraged) =>
            enraged
                ? KulEjderiDefaults.EnrageSmallThreatIntervalMs
                : _rng.Next(KulEjderiDefaults.SmallThreatIntervalMinMs, KulEjderiDefaults.SmallThreatIntervalMaxMs + 1);

        static KulEjderiSmallThreat Create(int kindIndex, double worldMs)
        {
            switch ((KulEjderiSmallThreatKind)kindIndex)
            {
                case KulEjderiSmallThreatKind.CrackFlame:
                    return new KulEjderiSmallThreat(KulEjderiSmallThreatKind.CrackFlame, worldMs,
                        KulEjderiDefaults.CrackFlameTelegraphMs, KulEjderiDefaults.CrackFlameDamagePct,
                        KulEjderiShape.Strip, KulEjderiDefaults.CrackFlameWidthM, KulEjderiDefaults.CrackFlameLengthM);
                case KulEjderiSmallThreatKind.FallingAsh:
                    return new KulEjderiSmallThreat(KulEjderiSmallThreatKind.FallingAsh, worldMs,
                        KulEjderiDefaults.FallingAshTelegraphMs, KulEjderiDefaults.FallingAshDamagePct,
                        KulEjderiShape.Circle, KulEjderiDefaults.FallingAshRadiusM, 0f);
                default:
                    return new KulEjderiSmallThreat(KulEjderiSmallThreatKind.TremblingGround, worldMs,
                        KulEjderiDefaults.TremblingGroundTelegraphMs, KulEjderiDefaults.TremblingGroundDamagePct,
                        KulEjderiShape.Circle, KulEjderiDefaults.TremblingGroundRadiusM, 0f);
            }
        }
    }
}
