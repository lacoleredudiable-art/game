using System;

namespace Dovus.Core.Boss
{
    /// <summary>Faz eşikleri, eşik koruması (KE-3.5) ve can katmanları (KE-3.4). Saf hesap.</summary>
    public static class KulEjderiPhaseRules
    {
        /// <summary>Can oranının ait olduğu faz (eşikte olan bir sonraki fazdadır).</summary>
        public static KulEjderiPhase PhaseForRatio(float hpRatio)
        {
            if (hpRatio <= KulEjderiDefaults.Phase3HpRatio)
                return KulEjderiPhase.Phase3;
            if (hpRatio <= KulEjderiDefaults.Phase2HpRatio)
                return KulEjderiPhase.Phase2;
            return KulEjderiPhase.Phase1;
        }

        /// <summary>Fazın alt sınırı (can oranı); faz 3 için 0 (ölüm).</summary>
        public static float FloorRatio(KulEjderiPhase phase)
        {
            switch (phase)
            {
                case KulEjderiPhase.Phase1:
                    return KulEjderiDefaults.Phase2HpRatio;
                case KulEjderiPhase.Phase2:
                    return KulEjderiDefaults.Phase3HpRatio;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// KE-3.5: faz eşiğine gelen hasar fazlası taşmaz; can eşikte durur, bir vuruşla iki faz atlanmaz.
        /// Dönen değer gerçekten uygulanacak hasardır.
        /// </summary>
        public static float ClampDamage(float hpBefore, float maxHp, float damage, KulEjderiPhase phase)
        {
            if (damage <= 0f || maxHp <= 0f)
                return 0f;
            float floorHp = FloorRatio(phase) * maxHp;
            float room = hpBefore - floorHp;
            if (room <= 0f)
                return 0f;
            return Math.Min(damage, room);
        }

        /// <summary>KE-3.4: can çubuğunun "xN" sayısı; 5 katmandan kaç tanesi sürüyor (can 0 = 0).</summary>
        public static int RemainingLayers(float hpRatio)
        {
            if (hpRatio <= 0f)
                return 0;
            int layers = (int)Math.Ceiling(hpRatio * KulEjderiDefaults.HealthLayerCount);
            return Math.Min(layers, KulEjderiDefaults.HealthLayerCount);
        }
    }
}
