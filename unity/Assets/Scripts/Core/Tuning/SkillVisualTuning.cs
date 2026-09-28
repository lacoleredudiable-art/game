namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Gramer → görsel reçete dizilim sayıları (MechanicVisualComposer). Boyutlar gramerin
    /// MechanicBody.SizeM/ReachM değerinden gelir; buradakiler yalnız dizilim/zamanlama hissidir.
    /// Hepsi "önerilen" (docs/durum.md) — göz ayarı, JSON'da karşılığı yok.
    /// </summary>
    [System.Serializable]
    public sealed class SkillVisualTuning
    {
        /// <summary>Bir parçanın çapı = gövde boyu × bu oran.</summary>
        public float PieceSizeFrac = 0.32f;
        public float PieceMinSizeM = 0.35f;
        /// <summary>Büyük alan daha büyük değil daha çok parça üretir.</summary>
        public float PieceMaxSizeM = 1.2f;
        /// <summary>Hat/yay boyunca parça aralığı (parça çapı katı).</summary>
        public float SpacingPieces = 0.9f;
        public int MaxPiecesPerRow = 9;

        public float BeamSpeedMps = 22f;
        public float ThrustSpeedMps = 16f;
        public float SweepSec = 0.18f;
        public float HeavySweepSec = 0.3f;
        public float SweepArcDeg = 110f;
        public float SlamRingDelaySec = 0.06f;
        public int SlamRingPieces = 6;
        public int ScatterPieces = 5;
        public int OrbitPieces = 4;

        public float HeavyScale = 1.35f;
        public float SingleTargetScale = 1.6f;
        public float PierceStretch = 2.2f;
        public float RampStepScale = 0.18f;
        public float GrowStartScale = 0.35f;
        public float CloudScale = 1.8f;
        public int CloudPieces = 6;
        public float RiseSec = 0.25f;
        public float CurveM = 0.9f;
        public float PullStartFrac = 1.4f;
        public float ChainHopM = 1.6f;
        public float ChainHopSec = 0.12f;
        public float EchoOffsetM = 0.45f;
        public float LoopEverySec = 0.5f;
        /// <summary>Taşınan parça (mermi) yol boyunca bu aralıkla yere iz bırakır.</summary>
        public float TrailEverySec = 0.1f;
        /// <summary>Parça hareketi (yükselme/çekim/kavis) süresi.</summary>
        public float PieceMoveSec = 0.35f;

        /// <summary>Katı parça (kaya/buz) ömrünün bu kesrine kadar yerde durur, sonra batar.</summary>
        public float ChunkHoldFrac = 0.7f;
        public float ChunkTiltDeg = 12f;
        /// <summary>Mermiyle taşınan parça takla hızı.</summary>
        public float ChunkSpinDegPerSec = 360f;
        /// <summary>Executor origin'i sahibe bu kadar yakınsa BornAt ileri kaydırması uygulanır.</summary>
        public float CasterOriginToleranceM = 0.75f;
        /// <summary>Karanlık arenada okunurluk: parça element renginde bu kadar ışır.</summary>
        public float ChunkEmission = 0.35f;
    }
}
