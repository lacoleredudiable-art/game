using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Game.Config
{
    /// <summary>T10: hazır setler — teknoloji-kararlari §6 "Hazır setler" maddesi.</summary>
    public enum TuningPreset
    {
        Agir,
        Cevik,
        Anime
    }

    /// <summary>
    /// Üç hazır his profili. Sayılar SPEC DEĞİL — durum.md'de T10 sapması olarak kayıtlı,
    /// telefonda hızlı deneme için icat edilmiş başlangıç noktaları. Her preset kendi içinde
    /// GradeTuning.TemizGapMaxMs &lt; DodgeTuning.IframeMs kısıtını (§6) korur.
    /// Alanlar TEK TEK yazılır — nesne referansları (Combat.Dodge vb.) DEĞİŞTİRİLMEZ, yoksa
    /// DodgeState/SentenceEngine gibi tüketiciler eski nesneye bakmayı sürdürür (bkz. CombatTuning).
    /// </summary>
    public static class TuningPresets
    {
        public static void Apply(TuningPreset preset, CombatTuning combat, PrototypeTuning proto)
        {
            switch (preset)
            {
                case TuningPreset.Agir:
                    ApplyAgir(combat, proto);
                    break;
                case TuningPreset.Cevik:
                    ApplyCevik(combat, proto);
                    break;
                case TuningPreset.Anime:
                    ApplyAnime(combat, proto);
                    break;
            }
        }

        /// <summary>Ağır: yavaş ve tok — büyük yer değiştirme, uzun toparlanma.</summary>
        static void ApplyAgir(CombatTuning c, PrototypeTuning p)
        {
            c.Dodge.DistanceM = 4.6f;
            c.Dodge.DurationMs = 340;
            c.Dodge.GlideTailMs = 260;
            p.Player.DodgeGlideSpeedMps = 2.4f;

            c.Dodge.PerfectWindowMs = 170; // O2: tek pencere, < HARİKA
            c.Grade.HarikaGapMaxMs = 190;
            c.Grade.TemizGapMaxMs = 240; // < IframeMs (260)

            c.Sentence.DwellMs = 280;
            c.Sentence.CancelWindowMs[0] = 460;
            c.Sentence.CancelWindowMs[1] = 400;
            c.Sentence.CancelWindowMs[2] = 340;
            for (int i = 0; i < c.Sentence.Steps.Length; i++)
                c.Sentence.Steps[i].RecoverySec *= 1.3f;

            c.Boss.WindupMs = 800;
            c.Boss.RecoveryMs = 900;
            c.Boss.RadiusM = 6.0f;
            c.Boss.Damage = 28;
            c.Boss.ApproachSpeedMps = 1.8f;
            p.Boss.BossApproachStopPadM = 0.6f;
            p.Player.PlayerMaxHp = 30;

            p.Camera.FollowSmoothTimeSec = 0.26f;
            c.Feel.CameraPerfectZoomKick = 0.18f;
            c.Feel.CameraDodgeZoomKick = 0.11f;
            c.Feel.CameraRollDeg = 2.2f;
            c.Feel.ShakePerfectPx = 9f;
            c.Feel.ShakeHitPx = 20f;
            c.Feel.ShakeDecay = 4f;

            c.Feel.ReadoutSizePx = 110f;
            c.Feel.ReadoutHoldMs = 1200;
            c.Feel.ReadoutFadeMs = 650;
            c.Feel.ReadoutPunchScale = 1.6f;
        }

        /// <summary>Çevik: hızlı ve dar — kısa pencereler, çabuk sıfırlanan dodge, hafif his.</summary>
        static void ApplyCevik(CombatTuning c, PrototypeTuning p)
        {
            c.Dodge.DistanceM = 3.4f;
            c.Dodge.DurationMs = 200;
            c.Dodge.GlideTailMs = 150;
            p.Player.DodgeGlideSpeedMps = 4.2f;

            c.Dodge.PerfectWindowMs = 120; // O2: tek pencere, < HARİKA
            c.Grade.HarikaGapMaxMs = 130;
            c.Grade.TemizGapMaxMs = 190; // < IframeMs (260)

            c.Sentence.DwellMs = 170;
            c.Sentence.CancelWindowMs[0] = 360;
            c.Sentence.CancelWindowMs[1] = 300;
            c.Sentence.CancelWindowMs[2] = 250;
            for (int i = 0; i < c.Sentence.Steps.Length; i++)
                c.Sentence.Steps[i].RecoverySec *= 0.75f;

            c.Boss.WindupMs = 500;
            c.Boss.RecoveryMs = 550;
            c.Boss.ApproachSpeedMps = 3.0f;
            c.Boss.RespawnMaxSec = 1.0f;
            p.Boss.BossApproachStopPadM = 0.25f;

            p.Camera.FollowSmoothTimeSec = 0.10f;
            p.Camera.LookAheadM = 1.8f;
            c.Feel.CameraPerfectZoomKick = 0.10f;
            c.Feel.CameraDodgeZoomKick = 0.06f;
            c.Feel.ShakePerfectPx = 4f;
            c.Feel.ShakeHitPx = 10f;
            c.Feel.ShakeDecay = 9f;

            c.Feel.ReadoutSizePx = 84f;
            c.Feel.ReadoutHoldMs = 650;
            c.Feel.ReadoutFadeMs = 350;
            p.Hud.ReadoutPunchInSec = 0.08f;
        }

        /// <summary>Anime: dramatik — büyük kamera yumruğu ve parlak yazı.</summary>
        static void ApplyAnime(CombatTuning c, PrototypeTuning p)
        {
            c.Dodge.DistanceM = 5.0f;
            c.Dodge.DurationMs = 180;
            c.Dodge.GlideTailMs = 350;
            p.Player.DodgeGlideSpeedMps = 5.5f;

            c.Boss.WindupMs = 900;
            c.Boss.RadiusM = 6.5f;

            c.Feel.CameraPerfectZoomKick = 0.30f;
            c.Feel.CameraDodgeZoomKick = 0.18f;
            c.Feel.CameraRollDeg = 4f;
            c.Feel.ShakePerfectPx = 12f;
            c.Feel.ShakeHitPx = 24f;
            c.Feel.ShakeDecay = 4f;

            c.Feel.ReadoutSizePx = 130f;
            c.Feel.ReadoutGlow = 60f;
            c.Feel.ReadoutHoldMs = 1300;
            c.Feel.ReadoutFadeMs = 700;
            c.Feel.ReadoutPunchScale = 2.0f;
            p.Hud.ReadoutPunchInSec = 0.2f;
        }
    }
}
