using System;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    public enum SkillMotionKind : byte
    {
        None = 0,
        ShortBlink,
        ForwardDash,
        ZenitsuPass,
        PlaceMark
    }

    /// <summary>Saf C# hareket planı — Unity transform'a Game katmanı uygular.</summary>
    public readonly struct SkillMotionPlan
    {
        public static SkillMotionPlan None { get; } = default;

        public SkillMotionPlan(
            SkillMotionKind kind,
            float destX, float destZ,
            float faceX, float faceZ,
            float durationSec,
            int iframeMs,
            float slashCommitMult,
            string markType)
        {
            Kind = kind;
            DestX = destX;
            DestZ = destZ;
            FaceX = faceX;
            FaceZ = faceZ;
            DurationSec = durationSec;
            IframeMs = iframeMs;
            SlashCommitMult = slashCommitMult;
            MarkType = markType ?? string.Empty;
        }

        public SkillMotionKind Kind { get; }
        public float DestX { get; }
        public float DestZ { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public float DurationSec { get; }
        public int IframeMs { get; }
        /// <summary>&gt;0 ise BaseDamage=0 olsa da commit × bu kadar boss hasarı.</summary>
        public float SlashCommitMult { get; }
        public string MarkType { get; }
        public bool IsEmpty => Kind == SkillMotionKind.None;
    }

    public readonly struct SkillMotionContext
    {
        public SkillMotionContext(
            float casterX, float casterZ,
            float faceX, float faceZ,
            float bossX, float bossZ,
            bool bossAlive,
            float arenaHalfSizeM)
        {
            CasterX = casterX;
            CasterZ = casterZ;
            FaceX = faceX;
            FaceZ = faceZ;
            BossX = bossX;
            BossZ = bossZ;
            BossAlive = bossAlive;
            ArenaHalfSizeM = arenaHalfSizeM;
        }

        public float CasterX { get; }
        public float CasterZ { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public float BossX { get; }
        public float BossZ { get; }
        public bool BossAlive { get; }
        public float ArenaHalfSizeM { get; }
    }

    /// <summary>
    /// Fiil + sıfat → hareket/işaret planı. Kombo tablosu yok; action / verb_id / adjective_id.
    /// </summary>
    public static class SkillMotionMotor
    {
        public const string MarkTypeBeacon = "isaretli_nokta";

        public static SkillMotionPlan Resolve(
            SkillResolution skill,
            in SkillMotionContext ctx,
            SkillMotionTuning tuning)
        {
            if (skill.IsEmpty || tuning == null)
                return SkillMotionPlan.None;

            NormalizeFacing(ctx.FaceX, ctx.FaceZ, out float fx, out float fz);

            if (IsTeleportVerb(skill))
            {
                if (IsAnchorAdjective(skill))
                    return PlaceMarkAtCaster(ctx, tuning);

                if (ctx.BossAlive && Distance(ctx.CasterX, ctx.CasterZ, ctx.BossX, ctx.BossZ) <= tuning.ZenitsuEngageRangeM)
                    return ZenitsuBehindBoss(ctx, tuning, fx, fz);

                return BlinkAlong(
                    SkillMotionKind.ShortBlink,
                    ctx, tuning, fx, fz,
                    tuning.ShortBlinkDistanceM, tuning.BlinkDurationSec, 0, 0f);
            }

            if (IsLightningSlash(skill) && ctx.BossAlive)
                return ZenitsuBehindBoss(ctx, tuning, fx, fz);

            if (IsDashVerb(skill))
                return BlinkAlong(
                    SkillMotionKind.ForwardDash,
                    ctx, tuning, fx, fz,
                    tuning.ForwardDashDistanceM, tuning.DashDurationSec, 0, 0f);

            return SkillMotionPlan.None;
        }

        static SkillMotionPlan PlaceMarkAtCaster(in SkillMotionContext ctx, SkillMotionTuning t) =>
            new SkillMotionPlan(
                SkillMotionKind.PlaceMark,
                ctx.CasterX, ctx.CasterZ,
                0f, 1f,
                0f,
                0,
                0f,
                MarkTypeBeacon);

        static SkillMotionPlan ZenitsuBehindBoss(in SkillMotionContext ctx, SkillMotionTuning t, float fx, float fz)
        {
            float dx = ctx.CasterX - ctx.BossX;
            float dz = ctx.CasterZ - ctx.BossZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 0.001f)
            {
                dx = -fx;
                dz = -fz;
                len = MathF.Sqrt(dx * dx + dz * dz);
                if (len < 0.001f) { dx = 0f; dz = -1f; len = 1f; }
            }

            dx /= len;
            dz /= len;
            float destX = Clamp(ctx.BossX - dx * t.ZenitsuBehindOffsetM, t.ArenaHalfSizeM);
            float destZ = Clamp(ctx.BossZ - dz * t.ZenitsuBehindOffsetM, t.ArenaHalfSizeM);
            float faceX = ctx.BossX - destX;
            float faceZ = ctx.BossZ - destZ;
            NormalizeFacing(faceX, faceZ, out faceX, out faceZ);

            return new SkillMotionPlan(
                SkillMotionKind.ZenitsuPass,
                destX, destZ,
                faceX, faceZ,
                t.ZenitsuDurationSec,
                t.ZenitsuIframeMs,
                t.ZenitsuSlashCommitMult,
                string.Empty);
        }

        static SkillMotionPlan BlinkAlong(
            SkillMotionKind kind,
            in SkillMotionContext ctx,
            SkillMotionTuning t,
            float fx, float fz,
            float distance,
            float duration,
            int iframeMs,
            float slashMult)
        {
            float destX = Clamp(ctx.CasterX + fx * distance, t.ArenaHalfSizeM);
            float destZ = Clamp(ctx.CasterZ + fz * distance, t.ArenaHalfSizeM);
            return new SkillMotionPlan(
                kind,
                destX, destZ,
                fx, fz,
                duration,
                iframeMs,
                slashMult,
                string.Empty);
        }

        public static bool IsTeleportVerb(SkillResolution skill)
        {
            if (string.Equals(skill.Action, "self_teleport", StringComparison.Ordinal))
                return true;
            if (string.Equals(skill.VerbId, "kisisel_isinlanma", StringComparison.Ordinal))
                return true;
            return false;
        }

        /// <summary>
        /// 16 Eylül düzeltmesi: "her yerden dash atıyorum" bug raporu — eski hali
        /// `VerbFamily == "motion"` olan HER fiili (Su'nun hız buff'ı, Hava'nın hız+
        /// görünmezliği dahil) dash'e çeviriyordu. Artık yalnızca gerçekten "dash" ya da
        /// "double_move" action'ına sahip fiiller (hareket/çift_hareket) dash tetikler.
        /// </summary>
        public static bool IsDashVerb(SkillResolution skill)
        {
            if (string.Equals(skill.Action, "dash", StringComparison.Ordinal))
                return true;
            if (string.Equals(skill.Action, "double_move", StringComparison.Ordinal))
                return true;
            if (string.Equals(skill.VerbId, "hareket", StringComparison.Ordinal))
                return true;
            if (string.Equals(skill.VerbId, "cift_hareket", StringComparison.Ordinal))
                return true;
            return false;
        }

        public static bool IsLightningSlash(SkillResolution skill) =>
            string.Equals(skill.VerbId, "zincirleme", StringComparison.Ordinal)
            || string.Equals(skill.AdjectiveId, "isnlama", StringComparison.Ordinal);

        public static bool IsAnchorAdjective(SkillResolution skill) =>
            string.Equals(skill.AdjectiveId, "sabitleme", StringComparison.Ordinal);

        static void NormalizeFacing(float x, float z, out float ox, out float oz)
        {
            float len = MathF.Sqrt(x * x + z * z);
            if (len < 0.001f)
            {
                ox = 0f;
                oz = 1f;
                return;
            }
            ox = x / len;
            oz = z / len;
        }

        static float Distance(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        static float Clamp(float v, float half) =>
            Math.Clamp(v, -half, half);
    }
}
