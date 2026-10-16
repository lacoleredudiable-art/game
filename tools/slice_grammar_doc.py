#!/usr/bin/env python3
"""Dilim SkillMotor grameri: SliceGrammarDefaults.Payload*.cs + kural-motoru-v4.json."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PAYLOAD_DIR = ROOT / "unity" / "Assets" / "Scripts" / "Core" / "RuleEngineV4"
RULE_V4 = ROOT / "unity" / "Assets" / "Resources" / "RuleEngineV4" / "kural-motoru-v4.json"
COMBO_MAX = 2


def _extract_payload_json() -> str:
    chunks: list[str] = []
    for path in sorted(PAYLOAD_DIR.glob("SliceGrammarDefaults.Payload*.cs")):
        cs_text = path.read_text(encoding="utf-8")
        m = re.search(r'=> @"\n', cs_text)
        if not m:
            raise ValueError(f"{path.name}: payload literal bulunamadı")
        start = m.end()
        end = cs_text.rfind('";', start)
        if end < 0:
            raise ValueError(f"{path.name}: payload kapanışı bulunamadı")
        chunks.append(cs_text[start:end].replace('""', '"'))
    return "".join(chunks)


def load_slice_grammar_document() -> dict:
    payload = json.loads(_extract_payload_json())
    v4 = json.loads(RULE_V4.read_text(encoding="utf-8"))
    scale = v4["scale"]
    payload["combo_system"]["current_max_length"] = COMBO_MAX
    gr = payload["global_rules"]
    gr["player_stats"]["max_hp"] = scale["player_max_hp"]
    gr["player_stats"]["max_mana"] = scale["mana_pool"]
    gr["boss_stats_default"]["max_hp"] = scale["boss_max_hp"]
    gr["boss_stats_default"]["poise_bar"] = scale["boss_poise_bar"]
    gr["resource_system"]["max_mana"] = scale["mana_pool"]
    gr["resource_system"]["regen_per_sec"] = scale["mana_regen_per_sec"]
    gr["cooldown_rules"]["global_cooldown_sec"] = scale["cooldown_sec"]
    payload["system"]["version"] = "v4-slice-inmemory"
    return payload
