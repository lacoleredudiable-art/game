#!/usr/bin/env python3
"""144 komboyu hareket kalıbına yazar. Oyun JSON'u okur; bu betik onu üretir."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTS = [
    ROOT / "unity" / "Assets" / "Resources" / "ElementSystem" / "motion-templates.json",
    ROOT / "docs" / "motion-templates.json",
]

FAMILIES = [
    (1, "Yükle ve bırak", True),
    (2, "Hızlı seri", True),
    (3, "Zıplayıp sekme", True),
    (4, "Yere çakılma", True),
    (5, "Yelpaze ve dönüş", True),
    (6, "Zincir fırlatma", True),
    (7, "Kanca ile çekme", True),
    (8, "Gizlenip çıkma", True),
    (9, "Havaya kaldırma", True),
    (10, "İşaret ve gecikme", True),
    (11, "Karşı noktada eş", True),
    (12, "Gölge tekrarı", True),
    (13, "Tek dokunuş", True),
    (14, "Tek atış", True),
    (15, "Basılı ışın", True),
    (16, "Seken mermi", True),
    (17, "Atılan küre", True),
    (18, "Yere dikilen nesne", True),
    (19, "Dışa yayılan dalga", True),
    (20, "Dağılan yağmur", True),
    (21, "Sis ve körlük", True),
    (22, "Yerden yükselen", True),
    (23, "Atılıp vurma", True),
    (24, "Geri sarma", True),
    (25, "Kapı ve ışınlanma", True),
    (26, "Geçiş kapısı", True),
    (27, "Yem", True),
    (28, "Doğru an", True),
    (29, "Basılı tutuş", True),
    (30, "Çalan vuruş", True),
    (31, "Girdap", True),
    (32, "Yer tuzağı", True),
    (33, "Patlayan ip", True),
    (34, "Güdümlü", True),
    (35, "Yerde iz", True),
    (36, "İp bağı", True),
    (37, "Çağrılan yaratık", True),
    (38, "Beliren gölge", True),
    (39, "Kapıdan akın", True),
    (40, "Muhafız", True),
    (41, "Yürüyen alan", True),
    (42, "Kum saati", True),
]


MOTION_ANIM = {
    "hold": "cast",
    "lunge": "lunge",
    "dash": "dash",
    "retreat": "backstep",
    "sidestep": "sidestep",
    "hop": "leap",
    "leap": "leap",
    "slam": "land",
    "pull": "dash",
    "spin": "spin",
    "fan": "spin",
    "throw": "hook_throw",
    "hover": "leap",
    "blink": "dash",
    "channel": "cast",
    "return": "dash",
}
ANIM_KEYS = {
    "windup", "lunge", "dash", "backstep", "sidestep",
    "spin", "leap", "land", "hook_throw", "recover", "cast",
}


def P(name, motion, sec, **kw):
    row = {"name": name, "motion": motion, "sec": sec}
    row.update(kw)
    if "anim" not in row:
        if motion == "hold" and row.get("gate") == "release":
            row["anim"] = "windup"
        elif motion == "hold" and name in ("bekle", "din", "ger", "tut", "uzaklas"):
            row["anim"] = "recover"
        else:
            row["anim"] = MOTION_ANIM.get(motion, "cast")
    return row


def H(anchor, at, share, shape="capsule", length=1.4, radius=0.32, payload="effect", every=None):
    hit = {
        "shape": shape,
        "anchor": anchor,
        "length_m": length,
        "radius_m": radius,
        "at": at,
        "share": share,
        "payload": payload,
    }
    if every:
        hit["every_sec"] = every
    return hit


TRACK = {"facing": "target", "homing": "track"}
TRAVEL = {"facing": "travel", "homing": "none"}

# Hareket düşmana göredir; fiilin etki hedefi (kendin/dost) bunu değiştirmez.
ENEMY_AIM = {
    "yukle_birak", "kan_ceken_pence", "basili_seri", "yan_yan_sekme", "basip_sekme",
    "yere_cakilan", "dalis_patlamasi", "yer_yarigi", "onde_yelpaze", "zincirli_firlatma",
    "silaha_kement", "kanca_cekis", "bossu_cek", "sokup_cekme", "dumandan_cikis",
    "havaya_kaldirma", "yerden_fiskirma", "havada_asma", "isarete_vur", "saplanan_fitil",
    "isaret_patlamasi", "sirtta_kapi", "onden_arkadan", "golge_yankisi", "sinir_noktasi",
    "diken_firlatma",
    # 15–42. Süreler dondurulmuş 144-kombo metninden (ışın 2/3 sn, fitil beklemesi,
    # geri dönüş 2 sn, süzülme 3 sn, kilit 3 sn). Ara adımlar bölüm 1 yedekleriyle aynı dosyada.
    "uzaktan_emme", "seken_bomba", "seken_disk", "seken_geri",
    "geri_donen_kure", "sis_bombasi", "goz_bagi", "isinlan_kes", "icinden_emme",
    "geri_sarma", "boss_geri", "ceken_girdap", "yutan_girdap", "cekip_firlat",
    "gerilip_kopan", "pesinden_av", "mayin_cakma", "dost_ip", "cember_sarma",
    "seken_yaratik", "yapisan", "dagilan_suru", "sirt_saplama", "kapidan_akin",
    "kum_saati", "kalkan_calma", "karsilikli_kilit", "zamanli_donma", "karsi_darbe",
    "tam_zamanli", "duvar_dikme", "duman_duvari", "taret_dikme", "patlayan_iz",
    "yem_kac", "iki_kapi", "yer_tuzagi",
}

PHASES = {
    "yukle_birak": [
        P("yukleme", "hold", 0.34, gate="release", max_hold_sec=0.9, **TRACK),
        P("saplama", "lunge", 0.18, distance_m=1.7, curve=[0, 0.12, 0.7, 1],
          hit=H("forward", 0.62, 1, length=1.8, radius=0.28), **TRACK),
    ],
    "kan_ceken_pence": [
        P("pence_1", "lunge", 0.12, distance_m=0.7,
          hit=H("forward", 0.7, 0.34, length=1.2, radius=0.28), **TRACK),
        P("pence_2", "lunge", 0.12, distance_m=0.65,
          hit=H("forward", 0.7, 0.33, length=1.2, radius=0.28), **TRACK),
        P("pence_3", "lunge", 0.14, distance_m=0.6,
          hit=H("forward", 0.7, 0.33, length=1.2, radius=0.28), **TRACK),
    ],
    "basili_seri": [
        P("seri", "channel", 3.0, drift_m=1.4, walk_mps=1.7,
          hit=H("forward", 0.083, 0.1666667, length=1.35, radius=0.3, every=0.5), **TRACK),
    ],
    "yan_yan_sekme": [
        P("sol", "sidestep", 0.18, side=-1, distance_m=1.35, forward_m=0.45,
          hit=H("target_side", 0.72, 0.5, length=1.3, radius=0.55), **TRACK),
        P("sag", "hop", 0.28, side=1, distance_m=2.7, forward_m=0.35, height_m=0.8,
          hit=H("target_side", 0.82, 0.5, length=1.3, radius=0.55), **TRACK),
    ],
    "sekmeli_ziplama": [
        P("sek_1", "hop", 0.2, side=1, distance_m=0.7, forward_m=1.35, height_m=0.55,
          hit=H("self", 0.7, 0.34, shape="sphere", length=0.6, radius=0.45), **TRAVEL),
        P("sek_2", "hop", 0.2, side=-1, distance_m=1.15, forward_m=1.15, height_m=0.5,
          hit=H("self", 0.7, 0.33, shape="sphere", length=0.6, radius=0.45), **TRAVEL),
        P("sek_3", "hop", 0.2, side=1, distance_m=0.55, forward_m=1.25, height_m=0.5,
          hit=H("self", 0.7, 0.33, shape="sphere", length=0.6, radius=0.45), **TRAVEL),
    ],
    "basip_sekme": [
        P("bas", "leap", 0.3, distance_m=2.4, height_m=1.15, gap_m=0.45,
          hit=H("self", 0.92, 1, shape="sphere", length=0.8, radius=0.7), **TRACK),
        P("sek", "hop", 0.22, side=1, distance_m=0, forward_m=-1.7, height_m=0.55, **TRAVEL),
    ],
    "yere_cakilan": [
        P("cak", "hop", 0.42, side=1, distance_m=0, forward_m=0.2, height_m=0.85,
          hit=H("self", 0.9, 1, shape="sphere", length=1.1, radius=0.85), **TRACK),
    ],
    "sicrayip_cakilma": [
        P("sicra", "leap", 0.6, distance_m=2.6, height_m=1.35, gap_m=0.4, **TRAVEL),
        P("cak", "slam", 0.22, distance_m=0.8, height_m=1.2,
          hit=H("self", 0.75, 1, shape="sphere", length=1.2, radius=0.8), **TRAVEL),
    ],
    "dalis_patlamasi": [
        P("dalis", "dash", 0.26, distance_m=3.2, overshoot_m=0, **TRACK),
        P("patlama", "hold", 0.16,
          hit=H("self", 0.35, 1, shape="sphere", length=1.6, radius=1.35), **TRACK),
    ],
    "donen_kesik": [
        P("don", "spin", 0.55, yaw_deg=360,
          hit=H("ring", 0.45, 1, shape="sphere", length=1.6, radius=1.55), facing="hold", homing="none"),
    ],
    "yer_yarigi": [
        P("yumruk", "slam", 0.16, distance_m=0.35, height_m=0.35, **TRACK),
        P("catlak_1", "sidestep", 0.14, side=-1, distance_m=0.7, forward_m=1.3,
          hit=H("forward", 0.8, 0.34, length=1.5, radius=0.4), **TRACK),
        P("catlak_2", "lunge", 0.12, distance_m=1.5,
          hit=H("forward", 0.75, 0.33, length=1.6, radius=0.4), **TRACK),
        P("catlak_3", "sidestep", 0.14, side=1, distance_m=0.7, forward_m=1.2,
          hit=H("forward", 0.8, 0.33, length=1.5, radius=0.4), **TRACK),
    ],
    "onde_yelpaze": [
        P("dik", "hold", 0.18, **TRACK),
        P("yelpaze", "fan", 0.45, yaw_deg=70,
          hit=H("forward", 0.6, 1, shape="cone", length=3.2, radius=0.9), facing="hold", homing="none"),
    ],
    "zincirli_firlatma": [
        P("firlat", "throw", 0.22, shot_m=4.5,
          hit=H("shot", 0.85, 0.45, length=0.9, radius=0.25), **TRACK),
        P("ger", "hold", 0.28, **TRACK),
        P("cek_kes", "lunge", 0.2, distance_m=1.5,
          hit=H("forward", 0.6, 0.55, length=1.5, radius=0.32), **TRACK),
    ],
    "silaha_kement": [
        P("kement", "throw", 0.24, shot_m=3.8,
          hit=H("shot", 0.8, 1, length=0.8, radius=0.22), **TRACK),
        P("tut", "hold", 0.7, **TRACK),
    ],
    "kanca_cekis": [
        P("at", "throw", 0.16, shot_m=5, **TRACK),
        P("cek", "pull", 0.32, distance_m=2.2, gap_m=1.15, **TRACK),
        P("as", "hop", 0.26, side=1, distance_m=0, forward_m=0, height_m=0.95, land="behind",
          hit=H("behind", 0.7, 1, shape="sphere", length=0.8, radius=0.55), **TRAVEL),
    ],
    "dostu_kanca": [
        P("at", "throw", 0.18, shot_m=4, **TRACK),
        P("cek", "pull", 0.34, distance_m=2, gap_m=1.4,
          hit=H("self", 0.8, 1, length=0.8, radius=0.45), **TRACK),
    ],
    "bossu_cek": [
        P("at", "throw", 0.16, shot_m=4.2, **TRACK),
        P("cek", "retreat", 0.28, distance_m=1.6,
          hit=H("forward", 0.45, 1, length=1.6, radius=0.35), **TRACK),
    ],
    "sokup_cekme": [
        P("sok", "throw", 0.18, shot_m=3.6,
          hit=H("shot", 0.8, 0.5, length=0.7, radius=0.22), **TRACK),
        P("cek", "retreat", 0.26, distance_m=1.3,
          hit=H("forward", 0.5, 0.5, length=1.4, radius=0.3), **TRACK),
    ],
    "dumandan_cikis": [
        P("bat", "hold", 0.28, **TRACK),
        P("cik", "lunge", 0.2, distance_m=1.8,
          hit=H("forward", 0.55, 1, length=1.6, radius=0.32), **TRACK),
    ],
    "duman_kaybolma": [
        P("atil", "dash", 0.24, distance_m=2.8, overshoot_m=0,
          hit=H("self", 0.6, 1, shape="sphere", length=0.8, radius=0.6), **TRAVEL),
        P("din", "hold", 0.35, facing="hold", homing="none"),
    ],
    "havaya_kaldirma": [
        P("savur", "hop", 0.34, side=1, distance_m=0, forward_m=1.1, height_m=1.2,
          hit=H("forward", 0.42, 1, length=1.5, radius=0.35), **TRACK),
    ],
    "yerden_fiskirma": [
        P("hazir", "hold", 0.16, **TRACK),
        P("fiskir", "hop", 0.28, side=1, distance_m=0, forward_m=0.4, height_m=1.05,
          hit=H("target", 0.55, 1, shape="sphere", length=1.2, radius=1.05), **TRACK),
    ],
    "havada_asma": [
        P("sicra", "leap", 0.34, distance_m=2.2, height_m=1.25, gap_m=0.7, **TRACK),
        P("as", "hover", 0.8, height_m=1.15,
          hit=H("self", 0.2, 1, shape="sphere", length=0.9, radius=0.6), **TRACK),
    ],
    "isarete_vur": [
        P("isaret", "hold", 0.22, **TRACK),
        P("vur", "lunge", 0.16, distance_m=1.35,
          hit=H("forward", 0.6, 1, length=1.5, radius=0.28), **TRACK),
    ],
    "saplanan_fitil": [
        P("sapla", "lunge", 0.12, distance_m=0.7, plant=True, **TRACK),
        P("uzaklas", "retreat", 0.26, distance_m=1.5, **TRACK),
        P("patla", "hold", 1.24,
          hit=H("plant", 0.99, 1, shape="sphere", length=0.9, radius=0.9), **TRACK),
    ],
    "isaret_patlamasi": [
        P("at", "throw", 0.2, shot_m=4.5, **TRACK),
        P("bekle", "hold", 1.6, **TRACK),
        P("patla", "hold", 0.12,
          hit=H("target", 0.5, 1, shape="sphere", length=1.1, radius=0.95), **TRACK),
    ],
    "koruyucu_isaret": [
        P("koy", "lunge", 0.2, distance_m=0.8,
          hit=H("self", 0.7, 1, length=0.7, radius=0.4), **TRACK),
    ],
    "sirtta_kapi": [
        P("on", "lunge", 0.18, distance_m=1.2, behind_m=1.15,
          hit=H("forward", 0.65, 0.5, length=1.4, radius=0.3), **TRACK),
        P("sirt", "hold", 0.14, behind_m=1.15,
          hit=H("behind", 0.4, 0.5, length=1.1, radius=0.32), **TRACK),
    ],
    "onden_arkadan": [
        P("on", "lunge", 0.16, distance_m=0.9, behind_m=1.2,
          hit=H("forward", 0.7, 0.5, length=1.3, radius=0.32), **TRACK),
        P("arka", "hold", 0.18, behind_m=1.2,
          hit=H("behind", 0.35, 0.5, length=1.2, radius=0.32), **TRACK),
    ],
    "ters_es": [
        P("es", "hold", 0.28, hit=H("self", 0.5, 1, length=0.8, radius=0.45), **TRACK),
        P("ters", "hold", 0.16, hit=H("target", 0.4, 0, payload="none", shape="sphere", length=0.6, radius=0.4), **TRACK),
    ],
    "golge_yankisi": [
        P("vur", "lunge", 0.16, distance_m=0.9, side_m=1.25,
          hit=H("forward", 0.7, 0.55, length=1.35, radius=0.3), **TRACK),
        P("bekle", "hold", 0.24, **TRACK),
        P("yanki", "hold", 0.1, side=-1, side_m=1.25,
          hit=H("side", 0.2, 0.45, length=1.2, radius=0.3), **TRACK),
    ],
    "golge_ulak": [
        P("ver", "hold", 0.2, hit=H("self", 0.6, 0.6, length=0.7, radius=0.4), **TRACK),
        P("kos", "hold", 1.0, **TRACK),
        P("ulak", "sidestep", 0.16, side=1, distance_m=1.4, forward_m=0.2,
          hit=H("side", 0.5, 0.4, length=0.8, radius=0.35), **TRACK),
    ],
    "ayna_klonlar": [
        P("cagir", "hold", 0.18, **TRACK),
        P("sol", "hold", 0.16, side=-1, side_m=1.35,
          hit=H("side", 0.4, 0.5, length=0.8, radius=0.35), **TRACK),
        P("sag", "hold", 0.16, side=1, side_m=1.35,
          hit=H("side", 0.4, 0.5, length=0.8, radius=0.35), **TRACK),
    ],
    "tek_dokunus": [
        P("dokun", "lunge", 0.16, distance_m=0.55,
          hit=H("forward", 0.65, 1, length=0.8, radius=0.3), **TRACK),
    ],
    "sinir_noktasi": [
        P("yapis", "pull", 0.22, distance_m=1.6, gap_m=0.55, **TRACK),
        P("bas", "lunge", 0.12, distance_m=0.35,
          hit=H("forward", 0.6, 1, length=0.7, radius=0.25), **TRACK),
    ],
    "diken_firlatma": [
        P("hazir", "hold", 0.12, **TRACK),
        P("at", "throw", 0.28, shot_m=6.5,
          hit=H("shot", 0.85, 1, length=0.6, radius=0.2), **TRACK),
    ],
    # —— 15 Basılı ışın. 2-2 iki saniye emer; 2-12 ve 7-12 üç saniye akar.
    "uzaktan_emme": [
        P("uzat", "hold", 0.16, **TRACK),
        P("em", "channel", 2.0, drift_m=0.35, walk_mps=0.6,
          hit=H("forward", 0.25, 0.25, length=3.2, radius=0.35, every=0.5), **TRACK),
    ],
    "basili_isik": [
        P("isik", "channel", 3.0, drift_m=1.2, walk_mps=1.5,
          hit=H("forward", 0.0833333, 0.1666667, length=3.2, radius=0.32, every=0.5), **TRACK),
    ],
    # —— 16 Seken mermi. Üç sekme; ilk vuruş öndeki hedefe değer.
    "dosttan_dosta": [
        P("at", "throw", 0.18, shot_m=3.6,
          hit=H("shot", 0.85, 0.34, length=0.7, radius=0.4), **TRACK),
        P("sek_2", "hold", 0.18,
          hit=H("target", 0.45, 0.33, shape="sphere", length=0.7, radius=0.55), **TRACK),
        P("sek_3", "hold", 0.18, side=1,
          hit=H("target_side", 0.5, 0.33, shape="sphere", length=0.7, radius=0.55), **TRACK),
    ],
    "seken_bomba": [
        P("at", "throw", 0.18, shot_m=3.6,
          hit=H("shot", 0.85, 0.34, length=0.8, radius=0.5), **TRACK),
        P("sek_2", "hold", 0.2,
          hit=H("target", 0.5, 0.33, shape="sphere", length=0.9, radius=0.75), **TRACK),
        P("sek_3", "hold", 0.2, side=-1,
          hit=H("target_side", 0.5, 0.33, shape="sphere", length=0.9, radius=0.7), **TRACK),
    ],
    "seken_disk": [
        P("at", "throw", 0.2, shot_m=3.6,
          hit=H("shot", 0.85, 0.6, length=0.6, radius=0.35), **TRACK),
        P("sek", "hold", 0.22, side=1, side_m=1.4,
          hit=H("side", 0.5, 0.4, length=0.6, radius=0.4), **TRACK),
    ],
    "seken_geri": [
        P("vur", "lunge", 0.16, distance_m=1.1,
          hit=H("forward", 0.6, 0.55, length=1.8, radius=0.4), **TRACK),
        P("sek", "hold", 0.2,
          hit=H("target", 0.45, 0.45, shape="sphere", length=0.7, radius=0.6), **TRACK),
    ],
    # —— 17 Atılan küre.
    "pas_verme": [
        P("at", "throw", 0.24, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.6, radius=0.35), **TRACK),
    ],
    "geri_donen_kure": [
        P("at", "throw", 0.22, shot_m=3.6,
          hit=H("shot", 0.8, 0.55, length=0.6, radius=0.4), **TRACK),
        P("don", "hold", 0.28,
          hit=H("forward", 0.7, 0.45, length=2.4, radius=0.4), **TRACK),
    ],
    # —— 18 Yere dikilen nesne. Oyuncu yerinde kalır; duvar ve taret öne bakar.
    "totem_dikme": [
        P("dik", "slam", 0.3, distance_m=0.15, height_m=0.4,
          hit=H("self", 0.85, 1, shape="sphere", length=0.8, radius=0.55), **TRACK),
    ],
    "duvar_dikme": [
        P("dik", "hold", 0.32,
          hit=H("forward", 0.65, 1, length=2.8, radius=0.9), **TRACK),
    ],
    "duman_duvari": [
        P("vur", "slam", 0.16, distance_m=0.2, height_m=0.3, **TRACK),
        P("duvar", "fan", 0.36, yaw_deg=40,
          hit=H("forward", 0.12, 1, length=2.8, radius=1.05), facing="hold", homing="none"),
    ],
    "taret_dikme": [
        P("dik", "hold", 0.22,
          hit=H("self", 0.55, 0, shape="sphere", length=0.4, radius=0.35, payload="none"), **TRACK),
        P("ates", "hold", 0.2, shot_m=3.6,
          hit=H("shot", 0.8, 1, length=0.55, radius=0.28), **TRACK),
    ],
    # —— 19 Dışa yayılan dalga. Halka öndeki hedefi içine alır.
    "genisleyen_halka": [
        P("dokun", "hold", 0.16, **TRACK),
        P("halka", "spin", 0.42, yaw_deg=80,
          hit=H("ring", 0.7, 1, shape="sphere", length=3.2, radius=3.2), facing="hold", homing="none"),
    ],
    "savas_narasi": [
        P("nara", "hold", 0.36,
          hit=H("ring", 0.55, 1, shape="sphere", length=3.1, radius=3.1), facing="hold", homing="none"),
    ],
    "nabiz": [
        P("diz", "hold", 0.18, facing="hold", homing="none"),
        P("nabiz", "channel", 3.0, drift_m=0, walk_mps=0,
          hit=H("ring", 0.3333333, 0.3333333, shape="sphere", length=3.1, radius=3.1, every=1.0),
          facing="hold", homing="none"),
    ],
    # —— 20 Dağılan yağmur.
    "dagilan_yagmur": [
        P("at", "throw", 0.18, shot_m=1.4, **TRACK),
        P("yagmur", "hold", 0.4,
          hit=H("ring", 0.55, 1, shape="sphere", length=3.3, radius=3.3), facing="hold", homing="none"),
    ],
    # —— 21 Sis ve körlük.
    "sis_ortusu": [
        P("cokert", "hold", 0.3,
          hit=H("target", 0.6, 1, shape="sphere", length=1.4, radius=1.25), **TRACK),
    ],
    "sis_bombasi": [
        P("at", "throw", 0.22, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.7, radius=0.7), **TRACK),
    ],
    "goz_bagi": [
        P("at", "throw", 0.2, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.55, radius=0.32), **TRACK),
    ],
    # —— 22 Yerden yükselen.
    "yerden_buyuyen": [
        P("filiz", "hop", 0.42, side=1, distance_m=0, forward_m=0.25, height_m=0.95,
          hit=H("target", 0.7, 1, shape="sphere", length=0.9, radius=0.75), **TRACK),
    ],
    "isik_sutunu": [
        P("sutun", "hop", 0.48, side=1, distance_m=0, forward_m=0.1, height_m=1.2,
          hit=H("self", 0.72, 1, shape="sphere", length=0.8, radius=0.55), **TRACK),
    ],
    # —— 23 Atılıp vurma. Işınlan ve kes boss'un arkasına iner (3-1 ve 3-9 aynı kalıp).
    # İçinden geçiş overshoot ile öte kenarda biter.
    "isinlan_kes": [
        P("kilit", "hold", 0.30, **TRACK),
        P("isin", "blink", 0.18, snap_at=0.4, distance_m=5.0, land="behind", behind_m=1.15,
          hit=H("behind", 0.72, 1, shape="sphere", length=0.9, radius=0.55), **TRACK),
    ],
    "icinden_emme": [
        P("gec", "dash", 0.28, distance_m=3.0, overshoot_m=1.15,
          hit=H("forward", 0.55, 1, length=1.6, radius=0.45), **TRACK),
    ],
    # —— 24 Geri sarma. Dönüş ışınlanma değil, kısa atılmadır. 12-11 beklemesi 2 sn.
    "geri_sarma": [
        P("capa", "hold", 0.14, **TRACK),
        P("atil", "dash", 0.24, distance_m=3.0, **TRACK),
        P("bekle", "hold", 2.0, facing="hold", homing="none"),
        P("don", "return", 0.28, distance_m=3.0, facing="travel", homing="none"),
    ],
    "boss_geri": [
        P("at", "throw", 0.22, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.6, radius=0.35), **TRACK),
        P("sar", "hold", 0.24, **TRACK),
    ],
    # —— 25 Kapı ve ışınlanma. Mekanik yok; gövde kapıyı kurar, yer değiştirme kenarda durur.
    "iki_kapi": [
        P("birak", "hold", 0.12,
          hit=H("self", 0.5, 0.5, shape="sphere", length=0.5, radius=0.4), **TRACK),
        P("atil", "dash", 0.26, distance_m=3.0, **TRACK),
        P("kapi", "hold", 0.16,
          hit=H("self", 0.45, 0.5, shape="sphere", length=0.6, radius=0.45), **TRACK),
    ],
    "takim_kapisi": [
        P("ac", "hold", 0.24,
          hit=H("self", 0.55, 1, shape="sphere", length=0.8, radius=0.55), **TRACK),
        P("bekle", "hold", 1.0, facing="hold", homing="none"),
    ],
    "yer_degistir": [
        P("degis", "blink", 0.22, snap_at=0.5, distance_m=3.0, **TRACK),
        P("don", "hold", 0.12,
          hit=H("forward", 0.5, 1, length=1.4, radius=0.4), **TRACK),
    ],
    "dostu_yerden": [
        P("bat", "hold", 0.18, **TRACK),
        P("cik", "hop", 0.6, side=1, distance_m=0.9, forward_m=0.15, height_m=0.85,
          hit=H("side", 0.75, 1, length=0.6, radius=0.4), **TRACK),
    ],
    # —— 26 Geçiş kapısı.
    "kuculten_kapi": [
        P("ac", "hold", 0.28,
          hit=H("forward", 0.6, 1, length=2.8, radius=0.7), **TRACK),
    ],
    "buyuten_kemer": [
        P("yuksel", "hop", 0.42, side=1, distance_m=0, forward_m=0.3, height_m=1.05,
          hit=H("forward", 0.7, 1, length=2.6, radius=0.7), **TRACK),
    ],
    # —— 27 Yem.
    "yem_kac": [
        P("birak", "hold", 0.12,
          hit=H("self", 0.45, 1, shape="sphere", length=0.45, radius=0.4), **TRACK),
        P("kac", "retreat", 0.28, distance_m=2.8, **TRACK),
    ],
    "yem_kopya": [
        P("cik", "hold", 0.24, side=1, side_m=1.25,
          hit=H("side", 0.55, 1, length=0.6, radius=0.4), **TRACK),
    ],
    "sahte_goruntu": [
        P("sol", "hold", 0.14, side=-1, side_m=1.35,
          hit=H("side", 0.5, 0.34, length=0.5, radius=0.35), **TRACK),
        P("sag", "hold", 0.14, side=1, side_m=1.35,
          hit=H("side", 0.5, 0.33, length=0.5, radius=0.35), **TRACK),
        P("geri", "retreat", 0.16, distance_m=0.9,
          hit=H("self", 0.6, 0.33, shape="sphere", length=0.4, radius=0.35), **TRAVEL),
    ],
    # —— 28 Doğru an. Blok penceresi 0,5 sn.
    "tam_zamanli": [
        P("kaldir", "hold", 0.5,
          hit=H("forward", 0.7, 1, length=2.8, radius=0.5), **TRACK),
    ],
    "zamanli_donma": [
        P("bekle", "hold", 0.34, **TRACK),
        P("dokun", "lunge", 0.16, distance_m=1.6,
          hit=H("forward", 0.6, 1, length=1.5, radius=0.32), **TRACK),
    ],
    "karsi_darbe": [
        P("hazir", "hold", 0.2, **TRACK),
        P("ters", "lunge", 0.16, distance_m=1.3,
          hit=H("forward", 0.55, 1, length=1.6, radius=0.35), **TRACK),
    ],
    # —— 29 Basılı tutuş. Siper ve kilit 3 sn.
    "basili_siper": [
        P("siper", "channel", 3.0, drift_m=0.8, walk_mps=1.2,
          hit=H("forward", 0.25, 0.3333333, length=2.8, radius=0.55, every=1.0), **TRACK),
    ],
    "karsilikli_kilit": [
        P("yapis", "pull", 0.24, distance_m=1.8, gap_m=0.55, **TRACK),
        P("tut", "hold", 3.0,
          hit=H("forward", 0.08, 1, length=1.3, radius=0.4), **TRACK),
    ],
    # —— 30 Çalan vuruş.
    "kalkan_calma": [
        P("vur", "lunge", 0.2, distance_m=1.5,
          hit=H("forward", 0.65, 1, length=1.5, radius=0.4), **TRACK),
    ],
    "kan_muhru": [
        P("muhur", "hold", 0.32,
          hit=H("self", 0.6, 1, shape="sphere", length=0.5, radius=0.4), **TRACK),
    ],
    # —— 31 Girdap. Çeken girdap 1 sn sonra patlar; yutan girdap 3 sn.
    "ceken_girdap": [
        P("ac", "throw", 0.18, shot_m=3.6, **TRACK),
        P("cek", "hold", 1.0,
          hit=H("target", 0.5, 0.35, shape="sphere", length=1.2, radius=1.15, every=0.5), **TRACK),
        P("pat", "hold", 0.14,
          hit=H("target", 0.4, 0.3, shape="sphere", length=1.3, radius=1.25), **TRACK),
    ],
    "yutan_girdap": [
        P("ac", "hold", 0.18, **TRACK),
        P("yut", "hold", 3.0,
          hit=H("forward", 0.25, 0.25, length=2.8, radius=0.85, every=0.75), **TRACK),
    ],
    "cekip_firlat": [
        P("cek", "hold", 0.28,
          hit=H("self", 0.6, 0.4, length=0.7, radius=0.4), **TRACK),
        P("firlat", "throw", 0.22, shot_m=3.6,
          hit=H("shot", 0.85, 0.6, length=0.6, radius=0.32), **TRACK),
    ],
    # —— 32 Yer tuzağı.
    "mayin_cakma": [
        P("cak", "hold", 0.22, plant=True, **TRACK),
        P("bekle", "hold", 0.2,
          hit=H("plant", 0.4, 1, shape="sphere", length=0.8, radius=0.75), **TRACK),
    ],
    "yer_tuzagi": [
        P("ser", "hold", 0.28,
          hit=H("forward", 0.6, 1, length=2.8, radius=1.05), **TRACK),
    ],
    # —— 33 Patlayan ip.
    "gerilip_kopan": [
        P("yapis", "lunge", 0.16, distance_m=1.3, plant=True, **TRACK),
        P("uzak", "retreat", 0.28, distance_m=1.8, **TRACK),
        P("kop", "hold", 0.14,
          hit=H("plant", 0.45, 1, shape="sphere", length=1.0, radius=0.9), **TRACK),
    ],
    "dost_ip": [
        P("bag", "throw", 0.22, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.55, radius=0.3), **TRACK),
        P("ger", "hold", 0.4, **TRACK),
    ],
    # —— 34 Güdümlü. Top hedef neredeyse orada patlar.
    "gudumlu_top": [
        P("at", "throw", 0.4, shot_m=4.0, **TRACK),
        P("car", "hold", 0.16,
          hit=H("target", 0.45, 1, shape="sphere", length=0.9, radius=0.85), **TRACK),
    ],
    "pesinden_av": [
        P("cagir", "hold", 0.18, **TRACK),
        P("kos", "hold", 2.0, **TRACK),
        P("isir", "hold", 0.14,
          hit=H("target", 0.5, 1, shape="sphere", length=0.7, radius=0.55), **TRACK),
    ],
    "mermi_sondur": [
        P("dokun", "hold", 0.12, **TRACK),
        P("ok", "throw", 0.26, shot_m=3.6,
          hit=H("target", 0.75, 1, shape="sphere", length=0.45, radius=0.35), **TRACK),
    ],
    # —— 35 Yerde iz. Patlayan iz 3 sn; akıntı yere çizilir.
    "patlayan_iz": [
        P("kos", "channel", 3.0, drift_m=2.2, walk_mps=1.8,
          hit=H("forward", 0.15, 0.1666667, length=1.8, radius=0.45, every=0.5), **TRACK),
    ],
    "akinti_yolu": [
        P("ciz", "channel", 0.7, drift_m=1.4, walk_mps=1.1, **TRACK),
        P("ak", "hold", 0.24,
          hit=H("forward", 0.45, 1, length=2.8, radius=0.65), **TRACK),
    ],
    # —— 36 İp bağı. Çember boss'un yanından arkasına döner.
    "iki_uclu": [
        P("at", "throw", 0.2, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.55, radius=0.3), **TRACK),
        P("bag", "hold", 0.45, **TRACK),
    ],
    "dost_dosta": [
        P("sol", "hold", 0.16, side=-1, side_m=1.2,
          hit=H("side", 0.5, 0, length=0.4, radius=0.3, payload="none"), **TRACK),
        P("sag", "sidestep", 0.2, side=1, distance_m=1.4, forward_m=0.15,
          hit=H("side", 0.55, 0, length=0.4, radius=0.3, payload="none"), **TRACK),
        P("ip", "hold", 0.16,
          hit=H("forward", 0.5, 1, length=2.8, radius=0.4), **TRACK),
    ],
    "cember_sarma": [
        P("sol", "sidestep", 0.22, side=-1, distance_m=1.7, forward_m=1.15, **TRACK),
        P("arka", "hop", 0.3, land="behind", behind_m=1.15, height_m=0.4, distance_m=1.5, **TRACK),
        P("sag", "sidestep", 0.22, side=1, distance_m=1.6, forward_m=0.2, **TRACK),
        P("sar", "hold", 0.16,
          hit=H("target", 0.4, 1, shape="sphere", length=1.2, radius=1.2), **TRACK),
    ],
    # —— 37 Çağrılan yaratık.
    "seken_yaratik": [
        P("at", "throw", 0.16, shot_m=3.6, **TRACK),
        P("isir_1", "hold", 0.16,
          hit=H("target", 0.5, 0.34, shape="sphere", length=0.6, radius=0.45), **TRACK),
        P("isir_2", "hold", 0.16, side=-1,
          hit=H("target_side", 0.5, 0.33, shape="sphere", length=0.6, radius=0.45), **TRACK),
        P("isir_3", "hold", 0.16, side=1,
          hit=H("target_side", 0.5, 0.33, shape="sphere", length=0.6, radius=0.45), **TRACK),
    ],
    "yapisan": [
        P("at", "throw", 0.18, shot_m=3.6,
          hit=H("shot", 0.85, 0, length=0.4, radius=0.25, payload="none"), **TRACK),
        P("em", "hold", 5.0,
          hit=H("target", 0.2, 0.2, shape="sphere", length=0.6, radius=0.5, every=1.0), **TRACK),
    ],
    "dagilan_suru": [
        P("sol", "hold", 0.14, side=-1,
          hit=H("target_side", 0.5, 0.34, shape="sphere", length=0.6, radius=0.5), **TRACK),
        P("orta", "hold", 0.14,
          hit=H("target", 0.5, 0.33, shape="sphere", length=0.6, radius=0.5), **TRACK),
        P("sag", "hold", 0.14, side=1,
          hit=H("target_side", 0.5, 0.33, shape="sphere", length=0.6, radius=0.5), **TRACK),
    ],
    # —— 38 Beliren gölge. Saplama hedefin sırtındadır; oyuncu yerinde kalır.
    "sirt_saplama": [
        P("belir", "hold", 0.18, **TRACK),
        P("sap", "hold", 0.14,
          hit=H("behind", 0.55, 1, shape="sphere", length=0.8, radius=0.45), **TRACK),
    ],
    # —— 39 Kapıdan akın. 4 sn boyunca hedefe akar.
    "kapidan_akin": [
        P("ac", "hold", 0.2, plant=True, **TRACK),
        P("akin", "hold", 4.0,
          hit=H("target", 0.2, 0.2, shape="sphere", length=0.5, radius=0.4, every=0.8), **TRACK),
    ],
    # —— 40 Muhafız.
    "muhafiz": [
        P("cik", "hold", 0.24, side=1, side_m=1.15,
          hit=H("side", 0.6, 1, length=0.7, radius=0.45), **TRACK),
        P("dur", "hold", 0.28, **TRACK),
    ],
    # —— 41 Yürüyen alan. Balon 4 sn (12-12); süzülme 3 sn, yerden yüksek, çubukla döner.
    "yuruyen_balon": [
        P("ac", "hold", 0.2,
          hit=H("ring", 0.5, 0.4, shape="sphere", length=2.6, radius=2.6), facing="hold", homing="none"),
        P("yuru", "channel", 4.0, drift_m=1.2, walk_mps=1.5,
          hit=H("ring", 0.25, 0.15, shape="sphere", length=2.6, radius=2.6, every=1.0), **TRACK),
    ],
    "suzulme": [
        P("suz", "channel", 3.0, drift_m=1.1, walk_mps=3.4, height_m=0.85, **TRAVEL),
    ],
    # —— 42 Kum saati.
    "kum_saati": [
        P("at", "throw", 0.22, shot_m=3.6,
          hit=H("shot", 0.85, 1, length=0.55, radius=0.32), **TRACK),
        P("titr", "hold", 0.4, **TRACK),
    ],
}

# skill, template id, template name, family, tags, sinir
ROWS = [
    ("1-1", "yukle_birak", "Yükle ve bırak", 1, ["silah_kesme"], None),
    ("1-2", "kan_ceken_pence", "Kan çeken pençe", 2, ["sinir_modu"], 0.2),
    ("1-3", "yan_yan_sekme", "Yan yan sekme", 3, [], None),
    ("1-4", "yere_cakilan", "Yere çakılan darbe", 4, ["silah_kesme"], None),
    ("1-5", "donen_kesik", "Dönen kesik", 5, ["silah_kesme"], None),
    ("1-6", "zincirli_firlatma", "Zincirli fırlatma", 6, [], None),
    ("1-7", "dumandan_cikis", "Dumandan çıkış", 8, [], None),
    ("1-8", "havaya_kaldirma", "Havaya kaldırma", 9, ["sinir_modu", "silah_kesme"], 0.1),
    ("1-9", "isarete_vur", "İşarete vur", 10, [], None),
    ("1-10", "sirtta_kapi", "Sırtta açılan kapı", 11, ["portal"], None),
    ("1-11", "golge_yankisi", "Gölge yankısı", 12, [], None),
    ("1-12", "basili_seri", "Basılı seri", 2, ["silah_kesme"], None),
    ("2-1", "tek_dokunus", "Tek dokunuş", 13, [], None),
    ("2-2", "uzaktan_emme", "Uzaktan emme", 15, [], None),
    ("2-3", "dosttan_dosta", "Dosttan dosta sekme", 16, [], None),
    ("2-4", "totem_dikme", "Totem dikme", 18, [], None),
    ("2-5", "genisleyen_halka", "Genişleyen halka", 19, [], None),
    ("2-6", "dostu_kanca", "Dostu kancayla çekme", 7, ["portal"], None),
    ("2-7", "sis_ortusu", "Sis örtüsü", 21, [], None),
    ("2-8", "yerden_buyuyen", "Yerden büyüyen", 22, [], None),
    ("2-9", "koruyucu_isaret", "Koruyucu işaret", 10, [], None),
    ("2-10", "ters_es", "Ters eş", 11, [], None),
    ("2-11", "golge_ulak", "Gölge ulak", 12, [], None),
    ("2-12", "basili_isik", "Basılı ışın", 15, [], None),
    ("3-1", "isinlan_kes", "Işınlan ve kes", 23, [], None),
    ("3-2", "icinden_emme", "İçinden geçip emme", 23, ["sinir_modu"], 0.2),
    ("3-3", "sekmeli_ziplama", "Sekmeli zıplama", 3, [], None),
    ("3-4", "geri_sarma", "Geri sarma", 24, ["portal"], None),
    ("3-5", "dalis_patlamasi", "Dalış patlaması", 4, ["silah_kesme"], None),
    ("3-6", "kanca_cekis", "Kanca çekiş", 7, [], None),
    ("3-7", "duman_kaybolma", "Duman olup kaybolma", 8, [], None),
    ("3-8", "sicrayip_cakilma", "Sıçrayıp çakılma", 4, ["silah_kesme"], None),
    ("3-9", "isinlan_kes", "Işınlan ve kes", 23, [], None),
    ("3-10", "iki_kapi", "İki kapılı portal", 25, ["portal", "takim_kombosu"], None),
    ("3-11", "yem_kac", "Yem bırakıp kaçma", 27, [], None),
    ("3-12", "suzulme", "Süzülme", 41, [], None),
    ("4-1", "tam_zamanli", "Tam zamanlı blok", 28, [], None),
    ("4-2", "kalkan_calma", "Kalkan çalma", 30, [], None),
    ("4-3", "seken_disk", "Seken disk", 16, [], None),
    ("4-4", "duvar_dikme", "Duvar dikme", 18, [], None),
    ("4-5", "onde_yelpaze", "Önde yelpaze alan", 5, [], None),
    ("4-6", "zincirli_firlatma", "Zincirli fırlatma", 6, [], None),
    ("4-7", "duman_duvari", "Duman duvarı", 18, [], None),
    ("4-8", "yerden_buyuyen", "Yerden büyüyen", 22, [], None),
    ("4-9", "koruyucu_isaret", "Koruyucu işaret", 10, [], None),
    ("4-10", "sirtta_kapi", "Sırtta açılan kapı", 11, [], None),
    ("4-11", "yem_kopya", "Yem kopya", 27, [], None),
    ("4-12", "basili_siper", "Basılı siper", 29, [], None),
    ("5-1", "saplanan_fitil", "Saplanan fitil", 10, [], None),
    ("5-2", "ceken_girdap", "Çeken girdap", 31, [], None),
    ("5-3", "seken_bomba", "Seken bomba", 16, [], None),
    ("5-4", "mayin_cakma", "Mayın çakma", 32, ["takim_kombosu"], None),
    ("5-5", "yer_yarigi", "Yer yarığı", 5, ["silah_kesme"], None),
    ("5-6", "gerilip_kopan", "Gerilip kopan bağ", 33, [], None),
    ("5-7", "sis_bombasi", "Sis bombası", 21, [], None),
    ("5-8", "yerden_fiskirma", "Yerden fışkırma", 9, [], None),
    ("5-9", "gudumlu_top", "Güdümlü top", 34, [], None),
    ("5-10", "karsi_darbe", "Karşı darbe", 28, [], None),
    ("5-11", "golge_yankisi", "Gölge yankısı", 12, [], None),
    ("5-12", "patlayan_iz", "Patlayan iz", 35, [], None),
    ("6-1", "sinir_noktasi", "Sinir noktası", 13, [], None),
    ("6-2", "bossu_cek", "Boss'u kendine çekme", 7, [], None),
    ("6-3", "basip_sekme", "Basıp sekme", 3, ["silah_kesme"], None),
    ("6-4", "karsilikli_kilit", "Karşılıklı kilit", 29, [], None),
    ("6-5", "yer_tuzagi", "Yer tuzağı", 32, [], None),
    ("6-6", "iki_uclu", "İki uçlu bağ", 36, [], None),
    ("6-7", "goz_bagi", "Göz bağı", 21, [], None),
    ("6-8", "havada_asma", "Havada asma", 9, ["takim_kombosu"], None),
    ("6-9", "zamanli_donma", "Zamanlı donma", 28, [], None),
    ("6-10", "karsi_darbe", "Karşı darbe", 28, [], None),
    ("6-11", "onden_arkadan", "Önden arkadan kıskaç", 11, [], None),
    ("6-12", "cember_sarma", "Çember çizip sarma", 36, [], None),
    ("7-1", "diken_firlatma", "Diken fırlatma", 14, [], None),
    ("7-2", "sokup_cekme", "Söküp çekme", 7, [], None),
    ("7-3", "seken_yaratik", "Seken yaratık", 37, [], None),
    ("7-4", "mayin_cakma", "Mayın çakma", 32, [], None),
    ("7-5", "onde_yelpaze", "Önde yelpaze alan", 5, [], None),
    ("7-6", "dost_ip", "Dostun patlattığı ip", 33, ["takim_kombosu"], None),
    ("7-7", "sis_bombasi", "Sis bombası", 21, [], None),
    ("7-8", "yerden_fiskirma", "Yerden fışkırma", 9, [], None),
    ("7-9", "isaret_patlamasi", "İşaret patlaması", 10, ["takim_kombosu"], None),
    ("7-10", "onden_arkadan", "Önden arkadan kıskaç", 11, [], None),
    ("7-11", "golge_yankisi", "Gölge yankısı", 12, [], None),
    ("7-12", "basili_isik", "Basılı ışın", 15, [], None),
    ("8-1", "kuculten_kapi", "Küçülten kapı", 26, ["portal"], None),
    ("8-2", "kan_muhru", "Kan mührü", 30, ["sinir_modu"], 0.2),
    ("8-3", "pas_verme", "Pas verme", 17, ["takim_kombosu"], None),
    ("8-4", "totem_dikme", "Totem dikme", 18, [], None),
    ("8-5", "savas_narasi", "Savaş narası", 19, [], None),
    ("8-6", "dost_dosta", "Dost dosta ip", 36, ["takim_kombosu"], None),
    ("8-7", "goz_bagi", "Göz bağı", 21, [], None),
    ("8-8", "buyuten_kemer", "Büyüten kemer", 26, ["portal"], None),
    ("8-9", "gudumlu_top", "Güdümlü top", 34, [], None),
    ("8-10", "ters_es", "Ters eş", 11, [], None),
    ("8-11", "golge_ulak", "Gölge ulak", 12, [], None),
    ("8-12", "akinti_yolu", "Akıntı yolu", 35, [], None),
    ("9-1", "tek_dokunus", "Tek dokunuş", 13, [], None),
    ("9-2", "cekip_firlat", "Çekip fırlatma", 31, [], None),
    ("9-3", "dosttan_dosta", "Dosttan dosta sekme", 16, [], None),
    ("9-4", "yer_tuzagi", "Yer tuzağı", 32, [], None),
    ("9-5", "dagilan_yagmur", "Dağılan yağmur", 20, [], None),
    ("9-6", "iki_uclu", "İki uçlu bağ", 36, [], None),
    ("9-7", "sis_ortusu", "Sis örtüsü", 21, [], None),
    ("9-8", "isik_sutunu", "Işık sütunu", 22, [], None),
    ("9-9", "mermi_sondur", "Mermi söndürme", 34, [], None),
    ("9-10", "yer_degistir", "Yer değiştirme", 25, ["portal"], None),
    ("9-11", "yem_kopya", "Yem kopya", 27, [], None),
    ("9-12", "nabiz", "Nabız atışı", 19, [], None),
    ("10-1", "tam_zamanli", "Tam zamanlı blok", 28, ["silah_kesme"], None),
    ("10-2", "yutan_girdap", "Yutan girdap", 31, [], None),
    ("10-3", "seken_geri", "Seken geri vuruş", 16, [], None),
    ("10-4", "duvar_dikme", "Duvar dikme", 18, [], None),
    ("10-5", "yuruyen_balon", "Yürüyen balon", 41, [], None),
    ("10-6", "silaha_kement", "Silaha kement", 6, [], None),
    ("10-7", "sahte_goruntu", "Sahte görüntüler", 27, [], None),
    ("10-8", "yerden_buyuyen", "Yerden büyüyen", 22, [], None),
    ("10-9", "koruyucu_isaret", "Koruyucu işaret", 10, [], None),
    ("10-10", "iki_kapi", "İki kapılı portal", 25, ["portal", "takim_kombosu"], None),
    ("10-11", "yem_kopya", "Yem kopya", 27, [], None),
    ("10-12", "basili_siper", "Basılı siper", 29, [], None),
    ("11-1", "sirt_saplama", "Uzaktan sırt saplama", 38, [], None),
    ("11-2", "yapisan", "Yapışan yaratık", 37, [], None),
    ("11-3", "seken_yaratik", "Seken yaratık", 37, [], None),
    ("11-4", "taret_dikme", "Taret dikme", 18, ["takim_kombosu"], None),
    ("11-5", "dagilan_suru", "Dağılan sürü", 37, [], None),
    ("11-6", "muhafiz", "Yanında muhafız", 40, [], None),
    ("11-7", "sirt_saplama", "Uzaktan sırt saplama", 38, [], None),
    ("11-8", "dostu_yerden", "Dostu yerden çağırma", 25, ["portal"], None),
    ("11-9", "pesinden_av", "Peşinden giden av", 34, [], None),
    ("11-10", "takim_kapisi", "Takım kapısı", 25, ["portal"], None),
    ("11-11", "ayna_klonlar", "Ayna klonlar", 12, [], None),
    ("11-12", "kapidan_akin", "Kapıdan akın", 39, [], None),
    ("12-1", "zamanli_donma", "Zamanlı donma", 28, [], None),
    ("12-2", "ceken_girdap", "Çeken girdap", 31, [], None),
    ("12-3", "geri_donen_kure", "Geri dönen küre", 17, [], None),
    ("12-4", "totem_dikme", "Totem dikme", 18, [], None),
    ("12-5", "genisleyen_halka", "Genişleyen halka", 19, [], None),
    ("12-6", "iki_uclu", "İki uçlu bağ", 36, ["takim_kombosu"], None),
    ("12-7", "kum_saati", "Titreyen zaman", 42, [], None),
    ("12-8", "isik_sutunu", "Işık sütunu", 22, ["sinir_modu"], 0.1),
    ("12-9", "isaret_patlamasi", "İşaret patlaması", 10, [], None),
    ("12-10", "boss_geri", "Boss'u geri sarma", 24, [], None),
    ("12-11", "geri_sarma", "Geri sarma", 24, [], None),
    ("12-12", "yuruyen_balon", "Yürüyen balon", 41, [], None),
]


def main():
    expected = {f"{v}-{a}" for v in range(1, 13) for a in range(1, 13)}
    got = [row[0] for row in ROWS]
    if len(got) != 144 or set(got) != expected or len(set(got)) != 144:
        missing = sorted(expected - set(got))
        extra = sorted(set(got) - expected)
        raise SystemExit(f"kombo {len(got)} eksik={missing} fazla={extra}")

    by_id = {}
    for skill, tid, name, fam, tags, sinir in ROWS:
        bucket = by_id.setdefault(tid, {"name": name, "family": fam, "combos": []})
        if bucket["name"] != name or bucket["family"] != fam:
            raise SystemExit(f"kalıp çelişkisi {tid}")
        combo = {"id": skill, "tags": tags}
        if sinir is not None:
            combo["sinir"] = sinir
        bucket["combos"].append(combo)

    if len(by_id) != 101:
        raise SystemExit(f"kalıp sayısı {len(by_id)} != 101")

    templates = []
    for tid, info in by_id.items():
        if tid not in PHASES:
            raise SystemExit(f"fazsız kalıp: {tid}")
        phases = PHASES[tid]
        for phase in phases:
            if phase.get("anim") not in ANIM_KEYS:
                raise SystemExit(f"anim anahtarı yok: {tid} {phase.get('name')} {phase.get('anim')}")
        templates.append({
            "id": tid,
            "name": info["name"],
            "family": info["family"],
            "aim": "enemy" if tid in ENEMY_AIM else "effect",
            "combos": info["combos"],
            "phases": phases,
        })

    doc = {
        "version": 1,
        "note": "Hareket kalıbı katmanı. Fiil türü seçer, kalıp şekli seçer. 144 kombo 101 kalıba, 42 aileye bağlıdır. Aile 1-42 oynanır. Sayılar buradadır. Etiketler (portal, sinir_modu, takim_kombosu, silah_kesme) saklanır, uygulanmaz. anim_bridge silah klibine giden tek tablodur.",
        "anim_bridge": {
            "fallback_state": "Locomotion",
            "rows": [
                {"key": "windup", "weapon": 0, "verb": 0, "state": "CastPierce", "trigger": ""},
                {"key": "lunge", "weapon": 0, "verb": 0, "state": "BasicStrike", "trigger": ""},
                {"key": "dash", "weapon": 0, "verb": 0, "state": "Locomotion", "trigger": ""},
                {"key": "backstep", "weapon": 0, "verb": 0, "state": "Locomotion", "trigger": ""},
                {"key": "sidestep", "weapon": 0, "verb": 0, "state": "Locomotion", "trigger": ""},
                {"key": "spin", "weapon": 0, "verb": 0, "state": "CastSweep", "trigger": ""},
                {"key": "leap", "weapon": 0, "verb": 0, "state": "CastSlam", "trigger": ""},
                {"key": "land", "weapon": 0, "verb": 0, "state": "CastSlam", "trigger": ""},
                {"key": "hook_throw", "weapon": 0, "verb": 0, "state": "CastShoot", "trigger": ""},
                {"key": "recover", "weapon": 0, "verb": 0, "state": "Locomotion", "trigger": ""},
                {"key": "cast", "weapon": 0, "verb": 0, "state": "CastChannel", "trigger": ""},
            ],
        },
        "tag_labels": {
            "portal": "Portal",
            "sinir_modu": "Sınır modu",
            "takim_kombosu": "Takım kombosu",
            "silah_kesme": "Silah kesme",
        },
        "fallbacks": {
            "phase_sec": 0.28,
            "step_m": 1.2,
            "hit_length_m": 1.5,
            "hit_radius_m": 0.4,
            "max_hold_sec": 0.9,
            "walk_mps": 1.6,
        "height_m": 0.9,
        "gap_m": 0.9,
        "stop_gap_m": 0.15,
    },
        "families": [
            {"id": i, "name": name, "implemented": done}
            for i, name, done in FAMILIES
        ],
        "templates": templates,
    }
    text = json.dumps(doc, ensure_ascii=False, indent=2) + "\n"
    for path in OUTS:
        path.write_text(text, encoding="utf-8", newline="\n")
        print(path, "templates", len(templates), "skills", len(got))


if __name__ == "__main__":
    main()
