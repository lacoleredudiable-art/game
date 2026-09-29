#!/usr/bin/env python3
"""Compile every runtime game script without the Unity editor.

Uses the Unity 2021 reference assembly plus small module stubs. Newer
FindAnyObjectByType calls are rewritten only in the compile copy.
"""
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GEN = ROOT / "tools" / "GameCompile" / "gen"
PROJ = ROOT / "tools" / "GameCompile" / "GameCompile.csproj"
DOTNET = Path.home() / ".dotnet" / "dotnet"
SKIP = ("/Editor/", "\\Editor\\")


def rewrite(src: Path, dest: Path):
    text = src.read_text(encoding="utf-8")
    text = text.replace("FindObjectsSortMode", "CompileShims.SortMode")
    text = text.replace("Object.FindObjectsByType", "CompileShims.FindObjectsByType")
    text = text.replace("Object.FindAnyObjectByType", "CompileShims.FindAnyObjectByType")
    text = re.sub(r"(?<!\.)FindObjectsByType", "CompileShims.FindObjectsByType", text)
    text = re.sub(r"(?<!\.)FindAnyObjectByType", "CompileShims.FindAnyObjectByType", text)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(text, encoding="utf-8", newline="\n")


def copy_tree(rel):
    base = ROOT / rel
    for src in base.rglob("*.cs"):
        posix = src.as_posix()
        if any(s in posix for s in ("/Editor/",)):
            continue
        dest = GEN / rel / src.relative_to(base)
        rewrite(src, dest)


def main():
    if GEN.exists():
        shutil.rmtree(GEN)
    copy_tree("unity/Assets/Scripts/Core")
    copy_tree("unity/Assets/Scripts/Game")
    dotnet = str(DOTNET if DOTNET.exists() else "dotnet")
    proc = subprocess.run(
        [dotnet, "build", str(PROJ), "--nologo", "-v", "q"],
        cwd=ROOT,
        capture_output=True,
        text=True,
    )
    log = proc.stdout + "\n" + proc.stderr
    if proc.returncode != 0:
        print("game layer did not compile:")
        for line in log.splitlines():
            if "error CS" in line:
                print(line.strip())
        return 1
    print("game layer compiled")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
