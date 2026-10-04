using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// El prop ofseti: <see cref="WeaponVisualRegistry"/> değerlerine eklenir (Mixamo vs Synty el ekseni).
    /// Görsel prefab kökünde; <see cref="WeaponHandProps"/> animator üzerinden okur.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponGripProfile : MonoBehaviour
    {
        [SerializeField] GripOffset _right = GripOffset.Identity;
        [SerializeField] GripOffset _left = GripOffset.Identity;

        [System.Serializable]
        public struct GripOffset
        {
            public Vector3 LocalPosition;
            public Vector3 LocalEulerAngles;
            public Vector3 LocalScale;

            public static GripOffset Identity => new GripOffset
            {
                LocalPosition = Vector3.zero,
                LocalEulerAngles = Vector3.zero,
                LocalScale = Vector3.one,
            };
        }

        /// <summary>Mixamo Paladin: silah anahtarına göre ek tutuş (Synty'ye uygulanmaz).</summary>
        public void ApplyMixamoWeapon(string weaponKey, bool isRight, ref Vector3 localPos, ref Quaternion localRot, ref Vector3 localScale)
        {
            if (weaponKey == "kilic")
            {
                if (isRight)
                    Apply(_right, ref localPos, ref localRot, ref localScale);
                else
                    Apply(_left, ref localPos, ref localRot, ref localScale);
                return;
            }

            if (TryGetMixamoWeaponOffset(weaponKey, isRight, out GripOffset o))
                Apply(o, ref localPos, ref localRot, ref localScale);
        }

        public static bool TryGetMixamoWeaponOffset(string weaponKey, bool isRight, out GripOffset offset)
        {
            offset = default;
            switch (weaponKey)
            {
                case "kalkan" when isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripProfileDefaults.KalkanRightLocalPosX, WeaponGripProfileDefaults.KalkanRightLocalPosY, WeaponGripProfileDefaults.KalkanRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.KalkanRightEulerX, WeaponGripProfileDefaults.KalkanRightEulerY, WeaponGripProfileDefaults.KalkanRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "kalkan" when !isRight:
                    offset = DefaultMixamoLeftShield();
                    return true;
                case "yay" when !isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripProfileDefaults.YayLeftLocalPosX, WeaponGripProfileDefaults.YayLeftLocalPosY, WeaponGripProfileDefaults.YayLeftLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.YayLeftEulerX, WeaponGripProfileDefaults.YayLeftEulerY, WeaponGripProfileDefaults.YayLeftEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "kitap" when !isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripProfileDefaults.KitapLeftLocalPosX, WeaponGripProfileDefaults.KitapLeftLocalPosY, WeaponGripProfileDefaults.KitapLeftLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.KitapLeftEulerX, WeaponGripProfileDefaults.KitapLeftEulerY, WeaponGripProfileDefaults.KitapLeftEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "asa" when isRight:
                    // cw-2s: asa (Staff_Quaternius) uzun ekseni prefab'ta local +Y (OffsetChildToGrip
                    // longAxis=up) — kılıcın "forward" kalıbıyla kopyalanan eski euler (188/96) onu
                    // ~70° yatık/öne uzanmış gösteriyordu (ölçülen). Hand-local "dünya yukarı" yönü +
                    // hafif öne yatış (dünya ileri'ye %18 slerp) ile analitik çözüm: dikey, hafif öne
                    // (ölçülen: dikeyden 16,2°, gap 0,042 m). Not: Quaternion.FromToRotation(up, hedef)
                    // hedef ≈ -up'a yakınken (bu el kemiği için öyle) kararsız/keyfi roll seçer — bu
                    // euler, çapraz çarpımla sabit bir taban kuran kararlı bir yöntemden geldi.
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripProfileDefaults.AsaRightLocalPosX, 0f, WeaponGripProfileDefaults.AsaRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.AsaRightEulerX, WeaponGripProfileDefaults.AsaRightEulerY, WeaponGripProfileDefaults.AsaRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "kure" when !isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(0f, WeaponGripProfileDefaults.KureLeftLocalPosY, WeaponGripProfileDefaults.KureLeftLocalPosZ),
                        LocalEulerAngles = Vector3.zero,
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "tilsim" when isRight:
                    // cw-2s: Talisman_Necklace uzun ekseni prefab'ta local +Y (OffsetChildToGrip
                    // longAxis=up, grip %72 üstten — kolye ucu/pandantif local -Y'de sarkar). Eski
                    // euler (200/88) kılıç kalıbından kopyaydı, ölçülen prop.up dünya yatayına
                    // yakındı (dikeyden ~90°) — pandantif sarkmıyordu. Hand-local "dünya yukarı"
                    // analitik çözüm (kararlı taban, bkz. asa yorumu): tam dikey, pandantif
                    // yumruğun altında sarkar (ölçülen: dikeyden 0°, gap 0,035 m).
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripProfileDefaults.TilsimRightLocalPosX, WeaponGripProfileDefaults.TilsimRightLocalPosY, WeaponGripProfileDefaults.TilsimRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.TilsimRightEulerX, WeaponGripProfileDefaults.TilsimRightEulerY, WeaponGripProfileDefaults.TilsimRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "top" when isRight:
                    // cw-2s: HandCannon_Shotgun uzun ekseni prefab'ta local +X (OffsetChildToGrip
                    // longAxis=right, namlu +X). Eski euler (188/92) kılıç kalıbından kopyaydı,
                    // namlu dünya ileri'yle dot≈0,38 (çoğunlukla yana/yukarı) ölçtü. Hand-local
                    // "dünya ileri" + çapraz çarpımla kararlı taban: namlu artık dot=1 (tam öne),
                    // gap 0,028 m.
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripProfileDefaults.TopRightLocalPosX, WeaponGripProfileDefaults.TopRightLocalPosY, WeaponGripProfileDefaults.TopRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.TopRightEulerX, WeaponGripProfileDefaults.TopRightEulerY, WeaponGripProfileDefaults.TopRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "cekic" when isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(0f, 0f, WeaponGripProfileDefaults.CekicRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.CekicRightEulerX, WeaponGripProfileDefaults.CekicRightEulerY, WeaponGripProfileDefaults.CekicRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
            }

            return false;
        }

        static void Apply(GripOffset o, ref Vector3 localPos, ref Quaternion localRot, ref Vector3 localScale)
        {
            localPos += o.LocalPosition;
            localRot *= Quaternion.Euler(o.LocalEulerAngles);
            localScale = new Vector3(
                localScale.x * o.LocalScale.x,
                localScale.y * o.LocalScale.y,
                localScale.z * o.LocalScale.z);
        }

        /// <summary>Mixamo humanoid (mixamorig) — Synty registry ofsetleri yetmez.</summary>
        public static bool IsMixamoRig(Animator animator)
        {
            if (animator == null)
                return false;
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null && hand.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return animator.transform.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Paladin / Mixamo oyuncu görseli için önerilen kılıç tutuşu (editor binder yazır).
        /// ff-4: idle'da ~127° (aşağı/öne) ölçülüyordu — dikeyden 30–45° öne-yukarı hedefine
        /// (mixamorig:RightHand uzayında analitik çözüm: blade.forward ≈ dikeyden 37.5°, yan
        /// kayma yok) güncellendi. <see cref="WeaponHandProps.LogMixamoSwordAngle"/> doğrular.
        /// </summary>
        public static GripOffset DefaultMixamoRightSword() => new GripOffset
        {
            LocalPosition = new Vector3(WeaponGripProfileDefaults.MixamoSwordLocalPosX, WeaponGripProfileDefaults.MixamoSwordLocalPosY, WeaponGripProfileDefaults.MixamoSwordLocalPosZ),
            LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.MixamoSwordEulerX, WeaponGripProfileDefaults.MixamoSwordEulerY, WeaponGripProfileDefaults.MixamoSwordEulerZ),
            LocalScale = Vector3.one,
        };

        public static GripOffset DefaultMixamoLeftShield() => new GripOffset
        {
            // cw-2s: 0.064m ofset → forearm bone'a 0,060m boşluk ölçtü (<0,05m hedefi kaçırıyordu).
            // Aynı yöne, kısaltılmış vektör: 0,045m (ölçülen boşluk 0,045m), yüz/açı değişmedi.
            LocalPosition = new Vector3(WeaponGripProfileDefaults.MixamoShieldLocalPosX, WeaponGripProfileDefaults.MixamoShieldLocalPosY, WeaponGripProfileDefaults.MixamoShieldLocalPosZ),
            // Quaternius Shield_Heater dekor yüzü +Z; ön cepheye (~sol-ön) bakacak şekilde forearm eksenine paralel.
            LocalEulerAngles = new Vector3(WeaponGripProfileDefaults.MixamoShieldEulerX, WeaponGripProfileDefaults.MixamoShieldEulerY, WeaponGripProfileDefaults.MixamoShieldEulerZ),
            LocalScale = Vector3.one,
        };

#if UNITY_EDITOR
        public void SetMixamoDefaults()
        {
            _right = DefaultMixamoRightSword();
            _left = DefaultMixamoLeftShield();
        }
#endif
    }
}
