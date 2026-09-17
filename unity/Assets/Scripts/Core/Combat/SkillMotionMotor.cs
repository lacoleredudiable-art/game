using System;
using System.Collections.Generic;
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

    /// <summary>JSON space_layer.effects[].type — yalnızca mevcut hareketlere eşlenenler uygulanır.</summary>
    public static class SpaceEffectTypes
    {
        public const string ShortBlink = "short_blink";
        public const string PhaseBlink = "phase_blink";
        public const string StealthShift = "stealth_shift";
        public const string InvisibleLink = "invisible_link";
        public const string Tear = "tear";
    }

    /// <summary>
    /// Fiil + sıfat → hareket/işaret planı. Kombo tablosu yok; action / verb_id / adjective_id.
    /// space_layer eşleşen effect varsa distance/i_frame JSON otoritesi (tuning yedek).
    /// invisible_link / tear → SpaceDirector (Manifestation bang); burada blink değil.
    /// </summary>
    public static class SkillMotionMotor
    {
        public const string MarkTypeBeacon = "isaretli_nokta";

        public static SkillMotionPlan Resolve(
            SkillResolution skill,
            in SkillMotionContext ctx,
            SkillMotionTuning tuning,
            IReadOnlyList<SpaceEffectNode>? spaceEffects = null)
        {
            if (skill.IsEmpty || tuning == null)
                return SkillMotionPlan.None;

            NormalizeFacing(ctx.FaceX, ctx.FaceZ, out float fx, out float fz);

            if (IsTeleportVerb(skill))
            {
                if (IsAnchorAdjective(skill))
                    return PlaceMarkAtCaster(ctx, tuning);

                float engage = ResolvePhaseEngageRangeM(tuning, spaceEffects);
                if (ctx.BossAlive && Distance(ctx.CasterX, ctx.CasterZ, ctx.BossX, ctx.BossZ) <= engage)
                    return ZenitsuBehindBoss(ctx, tuning, fx, fz, spaceEffects);

                ResolveBlinkScalars(skill, tuning, spaceEffects, out float blinkDist, out int blinkIframe);
                return BlinkAlong(
                    SkillMotionKind.ShortBlink,
                    ctx, tuning, fx, fz,
                    blinkDist, tuning.BlinkDurationSec, blinkIframe, 0f);
            }

            if (IsLightningSlash(skill) && ctx.BossAlive)
                return ZenitsuBehindBoss(ctx, tuning, fx, fz, spaceEffects);

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

        static SkillMotionPlan ZenitsuBehindBoss(
            in SkillMotionContext ctx,
            SkillMotionTuning t,
            float fx,
            float fz,
            IReadOnlyList<SpaceEffectNode>? spaceEffects)
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

            int iframeMs = t.ZenitsuIframeMs;
            float slash = t.ZenitsuSlashCommitMult;
            if (TryFindSpace(spaceEffects, "Yıldırım", SpaceEffectTypes.PhaseBlink, out SpaceEffectNode phase)
                || TryFindSpace(spaceEffects, null, SpaceEffectTypes.PhaseBlink, out phase))
            {
                if (phase.HasIFrameMs)
                    iframeMs = phase.IFrameMs;
                if (phase.HasDamageOnPass && !phase.DamageOnPass)
                    slash = 0f;
            }

            return new SkillMotionPlan(
                SkillMotionKind.ZenitsuPass,
                destX, destZ,
                faceX, faceZ,
                t.ZenitsuDurationSec,
                iframeMs,
                slash,
                string.Empty);
        }

        static void ResolveBlinkScalars(
            SkillResolution skill,
            SkillMotionTuning tuning,
            IReadOnlyList<SpaceEffectNode>? spaceEffects,
            out float distanceM,
            out int iframeMs)
        {
            distanceM = tuning.ShortBlinkDistanceM;
            iframeMs = 0;

            if (TryPickBlinkEffect(spaceEffects, skill, out SpaceEffectNode fx))
            {
                if (fx.HasDistanceM)
                    distanceM = fx.DistanceM;
                if (fx.HasIFrameMs)
                    iframeMs = fx.IFrameMs;
            }
        }

        /// <summary>
        /// Blink tip seçimi: ElementOrigin → ElementName → FlavorElement → ilk stealth/short.
        /// Origin Alev iken Name=Pus olsa bile Alev short_blink kazanır.
        /// </summary>
        static bool TryPickBlinkEffect(
            IReadOnlyList<SpaceEffectNode>? effects,
            SkillResolution skill,
            out SpaceEffectNode found)
        {
            found = default;
            if (effects == null || effects.Count == 0)
                return false;

            if (TryBlinkForElement(effects, skill.ElementOrigin, out found))
                return true;
            if (TryBlinkForElement(effects, skill.ElementName, out found))
                return true;
            if (TryBlinkForElement(effects, skill.FlavorElement, out found))
                return true;
            if (TryFindSpace(effects, null, SpaceEffectTypes.StealthShift, out found))
                return true;
            return TryFindSpace(effects, null, SpaceEffectTypes.ShortBlink, out found);
        }

        static bool TryBlinkForElement(
            IReadOnlyList<SpaceEffectNode>? effects,
            string element,
            out SpaceEffectNode found)
        {
            found = default;
            if (string.IsNullOrEmpty(element))
                return false;
            // stealth_shift ve short_blink aynı "blink" ailesi — element hangisine sahipse o.
            if (TryFindSpace(effects, element, SpaceEffectTypes.StealthShift, out found))
                return true;
            return TryFindSpace(effects, element, SpaceEffectTypes.ShortBlink, out found);
        }

        static float ResolvePhaseEngageRangeM(
            SkillMotionTuning tuning,
            IReadOnlyList<SpaceEffectNode>? spaceEffects)
        {
            // phase_blink.distance_m → Zenitsu engage (durum.md karşılaştırma tablosu).
            if (TryFindSpace(spaceEffects, "Yıldırım", SpaceEffectTypes.PhaseBlink, out SpaceEffectNode phase)
                || TryFindSpace(spaceEffects, null, SpaceEffectTypes.PhaseBlink, out phase))
            {
                if (phase.HasDistanceM)
                    return phase.DistanceM;
            }
            return tuning.ZenitsuEngageRangeM;
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

        /// <summary>
        /// ElementOrigin / ElementName / FlavorElement ile effect.Element eşleşir.
        /// elementHint null → yalnızca tip (ilk eşleşen).
        /// </summary>
        public static bool TryFindSpace(
            IReadOnlyList<SpaceEffectNode>? effects,
            string? elementHint,
            string type,
            out SpaceEffectNode found)
        {
            found = default;
            if (effects == null || effects.Count == 0 || string.IsNullOrEmpty(type))
                return false;

            for (int i = 0; i < effects.Count; i++)
            {
                SpaceEffectNode e = effects[i];
                if (!string.Equals(e.Type, type, StringComparison.Ordinal))
                    continue;
                if (string.IsNullOrEmpty(elementHint)
                    || string.Equals(e.Element, elementHint, StringComparison.OrdinalIgnoreCase))
                {
                    found = e;
                    return true;
                }
            }
            return false;
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
