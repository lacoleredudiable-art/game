#!/usr/bin/env python3
"""Skill cümle önizleyici — rün tanımları + gramer → anlatım.

Kullanım:
  python3 preview.py 5-1-2-4
  python3 preview.py 5-1-2 --target ally
  python3 preview.py --list
  python3 preview.py --interactive

Rünleri düzenle: runes.json (veya --runes yol.json)
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


HERE = Path(__file__).resolve().parent
DEFAULT_RUNES = HERE / "runes.json"


def load_config(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def parse_sentence(raw: str) -> list[str]:
    raw = raw.strip().replace(" ", "")
    if not raw:
        raise ValueError("Boş cümle.")
    parts = raw.replace(",", "-").split("-")
    dots: list[str] = []
    for p in parts:
        if not p.isdigit():
            raise ValueError(f"Geçersiz parça: {p!r} (1-5 arası sayı beklenir)")
        if p not in {"1", "2", "3", "4", "5"}:
            raise ValueError(f"Nokta {p} yok — beşgende 1..5 olmalı")
        dots.append(p)
    return dots


def rune_of(cfg: dict[str, Any], slot: str) -> dict[str, Any]:
    runes = cfg["runes"]
    if slot not in runes:
        raise ValueError(f"runes.json içinde {slot} tanımlı değil")
    return runes[slot]


def find_transition(
    cfg: dict[str, Any], from_regime: str, adj_slot: str
) -> dict[str, Any] | None:
    for t in cfg.get("transitions", []):
        if t.get("from") == from_regime and str(t.get("adj")) == adj_slot:
            return t
    return None


def regime_info(cfg: dict[str, Any], key: str) -> dict[str, str]:
    regimes = cfg.get("regimes", {})
    return regimes.get(key) or regimes.get("Unknown") or {
        "label": key,
        "read": "(tanımsız rejim)",
    }


def narrate(
    cfg: dict[str, Any],
    dots: list[str],
    target: str = "boss",
) -> str:
    g = cfg["grammar"]
    max_dots = int(g.get("maxDots", 4))
    lines: list[str] = []

    # --- başlık ---
    chain = "-".join(dots)
    names = [rune_of(cfg, d)["id"] for d in dots]
    lines.append(f"═══ Cümle: {chain}  →  {' · '.join(names)} ═══")
    lines.append(f"Muhatap: {target}")
    hint = cfg.get("addresseeHints", {}).get(target)
    if hint:
        lines.append(f"  ({hint})")
    lines.append("")

    if len(dots) > max_dots:
        head, rest = dots[:max_dots], dots[max_dots:]
        lines.append(
            f"⚠ maxDots={max_dots}: ilk {max_dots} nokta bir cümle; "
            f"fazlası ({'-'.join(rest)}) YENİ fiil / yeni cümle başlatır."
        )
        lines.append(f"   Bu önizleme yalnızca: {'-'.join(head)}")
        lines.append("")
        dots = head
        names = [rune_of(cfg, d)["id"] for d in dots]

    # --- gramer rolleri ---
    verb_slot = dots[0]
    verb = rune_of(cfg, verb_slot)
    adjs = dots[1:]

    lines.append("── Gramer ──")
    lines.append(f"Fiil  [{verb_slot}] {verb['id']} — {verb['function']}")
    lines.append(f"       {verb['verb']}")
    if not adjs:
        lines.append("Sıfat (yok) — tek kelimelik cümle / düz tohum.")
    else:
        for i, slot in enumerate(adjs, start=1):
            r = rune_of(cfg, slot)
            lines.append(f"Sıfat{i} [{slot}] {r['id']} — {r['function']}")
            lines.append(f"       {r['adjective']}")
    lines.append("")

    # --- yaşayan timeline + rejim ---
    lines.append("── Yaşayan etki (timeline) ──")
    regime = verb.get("seedRegime", "Unknown")
    info = regime_info(cfg, regime)
    lines.append(f"1. TOHUM ({verb['id']} fiil)")
    lines.append(f"   Rejim: {regime} — {info['label']}")
    lines.append(f"   {info['read']}")
    lines.append(f"   Görsel ipucu: {verb.get('visualHint', '—')}")
    lines.append(f"   Zenitsu iskeleti: anticipation → travel başlar.")

    for i, slot in enumerate(adjs, start=1):
        r = rune_of(cfg, slot)
        tr = find_transition(cfg, regime, slot)
        lines.append("")
        lines.append(f"{i + 1}. SIFAT ({r['id']}) travel sürerken gelir")
        if tr:
            new_r = tr["to"]
            beat = tr.get("beat", "")
            old = regime
            regime = new_r
            info = regime_info(cfg, regime)
            lines.append(f"   Geçiş: {old} → {regime}  [{info['label']}]")
            lines.append(f"   Beat: {beat}")
            lines.append(f"   Okuma: {info['read']}")
            # nitel vs nicel
            if old != regime:
                lines.append(
                    "   ★ Nitel kırılma — aynı şeklin büyüğü değil, yeni silüet/spawn."
                )
            else:
                lines.append(
                    "   · Yoğunlaştırma — rejim aynı, silüet keskinleşir/kalınlaşır."
                )
        else:
            lines.append(
                f"   (Kayıtlı geçiş yok: {regime} + {r['id']}) — açıklamadan türet:"
            )
            lines.append(f"   Sıfat etkisi: {r['adjective']}")
            lines.append(
                "   → runes.json transitions[]'a satır ekle; yoksa 'biraz daha X' riski."
            )
            regime = "Unknown"

    lines.append("")
    last = rune_of(cfg, dots[-1])
    lines.append(f"{len(dots) + 1}. KAPANIŞ (son rün = {last['id']})")
    lines.append(f"   Tür: {last['closing']}")
    lines.append(
        "   Bang + scar + boss/dost tepkisi; ödül miktarı uzunluktan, türü son ründen."
    )
    lines.append("   Aftermath: hitstop / kısa sessizlik / durum tohumu.")
    lines.append("")

    # --- ekonomi ---
    n = str(len(dots))
    reward = g.get("lengthReward", {}).get(n)
    lines.append("── Ekonomi (§5 tablosu) ──")
    if reward:
        lines.append(
            f"{n} nokta → ~{reward['seconds']}s kurulum, "
            f"etki {reward['effect']}, toparlanma ~{reward['recoverySec']}s"
        )
    windows = g.get("windowsMs", {})
    if windows and adjs:
        lines.append(
            f"İptal pencereleri (dünya ms): fiil {windows.get('verb')}, "
            f"1.sıfat {windows.get('adj1')}, 2.sıfat {windows.get('adj2')}"
        )
    lines.append("")

    # --- özet cümle ---
    lines.append("── Tek cümlelik özet ──")
    summary = build_summary(cfg, dots, names, regime, target)
    lines.append(summary)
    lines.append("")
    return "\n".join(lines)


def build_summary(
    cfg: dict[str, Any],
    dots: list[str],
    names: list[str],
    final_regime: str,
    target: str,
) -> str:
    verb = names[0]
    info = regime_info(cfg, final_regime)
    last = names[-1]
    adj_part = ""
    if len(names) > 1:
        adj_part = " + " + " + ".join(names[1:])
    return (
        f"{target.upper()} muhatabına {verb} tohumu{adj_part}; "
        f"son silüet «{info['label']}» ({final_regime}); "
        f"kapanış türü {last}. "
        f"{info['read']}."
    )


def list_runes(cfg: dict[str, Any]) -> str:
    lines = [f"Set: {cfg.get('meta', {}).get('title', '—')}", ""]
    for slot in sorted(cfg["runes"].keys(), key=int):
        r = cfg["runes"][slot]
        lines.append(f"[{slot}] {r['id']} — {r['function']}")
        lines.append(f"     fiil:  {r['verb']}")
        lines.append(f"     sıfat: {r['adjective']}")
        lines.append(f"     tohum: {r.get('seedRegime', '?')}")
        lines.append("")
    notes = cfg.get("meta", {}).get("notes")
    if notes:
        lines.append(f"Not: {notes}")
    return "\n".join(lines)


def interactive(cfg: dict[str, Any], target: str) -> None:
    print(list_runes(cfg))
    print("Cümle yaz (örn. 5-1-2-4). Çıkmak için q / boş.\n")
    while True:
        try:
            raw = input(f"cümle [{target}]> ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break
        if not raw or raw.lower() in {"q", "quit", "exit"}:
            break
        if raw.startswith("target "):
            target = raw.split(None, 1)[1].strip()
            print(f"Muhatap → {target}\n")
            continue
        try:
            dots = parse_sentence(raw)
            print(narrate(cfg, dots, target=target))
        except ValueError as e:
            print(f"Hata: {e}\n")


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(
        description="Rün cümlesi önizleyici (skill motoru taslak anlatıcı)"
    )
    p.add_argument(
        "sentence",
        nargs="?",
        help="Örn. 5-1-2-4",
    )
    p.add_argument(
        "--runes",
        type=Path,
        default=DEFAULT_RUNES,
        help="Rün tanım JSON (varsayılan: runes.json)",
    )
    p.add_argument(
        "--target",
        default="boss",
        choices=["boss", "ally", "self", "ground"],
        help="Muhatap",
    )
    p.add_argument("--list", action="store_true", help="Rünleri listele")
    p.add_argument(
        "--interactive", "-i", action="store_true", help="Etkileşimli mod"
    )
    args = p.parse_args(argv)

    if not args.runes.is_file():
        print(f"Dosya yok: {args.runes}", file=sys.stderr)
        return 1

    cfg = load_config(args.runes)

    if args.list:
        print(list_runes(cfg))
        return 0
    if args.interactive or not args.sentence:
        if not args.sentence:
            interactive(cfg, args.target)
            return 0
    try:
        dots = parse_sentence(args.sentence)
    except ValueError as e:
        print(f"Hata: {e}", file=sys.stderr)
        return 2

    print(narrate(cfg, dots, target=args.target))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
