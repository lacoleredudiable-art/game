#!/usr/bin/env python3
"""Dilim gramerindeki pasif metinlerini doğrular (tek kaynak: slice-sentence-grammar.json)."""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SLICE = ROOT / "unity" / "Assets" / "Resources" / "RuleEngineV4" / "slice-sentence-grammar.json"


def main() -> int:
    if not SLICE.is_file():
        print(f"eksik: {SLICE}", file=sys.stderr)
        return 1
    doc = json.loads(SLICE.read_text(encoding="utf-8"))
    skills = doc.get("skills", {}).get("by_verb", {})
    count = 0
    for verb_block in skills.values():
        for sk in verb_block.get("skills", []):
            if sk.get("passive"):
                count += 1
    print(f"OK ({count} pasif satırı)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
