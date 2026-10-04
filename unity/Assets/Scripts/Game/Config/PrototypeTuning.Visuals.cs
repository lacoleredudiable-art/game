using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
    {
        [Header("Renk dili (§10)")]
        public Color PlayerColor = new Color(0.373f, 0.941f, 1f);
        public Color BossColor = new Color(0.18f, 0.19f, 0.22f);
        public Color GroundColor = new Color(0.44f, 0.46f, 0.49f);
        public Color BackgroundColor = new Color(0.69f, 0.718f, 0.737f);
        public Color InkPurple = new Color(0.725f, 0.549f, 1f);   // #B98CFF
        public Color InkCyan = new Color(0.373f, 0.941f, 1f);     // #5FF0FF
        public Color AcidGreen = new Color(0.608f, 0.910f, 0.235f); // #9BE83C — §10 zehir birikintisi

        // Altı çekirdek için mevcut prototip renkleri. Ateş sıcak magenta.
        [Header("Element renkleri (çizgi/tezahür)")]
        public Color ElementFire = new Color(1f, 0.42f, 0.62f);       // Ateş — sıcak magenta
        public Color ElementWater = new Color(0.28f, 0.72f, 1f);      // Su
        public Color ElementAir = new Color(0.50f, 0.70f, 0.62f);     // Hava — muted teal (prezentasyon)
        public Color ElementEarth = new Color(0.62f, 0.78f, 0.42f);   // Toprak
        public Color ElementLight = new Color(1f, 0.96f, 0.82f);      // Aydınlık
        public Color ElementDark = new Color(0.48f, 0.28f, 0.78f);    // Karanlık

        [Header("Kenney parçacık dokuları (Resources/Vfx/Kenney, uzantı yok)")]
        public string VfxTexFire = "flame_02";
        public string VfxTexWater = "circle_03";
        public string VfxTexAir = "twirl_01";
        public string VfxTexEarth = "dirt_01";
        public string VfxTexLight = "star_04";
        public string VfxTexDark = "magic_04";
        public string VfxTexHit = "spark_05";
        public string VfxTexInk = "light_01";

        public Color HexagonDotColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        // T6.2: merkez artık "vur" demek — oyuncu rengine çekildi (§10 camgöbeği).
        public Color HexagonCenterColor = new Color(0.373f, 0.941f, 1f, 0.9f);
        // Dodge diski mevcut mor oyuncu vurgusunu kullanır.
        public Color DodgeButtonColor = new Color(0.725f, 0.549f, 1f, 0.9f);
        // Boss telegrafının mevcut sıcak renkleri.
        public Color TelegraphHot = new Color(1f, 0.302f, 0.141f);   // #FF4D24
        public Color TelegraphWarm = new Color(1f, 0.604f, 0.235f);  // #FF9A3C

        [Header("Tezahür çizgisi (T7.2, LivingEffectView)")]
        public float EffectLineWidthDefaultM = 0.28f;
        public float EffectLineWidthWideM = 0.55f;
        public float EffectLineWidthNarrowM = 0.16f;
        public float EffectSarsintiWidthWideM = 0.22f;
        public float EffectSarsintiWidthNarrowM = 0.1f;

        [Header("Tezahür silüet eşikleri (T7.2, LivingEffectView)")]
        public float EffectShowMinFocus = 0.2f;
        public float EffectIgneShowMinSpread = 0.2f;
        public float EffectFocusRingMax = 0.35f;
        public float EffectFocusArcMax = 0.75f;
        public float EffectPierceNeedleShowMin = 0.45f;
        public float EffectFocusSwarmAlongLineMin = 0.45f;

        [Header("Tezahür şekil ölçekleri (T7.2, LivingEffectView)")]
        public float EffectBlobScaleBaseM = 0.38f;
        public float EffectBlobScalePerSpreadM = 0.18f;
        public float EffectNeedleThickWideM = 0.35f;
        public float EffectNeedleThickNarrowM = 0.14f;
        public float EffectNeedleLenBaseM = 0.7f;
        public float EffectNeedleLenPerPierceM = 0.5f;

        // T14 — hareket karakteri görsel ölçüleri (spec yok; durum.md T14 sapmaları).
        [Header("Tezahür hareket (T14, LivingEffectView)")]
        public float EffectNeedleWindupLenMul = 1.55f;
        public float EffectNeedleArrivalLenMul = 0.72f;
        public float EffectNeedleAfterimageAlpha = 0.35f;
        public float EffectSwarmJitterM = 0.65f;
        public float EffectSwarmMinBlobs = 4f;
        public float EffectSarsintiGroundY = 0.02f;
        public float EffectSarsintiMassWidthMul = 1.35f;
        public float EffectBasicStrikeLenM = 0.9f;
        public float EffectBasicStrikeThickM = 0.16f;
        public float EffectBasicStrikeHeightM = 0.55f;

        // Rün başına squash/stretch + poz süresi (T1/T5) — değer aynı, yeri ActorPose'dan taşındı.
        [Header("Aktör poz (T7.2, ActorPose)")]
        public float ActorPoseDurationMs = 180f;
        public Vector3 PoseIgne = new Vector3(0.78f, 0.88f, 1.35f);
        public Vector3 PoseSuru = new Vector3(1.35f, 0.9f, 1.1f);
        public Vector3 PoseSarsinti = new Vector3(1.2f, 0.55f, 1.2f);
        public Vector3 PoseKabuk = new Vector3(1.15f, 1.05f, 1.15f);
        public Vector3 PoseZehir = new Vector3(1.05f, 0.95f, 1.25f);
        [Header("Kalıcı iz tavanı (T7.2, GroundScarField)")]
        public int GroundScarCapCount = 60;
    }
}
