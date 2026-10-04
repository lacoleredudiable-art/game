#!/usr/bin/env python3
"""docs/OYUN.md sayısal bölümünü koddan/JSON'dan üretir. Yalnız standart kütüphane."""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ELEMENT_JSON = ROOT / "unity" / "Assets" / "Resources" / "ElementSystem" / "element-sistemi.json"
SCENE_UNITY = ROOT / "unity" / "Assets" / "Scenes" / "Prototype.unity"
OUT_MD = ROOT / "docs" / "OYUN.md"

GEN_BEGIN = "<!-- gen:begin -->"
GEN_END = "<!-- gen:end -->"


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def _regex_float(source: str, pattern: str, default: float | None = None) -> float:
    m = re.search(pattern, source, re.MULTILINE)
    if not m:
        if default is not None:
            return default
        raise ValueError(f"regex bulunamadı: {pattern}")
    raw = m.group(1).rstrip("fF")
    return float(raw)


def _regex_int(source: str, pattern: str, default: int | None = None) -> int:
    m = re.search(pattern, source, re.MULTILINE)
    if not m:
        if default is not None:
            return default
        raise ValueError(f"regex bulunamadı: {pattern}")
    return int(m.group(1))


def _regex_bool(source: str, pattern: str) -> bool:
    m = re.search(pattern, source, re.MULTILINE)
    if not m:
        raise ValueError(f"regex bulunamadı: {pattern}")
    return m.group(1).lower() == "true"


def load_scene_fields(text: str) -> tuple[str, float]:
    boss_m = re.search(r"ActiveBossId:\s*(\S+)", text)
    arena_m = re.search(r"ArenaHalfSizeM:\s*([0-9.]+)", text)
    if not boss_m or not arena_m:
        raise ValueError("Prototype.unity: ActiveBossId veya ArenaHalfSizeM bulunamadı")
    return boss_m.group(1).strip(), float(arena_m.group(1))


def scaled_hp(raw: float, scale: float) -> int:
    return int(round(raw * scale))


def attack_kind_label(kind: str) -> str:
    mapping = {
        "slam": "Slam",
        "volley": "Volley",
        "fire_cone": "FireCone",
        "web_field": "WebField",
        "pounce": "Pounce",
    }
    return mapping.get(kind, kind)


def build_generated_section() -> str:
    element = json.loads(_read_text(ELEMENT_JSON))
    combo_max = element["combo_system"]["current_max_length"]
    combo_source = "`element-sistemi.json` → `combo_system.current_max_length` → `SkillMotor` / `HexagonInputController`"

    combat_scale_cs = _read_text(ROOT / "unity/Assets/Scripts/Core/Damage/CombatScale.cs")
    scale = _regex_float(combat_scale_cs, r"public const float DamageAndHp = ([0-9.]+f?)")

    player_raw = element["global_rules"]["player_stats"]["max_hp"]
    boss_raw = element["global_rules"]["boss_stats_default"]["max_hp"]
    player_hp = scaled_hp(float(player_raw), scale)
    boss_hp = scaled_hp(float(boss_raw), scale)

    combat_tuning = _read_text(ROOT / "unity/Assets/Scripts/Core/Tuning/CombatTuning.cs")
    enforce_mana = _regex_bool(combat_tuning, r"public bool EnforceResourceCost = (true|false)")
    enforce_cd = _regex_bool(combat_tuning, r"public bool EnforceCooldown = (true|false)")

    dodge_tuning = _read_text(ROOT / "unity/Assets/Scripts/Core/Tuning/DodgeTuning.cs")
    dodge_charges = _regex_int(dodge_tuning, r"public int MaxCharges = (\d+)")
    recharge_ms = _regex_int(dodge_tuning, r"public int ChargeRechargeMs = (\d+)")
    recharge_sec = recharge_ms / 1000.0

    dev_hp_cs = _read_text(ROOT / "unity/Assets/Scripts/Core/Boss/DevPlayerHp.cs")
    dev_pool = _regex_int(dev_hp_cs, r"public const int Pool = ([0-9_]+)", default=None)
    dev_pool = int(str(dev_pool).replace("_", ""))

    crit = element.get("crit_system", {})
    crit_chance = float(crit.get("base_crit_chance", 0.05))
    crit_mult = float(crit.get("crit_multiplier", 2.0))

    scene_text = _read_text(SCENE_UNITY)
    active_boss_id, arena_half = load_scene_fields(scene_text)

    boss_path = ROOT / "unity" / "Assets" / "Resources" / "Bosses" / f"{active_boss_id.replace('_', '-')}.json"
    if not boss_path.exists():
        alt = ROOT / "unity" / "Assets" / "Resources" / "Bosses" / f"{active_boss_id}.json"
        boss_path = alt if alt.exists() else boss_path
    boss_doc = json.loads(_read_text(boss_path))
    boss_name = boss_doc.get("name", active_boss_id)
    attacks = boss_doc.get("attacks", [])
    attack_lines = []
    for a in attacks:
        kind = a.get("kind", "?")
        attack_lines.append(f"- `{a.get('id', '?')}` ({attack_kind_label(kind)}): {a.get('name', '')}")

    phases = boss_doc.get("vitals", {}).get("phases", [])
    phase_lines = []
    for p in phases:
        pr = p.get("range", [])
        atk = ", ".join(f"`{x}`" for x in p.get("attacks", []))
        phase_lines.append(f"- Faz {p.get('phase', '?')} ({p.get('name', '')}): can %{pr[0]}→%{pr[1] if len(pr) > 1 else '?'} — {atk}")

    runes = element.get("runes", [])
    weapons = element.get("weapons", [])
    elements = element.get("elements", [])
    rune_names = [r.get("name") or r.get("id") for r in runes]
    weapon_names = [w.get("name") or w.get("id") for w in weapons]
    element_names = [e.get("name") or e.get("id") for e in elements]

    vitals_note = (
        f"Boss JSON `vitals.total_hp` / `player_hp` ({boss_doc.get('vitals', {}).get('total_hp')}/"
        f"{boss_doc.get('vitals', {}).get('player_hp')}) **kod tarafından okunmuyor**; yalnız `phases` uygulanır."
    )

    lines = [
        "## Sayılar ve veri (üretilmiş)",
        "",
        "| Konu | Değer | Kaynak |",
        "|------|-------|--------|",
        f"| Kombo üst sınırı (rün) | **{combo_max}** (fiil + {combo_max - 1} sıfat) | {combo_source} |",
        f"| Hasar/can ölçeği | ×{int(scale)} (`CombatScale.DamageAndHp`) | `global_rules.*.max_hp` ham → oyun canı |",
        f"| Oyuncu max can | **{player_hp:,}** (ham {player_raw} × {int(scale)}) | `BossCombatProfile` / `element-sistemi.json` |",
        f"| Boss max can | **{boss_hp:,}** (ham {boss_raw} × {int(scale)}) | aynı |",
        f"| Arena yarıçapı | **{arena_half:g} m** (çap {arena_half * 2:g} m) | `Prototype.unity` `ArenaHalfSizeM` (kod yedeği 25 m) |",
        f"| Dodge hakları | **{dodge_charges}** | `DodgeTuning.MaxCharges` |",
        f"| Dodge dolum | **{recharge_sec:g} sn**/hak | `DodgeTuning.ChargeRechargeMs` |",
        f"| Mana maliyeti zorunlu | **{str(enforce_mana).lower()}** | `CombatTuning.EnforceResourceCost` |",
        f"| Bekleme (CD) zorunlu | **{str(enforce_cd).lower()}** | `CombatTuning.EnforceCooldown` |",
        f"| Kritik | **{crit_chance * 100:g}%** / ×**{crit_mult:g}** | `crit_system` JSON + `CritSystem` |",
        f"| Dev HP (debug) | **{dev_pool:,}** | `DevPlayerHp.Pool` (`DebugConfig` açık build) |",
        "",
        f"### Aktif boss: `{active_boss_id}` — {boss_name}",
        "",
        "Sahne `ActiveBossId`; saldırı türleri `BossAttackKind` ile eşlenir.",
        "",
        "**Saldırılar:**",
        *attack_lines,
        "",
        "**Fazlar:**",
        *phase_lines,
        "",
        f"### Rünler ({len(runes)})",
        "",
        ", ".join(f"**{n}**" for n in rune_names),
        "",
        f"### Silahlar ({len(weapons)})",
        "",
        ", ".join(f"**{n}**" for n in weapon_names),
        "",
        f"### Elementler ({len(elements)})",
        "",
        ", ".join(f"**{n}**" for n in element_names),
        " (oynanış çarpanı `element_mult` = 1.0; ağırlıklı VFX/boya)",
        "",
        "### Okunmayan boss JSON alanları",
        "",
        vitals_note,
        "",
    ]
    return "\n".join(lines)


def wrap_manual(manual: str, generated: str) -> str:
    header = (
        "# OYUN.md — tek oyun özeti\n\n"
        "Alfa prototip: tek boss dövüşü, altıgen kombo girdisi, dodge ve CD/mana kapıları.\n"
        "Sayılar aşağıdaki üretilmiş blokta; elle yazılan kısım yalnız akış.\n\n"
        "## Bir dövüş nasıl akar\n\n"
        "Sol joystick hareket. Sağ altıgende ilk nokta fiil, ikinci nokta (en fazla bir) sıfat; "
        "cümle tamamlanınca `CastPipeline` skill'i çözer ve `ManifestationDirector` yürütür. "
        "Merkeze kısa dokunuş düz vuruş. Dodge ayrı düğme: iki hak, 6 sn dolum; çift basış birleşik kaçış. "
        "Mükemmel dodge yalnız **görsel donma** (dünya/boss/CD akmaya devam). Boss saldırıları `BossBrain` + `BossDirector`; "
        "oyuncu canı `PlayerHealth`, ölçek `CombatScale`.\n\n"
    )
    return f"{header}{GEN_BEGIN}\n{generated.rstrip()}\n{GEN_END}\n"


def write_out(content: str) -> None:
    OUT_MD.parent.mkdir(parents=True, exist_ok=True)
    OUT_MD.write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="docs/OYUN.md üretilen bölüm güncel mi")
    parser.add_argument("--write", action="store_true", help="docs/OYUN.md dosyasını yaz")
    args = parser.parse_args()
    generated = build_generated_section()
    if args.check:
        if not OUT_MD.exists():
            print("OYUN.md yok", file=sys.stderr)
            return 1
        text = _read_text(OUT_MD)
        if GEN_BEGIN not in text or GEN_END not in text:
            print("gen işaretleri yok", file=sys.stderr)
            return 1
        start = text.index(GEN_BEGIN) + len(GEN_BEGIN)
        end = text.index(GEN_END)
        existing = text[start:end].strip("\n")
        if existing != generated.rstrip():
            print("üretilen bölüm farklı", file=sys.stderr)
            return 1
        print("OK")
        return 0
    if args.write or not args.check:
        manual = ""
        if OUT_MD.exists():
            old = _read_text(OUT_MD)
            if "## Bir dövüş nasıl akar" in old:
                manual = old.split("## Bir dövüş nasıl akar", 1)[0]
        content = wrap_manual(manual, generated)
        write_out(content)
        print(f"wrote {OUT_MD.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
