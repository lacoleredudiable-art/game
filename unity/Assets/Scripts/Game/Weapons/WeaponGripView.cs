using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// El prop ofseti: <see cref="WeaponVisualRegistry"/> değerlerine eklenir (Mixamo vs Synty el ekseni).
    /// Görsel prefab kökünde; <see cref="WeaponHandPropsView"/> animator üzerinden okur.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponGripView : MonoBehaviour
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
                        LocalPosition = new Vector3(WeaponGripViewDefaults.ShieldRightLocalPosX, WeaponGripViewDefaults.ShieldRightLocalPosY, WeaponGripViewDefaults.ShieldRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.ShieldRightEulerX, WeaponGripViewDefaults.ShieldRightEulerY, WeaponGripViewDefaults.ShieldRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "kalkan" when !isRight:
                    offset = DefaultMixamoLeftShield();
                    return true;
                case "yay" when !isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripViewDefaults.BowLeftLocalPosX, WeaponGripViewDefaults.BowLeftLocalPosY, WeaponGripViewDefaults.BowLeftLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.BowLeftEulerX, WeaponGripViewDefaults.BowLeftEulerY, WeaponGripViewDefaults.BowLeftEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "kitap" when !isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(WeaponGripViewDefaults.KitapLeftLocalPosX, WeaponGripViewDefaults.KitapLeftLocalPosY, WeaponGripViewDefaults.KitapLeftLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.KitapLeftEulerX, WeaponGripViewDefaults.KitapLeftEulerY, WeaponGripViewDefaults.KitapLeftEulerZ),
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
                        LocalPosition = new Vector3(WeaponGripViewDefaults.StaffRightLocalPosX, 0f, WeaponGripViewDefaults.StaffRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.StaffRightEulerX, WeaponGripViewDefaults.StaffRightEulerY, WeaponGripViewDefaults.StaffRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "kure" when !isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(0f, WeaponGripViewDefaults.KureLeftLocalPosY, WeaponGripViewDefaults.KureLeftLocalPosZ),
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
                        LocalPosition = new Vector3(WeaponGripViewDefaults.TilsimRightLocalPosX, WeaponGripViewDefaults.TilsimRightLocalPosY, WeaponGripViewDefaults.TilsimRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.TilsimRightEulerX, WeaponGripViewDefaults.TilsimRightEulerY, WeaponGripViewDefaults.TilsimRightEulerZ),
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
                        LocalPosition = new Vector3(WeaponGripViewDefaults.TopRightLocalPosX, WeaponGripViewDefaults.TopRightLocalPosY, WeaponGripViewDefaults.TopRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.TopRightEulerX, WeaponGripViewDefaults.TopRightEulerY, WeaponGripViewDefaults.TopRightEulerZ),
                        LocalScale = Vector3.one,
                    };
                    return true;
                case "cekic" when isRight:
                    offset = new GripOffset
                    {
                        LocalPosition = new Vector3(0f, 0f, WeaponGripViewDefaults.CekicRightLocalPosZ),
                        LocalEulerAngles = new Vector3(WeaponGripViewDefaults.CekicRightEulerX, WeaponGripViewDefaults.CekicRightEulerY, WeaponGripViewDefaults.CekicRightEulerZ),
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
        /// kayma yok) güncellendi. <see cref="WeaponHandPropsView.LogMixamoSwordAngle"/> doğrular.
        /// </summary>
        public static GripOffset DefaultMixamoRightSword() => new GripOffset
        {
            LocalPosition = new Vector3(WeaponGripViewDefaults.MixamoSwordLocalPosX, WeaponGripViewDefaults.MixamoSwordLocalPosY, WeaponGripViewDefaults.MixamoSwordLocalPosZ),
            LocalEulerAngles = new Vector3(WeaponGripViewDefaults.MixamoSwordEulerX, WeaponGripViewDefaults.MixamoSwordEulerY, WeaponGripViewDefaults.MixamoSwordEulerZ),
            LocalScale = Vector3.one,
        };

        public static GripOffset DefaultMixamoLeftShield() => new GripOffset
        {
            // cw-2s: 0.064m ofset → forearm bone'a 0,060m boşluk ölçtü (<0,05m hedefi kaçırıyordu).
            // Aynı yöne, kısaltılmış vektör: 0,045m (ölçülen boşluk 0,045m), yüz/açı değişmedi.
            LocalPosition = new Vector3(WeaponGripViewDefaults.MixamoShieldLocalPosX, WeaponGripViewDefaults.MixamoShieldLocalPosY, WeaponGripViewDefaults.MixamoShieldLocalPosZ),
            // Quaternius Shield_Heater dekor yüzü +Z; ön cepheye (~sol-ön) bakacak şekilde forearm eksenine paralel.
            LocalEulerAngles = new Vector3(WeaponGripViewDefaults.MixamoShieldEulerX, WeaponGripViewDefaults.MixamoShieldEulerY, WeaponGripViewDefaults.MixamoShieldEulerZ),
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
