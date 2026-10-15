#!/usr/bin/env python3
"""Bağımlılık haritası (C4): dilimin eriştiği eski motor dosyası sayısı.

Kökler: v4 dosyaları (yolunda `RuleEngineV4` geçen her .cs). Kenar: bir dosyanın
başka bir dosyada tanımlı tip adını (class/struct/interface/enum/record/delegate)
kullanması. Erişilebilir küme köklerden geçişli olarak hesaplanır; `eski-motor.txt`
desenlerine uyan erişilebilir dosyalar sayılır.

Sınır (sayı yaklaşıktır): eşleme tip adı regex'i ile yapılır,
Roslyn yok. Yansıma, Resources, sahne/prefab bağlamaları görünmez; yaygın bir
kelimeyle aynı adlı tip fazladan kenar üretebilir.
"""
import argparse
import re
import sys
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DEFAULT_SRC = REPO / "unity" / "Assets" / "Scripts"
DEFAULT_LIST = Path(__file__).resolve().parent / "eski-motor.txt"
ROOT_MARK = "RuleEngineV4"
SKIP_DIRS = ("/Editor/", "/DevTools/")

DECL = re.compile(r"\b(?:class|struct|interface|enum|record|delegate\s+[\w<>\[\],.?]+)\s+([A-Za-z_]\w*)")
TOKEN = re.compile(r"[A-Za-z_]\w*")
STRIP = re.compile(r'//[^\n]*|/\*.*?\*/|@?"(?:[^"\\\n]|\\.)*"', re.S)


def load_patterns(path):
    pats = []
    for line in Path(path).read_text(encoding="utf-8").splitlines():
        line = line.split("#", 1)[0].strip()
        if line:
            pats.append(re.compile(line))
    return pats


def scan(src):
    """{göreli posix yol: (tanımlı tipler, kullanılan token kümesi)}"""
    files = {}
    for p in sorted(src.rglob("*.cs")):
        rel = p.relative_to(src).as_posix()
        if any(s in "/" + rel for s in SKIP_DIRS):
            continue
        text = STRIP.sub(" ", p.read_text(encoding="utf-8-sig", errors="replace"))
        files[rel] = (set(DECL.findall(text)), set(TOKEN.findall(text)))
    return files


def build_graph(files):
    owners = defaultdict(set)  # tip adı -> tanımlandığı dosyalar
    for rel, (types, _) in files.items():
        for t in types:
            owners[t].add(rel)
    graph = {}
    for rel, (_, tokens) in files.items():
        deps = set()
        for t in tokens & owners.keys():
            deps |= owners[t]
        deps.discard(rel)
        graph[rel] = deps
    return graph


def reachable(graph, roots):
    seen, stack = set(roots), list(roots)
    while stack:
        for d in graph[stack.pop()]:
            if d not in seen:
                seen.add(d)
                stack.append(d)
    return seen


def analyze(src, patterns):
    files = scan(src)
    graph = build_graph(files)
    roots = {r for r in files if ROOT_MARK in r}
    reach = reachable(graph, roots)
    old = sorted(r for r in reach if r not in roots and any(p.search(r) for p in patterns))
    old_all = sorted(r for r in files if any(p.search(r) for p in patterns))
    return {"total": len(files), "roots": len(roots), "reachable": len(reach), "old": old, "old_all": old_all}


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", type=Path, default=DEFAULT_SRC)
    ap.add_argument("--list", type=Path, default=DEFAULT_LIST)
    ap.add_argument("--out", type=Path, help="raporu bu dosyaya da yaz")
    a = ap.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    r = analyze(a.src, load_patterns(a.list))
    lines = [
        f"dilimin eriştiği eski motor dosyası: {len(r['old'])}",
        f"eski motor dosyası (toplam, erişilen + erişilmeyen): {len(r['old_all'])}",
        f"v4 kök dosya: {r['roots']}; erişilebilir dosya: {r['reachable']}; toplam .cs: {r['total']}",
        "(tip adı regex'iyle yaklaşık ölçüm: yansıma/Resources/sahne bağları görünmez)",
        "",
    ] + r["old"]
    text = "\n".join(lines) + "\n"
    sys.stdout.write(text)
    if a.out:
        a.out.parent.mkdir(parents=True, exist_ok=True)
        a.out.write_text(text, encoding="utf-8", newline="\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
