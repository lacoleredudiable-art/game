namespace Dovus.Core.RuleEngineV4
{
    /// <summary>slice_arena_visuals yedekleri; nihai değerler kural-motoru-v4.json'da.</summary>
    public static class RuleEngineV4SliceArenaVisualsDefaults
    {
        public const string ColumnModel = "sutun";
        public const string CocoonModel = "koza";
        public const string RibModel = "kaburga";
        public const string RibLod1Model = "kaburga_LOD1";
        public const string WallModel = "duvar";
        public const string RuneStoneModel = "runtasi";
        public const string AxisNorthToSouth = "north_to_south";
        public const string AxisSouthToNorth = "south_to_north";

        public const int RibCount = 7;
        public const float RibScale = 2.3f;
        public const float RibSpacingM = 5f;
        /// <summary>Kuzey ucu: (count-1)·spacing/2 → dizi arena merkezine ortalanır.</summary>
        public const float RibStartZM = 15f;
        public const int RibHeartIndex = 0;
        public const float RibHeartScaleMult = 1.15f;
        public const float RibJitterScale = 0.1f;
        /// <summary>±%10 → çeyrek turun %10'u (spec'te derece yok).</summary>
        public const float RibJitterRotDeg = 9f;
        public const int RibSeed = 1234;
        /// <summary>spec'te yok: LOD0→LOD1 ekran yüksekliği oranı.</summary>
        public const float RibLod1ScreenHeight = 0.45f;
        /// <summary>spec'te yok: 0 = LOD1 hiç kırpılmaz (kemerler ufukta da görünür).</summary>
        public const float RibCullScreenHeight = 0f;

        /// <summary>Kemik |x| &lt; 22 m bandında yerden 2,5 m altına inmez (ayaklar arena dışında).</summary>
        public const float RibKeepClearXM = 22f;
        public const float RibKeepClearBelowM = 2.5f;
        /// <summary>
        /// spec'te yok: yarım kaburga eğimi/büyütmesi kaburga modeli üzerinde ölçüldü — en ince kemerde (ölçek
        /// 2,217) 2,5 m altı |x| ≥ 22,96 m, tepe 27,5–33,5 m. Pivot taç gövdesinin üst kenarı (model 11 m);
        /// taç yüksekliği ayak tabanını ~0,1 kemer-metre gömer; bindirme 2,7 m eğik yarımlar arası yarığı kapatır
        /// (sol bacağın iç kenarı modelde 3,1 m'de, kesim ona değmez).
        /// </summary>
        public const float RibHalfLeanDeg = 29f;
        public const float RibHalfScaleMult = 1.3f;
        public const float RibHalfPivotYM = 11f;
        public const float RibHalfCrownYM = 9.67f;
        public const float RibHalfCrownOverlapM = 2.7f;

        /// <summary>spec'te yok: ocak kor diski yarıçapı.</summary>
        public const float HearthEmberRadiusM = 0.9f;
        /// <summary>spec'te yok: ocak taş halkası.</summary>
        public const int HearthStoneCount = 6;
        public const float HearthStoneRingM = 1.2f;
        public const float HearthStoneScale = 0.35f;

        public const float RuneStoneXM = 0f;
        public const float RuneStoneZM = -19f;

        /// <summary>spec'te yok: zemin kor örtüsü (18 m çarpışma halkası içi).</summary>
        public const float FloorGlowRadiusM = 18f;
        public const float FloorGlowLiftM = 0.02f;

        /// <summary>O-7.7: duvar ötesi düz zemin yarıçapı.</summary>
        public const float BeyondGroundRadiusM = 300f;
        /// <summary>O-8.2 kül.</summary>
        public const string BeyondGroundColorHex = "#5B5A57";
        /// <summary>O-8.7: üstel sis.</summary>
        public const float BeyondFogDensity = 0.012f;
        /// <summary>O-8.2 / O-8.7 uzak sis.</summary>
        public const string BeyondFogColorHex = "#4A5260";
    }
}
