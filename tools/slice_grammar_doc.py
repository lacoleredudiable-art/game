"""Dilim SkillMotor JSON — SliceGrammarBuilder ile aynı çıktı (gen-game-overview)."""
from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
RULE_V4 = ROOT / "unity" / "Assets" / "Resources" / "RuleEngineV4" / "kural-motoru-v4.json"
CATALOG = ROOT / "tools" / "slice-grammar-catalog.json"
LEXICON_CS = ROOT / "unity" / "Assets" / "Scripts" / "Core" / "RuleEngineV4" / "SliceGrammarCompatLexicon.cs"
COMBO_MAX = 2


def _extract_raw_string(cs_path: Path, const_name: str) -> str:
    text = cs_path.read_text(encoding="utf-8")
    pattern = rf'const string {const_name} = """\n(.*?)""";'
    m = re.search(pattern, text, re.DOTALL)
    if not m:
        raise ValueError(f"{cs_path.name}: {const_name} bulunamadı")
    return m.group(1)


def _filter_map(obj: dict[str, Any], slice_runes: list[int]) -> dict[str, Any]:
    return {k: v for k, v in obj.items() if k.isdigit() and int(k) in slice_runes}


def _parse_skills(catalog: dict, slice_runes: list[int]) -> dict:
    by_verb: dict[str, Any] = {}
    for verb_key, block in catalog["skills"]["by_verb"].items():
        if not verb_key.isdigit() or int(verb_key) not in slice_runes:
            continue
        kept = [
            sk
            for sk in block.get("skills", [])
            if str(sk["id"]).split("-")[1].isdigit() and int(str(sk["id"]).split("-")[1]) in slice_runes
        ]
        by_verb[verb_key] = {"skills": kept}
    return {"by_verb": by_verb}


def _bind_scale(root: dict, scale: dict) -> None:
    gr = root["global_rules"]
    gr["player_stats"]["max_hp"] = scale["player_max_hp"]
    gr["player_stats"]["max_mana"] = scale["mana_pool"]
    gr["boss_stats_default"]["max_hp"] = scale["boss_max_hp"]
    gr["boss_stats_default"]["poise_bar"] = scale["boss_poise_bar"]
    gr["resource_system"]["max_mana"] = scale["mana_pool"]
    gr["resource_system"]["regen_per_sec"] = scale["mana_regen_per_sec"]
    gr["cooldown_rules"]["global_cooldown_sec"] = scale["cooldown_sec"]


def load_slice_grammar_document() -> dict:
    v4 = json.loads(RULE_V4.read_text(encoding="utf-8"))
    slice_runes = list(v4["slice_runes"])
    scale = v4["scale"]
    weapons_v4 = {int(k): w for k, w in v4.get("weapons", {}).items()}
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))

    root = json.loads(_extract_raw_string(LEXICON_CS, "StaticJson"))
    root["runes"] = [r for r in catalog["runes"] if r["id"] in slice_runes]
    root["verb_base"] = _filter_map(catalog["verb_base"], slice_runes)
    root["adjective_mods"] = _filter_map(catalog["adjective_mods"], slice_runes)
    weapons = []
    for w in catalog["weapons"]:
        wid = w["id"]
        if wid in weapons_v4:
            w = {**w, "range_mult": weapons_v4[wid]["range_mult"]}
        weapons.append(w)
    root["weapons"] = weapons
    root["skills"] = _parse_skills(catalog, slice_runes)
    _bind_scale(root, scale)
    root["system"]["version"] = "v4-slice-inmemory"
    root["combo_system"]["current_max_length"] = COMBO_MAX
    return root
