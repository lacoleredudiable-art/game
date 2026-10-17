using UnityEngine;

namespace Dovus.Game.Config.Sections
{
    [System.Serializable]
    public sealed class ArenaSettings
    {
                [Header("Arena")]
                /// <summary>Daire salon yarıçapı (çap = 2×). Eski kare yarım-kenar adı korundu.</summary>
                public float ArenaHalfSizeM = 25f;
                /// <summary>Çevre duvar yüksekliği (tavansız salon).</summary>
                public float ArenaWallHeightM = 18f;
                /// <summary>Çevre duvar kalınlığı.</summary>
                public float ArenaWallThicknessM = 1.4f;
                /// <summary>Quaternius arena prefab ölçeği — daire salonda 1.</summary>
                public float ArenaVisualScale = 1.0f;
                // Ambiyans portu (PR #43 "deneme sahnesi"): gri bulutlu ışık, ~%35 doygunluk düşüşü,
                // açık gri sis — sert/sakin ama her şey görünür (karanlık değil, renkli değil). Sıcak
                // vurgu yalnız lav (LavaDecor/LavaCracks) ve VFX'te kalır. docs'ta sayı yok — PR #43'ün
                // kendi commit'lerinde kullandığı değerler (DenemeSahnesi_PostFX.asset) buraya taşındı.
                [Header("Arena atmosferi — mobil URP (ambiyans: PR #43 açık gri lav ovası)")]
                public Color AmbientSky = new Color(0.75f, 0.78f, 0.82f);
                public Color AmbientEquator = new Color(0.62f, 0.64f, 0.67f);
                public Color AmbientGround = new Color(0.42f, 0.43f, 0.45f);
                public Color FogColor = new Color(0.69f, 0.718f, 0.737f);
                public float FogDensity = 0.0032f;
                public Color KeyLightColor = new Color(1f, 1f, 1f);
                /// <summary>Linear renk uzayı değeri. Gamma'da 1,6 idi; Linear'da ışık şiddeti doğrusal toplanır,
                /// o yüzden aynı ekran parlaklığı için dönüştürüldü: ((A + I·NL)^2,2 − A^2,2) / NL, A = gökyüzü
                /// ortam 0,78, I = 1,6, NL = sin 52° = 0,788 → 5,36 ≈ 5,4. Gölgedeki ve en aydınlık nokta Gamma ile
                /// aynı çıkar, aradaki yarı ton Linear'da doğal. (tahmin; telefonda ölçülüp ayarlanır)</summary>
                public float KeyLightIntensity = 5.4f;
                public Vector3 KeyLightEuler = new Vector3(52f, -30f, 0f);
                public float KeyShadowStrength = 0.75f;
                public Color RimLightColor = new Color(0.38f, 0.55f, 1f);
                public float RimLightIntensity = 0f;
                public Vector3 RimLightEuler = new Vector3(28f, 145f, 0f);
                public float BloomIntensity = 0.55f;
                public float BloomThreshold = 0.95f;
                public float BloomScatter = 0.55f;
                public Color BloomTint = new Color(1f, 0.97f, 0.92f);
                public float PostExposure = 0.3f;
                public float ColorContrast = 10f;
                /// <summary>Spec "~%35 doygunluk düşüşü" (task-ambience-fix); PR #43'ün gönderdiği -22
                /// değil, görevin kendi istediği oran — sahnede görünür olmalı.</summary>
                public float ColorSaturation = -5f;
                public Color ColorFilterTint = new Color(1f, 1f, 1f);
                public float PostVignetteIntensity = 0.18f;
                /// <summary>Ufuk siluet/kayaları görünür kalsın diye uzak kırpma düzlemi büyütüldü (eski 120m).</summary>
                public float CameraFarClipM = 320f;
    }
}
