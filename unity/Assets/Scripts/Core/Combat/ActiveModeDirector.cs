using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json "active_modes" (ulti sistemi): aynı elementin dörtlüsü
    /// (X-X-X-X) özel bir mekanik açar. 16 Eylül: sahibi "v5.2.1 hiç tam aktif olmadı, aynısı
    /// v5.3'e olmasın" dedi — bu sınıf sadece durum makinesi (tetik/süre/soğuma); efektlerin
    /// dünyaya uygulanması Game/ManifestationDirector işi, ama okuma buradan geçmeli.
    /// Kombo tablosu DEĞİL: 6 mod da JSON'dan gelir, burada elle yazılmış tek bir dizi yok.
    /// </summary>
    public sealed class ActiveModeDirector
    {
        readonly List<ActiveModeNode> _modes;
        readonly Dictionary<string, double> _cooldownReadyAtMs = new(StringComparer.Ordinal);

        double _activeSinceMs;

        public ActiveModeDirector(IReadOnlyList<ActiveModeNode> modes)
        {
            _modes = new List<ActiveModeNode>(modes ?? Array.Empty<ActiveModeNode>());
        }

        public ActiveModeNode? Active { get; private set; }

        /// <summary>Süresiz modda (Kan Çılgınlığı) -1 döner — HUD "AKTİF" yazsın, sayaç değil.</summary>
        public float RemainingSec(double worldMs)
        {
            if (Active == null || !Active.Value.HasDuration)
                return -1f;
            double elapsed = (worldMs - _activeSinceMs) / 1000.0;
            return Math.Max(0f, Active.Value.DurationSec - (float)elapsed);
        }

        public float CooldownRemainingSec(string modeId, double worldMs)
        {
            if (string.IsNullOrEmpty(modeId) || !_cooldownReadyAtMs.TryGetValue(modeId, out double readyAt))
                return 0f;
            return Math.Max(0f, (float)((readyAt - worldMs) / 1000.0));
        }

        /// <summary>
        /// Dört-aynı-rün kapanışı geldiğinde çağrılır (dot = 1..6). Zaten bir mod aktifse,
        /// soğumadaysa veya aktivasyon koşulu tutmuyorsa null döner — sessizce yok sayılır
        /// (spec'te "koşul tutmazsa ne olur" tanımlı değil; en güvenli okuma budur).
        /// </summary>
        public ActiveModeNode? TryTrigger(int dot, in ActiveModeContext ctx, double worldMs)
        {
            if (Active != null)
                return null;

            for (int i = 0; i < _modes.Count; i++)
            {
                ActiveModeNode m = _modes[i];
                if (m.TriggerDot != dot)
                    continue;

                if (_cooldownReadyAtMs.TryGetValue(m.Id, out double readyAt) && worldMs < readyAt)
                    return null;
                if (!ConditionMet(m.ActivationType, m.ActivationWithinSec, m.ActivationThreshold, m.ActivationMinCount, ctx))
                    return null;

                Active = m;
                _activeSinceMs = worldMs;
                _cooldownReadyAtMs[m.Id] = worldMs + m.CooldownSec * 1000.0;
                return m;
            }
            return null;
        }

        /// <summary>Süre doldu ya da bırakma koşulu sağlandıysa modu kapatır; kapandıysa true.</summary>
        public bool Tick(double worldMs, in ActiveModeContext ctx)
        {
            if (Active == null)
                return false;

            ActiveModeNode m = Active.Value;
            bool expire = m.HasDuration && (worldMs - _activeSinceMs) / 1000.0 >= m.DurationSec;
            if (!expire && !string.IsNullOrEmpty(m.DeactivationType))
                expire = ConditionMet(m.DeactivationType, 0f, m.DeactivationThreshold, 0, ctx);

            if (!expire)
                return false;

            Active = null;
            return true;
        }

        /// <summary>"healer iyileştirirse biter" (Kan Çılgınlığı) — dışarıdan heal bildirimi. Bitirdiyse true.</summary>
        public bool NotifyHealed()
        {
            if (Active == null || !Active.Value.HealBreaksMode)
                return false;
            Active = null;
            return true;
        }

        static bool ConditionMet(string type, float withinSec, float threshold, int minCount, in ActiveModeContext ctx)
        {
            return type switch
            {
                "dealt_damage_recently" => ctx.SecondsSinceLastDamageDealt <= withinSec,
                "moving_recently" => ctx.SecondsSinceLastMoved <= withinSec,
                "team_has_wounded" => ctx.MinTeamHpRatio < threshold,
                "hp_above_percent" => ctx.HpRatio > threshold,
                "hp_below_percent" => ctx.HpRatio < threshold,
                "team_has_debuffs" => ctx.TeamDebuffCount >= minCount,
                _ => true, // tip boş/bilinmeyen — engelleme
            };
        }

        // --- Aktif modun sürekli çarpanları (Active==null iken hepsi nötr) ---
        public float DamageMult => Active?.GetEffect("damage_mult", 1f) ?? 1f;
        public float DamageTakenMult => (Active?.GetEffect("damage_taken_mult", 1f) ?? 1f)
                                         * (Active?.DefenseDropMult ?? 1f);
        public float MoveSpeedMult => Active?.GetEffect("move_speed_mult", 1f) ?? 1f;
        public float Lifesteal => Active?.GetEffect("lifesteal", 0f) ?? 0f;
        public float CastTimeMult => Active?.GetEffect("cast_time_mult", 1f) ?? 1f;
        public float AttackSpeedMult => Active?.GetEffect("attack_speed_mult", 1f) ?? 1f;
        public float DashCooldownMult => Active?.GetEffect("dash_cooldown_mult", 1f) ?? 1f;
        /// <summary>Fırtına Akışı afterimage_count — 0 veya yoksa FeelTuning kullanılır.</summary>
        public int AfterimageCount => Active != null
            ? MathfRoundToInt(Active.Value.GetEffect("afterimage_count", 0f))
            : 0;
        /// <summary>Aşılmaz Duvar taunt_radius_m — 0 = yok.</summary>
        public float TauntRadiusM => Active?.GetEffect("taunt_radius_m", 0f) ?? 0f;
        public string VisualAura => Active?.VisualAura ?? string.Empty;
        public string VisualScreenEdges => Active?.VisualScreenEdges ?? string.Empty;
        public bool BlocksMovement => Active?.BlocksMovement ?? false;
        public float HpPerSecPercentCost => Active?.HpPerSecPercentCost ?? 0f;

        static int MathfRoundToInt(float v) => (int)Math.Round(v);
    }

    public struct ActiveModeContext
    {
        public float HpRatio;
        /// <summary>Takımın (oyuncu+ally) en düşük can oranı — "team_has_wounded" için.</summary>
        public float MinTeamHpRatio;
        public double SecondsSinceLastDamageDealt;
        public double SecondsSinceLastMoved;
        /// <summary>Takım (oyuncu+ally) debuff sayısı — StatusIconStrip / active_modes.</summary>
        public int TeamDebuffCount;
    }
}
