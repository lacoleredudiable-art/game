using System;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Play Sweep hızı. Fizik adımı (fixedDelta) değişmez; dünya saati ve kare eşikleri hızla büyür.
    /// Referans kare 1/60 sn: sıçrama sınırı 25 m/s × dt + 0,2 m, ayak payı 0,05 m.
    /// Daha uzun karede her iki sınır aynı oranda büyür; 4× aynı hareketi kaçırmaz.
    /// Bitiş ölçüsü (0,03 m, skill bitiminden 0,30 sn) hızdan bağımsızdır.
    /// </summary>
    public static class SweepPace
    {
        public const float DefaultSpeed = 4f;
        public const float DebugSpeed = 1f;
        public const float ReferenceFrameSec = 1f / 60f;

        public const float JumpMaxSpeedMps = 25f;
        public const float JumpMarginM = 0.2f;

        /// <summary>Boşta saymadan önce bir referans kare.</summary>
        public const float IdleHoldSec = ReferenceFrameSec;

        public static float ClampSpeed(float speed) => speed < MotionDefaults.DebugSpeedThresholdMps ? DebugSpeed : DefaultSpeed;

        /// <summary>Cast öncesi bekleme: iki fizik adımı. Daha uzun tutmak taramayı uzatır.</summary>
        public static float SettleWaitSec(float fixedStepSec)
        {
            float step = fixedStepSec > 0f ? fixedStepSec : MotionDefaults.FixedStepFallbackSec;
            return MathF.Max(step * 2f, MotionDefaults.FixedStepFallbackSec);
        }

        /// <summary>Duvar süresinden dünya karesi. Saat çarpanı Time.timeScale ile aynı olmalı.</summary>
        public static float EffectiveFrameSec(float unscaledDt, float speed) =>
            MathF.Max(0f, unscaledDt) * MathF.Max(1f, speed);

        /// <summary>fixedDeltaTime hızla çarpılmaz; adım boyu 1× ile aynı kalır.</summary>
        public static float FixedStepSec(float baseFixedStepSec) => baseFixedStepSec;

        /// <summary>maximumDeltaTime dünya saniyesidir; duvar tavanı hız yüzünden küçülmesin.</summary>
        public static float MaxDeltaSec(float baseMaxDeltaSec, float speed) =>
            baseMaxDeltaSec * MathF.Max(1f, speed);

        /// <summary>
        /// Referans karede 25 m/s × dt + 0,2 m. Pay da dt ile orantılıdır:
        /// 1×'te geçen bir adım, 4× karede dört katı yer değiştirince de geçer.
        /// </summary>
        public static float JumpLimit(float frameDt)
        {
            float d = MathF.Max(frameDt, ReferenceFrameSec);
            float atRef = JumpMaxSpeedMps * ReferenceFrameSec + JumpMarginM;
            return atRef * (d / ReferenceFrameSec);
        }

        /// <summary>Havada olmayan kare. 1/60 ve altı 0,05 m; daha uzun kare orantılı gevşer.</summary>
        public static float GroundSlack(float frameDt)
        {
            float scale = MathF.Max(1f, MathF.Max(0f, frameDt) / ReferenceFrameSec);
            return Grounding.LiveSlackM * scale;
        }
    }
}
