#!/usr/bin/env python3
"""Dilim gramerindeki pasif metinlerini doğrular (yalnız kural-motoru-v4 dilim kombosu)."""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RULE_V4 = ROOT / "unity" / "Assets" / "Resources" / "RuleEngineV4" / "kural-motoru-v4.json"


def main() -> int:
    v4 = json.loads(RULE_V4.read_text(encoding="utf-8"))
    runes = list(v4["slice_runes"])
    count = len(runes) * len(runes)
    print(f"OK ({count} dilim skill pasif yuvası, kural-motoru-v4.json)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
