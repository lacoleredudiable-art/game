"""Ejder efekt atlası: harf (2048x768) + siluet (1024x512) + hedef halkası + kül tozu → tek 2048² doku.

Yerleşim kural-motoru-v4.json "slice_vfx.atlas"tan okunur (Core RuleEngineV4VfxAtlas ile aynı).
Kaynak PNG'ler silinmez/değişmez. Önizleme: tools/verify-out/vfx-atlas/.

Kullanım (repo kökünden): python tools/pack-vfx-atlas.py
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RESOURCES = os.path.join(ROOT, "unity", "Assets", "Resources")
CATALOG = os.path.join(RESOURCES, "RuleEngineV4", "kural-motoru-v4.json")
GLYPH_SRC = "Vfx/Ejder/Ejder_Glif_Atlas"
SILHOUETTE_SRC = "Vfx/Ejder/Ejder_Siluet_Atlas"
PREVIEW_DIR = os.path.join(ROOT, "tools", "verify-out", "vfx-atlas")


def resource_png(name):
    return os.path.join(RESOURCES, *name.split("/")) + ".png"


def paste_grid(atlas, grid, src_name):
    src = Image.open(resource_png(src_name)).convert("RGBA")
    want = (grid["cols"] * grid["cell_px"], grid["rows"] * grid["cell_px"])
    if src.size != want:
        sys.exit(f"{src_name}: boyut {src.size}, yerleşim {want} bekliyor")
    atlas.paste(src, (grid["x_px"], grid["y_px"]))


def ring_cell(size, radius_frac, line_frac):
    """Beyaz halka (renk köşe renginden). Orta çizgi radius_frac, kalınlık line_frac (yarı kenar oranı)."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    half = size / 2.0
    rc = radius_frac * half
    hw = line_frac * half / 2.0
    for y in range(size):
        for x in range(size):
            r = math.hypot(x + 0.5 - half, y + 0.5 - half)
            d = abs(r - rc) / hw if hw > 0 else 1.0
            a = max(0.0, 1.0 - d * d)
            if a > 0:
                px[x, y] = (255, 255, 255, int(round(a * 255)))
    return img


def dust_cell(size):
    """Yumuşak yuvarlak kül tozu noktası."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    half = size / 2.0
    for y in range(size):
        for x in range(size):
            u = math.hypot(x + 0.5 - half, y + 0.5 - half) / half
            a = max(0.0, 1.0 - u)
            a = a * a
            if a > 0:
                px[x, y] = (255, 255, 255, int(round(a * 255)))
    return img


def preview(atlas, lay):
    size = atlas.size[0]
    bg = Image.new("RGBA", atlas.size, (24, 20, 18, 255))
    check = 64
    d = ImageDraw.Draw(bg)
    for y in range(0, size, check):
        for x in range(0, size, check):
            if (x // check + y // check) % 2:
                d.rectangle([x, y, x + check - 1, y + check - 1], fill=(36, 30, 27, 255))
    out = Image.alpha_composite(bg, atlas)
    d = ImageDraw.Draw(out)
    regions = [
        ("harfler 8x3", lay["glyphs"], (255, 200, 80)),
        ("siluetler 4x2", lay["silhouettes"], (120, 200, 255)),
    ]
    for label, g, col in regions:
        x0, y0 = g["x_px"], g["y_px"]
        x1 = x0 + g["cols"] * g["cell_px"] - 1
        y1 = y0 + g["rows"] * g["cell_px"] - 1
        for c in range(1, g["cols"]):
            xx = x0 + c * g["cell_px"]
            d.line([xx, y0, xx, y1], fill=col + (90,), width=1)
        for r in range(1, g["rows"]):
            yy = y0 + r * g["cell_px"]
            d.line([x0, yy, x1, yy], fill=col + (90,), width=1)
        d.rectangle([x0, y0, x1, y1], outline=col + (255,), width=4)
        d.text((x0 + 8, y1 - 20), label, fill=col + (255,))
    for label, c, col in [("halka", lay["ring"], (255, 120, 90)), ("kul tozu", lay["dust"], (200, 200, 200))]:
        x0, y0, s = c["x_px"], c["y_px"], c["size_px"]
        d.rectangle([x0, y0, x0 + s - 1, y0 + s - 1], outline=col + (255,), width=4)
        d.text((x0 + 8, y0 + 8), label, fill=col + (255,))
    return out


def main():
    with open(CATALOG, encoding="utf-8") as f:
        lay = json.load(f)["slice_vfx"]["atlas"]
    size = lay["size_px"]
    atlas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    paste_grid(atlas, lay["glyphs"], GLYPH_SRC)
    paste_grid(atlas, lay["silhouettes"], SILHOUETTE_SRC)
    ring = lay["ring"]
    atlas.paste(ring_cell(ring["size_px"], ring["radius_frac"], ring["line_frac"]), (ring["x_px"], ring["y_px"]))
    dust = lay["dust"]
    atlas.paste(dust_cell(dust["size_px"]), (dust["x_px"], dust["y_px"]))

    out_path = resource_png(lay["resource"])
    atlas.save(out_path, optimize=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    prev_path = os.path.join(PREVIEW_DIR, "Ejder_Vfx_Atlas_onizleme.png")
    preview(atlas, lay).save(prev_path)
    small = os.path.join(PREVIEW_DIR, "Ejder_Vfx_Atlas_onizleme_1024.png")
    preview(atlas, lay).resize((size // 2, size // 2), Image.LANCZOS).save(small)
    print(f"atlas: {out_path} ({size}x{size})")
    print(f"önizleme: {prev_path}")


if __name__ == "__main__":
    main()
