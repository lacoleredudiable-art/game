#!/usr/bin/env python3
"""Regenerate skills[*].passive lines that use '{N} sn pasif: {sıfat}' from rune passive_duration_default.
Other passive texts that start with a duration ('{N} sn boyunca ...', '{N} sn: ...') get only that leading
number replaced (the runtime slot passive always lasts the adjective rune's passive_duration_default).

Does not json.dump — only replaces passive string values in place (both JSON copies stay byte-aligned).
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATHS = [
    ROOT / "docs" / "element-sistemi.json",
    ROOT / "unity" / "Assets" / "Resources" / "ElementSystem" / "element-sistemi.json",
]

PASSIVE_SLOT = re.compile(
    r'("id":\s*"(?P<id>[^"]+)"[\s\S]*?"passive":\s*")(?P<val>[^"]*)(")',
    re.MULTILINE,
)


def format_duration(duration: float) -> str:
    rounded = round(duration)
    if abs(duration - rounded) < 1e-9:
        return str(int(rounded))
    text = f"{duration:.10f}".rstrip("0").rstrip(".")
    return text


def passive_text(adjective: str, duration: float) -> str:
    return f"{format_duration(duration)} sn pasif: {adjective}"


LEADING_DURATION = re.compile(r"^(\d+(?:\.\d+)?)( sn[ :])")


def expected_passives(root: dict) -> dict[str, str]:
    durations = {int(r["id"]): float(r["passive_duration_default"]) for r in root["runes"]}
    faces = {int(r["id"]): r["adjective_face"] for r in root["runes"]}
    out: dict[str, str] = {}
    for block in root["skills"]["by_verb"].values():
        for skill in block["skills"]:
            passive = skill.get("passive", "")
            sid = skill["id"]
            adj_id = int(sid.split("-", 1)[1])
            if " sn pasif: " not in passive:
                if LEADING_DURATION.match(passive):
                    dur_text = format_duration(durations[adj_id])
                    out[sid] = LEADING_DURATION.sub(lambda m: dur_text + m.group(2), passive, count=1)
                continue
            adj = skill.get("adjective") or faces[adj_id]
            out[sid] = passive_text(adj, durations[adj_id])
    return out


def sync_file(path: Path, expected: dict[str, str]) -> bool:
    raw = path.read_bytes()
    text = raw.decode("utf-8")
    changed = False

    def repl(match: re.Match[str]) -> str:
        nonlocal changed
        sid = match.group("id")
        if sid not in expected:
            return match.group(0)
        new_val = expected[sid]
        old_val = match.group("val")
        if old_val == new_val:
            return match.group(0)
        changed = True
        return match.group(1) + new_val + match.group(4)

    new_text = PASSIVE_SLOT.sub(repl, text)
    if changed:
        path.write_bytes(new_text.encode("utf-8"))
    return changed


def main() -> int:
    doc = json.loads(PATHS[0].read_text(encoding="utf-8"))
    expected = expected_passives(doc)
    any_change = False
    for path in PATHS:
        if sync_file(path, expected):
            any_change = True
            print(f"updated {path.relative_to(ROOT)}")
    if not any_change:
        print("passive text already in sync")
    return 0


if __name__ == "__main__":
    sys.exit(main())
