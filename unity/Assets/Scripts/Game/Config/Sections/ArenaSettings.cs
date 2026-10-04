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
                public Color AmbientSky = new Color(0.66f, 0.70f, 0.73f);
                public Color AmbientEquator = new Color(0.52f, 0.55f, 0.58f);
                public Color AmbientGround = new Color(0.30f, 0.30f, 0.31f);
                public Color FogColor = new Color(0.69f, 0.718f, 0.737f);
                public float FogDensity = 0.0032f;
                public Color KeyLightColor = new Color(0.86f, 0.89f, 0.92f);
                public float KeyLightIntensity = 0.85f;
                public Vector3 KeyLightEuler = new Vector3(52f, -30f, 0f);
                public float KeyShadowStrength = 0.38f;
                public Color RimLightColor = new Color(0.38f, 0.55f, 1f);
                public float RimLightIntensity = 0.18f;
                public Vector3 RimLightEuler = new Vector3(28f, 145f, 0f);
                public float BloomIntensity = 0.65f;
                public float BloomThreshold = 0.95f;
                public float BloomScatter = 0.55f;
                public Color BloomTint = new Color(1f, 0.86f, 0.72f);
                public float PostExposure = 0.15f;
                public float ColorContrast = -14f;
                /// <summary>Spec "~%35 doygunluk düşüşü" (task-ambience-fix); PR #43'ün gönderdiği -22
                /// değil, görevin kendi istediği oran — sahnede görünür olmalı.</summary>
                public float ColorSaturation = -35f;
                public Color ColorFilterTint = new Color(0.95f, 0.975f, 1f);
                public float PostVignetteIntensity = 0.12f;
                /// <summary>Ufuk siluet/kayaları görünür kalsın diye uzak kırpma düzlemi büyütüldü (eski 120m).</summary>
                public float CameraFarClipM = 320f;
    }
}
