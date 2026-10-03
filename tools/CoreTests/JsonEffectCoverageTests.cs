using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoreTests;

/// <summary>
/// JSON etkileri CI kapısı: docs/element-sistemi.json'daki her motor anahtarı, gramer statı/modu,
/// gövde özelliği ve silah düz vuruş alanı ya bir koda bağlı (Handlers) ya da boss tasarımına
/// açıkça ertelenmiş (Deferred) olmalı. Yeni bir anahtar eklenip kodu yazılmazsa bu test kırılır.
/// </summary>
[TestFixture]
public class JsonEffectCoverageTests
{
    // Bu dosyalar anahtarı ÜRETİR ya da yalnız etiketler; kanıt sayılmaz.
    static readonly string[] ProducerFiles =
    {
        "MechanicGrammar.cs", "MechanicLabels.cs", "MechanicVisual.cs", "MechanicDescriber.cs"
    };

    static readonly string[] CheckedTraits = { "mayin", "cit", "inen_akis_alani" };

    // kind, key, evidence file (repo-relative), token that must appear in that file
    static readonly (string Kind, string Key, string File, string Token)[] Handlers =
    {
        ("mode", "akinti", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"akinti\""),
        ("mode", "akis", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "\"akis\""),
        ("mode", "aktarim", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"aktarim\""),
        ("mode", "alan", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"alan\""),
        ("mode", "ardinda_kopya", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "SpawnMechanicDecoy"),
        ("mode", "arinma_alani", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"arinma_alani\""),
        ("mode", "artan_oran", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"artan_oran\""),
        ("mode", "aura", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"aura\""),
        ("mode", "ayna_klon", "unity/Assets/Scripts/Core/Mechanic/MechanicWorldProfile.cs", "\"ayna_klon\""),
        ("mode", "ayna_sifati", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "GrantReflect"),
        ("mode", "ayna_yuzey", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"ayna_yuzey\""),
        ("mode", "bag", "unity/Assets/Scripts/Core/Mechanic/MechanicModel.cs", "\"bag\""),
        ("mode", "bag_akisi", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"bag_akisi\""),
        ("mode", "bag_bagisiklik", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"bag_bagisiklik\""),
        ("mode", "bag_boyunca", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "PullBossToPlayerContact"),
        ("mode", "bag_ucu", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"link:\""),
        ("mode", "bagli_muhafiz", "unity/Assets/Scripts/Core/Mechanic/MechanicWorldProfile.cs", "\"bagli_muhafiz\""),
        ("mode", "bataklik", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"bataklik\""),
        ("mode", "bolunen", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"bolunen\""),
        ("mode", "bulut_ici", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"bulut_ici\""),
        ("mode", "bulut_tik", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"bulut_tik\""),
        ("mode", "buyuyen", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"buyuyen\""),
        ("mode", "can_bagi", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"hasar_paylasimi\""),
        ("mode", "can_emen", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"can_emen\""),
        ("mode", "cana_cevir", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "_emHealRatio"),
        ("mode", "dalga", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"dalga\""),
        ("mode", "dikkat_ceker", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"dikkat_ceker\""),
        ("mode", "dokunulana", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"dokunulana\""),
        ("mode", "dondur", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"dondur\""),
        ("mode", "dosttan_dosta", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"dosttan_dosta\""),
        ("mode", "dunyada", "unity/Assets/Scripts/Core/Mechanic/MechanicWorldProfile.cs", "Reflector"),
        ("mode", "emme", "unity/Assets/Scripts/Core/Mechanic/DrainNumbers.cs", "TryShare"),
        ("mode", "faz", "unity/Assets/Scripts/Core/Motion/MotionTemplateCatalog.cs", "\"faz\""),
        ("mode", "geri_donus", "unity/Assets/Scripts/Core/Motion/PositionOwnership.cs", "\"isaret_geri_don\""),
        ("mode", "geri_sarma", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "RewindBoss"),
        ("mode", "gizli", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"gizli\""),
        ("mode", "gorunmez", "unity/Assets/Scripts/Game/Skills/Execution/SummonExecutor.cs", "\"gorunmez\""),
        ("mode", "gorunmez_gecis", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "StatusKind.Stealth"),
        ("mode", "guce_cevir", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"guce_cevir\""),
        ("mode", "guclu", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "ActorHitDamage"),
        ("mode", "halka", "unity/Assets/Scripts/Game/Skills/Execution/SummonExecutor.cs", "\"halka\""),
        ("mode", "havada", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"havada\""),
        ("mode", "havaya_at", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"havaya_at\""),
        ("mode", "hizlanma", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "StatusKind.Haste"),
        ("mode", "iki_kez", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "\"iki_kez\""),
        ("mode", "iki_uc", "unity/Assets/Scripts/Core/Motion/PositionOwnership.cs", "\"yer_degistir\""),
        ("mode", "iki_uctan", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"iki_uctan\""),
        ("mode", "inis_dalgasi", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"inis_dalgasi\""),
        ("mode", "isaretli_an", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "\"isaretli_an\""),
        ("mode", "isinlanma", "unity/Assets/Scripts/Core/Motion/PositionOwnership.cs", "\"hedefin_arkasina\""),
        ("mode", "iskalamaz", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "order.Homing"),
        ("mode", "kiskac", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"kiskac\""),
        ("mode", "koruyucu_tetik", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"koruyucu_tetik\""),
        ("mode", "merkeze", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "PullBossToPlayerContact"),
        ("mode", "portal_cifti", "unity/Assets/Scripts/Core/Portal/PortalSystem.cs", "IsPortalSkill"),
        ("mode", "sana_dogru", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "PullBossToPlayerContact"),
        ("mode", "savusturma", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"savusturma\""),
        ("mode", "senin_kopyan", "unity/Assets/Scripts/Game/Skills/Execution/SummonExecutor.cs", "\"klon\""),
        ("mode", "senkron", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"senkron\""),
        ("mode", "sersem", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"sersem\""),
        ("mode", "sert", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"sert\""),
        ("mode", "sicrayip_cakil", "docs/motion-templates.json", "\"3-8\""),
        ("mode", "sis_patlamasi", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"sis_patlamasi\""),
        ("mode", "suikastci", "unity/Assets/Scripts/Core/Mechanic/MechanicWorldProfile.cs", "\"suikastci\""),
        ("mode", "surekli", "unity/Assets/Scripts/Core/Mechanic/MechanicModel.cs", "\"surekli\""),
        ("mode", "suzulme", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.TemplateDelivery.cs", "\"suzulme\""),
        ("mode", "taret", "unity/Assets/Scripts/Core/Mechanic/MechanicWorldProfile.cs", "\"taret\""),
        ("mode", "tasar", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"tasar\""),
        ("mode", "tasma", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "MoveHomeToward"),
        ("mode", "ters_kontrol", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"ters_kontrol\""),
        ("mode", "tek_hedef", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"tek_hedef\""),
        ("mode", "ters_hedef", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "PurgeBossBuffs"),
        ("mode", "ters_kopya", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"ters_kopya\""),
        ("mode", "titrer", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"titrer\""),
        ("mode", "totem", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"totem\""),
        ("mode", "tumunu_sil", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"tumunu_sil\""),
        ("mode", "tuzak", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"tuzak\""),
        ("mode", "uzun", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "case (\"hiz\", \"hareket\")"),
        ("mode", "yanki", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "\"onceki_skill_tekrar\""),
        ("mode", "yavas", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"link:\""),
        ("mode", "yukari_firlat", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"yukari_firlat\""),
        ("mode", "yukselen", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"self_damage_buff\""),
        ("mode", "zaman_alani", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"zaman_alani\""),
        ("mode", "ziplayan", "unity/Assets/Scripts/Game/Skills/Execution/SummonExecutor.cs", "\"ziplayan\""),
        ("mode", "zirh_yoksay", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Damage.cs", "\"ignore_armor\""),
        ("stat", "aktor_yarat", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"aktor_yarat\""),
        ("stat", "can", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MotionTemplate.cs", "\"can\""),
        ("stat", "cek", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"cek\""),
        ("stat", "durum_aktar", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"durum_aktar\""),
        ("stat", "durum_ekle", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"durum_ekle\""),
        ("stat", "durum_sil", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"durum_sil\""),
        ("stat", "em", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"em\""),
        ("stat", "geri_sar", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"geri_sar\""),
        ("stat", "gizlen", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"gizlen\""),
        ("stat", "hareket", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"hareket\""),
        ("stat", "hasar_buff", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"hasar_buff\""),
        ("stat", "hasar_paylasimi", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"hasar_paylasimi\""),
        ("stat", "hedefin_arkasina", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"hedefin_arkasina\""),
        ("stat", "isaret_geri_don", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"isaret_geri_don\""),
        ("stat", "it", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"it\""),
        ("stat", "iyi_durum_sil", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"iyi_durum_sil\""),
        ("stat", "kalkan", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MotionTemplate.cs", "\"kalkan\""),
        ("stat", "kendini_tasi", "docs/motion-templates.json", "\"3-1\""),
        ("stat", "klon", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"klon\""),
        ("stat", "kor", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"kor\""),
        ("stat", "onceki_skill_tekrar", "unity/Assets/Scripts/Core/Mechanic/TemplateDelivery.cs", "\"onceki_skill_tekrar\""),
        ("stat", "portal", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"portal\""),
        ("stat", "tempo", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"tempo\""),
        ("stat", "yansit", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"yansit\""),
        ("stat", "yem_kopya", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"yem_kopya\""),
        ("stat", "yer_degistir", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"yer_degistir\""),
        ("stat", "yonlendir", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"yonlendir\""),
        ("stat", "zirh", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicGrammar.cs", "\"zirh\""),
        ("engine", "accuracy_debuff", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"accuracy_debuff\""),
        ("engine", "action", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"action\""),
        ("engine", "aoe", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.JsonEffects.cs", "\"aoe\""),
        ("engine", "apply_root_sec", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"apply_root_sec\""),
        ("engine", "apply_slow", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"apply_slow\""),
        ("engine", "armor_add", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Damage.cs", "\"armor_add\""),
        ("engine", "base_cooldown", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"base_cooldown\""),
        ("engine", "base_cost", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"base_cost\""),
        ("engine", "base_damage", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"base_damage\""),
        ("engine", "base_heal", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"base_heal\""),
        ("engine", "base_poise", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"base_poise\""),
        ("engine", "bounce_damage_mult", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"bounce_damage_mult\""),
        ("engine", "bounce_targets", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"bounce_targets\""),
        ("engine", "buff_damage", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"buff_damage\""),
        ("engine", "buff_duration_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Damage.cs", "\"buff_duration_sec\""),
        ("engine", "cast_mobility", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"cast_mobility\""),
        ("engine", "cc_duration_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"cc_duration_sec\""),
        ("engine", "cc_kind", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"cc_kind\""),
        ("engine", "channel_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Weapons10.cs", "\"channel_sec\""),
        ("engine", "cleanse_count", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"cleanse_count\""),
        ("engine", "damage_mult", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"damage_mult\""),
        ("engine", "dash_distance_m", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MotionTemplate.cs", "\"dash_distance_m\""),
        ("engine", "debuff_armor", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Damage.cs", "\"debuff_armor\""),
        ("engine", "debuff_duration_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Damage.cs", "\"debuff_duration_sec\""),
        ("engine", "duplicate_cast", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"duplicate_cast\""),
        ("engine", "duplicate_damage_mult", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"duplicate_damage_mult\""),
        ("engine", "duplicate_delay_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"duplicate_delay_sec\""),
        ("engine", "enemy_slow", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"enemy_slow\""),
        ("engine", "hitbox", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"hitbox\""),
        ("engine", "hitbox_scale_mult", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MotionTemplate.cs", "\"hitbox_scale_mult\""),
        ("engine", "ignore_armor", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Damage.cs", "\"ignore_armor\""),
        ("engine", "label", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"label\""),
        ("engine", "lifesteal", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"lifesteal\""),
        ("engine", "lifetime_add", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"lifetime_add\""),
        ("engine", "max_targets", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.JsonEffects.cs", "\"max_targets\""),
        ("engine", "minion_count", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"minion_count\""),
        ("engine", "minion_duration_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"minion_duration_sec\""),
        ("engine", "no_global_timescale", "tools/CoreTests/JsonEffectCoverageTests.cs", "NoGlobalTimescale_IsEnforced"),
        ("engine", "poise_damage_mult", "unity/Assets/Scripts/Core/Grammar/SkillMotor.cs", "\"poise_damage_mult\""),
        ("engine", "reflect_duration_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"reflect_duration_sec\""),
        ("engine", "reflect_ratio", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"reflect_ratio\""),
        ("engine", "self_damage_buff", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.VerbExecution.cs", "\"self_damage_buff\""),
        ("engine", "self_haste", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"self_haste\""),
        ("engine", "shield_absorb", "unity/Assets/Scripts/Core/Status/StatusApplicator.cs", "\"shield_absorb\""),
        ("engine", "tempo_duration_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.cs", "\"tempo_duration_sec\""),
        ("engine", "tick_rate_mult", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.MechanicWorld.cs", "\"tick_rate_mult\""),
        ("trait", "mayin", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"mayin\""),
        ("trait", "cit", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"cit\""),
        ("trait", "inen_akis_alani", "unity/Assets/Scripts/Core/Mechanic/JsonEffectRules.cs", "\"inen_akis_alani\""),
        ("basic", "ally_heal", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.JsonEffects.cs", "BasicAllyHeal"),
        ("stat", "mermi_sil", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "ProjectileEraseRules.For"),
        ("stat", "mermi_sil", "unity/Assets/Scripts/Game/Boss/HostileProjectileHost.cs", "HostileProjectiles"),
        ("mode", "yut", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"yut\""),
        ("mode", "yut", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "EraseMode.Absorb"),
        ("mode", "engel", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"engel\""),
        ("mode", "geri_gonder", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"geri_gonder\""),
        ("mode", "geri_gonder", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "ReflectProjectile"),
        ("mode", "delici", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"delici\""),
        ("mode", "delici", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "EraseLine"),
        ("mode", "sis_perdesi", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"sis_perdesi\""),
        ("mode", "sis_perdesi", "unity/Assets/Scripts/Game/Boss/HostileProjectileHost.cs", "InShroud"),
        ("mode", "hedefli", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"hedefli\""),
        ("mode", "hedefli", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "EraseMostUrgent"),
        ("mode", "yukselen_perde", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"yukselen_perde\""),
        ("mode", "yukselen_perde", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "spec.GrowTo"),
        ("mode", "surekli_perde", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"surekli_perde\""),
        ("mode", "surekli_perde", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "EraseShape.Follow"),
        ("mode", "bag_hatti", "unity/Assets/Scripts/Core/Mechanic/ProjectileEraseRules.cs", "\"bag_hatti\""),
        ("mode", "bag_hatti", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Projectiles.cs", "EraseAlongLink"),
        ("engine", "decoy_aggro", "unity/Assets/Scripts/Core/Mechanic/MechanicRules.cs", "\"decoy_aggro\""),
        ("basic", "boss_push_m", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Weapons10.cs", "BossPushM"),
        ("basic", "hits", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.JsonEffects.cs", "BasicHits"),
        ("basic", "interval_sec", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.JsonEffects.cs", "BasicIntervalSec"),
        ("basic", "kind", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.JsonEffects.cs", "BasicKind"),
        ("basic", "radius_m", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.TemplateDelivery.cs", "BasicRadiusM"),
        ("basic", "reach_m", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Weapons10.cs", "BasicReachM"),
        ("basic", "recoil_m", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.Weapons10.cs", "RecoilM"),
        ("basic", "shape", "unity/Assets/Scripts/Game/Skills/ManifestationDirector.TemplateDelivery.cs", "HitShape"),
    };

    // Boss tasarımına ertelenenler — yalnız bunlar handler'sız kalabilir.
    static readonly Dictionary<string, string> Deferred = new(StringComparer.Ordinal)
    {
        ["seker"] = "chain to next hostile target, needs small monsters (boss design)",
        ["sekmeli"] = "dash chain between hostile targets, needs small monsters (boss design)",
    };

    static readonly string[] ExpectedDeferred =
    {
        "seker", "sekmeli"
    };

    string _root = null!;
    string _json = null!;
    MechanicGrammar _grammar = null!;
    MotionTemplateCatalog _motion = null!;
    readonly Dictionary<string, SortedSet<string>> _keys = new(StringComparer.Ordinal);
    readonly List<MechanicPlan> _plans = new();

    [OneTimeSetUp]
    public void Load()
    {
        _root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        if (!File.Exists(Path.Combine(_root, "docs", "element-sistemi.json")))
            _root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        _json = File.ReadAllText(Path.Combine(_root, "docs", "element-sistemi.json"));
        _grammar = new MechanicGrammar(MechanicRules.FromJson(_json));
        _motion = MotionTemplateCatalog.FromJson(File.ReadAllText(Path.Combine(_root, "docs", "motion-templates.json")));
        foreach (string kind in new[] { "engine", "stat", "mode", "trait", "basic" })
            _keys[kind] = new SortedSet<string>(StringComparer.Ordinal);

        using JsonDocument doc = JsonDocument.Parse(_json);
        JsonElement byVerb = doc.RootElement.GetProperty("skills").GetProperty("by_verb");
        foreach (JsonProperty verb in byVerb.EnumerateObject())
            foreach (JsonElement skill in verb.Value.GetProperty("skills").EnumerateArray())
                if (skill.TryGetProperty("engine", out JsonElement engine) && engine.ValueKind == JsonValueKind.Object)
                    foreach (JsonProperty p in engine.EnumerateObject())
                        _keys["engine"].Add(p.Name);
        foreach (JsonElement weapon in doc.RootElement.GetProperty("weapons").EnumerateArray())
            if (weapon.TryGetProperty("basic", out JsonElement basic) && basic.ValueKind == JsonValueKind.Object)
                foreach (JsonProperty p in basic.EnumerateObject())
                    _keys["basic"].Add(p.Name);

        foreach (MechanicWeapon w in _grammar.Rules.Weapons)
            for (int v = 1; v <= 12; v++)
                for (int a = 1; a <= 12; a++)
                {
                    MechanicPlan plan = _grammar.Compose(v, a, w);
                    _plans.Add(plan);
                    foreach (MechanicEffect e in plan.Effects)
                    {
                        _keys["stat"].Add(e.Stat);
                        foreach (string m in e.Modes)
                            if (!m.StartsWith("yol:", StringComparison.Ordinal) && !m.StartsWith("silahla:", StringComparison.Ordinal))
                                _keys["mode"].Add(m);
                    }
                    foreach (string t in CheckedTraits)
                        if (plan.Body.Traits.Contains(t))
                            _keys["trait"].Add(t);
                }
    }

    [Test]
    public void EveryJsonEffectKey_HasHandlerOrIsDeferred()
    {
        var failures = new List<string>();
        foreach (KeyValuePair<string, SortedSet<string>> kind in _keys)
        {
            foreach (string key in kind.Value)
            {
                if (Deferred.ContainsKey(key))
                    continue;
                var rows = Handlers.Where(h => h.Kind == kind.Key && h.Key == key).ToList();
                if (rows.Count == 0)
                {
                    failures.Add($"{kind.Key}:{key} — no handler row and not deferred");
                    continue;
                }
                foreach (var row in rows)
                {
                    string path = Path.Combine(_root, row.File.Replace('/', Path.DirectorySeparatorChar));
                    if (ProducerFiles.Contains(Path.GetFileName(path)))
                        failures.Add($"{kind.Key}:{key} — evidence {row.File} is a producer/label file");
                    else if (!File.Exists(path))
                        failures.Add($"{kind.Key}:{key} — evidence file missing: {row.File}");
                    else if (!File.ReadAllText(path).Contains(row.Token, StringComparison.Ordinal))
                        failures.Add($"{kind.Key}:{key} — token {row.Token} not found in {row.File}");
                }
            }
        }
        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void RouteModes_HaveTheirSystems()
    {
        var failures = new List<string>();
        foreach (MechanicPlan plan in _plans)
        {
            bool yol = plan.Effects.Any(e => e.Modes.Any(m => m.StartsWith("yol:", StringComparison.Ordinal)));
            if (yol && !_motion.TryPlay(plan.Verb + "-" + plan.Adjective, out _))
                failures.Add($"{plan.Verb}-{plan.Adjective}: yol: mode without motion template");
        }
        string summon = File.ReadAllText(Path.Combine(_root, "unity", "Assets", "Scripts", "Game", "Skills", "Execution", "SummonExecutor.cs"));
        Assert.That(summon, Does.Contain("silahla:"), "silahla: modes are consumed by SummonExecutor");
        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures.Distinct()));
    }

    [Test]
    public void Deferred_IsExactlyTheBossDesignList_AndNotStale()
    {
        Assert.That(Deferred.Keys, Is.EquivalentTo(ExpectedDeferred),
            "Only the boss-design items may be deferred. New gaps need code, not a whitelist entry.");
        var all = new HashSet<string>(_keys.Values.SelectMany(s => s), StringComparer.Ordinal);
        foreach (string key in Deferred.Keys)
            Assert.That(all.Contains(key), Is.True, $"deferred key {key} no longer appears in the JSON");
    }

    [Test]
    public void Handlers_NotStale()
    {
        foreach (var row in Handlers)
            Assert.That(_keys[row.Kind].Contains(row.Key), Is.True, $"handler row {row.Kind}:{row.Key} no longer appears in the JSON");
    }

    [Test]
    public void NoGlobalTimescale_IsEnforced()
    {
        using JsonDocument doc = JsonDocument.Parse(_json);
        int seen = 0;
        foreach (JsonProperty verb in doc.RootElement.GetProperty("skills").GetProperty("by_verb").EnumerateObject())
            foreach (JsonElement skill in verb.Value.GetProperty("skills").EnumerateArray())
                if (skill.TryGetProperty("engine", out JsonElement engine)
                    && engine.ValueKind == JsonValueKind.Object
                    && engine.TryGetProperty("no_global_timescale", out JsonElement flag))
                {
                    seen++;
                    Assert.That(flag.ValueKind, Is.EqualTo(JsonValueKind.True), skill.GetProperty("id").GetString());
                }
        Assert.That(seen, Is.GreaterThan(0));

        // Runtime code must never write the global time scale (the editor Play Sweep may).
        var writes = new List<string>();
        var rx = new Regex(@"Time\.timeScale\s*=[^=]");
        string scripts = Path.Combine(_root, "unity", "Assets", "Scripts");
        foreach (string file in Directory.EnumerateFiles(scripts, "*.cs", SearchOption.AllDirectories))
        {
            string norm = file.Replace('\\', '/');
            if (norm.Contains("/Editor/", StringComparison.Ordinal))
                continue;
            if (rx.IsMatch(File.ReadAllText(file)))
                writes.Add(Path.GetRelativePath(_root, file));
        }
        Assert.That(writes, Is.Empty, "Time.timeScale written in: " + string.Join(", ", writes));
    }
}
