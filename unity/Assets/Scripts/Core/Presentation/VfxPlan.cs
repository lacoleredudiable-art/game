namespace Dovus.Core.Presentation
{
    /// <summary>
    /// <c>VfxPlan = Coz(SkillSpec)</c> çıktısı. VFX olayları dinler; sonuç hesaplamaz (§6).
    /// Dilim: Kılıç ATIL (Hareket dash) + Zarar isabet katmanları tam; diğerleri soket için.
    /// </summary>
    public readonly struct VfxPlan
    {
        public static VfxPlan Empty { get; } = default;

        public VfxPlan(
            VfxColorRgb coreColor,
            VfxMotifKind motif,
            VfxEdgeStyle edge,
            VfxShapePrimitive shape,
            VfxCarrierKind carrier,
            VfxDeliveryClass delivery,
            bool skillAwaken,
            bool runeLetterFlash,
            bool lightningDashTrail,
            bool wingFootSparks,
            bool dragonTailArcSilhouette,
            bool slashArcOnHit,
            bool zararClawMarksOnHit,
            bool edgeStopEmberSpark,
            float trailLifeSec,
            float silhouetteLifeSec,
            int dragonAtlasCell,
            int verbRuneId,
            int adjectiveRuneId,
            string weaponKey)
        {
            CoreColor = coreColor;
            Motif = motif;
            Edge = edge;
            Shape = shape;
            Carrier = carrier;
            Delivery = delivery;
            SkillAwaken = skillAwaken;
            RuneLetterFlash = runeLetterFlash;
            LightningDashTrail = lightningDashTrail;
            WingFootSparks = wingFootSparks;
            DragonTailArcSilhouette = dragonTailArcSilhouette;
            SlashArcOnHit = slashArcOnHit;
            ZararClawMarksOnHit = zararClawMarksOnHit;
            EdgeStopEmberSpark = edgeStopEmberSpark;
            TrailLifeSec = trailLifeSec;
            SilhouetteLifeSec = silhouetteLifeSec;
            DragonAtlasCell = dragonAtlasCell;
            VerbRuneId = verbRuneId;
            AdjectiveRuneId = adjectiveRuneId;
            WeaponKey = weaponKey ?? string.Empty;
        }

        public VfxColorRgb CoreColor { get; }
        public VfxMotifKind Motif { get; }
        public VfxEdgeStyle Edge { get; }
        public VfxShapePrimitive Shape { get; }
        public VfxCarrierKind Carrier { get; }
        public VfxDeliveryClass Delivery { get; }

        public bool SkillAwaken { get; }
        public bool RuneLetterFlash { get; }
        public bool LightningDashTrail { get; }
        public bool WingFootSparks { get; }
        public bool DragonTailArcSilhouette { get; }
        public bool SlashArcOnHit { get; }
        public bool ZararClawMarksOnHit { get; }
        public bool EdgeStopEmberSpark { get; }

        public float TrailLifeSec { get; }
        public float SilhouetteLifeSec { get; }
        public int DragonAtlasCell { get; }
        public int VerbRuneId { get; }
        public int AdjectiveRuneId { get; }
        public string WeaponKey { get; }

        public bool IsEmpty => Motif == VfxMotifKind.None && Carrier == VfxCarrierKind.None;

        /// <summary>Kılıç Zenitsu ATIL dilimi (Hareket + kilic + dash).</summary>
        public bool IsSwordDashSlice =>
            LightningDashTrail
            && string.Equals(WeaponKey, VfxPlanDefaults.WeaponKeyKilic, System.StringComparison.Ordinal);
    }
}
