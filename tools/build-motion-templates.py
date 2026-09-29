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
    (15, "Basılı ışın", False),
    (16, "Seken mermi", False),
    (17, "Atılan küre", False),
    (18, "Yere dikilen nesne", False),
    (19, "Dışa yayılan dalga", False),
    (20, "Dağılan yağmur", False),
    (21, "Sis ve körlük", False),
    (22, "Yerden yükselen", False),
    (23, "Atılıp vurma", False),
    (24, "Geri sarma", False),
    (25, "Kapı ve ışınlanma", False),
    (26, "Geçiş kapısı", False),
    (27, "Yem", False),
    (28, "Doğru an", False),
    (29, "Basılı tutuş", False),
    (30, "Çalan vuruş", False),
    (31, "Girdap", False),
    (32, "Yer tuzağı", False),
    (33, "Patlayan ip", False),
    (34, "Güdümlü", False),
    (35, "Yerde iz", False),
    (36, "İp bağı", False),
    (37, "Çağrılan yaratık", False),
    (38, "Beliren gölge", False),
    (39, "Kapıdan akın", False),
    (40, "Muhafız", False),
    (41, "Yürüyen alan", False),
    (42, "Kum saati", False),
]


def P(name, motion, sec, **kw):
    row = {"name": name, "motion": motion, "sec": sec}
    row.update(kw)
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
          hit=H("side", 0.72, 0.5, length=1.3, radius=0.35), **TRACK),
        P("sag", "hop", 0.28, side=1, distance_m=2.7, forward_m=0.35, height_m=0.8,
          hit=H("side", 0.82, 0.5, length=1.3, radius=0.35), **TRACK),
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
        P("as", "hop", 0.26, side=1, distance_m=0, forward_m=2.15, height_m=0.95, behind_m=1.1,
          hit=H("behind", 0.7, 1, length=1.2, radius=0.4), **TRAVEL),
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
        P("sapla", "lunge", 0.12, distance_m=0.7, **TRACK),
        P("uzaklas", "retreat", 0.26, distance_m=1.5, **TRACK),
        P("patla", "hold", 1.24,
          hit=H("target", 0.99, 1, shape="sphere", length=0.8, radius=0.65), **TRACK),
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
        phases = PHASES.get(tid) or [P("bekliyor", "hold", 0.2, facing="target", homing="none")]
        if info["family"] <= 14 and tid not in PHASES:
            raise SystemExit(f"aile 1-14 fazsız: {tid}")
        templates.append({
            "id": tid,
            "name": info["name"],
            "family": info["family"],
            "combos": info["combos"],
            "phases": phases,
        })

    doc = {
        "version": 1,
        "note": "Hareket kalıbı katmanı. Fiil türü seçer, kalıp şekli seçer. 144 kombo 101 kalıba, 42 aileye bağlıdır. Aile 1-14 oynanır; 15-42 kayıtlıdır ama motor eski davranışı sürdürür. Sayılar buradadır. Etiketler (portal, sinir_modu, takim_kombosu, silah_kesme) saklanır, henüz uygulanmaz.",
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
