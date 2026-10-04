using UnityEngine;

namespace Dovus.Game.Config.Sections
{
    [System.Serializable]
    public sealed class CameraSettings
    {
                [Header("Kamera — omuz üstü savaş")]
                public float FollowSmoothTimeSec = 0.12f;
                public float LookAheadM = 0.65f;
                /// <summary>Oyuncu köküne göre omuz pivotu; mesafe ayrıca geriye uygulanır.</summary>
                public Vector3 CameraShoulderOffset = new Vector3(0.42f, 1.12f, -0.28f);
                public float CameraDistanceM = 5.85f;
                public float CameraLookHeightM = 0.38f;
                public float CameraFovDeg = 54f;
                public float CameraAimDampingSec = 0.12f;
                public float CameraSoftLockRangeM = 20f;
                [Range(0f, 1f)] public float CameraSoftLockStrength = 0.58f;
                /// <summary>Soft-lock aktifken bakış: oyuncu→boss arası (0=oyuncu, 1=boss).</summary>
                [Range(0f, 1f)] public float CameraBossFramingWeight = 0.40f;
                public float CameraBossAimHeightM = 2.05f;
                public float CameraDefaultPitchDeg = 21f;
                public float CameraLockOnMinDistanceM = 5.2f;
                public float CameraLockOnMaxDistanceM = 8.8f;
                /// <summary>Boss mesafesi arttıkça mesafe artışı (m / m ayrım).</summary>
                public float CameraLockOnDistancePerSepM = 0.14f;
                public float CameraLockOnMaxExtraDistanceM = 3.1f;
                public float CameraLockOnDistanceSmoothSec = 0.22f;
                public float CameraWindupDistanceMul = 1.42f;
                public float CameraWindupExtraHeightM = 0.68f;
                public float CameraWindupSmoothSec = 0.28f;
                /// <summary>Slam dışı geniş telegraf (FireCone vb.) için yarıçap eşiği (m).</summary>
                public float CameraWindupMinRadiusM = 3.5f;
                public float CameraCollisionSphereRadiusM = 0.25f;
                public float CameraCollisionMarginM = 0.12f;
                public float CameraCollisionMinDistanceM = 1.2f;
                public float CameraCollisionPullInSmoothSec = 0.05f;
                public float CameraCollisionPullOutSmoothSec = 0.35f;
                /// <summary>Lock-on omuz üstü yatay ofset (m).</summary>
                public float CameraLockOnShoulderSideM = 1.1f;
                public float CameraLockOnShoulderFlipHysteresis = 0.12f;
                [Range(0.35f, 0.75f)] public float CameraLockOnLookBlendToBoss = 0.58f;
                [Header("Kamera orbit")]
                public float OrbitDegreesPerDp = 0.35f;
                /// <summary>
                /// Dikey sürükleme eğimi (+ = kamera yükselip aşağı bakar). Alt sınır kameranın zemine
                /// inmemesi için; üst sınır boss'u kadrajdan atmaması için. Önerilen.
                /// </summary>
                public float CameraPitchMinDeg = -8f;
                public float CameraPitchMaxDeg = 35f;
                [Tooltip("Açıkken parmak yukarı = kamera yükselir (aşağı bakar).")]
                public bool OrbitInvertPitch = false;

                [Header("Soft aim / menzil")]
                public float SoftAimRangeM = 8f;
                // Yarım açı: boss bakış yönünün bu kadar dışındaysa kilit yok (arkası dönük vurmaz).
                public float SoftAimConeDeg = 70f;
    }
}
