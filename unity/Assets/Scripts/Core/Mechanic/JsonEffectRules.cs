using System;
using System.Collections.Generic;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// JSON etkileri: element-sistemi.json mod/anahtarlarının saf kuralları.
    /// Game katmanı yalnız uygular; sayılar mechanic_grammar.params'tan gelir.
    /// </summary>
    public static class JsonEffectRules
    {
        static readonly string[] BasePushModes = { "dalga", "inis_dalgasi", "sis_patlamasi", "iki_uctan" };
        static readonly string[] HardPushModes = { "sert", "tek_hedef" };

        static bool Any(MechanicPlan plan, Func<MechanicEffect, bool> match) =>
            plan != null && plan.Effects.Any(match);

        // A1/A2 — konum:it>dusman itme mesafesi. 0 = kendi yolunda (akinti/tuzak/yukari_firlat) ya da itme yok.
        public static float PushMeters(MechanicPlan plan, MechanicRules rules)
        {
            if (plan == null || rules == null)
                return 0f;
            MechanicEffect push = plan.Effects.FirstOrDefault(e => e.Atom == "konum" && e.Stat == "it" && e.Target == "dusman");
            if (push == null || push.Has("akinti") || push.Has("tuzak") || push.Has("yukari_firlat"))
                return 0f;
            double baseM = rules.Param("it_push_m");
            if (baseM <= 0)
                return 0f;
            double mult = 1;
            if (HardPushModes.Any(push.Has))
                mult = rules.Param("it_hard_mult") > 0 ? rules.Param("it_hard_mult") : 1;
            else if (BasePushModes.Any(push.Has))
                mult = 1;
            return (float)(baseM * mult);
        }

        /// <summary>3-5 iniş dalgası: kendine yönelik fiil olsa da itme uygulanır.</summary>
        public static bool LandingWavePush(MechanicPlan plan) =>
            Any(plan, e => e.Stat == "kendini_tasi" && e.Has("inis_dalgasi"))
            && Any(plan, e => e.Stat == "it" && e.Target == "dusman" && e.Has("dalga"));

        // H3 — havaya_at: boss'u görsel olarak kaldır.
        public static bool LiftsBoss(MechanicPlan plan) => Any(plan, e => e.Target == "dusman" && e.Has("havaya_at"));

        // B1 — zirh>kendin [aktarim]: çalınan zırh oranı × boss taban zırhı = düz zırh buff'ı.
        public static float StolenArmorFlat(double amount, float bossBaseArmor) =>
            amount <= 0 || bossBaseArmor <= 0 ? 0f : (float)(amount * bossBaseArmor);

        // C — yansıtma türleri
        public static bool HasReflectMode(MechanicPlan plan, string mode) => Any(plan, e => e.Stat == "yansit" && e.Has(mode));
        public static bool IsParry(MechanicPlan plan) => HasReflectMode(plan, "savusturma");
        public static bool IsSplitReflect(MechanicPlan plan) => HasReflectMode(plan, "bolunen");
        public static bool IsWorldMirror(MechanicPlan plan) => HasReflectMode(plan, "ayna_yuzey");
        public static bool IsRampReflect(MechanicPlan plan) => HasReflectMode(plan, "artan_oran");
        public static bool ReflectorFollowsAlly(MechanicPlan plan) => HasReflectMode(plan, "dokunulana");

        public static float ParryRatio(MechanicPlan plan)
        {
            MechanicEffect e = plan?.Effects.FirstOrDefault(x => x.Stat == "yansit" && x.Has("savusturma"));
            return e == null ? 0f : (float)Math.Min(1.0, Math.Max(0.0, e.Amount));
        }

        public static void SplitReflect(float amount, out float first, out float second)
        {
            first = amount * 0.5f;
            second = amount - first;
        }

        public static float RampedRatio(float baseRatio, double startMs, double untilMs, double nowMs, double rampMax)
        {
            if (untilMs <= startMs)
                return baseRatio;
            double t = Math.Min(1.0, Math.Max(0.0, (nowMs - startMs) / (untilMs - startMs)));
            double max = Math.Max(1.0, rampMax);
            return (float)(baseRatio * (1.0 + (max - 1.0) * t));
        }

        /// <summary>gizli: yansıtma (10-7) ya da silme (9-7) sürerken görünmezlik süresi (sn). 0 = yok.</summary>
        public static double HiddenSec(MechanicPlan plan, double reflectSec)
        {
            if (plan == null)
                return 0;
            double sec = 0;
            foreach (MechanicEffect e in plan.Effects)
            {
                if (!e.Has("gizli"))
                    continue;
                double d = e.Stat == "yansit" ? reflectSec : Math.Max(e.DurationSec, plan.Body.LifeSec);
                sec = Math.Max(sec, d);
            }
            return sec;
        }

        // D1 — durum_ekle
        public static int StatusAddCount(double amount) => Math.Max(1, (int)Math.Round(amount));

        // D2 — cleanse_count (+ tumunu_sil). int.MaxValue = hepsi.
        public static int CleanseCount(int engineCount, MechanicPlan plan)
        {
            if (Any(plan, e => e.Has("tumunu_sil")))
                return int.MaxValue;
            return engineCount > 0 ? engineCount : int.MaxValue;
        }

        // D3 — guce_cevir: silinen her durum için güç.
        public static bool PurgeGrantsPower(MechanicPlan plan) => Any(plan, e => e.Has("guce_cevir"));
        public static float PurgePower(int removed, double perStatus) =>
            removed <= 0 || perStatus <= 0 ? 0f : (float)(removed * perStatus);

        // D4 — ters_kopya on hasar_buff (8-10): boss'a zayıflatma.
        public static MechanicEffect MirroredEnemyDebuff(MechanicPlan plan) =>
            plan?.Effects.FirstOrDefault(e => e.Stat == "hasar_buff" && e.Target == "dusman" && e.Has("ters_kopya"));

        // E — dost hedefleri
        public static int FriendlyCap(int engineMaxTargets) => engineMaxTargets <= 0 ? 1 : engineMaxTargets;

        public static void SelectHealTargets(
            bool allyNeeds, bool selfNeeds, float allyRatio, float selfRatio,
            bool preferAlly, bool preferSelf, int cap,
            out bool healAlly, out bool healSelf)
        {
            bool many = cap >= 2;
            if (preferAlly)
            {
                healAlly = allyNeeds;
                healSelf = many && selfNeeds;
                return;
            }
            if (preferSelf)
            {
                healSelf = selfNeeds;
                healAlly = many && allyNeeds;
                return;
            }
            if (many)
            {
                healAlly = allyNeeds;
                healSelf = selfNeeds;
                return;
            }
            if (allyNeeds && selfNeeds)
            {
                healAlly = allyRatio < selfRatio; // eşitse kendine
                healSelf = !healAlly;
                return;
            }
            healAlly = allyNeeds;
            healSelf = !allyNeeds && selfNeeds;
        }

        public static bool IsFriendlyBounce(MechanicPlan plan) => Any(plan, e => e.Has("dosttan_dosta"));
        public static bool NextBounceIsAlly(bool lastWasAlly) => !lastWasAlly;

        public static bool Overflows(MechanicPlan plan, string stat) => Any(plan, e => e.Stat == stat && e.Has("tasar"));
        public static int OverflowHeal(int amount, int healed) => Math.Max(0, amount - Math.Max(0, healed));

        // F/G — dünya yükü (hacim tiki)
        public static VolumePayloadKind PayloadKind(MechanicEffect e)
        {
            if (e == null || e.Stat is "aktor_yarat" or "klon")
                return VolumePayloadKind.None;
            if (e.Has("tuzak")) return VolumePayloadKind.Trap;
            if (e.Has("totem")) return VolumePayloadKind.Totem;
            if (e.Has("bulut_tik")) return VolumePayloadKind.CloudTick;
            if (e.Has("buyuyen")) return VolumePayloadKind.Growing;
            return VolumePayloadKind.None;
        }

        public static bool HasPayload(MechanicPlan plan, VolumePayloadKind kind) =>
            Any(plan, e => PayloadKind(e) == kind);

        public static bool NeedsPayloadVolume(MechanicPlan plan) =>
            Any(plan, e => PayloadKind(e) != VolumePayloadKind.None);

        public static double TrapArmSec(MechanicBody body, MechanicRules rules)
        {
            if (rules == null)
                return 0;
            return body != null && body.Traits.Contains("mayin") ? rules.Param("mine_arm_sec") : rules.Param("trap_arm_sec");
        }

        /// <summary>cit (çapalı ışın): tuzak bir kez değil her tikte yakar.</summary>
        public static bool TrapRepeats(MechanicBody body) => body != null && body.Traits.Contains("cit");

        /// <summary>inen_akis_alani: akış tiki yalnız iniş alanındaki hedefe.</summary>
        public static bool LandingFieldOnly(MechanicBody body) => body != null && body.Traits.Contains("inen_akis_alani");

        public static float PayloadScale(VolumePayloadKind kind, double flowFraction, double trapMult, float growth)
        {
            double f = flowFraction > 0 ? flowFraction : MechanicDefaults.DefaultFlowFraction;
            switch (kind)
            {
                case VolumePayloadKind.Totem:
                case VolumePayloadKind.CloudTick: return (float)f;
                case VolumePayloadKind.Growing: return (float)(f * Math.Max(1f, growth));
                case VolumePayloadKind.Trap: return (float)(trapMult > 0 ? trapMult : 0.5);
                default: return 0f;
            }
        }

        public static bool AnyActorGrows(MechanicPlan plan) =>
            Any(plan, e => e.Stat is "aktor_yarat" or "klon" && e.Has("buyuyen"));

        // G3 — delici gövde: boss kalkanını ve hasar azaltmasını deler.
        public static bool PiercesDefenses(MechanicBody body) => body != null && body.Permeability == "delici";

        // H1/H2
        public static bool LinkFlowsDamage(MechanicPlan plan) => Any(plan, e => e.Has("bag_akisi"));
        public static bool WantsPincer(MechanicPlan plan) => Any(plan, e => e.Has("kiskac"));

        // I1 — aoe: alan vuruşu, boss alan içindeyse değer.
        public static bool AoeConnects(bool aoe, float distanceM, float hitRadiusM, float bodySizeM, float bossRadiusM) =>
            aoe && distanceM <= Math.Max(hitRadiusM, bodySizeM) + Math.Max(0f, bossRadiusM);

        // J — silah düz vuruşu
        public static float BasicSubHitScale(int hits) => 1f / Math.Max(1, hits);
        public static double BasicSubHitDelaySec(int index, float intervalSec) => Math.Max(0, index) * Math.Max(0f, intervalSec);

        public static bool BasicReady(double nowMs, double lastBasicMs, float intervalSec, int hits, bool enforce) =>
            !enforce || lastBasicMs < 0 || intervalSec <= 0f
            || nowMs - lastBasicMs >= intervalSec * Math.Max(1, hits) * MechanicDefaults.SecToMs - 0.5; // 0.5 ms float payı

        public static string BasicKindLabel(string kind, int hits)
        {
            if (string.IsNullOrEmpty(kind))
                return string.Empty;
            return hits > 1 ? kind + " ×" + hits : kind;
        }
    }
}
