namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Basılı tutarak hedef seçme halkası — spec'te yok; geçici görsel varsayılan (işaret decal'ıyla aynı ölçek).</summary>
    public static class RuleEngineV4TargetPickRingDefaults
    {
        /// <summary>Hedef gövde yarıçapına eklenen pay.</summary>
        public const float TargetPaddingM = 0.15f;
        /// <summary>Zemin halkasının z-çakışmasını önleyen yükseklik.</summary>
        public const float GroundOffsetM = 0.04f;
        /// <summary>VFX_Kor _Intensity (işaret decal'ının sönük koru).</summary>
        public const float Intensity = 1.2f;
    }
}
