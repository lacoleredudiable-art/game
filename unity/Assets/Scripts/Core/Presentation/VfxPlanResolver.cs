using System;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Presentation
{
    /// <summary>
    /// efekt-motoru §6: <c>VfxPlan = Coz(SkillSpec)</c>.
    /// Dilim tam: Kılıç + Hareket dash (ATIL) + Zarar isabet motifi. Diğer fiil/silah soketleri
    /// renk/şekil/taşıyıcı döner; oynatma katmanı sonra bağlanır.
    /// </summary>
    public static class VfxPlanResolver
    {
        public static VfxPlan Resolve(in SkillResolution skill, string weaponAnimationsKey)
        {
            if (skill.IsEmpty)
                return VfxPlan.Empty;

            string weapon = NormalizeWeaponKey(weaponAnimationsKey);
            int verbId = ParseRuneId(skill.Identity.Verb);
            int adjId = ParseRuneId(skill.Identity.Adjective);
            bool isDash = skill.Presentation.Action.Value == SkillActionWire.Kind.Dash
                || string.Equals(
                    skill.Presentation.Action.Raw,
                    VfxPlanDefaults.ActionDash,
                    StringComparison.OrdinalIgnoreCase);

            ResolveVerb(verbId, skill.Identity.VerbName, out VfxColorRgb color, out VfxMotifKind motif);
            VfxShapePrimitive shape = ResolveShape(adjId, skill.Identity.AdjectiveName);
            ResolveCarrier(weapon, out VfxCarrierKind carrier, out VfxDeliveryClass delivery);
            VfxEdgeStyle edge = ResolveEdge(verbId);

            bool kilic = string.Equals(weapon, VfxPlanDefaults.WeaponKeyKilic, StringComparison.Ordinal);
            bool hareket = verbId == (int)Rune.Move;
            bool zarar = verbId == (int)Rune.Attack;
            bool kilicAtil = kilic && hareket && isDash;

            bool slashOnHit = kilic && (hareket || zarar || delivery == VfxDeliveryClass.Melee);
            // Zarar kombo / hasar isabeti: pençe kesikleri. Hareket dash isabetinde de dilim gösterir
            // (kullanıcı "Zarar combo" dikey dilim isteği); fiil Zarar olmasa bile hasar > 0 ise açılır.
            // Dilim: Zenitsu isabetinde Zarar pençe katmanı (kullanıcı "Zarar combo" isteği).
            bool zararClaws = zarar || kilicAtil;

            return new VfxPlan(
                coreColor: color,
                motif: motif,
                edge: edge,
                shape: shape,
                carrier: carrier,
                delivery: delivery,
                skillAwaken: true,
                runeLetterFlash: true,
                lightningDashTrail: kilicAtil,
                wingFootSparks: kilicAtil && motif == VfxMotifKind.Wing,
                dragonTailArcSilhouette: kilicAtil,
                slashArcOnHit: slashOnHit,
                zararClawMarksOnHit: zararClaws,
                edgeStopEmberSpark: kilicAtil,
                trailLifeSec: kilicAtil ? VfxPlanDefaults.KilicIzAtilOmurSec : 0f,
                silhouetteLifeSec: kilicAtil ? VfxPlanDefaults.EjderKilicAtilOmurSec : 0f,
                dragonAtlasCell: kilicAtil ? VfxPlanDefaults.EjderAtlasCellF : -1,
                verbRuneId: verbId,
                adjectiveRuneId: adjId,
                weaponKey: weapon);
        }

        /// <summary>Zarar isabet katmanı (fiil Saldırı/Zarar veya açık bayrak).</summary>
        public static bool WantsZararHitMotif(in VfxPlan plan) => plan.ZararClawMarksOnHit;

        /// <summary>§1 fiil rengi; plan dışı tetikler (skill_anim entry_silhouette) için.</summary>
        public static VfxColorRgb VerbColor(int verbId)
        {
            ResolveVerb(verbId, string.Empty, out VfxColorRgb color, out _);
            return color;
        }

        static void ResolveVerb(int verbId, string verbName, out VfxColorRgb color, out VfxMotifKind motif)
        {
            // Spec fiil adları (Zarar…) + JSON verb_face (Saldırı…) birlikte.
            if (verbId == (int)Rune.Attack || NameIs(verbName, "Zarar", "Saldırı"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.ZararR, VfxPlanDefaults.ZararG, VfxPlanDefaults.ZararB,
                    VfxPlanDefaults.ZararIntensity);
                motif = VfxMotifKind.Claw;
                return;
            }

            if (verbId == (int)Rune.Move || NameIs(verbName, "Hareket"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.HareketR, VfxPlanDefaults.HareketG, VfxPlanDefaults.HareketB,
                    VfxPlanDefaults.HareketIntensity);
                motif = VfxMotifKind.Wing;
                return;
            }

            if (verbId == (int)Rune.Weaken || NameIs(verbName, "Zayıflatma"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.ZayiflatmaR, VfxPlanDefaults.ZayiflatmaG, VfxPlanDefaults.ZayiflatmaB,
                    VfxPlanDefaults.ZayiflatmaIntensity);
                motif = VfxMotifKind.Soot;
                return;
            }

            if (verbId == (int)Rune.Summon || NameIs(verbName, "Çağırma"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.CagirmaR, VfxPlanDefaults.CagirmaG, VfxPlanDefaults.CagirmaB,
                    VfxPlanDefaults.CagirmaIntensity);
                motif = VfxMotifKind.Bone;
                return;
            }

            if (verbId == (int)Rune.Defense || NameIs(verbName, "Koruma", "Savunma"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.KorumaR, VfxPlanDefaults.KorumaG, VfxPlanDefaults.KorumaB,
                    VfxPlanDefaults.KorumaIntensity);
                motif = VfxMotifKind.Scale;
                return;
            }

            if (verbId == (int)Rune.Reflect || NameIs(verbName, "Yansıma"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.YansimaR, VfxPlanDefaults.YansimaG, VfxPlanDefaults.YansimaB,
                    VfxPlanDefaults.YansimaIntensity);
                motif = VfxMotifKind.ScaleFlash;
                return;
            }

            if (verbId == (int)Rune.Control || NameIs(verbName, "Kontrol"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.KontrolR, VfxPlanDefaults.KontrolG, VfxPlanDefaults.KontrolB,
                    VfxPlanDefaults.KontrolIntensity);
                motif = VfxMotifKind.TailChain;
                return;
            }

            if (verbId == (int)Rune.Burst || NameIs(verbName, "Kuvvet", "Patlama"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.KuvvetR, VfxPlanDefaults.KuvvetG, VfxPlanDefaults.KuvvetB,
                    VfxPlanDefaults.KuvvetIntensity);
                motif = VfxMotifKind.TailArc;
                return;
            }

            if (verbId == (int)Rune.Heal || NameIs(verbName, "Şifa", "İyileştirme"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.SifaR, VfxPlanDefaults.SifaG, VfxPlanDefaults.SifaB,
                    VfxPlanDefaults.SifaIntensity);
                motif = VfxMotifKind.BreathMist;
                return;
            }

            if (verbId == (int)Rune.Empower || NameIs(verbName, "Güçlendirme"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.GuclendirmeR, VfxPlanDefaults.GuclendirmeG, VfxPlanDefaults.GuclendirmeB,
                    VfxPlanDefaults.GuclendirmeIntensity);
                motif = VfxMotifKind.HeartEmber;
                return;
            }

            if (verbId == (int)Rune.Cleanse || NameIs(verbName, "Arındırma"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.ArindirmaR, VfxPlanDefaults.ArindirmaG, VfxPlanDefaults.ArindirmaB,
                    VfxPlanDefaults.ArindirmaIntensity);
                motif = VfxMotifKind.ScaleShed;
                return;
            }

            if (verbId == (int)Rune.Time || NameIs(verbName, "Zaman"))
            {
                color = VfxColorRgb.FromLinear(
                    VfxPlanDefaults.ZamanR, VfxPlanDefaults.ZamanG, VfxPlanDefaults.ZamanB,
                    VfxPlanDefaults.ZamanIntensity);
                motif = VfxMotifKind.RingScale;
                return;
            }

            color = VfxColorRgb.FromLinear(
                VfxPlanDefaults.HareketR, VfxPlanDefaults.HareketG, VfxPlanDefaults.HareketB,
                VfxPlanDefaults.HareketIntensity);
            motif = VfxMotifKind.None;
        }

        static VfxShapePrimitive ResolveShape(int adjId, string adjName)
        {
            // Spec §2 sıra numarası = rün id (1..12) ile hizalı; yüz adı yedek.
            if (adjId == 1 || NameHas(adjName, "Yoğun"))
                return VfxShapePrimitive.CollapsePoint;
            if (adjId == 2 || NameHas(adjName, "Odak"))
                return VfxShapePrimitive.TargetLock;
            if (adjId == 3 || NameHas(adjName, "Sıçra"))
                return VfxShapePrimitive.BounceArc;
            if (adjId == 4 || NameHas(adjName, "Sabit"))
                return VfxShapePrimitive.Structure;
            if (adjId == 5 || NameHas(adjName, "Yay"))
                return VfxShapePrimitive.ExpandingRing;
            if (adjId == 6 || NameHas(adjName, "Güdüm"))
                return VfxShapePrimitive.LockTrail;
            if (adjId == 7 || NameHas(adjName, "Tetik"))
                return VfxShapePrimitive.TrapMark;
            if (adjId == 8 || NameHas(adjName, "Fedakar"))
                return VfxShapePrimitive.CostFlow;
            if (adjId == 9 || NameHas(adjName, "İşaret"))
                return VfxShapePrimitive.MarkChain;
            if (adjId == 10 || NameHas(adjName, "Çizgi"))
                return VfxShapePrimitive.Line;
            if (adjId == 11 || NameHas(adjName, "Ortak"))
                return VfxShapePrimitive.LoadRing;
            if (adjId == 12 || NameHas(adjName, "Yörünge"))
                return VfxShapePrimitive.OrbitParts;
            return VfxShapePrimitive.None;
        }

        static void ResolveCarrier(string weapon, out VfxCarrierKind carrier, out VfxDeliveryClass delivery)
        {
            switch (weapon)
            {
                case "yumruk":
                    carrier = VfxCarrierKind.FistRing;
                    delivery = VfxDeliveryClass.Melee;
                    return;
                case "yay":
                    carrier = VfxCarrierKind.EmberArrow;
                    delivery = VfxDeliveryClass.Projectile;
                    return;
                case "kitap":
                    carrier = VfxCarrierKind.LetterCluster;
                    delivery = VfxDeliveryClass.Projectile;
                    return;
                case VfxPlanDefaults.WeaponKeyKilic:
                    carrier = VfxCarrierKind.FlowingEmberTrail;
                    delivery = VfxDeliveryClass.Melee;
                    return;
                case "kure":
                    carrier = VfxCarrierKind.PalmCone;
                    delivery = VfxDeliveryClass.Area;
                    return;
                case "cekic":
                    carrier = VfxCarrierKind.GroundCrack;
                    delivery = VfxDeliveryClass.Melee;
                    return;
                case "top":
                    carrier = VfxCarrierKind.CannonBall;
                    delivery = VfxDeliveryClass.Projectile;
                    return;
                case "asa":
                    carrier = VfxCarrierKind.BeamQuad;
                    delivery = VfxDeliveryClass.Area;
                    return;
                case "tilsim":
                    carrier = VfxCarrierKind.SealDecal;
                    delivery = VfxDeliveryClass.Area;
                    return;
                case "kalkan":
                    carrier = VfxCarrierKind.ShieldShock;
                    delivery = VfxDeliveryClass.Melee;
                    return;
                default:
                    carrier = VfxCarrierKind.None;
                    delivery = VfxDeliveryClass.None;
                    return;
            }
        }

        static VfxEdgeStyle ResolveEdge(int verbId)
        {
            // Zararlı fiiller keskin; yararlı yumuşak; Hareket kendi üzerinde → yapışık.
            return verbId switch
            {
                (int)Rune.Attack or (int)Rune.Burst or (int)Rune.Control or (int)Rune.Weaken
                    => VfxEdgeStyle.Sharp,
                (int)Rune.Heal or (int)Rune.Empower or (int)Rune.Cleanse or (int)Rune.Defense
                    or (int)Rune.Reflect
                    => VfxEdgeStyle.SoftRising,
                (int)Rune.Move => VfxEdgeStyle.BodyCling,
                _ => VfxEdgeStyle.Sharp
            };
        }

        static string NormalizeWeaponKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            // Türkçe görünen ad → animations_key
            if (string.Equals(key, "Kılıç", StringComparison.OrdinalIgnoreCase))
                return VfxPlanDefaults.WeaponKeyKilic;
            return key.Trim().ToLowerInvariant();
        }

        static int ParseRuneId(RuneId id)
        {
            string raw = id;
            return int.TryParse(raw, out int n) ? n : 0;
        }

        static bool NameIs(string name, params string[] options)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            for (int i = 0; i < options.Length; i++)
            {
                if (string.Equals(name, options[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static bool NameHas(string name, string fragment)
        {
            return !string.IsNullOrEmpty(name)
                && name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
