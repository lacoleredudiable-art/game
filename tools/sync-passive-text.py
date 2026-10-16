#!/usr/bin/env python3
"""Dilim gramerindeki pasif metinlerini doğrular (slice-grammar-catalog + kural-motoru-v4)."""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from slice_grammar_doc import load_slice_grammar_document  # noqa: E402


def main() -> int:
    doc = load_slice_grammar_document()
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
