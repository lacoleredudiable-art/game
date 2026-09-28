#!/usr/bin/env python3
"""Generate the authored combat HUD rune and weapon icon set.

The shapes are deliberately deterministic vector-style silhouettes.  Re-run this
script after changing a glyph; the checked-in PNGs are the runtime source assets.
"""

from pathlib import Path
import hashlib
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "unity/Assets/Resources/UI"
S = 1024
FINAL = 256

WHITE = (244, 249, 255, 255)
SOFT = (151, 190, 220, 255)
CYAN = (72, 220, 255, 255)
GOLD = (255, 197, 83, 255)
DARK = (7, 12, 24, 248)


def pts(values):
    return [(int(x * S), int(y * S)) for x, y in values]


def line(draw, values, fill=WHITE, width=.055, joint="curve"):
    draw.line(pts(values), fill=fill, width=int(width * S), joint=joint)


def polygon(draw, values, fill=WHITE):
    draw.polygon(pts(values), fill=fill)


def ellipse(draw, box, fill=None, outline=None, width=.02):
    draw.ellipse(tuple(int(v * S) for v in box), fill=fill, outline=outline, width=int(width * S))


def arc(draw, box, start, end, fill=WHITE, width=.045):
    draw.arc(tuple(int(v * S) for v in box), start, end, fill=fill, width=int(width * S))


def base_icon(accent=CYAN):
    image = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    glow = Image.new("RGBA", image.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    ellipse(gd, (.09, .09, .91, .91), fill=(accent[0], accent[1], accent[2], 88))
    glow = glow.filter(ImageFilter.GaussianBlur(int(S * .055)))
    image.alpha_composite(glow)

    d = ImageDraw.Draw(image)
    ellipse(d, (.08, .08, .92, .92), fill=DARK, outline=(accent[0], accent[1], accent[2], 225), width=.028)
    ellipse(d, (.12, .12, .88, .88), outline=(126, 171, 210, 130), width=.009)
    # Three asymmetric registration marks make the medallions feel authored,
    # while preserving a calm silhouette at 28–40 dp.
    arc(d, (.055, .055, .945, .945), 206, 252, fill=WHITE, width=.014)
    arc(d, (.055, .055, .945, .945), 282, 326, fill=accent, width=.014)
    ellipse(d, (.486, .058, .514, .086), fill=GOLD)
    return image, d


def rune_1(d):  # Saldırı — focused blade + strike
    polygon(d, [(.49,.18),(.60,.34),(.54,.68),(.45,.79),(.42,.67),(.46,.35)], WHITE)
    polygon(d, [(.31,.66),(.69,.36),(.73,.43),(.37,.75)], CYAN)
    line(d, [(.34,.79),(.68,.79)], GOLD, .028)


def rune_2(d):  # İyileştirme — protected heart + restorative spark
    polygon(d, [(.50,.76),(.25,.50),(.27,.34),(.39,.27),(.50,.38),(.61,.27),(.73,.34),(.75,.50)], WHITE)
    line(d, [(.50,.35),(.50,.62)], CYAN, .052)
    line(d, [(.38,.49),(.62,.49)], CYAN, .052)
    ellipse(d, (.69,.22,.76,.29), fill=GOLD)


def rune_3(d):  # Hareket — winged forward chevrons
    polygon(d, [(.22,.58),(.44,.26),(.50,.49),(.72,.32),(.56,.70),(.48,.54),(.35,.76)], WHITE)
    line(d, [(.24,.35),(.36,.44)], CYAN, .034)
    line(d, [(.67,.67),(.79,.56)], GOLD, .034)


def rune_4(d):  # Savunma — split tower shield
    polygon(d, [(.50,.18),(.74,.29),(.70,.59),(.50,.80),(.30,.59),(.26,.29)], WHITE)
    polygon(d, [(.50,.26),(.65,.33),(.62,.55),(.50,.68)], DARK)
    line(d, [(.50,.25),(.50,.69)], CYAN, .026)


def rune_5(d):  # Patlama — controlled six-point detonation
    polygon(d, [(.50,.17),(.57,.37),(.75,.26),(.65,.45),(.84,.50),(.64,.57),(.75,.76),(.56,.65),(.50,.84),(.43,.64),(.25,.75),(.36,.56),(.16,.50),(.37,.44),(.26,.25),(.44,.37)], WHITE)
    ellipse(d, (.40,.40,.60,.60), fill=GOLD)
    ellipse(d, (.455,.455,.545,.545), fill=DARK)


def rune_6(d):  # Kontrol — linked restraint rings
    arc(d, (.19,.30,.55,.66), 40, 320, WHITE, .065)
    arc(d, (.45,.34,.81,.70), 220, 500, WHITE, .065)
    line(d, [(.38,.47),(.62,.53)], CYAN, .045)
    ellipse(d, (.46,.46,.54,.54), fill=GOLD)


def rune_7(d):  # Zayıflatma — fractured descending sigil
    polygon(d, [(.27,.24),(.69,.24),(.60,.43),(.71,.43),(.48,.80),(.50,.55),(.33,.55)], WHITE)
    polygon(d, [(.48,.24),(.55,.24),(.49,.41),(.57,.48),(.45,.57),(.50,.67),(.43,.78),(.40,.55),(.48,.46),(.41,.38)], DARK)
    line(d, [(.24,.73),(.36,.73)], CYAN, .025)


def rune_8(d):  # Güçlendirme — ascending crown/arrow
    polygon(d, [(.50,.17),(.70,.43),(.59,.43),(.59,.70),(.41,.70),(.41,.43),(.30,.43)], WHITE)
    polygon(d, [(.28,.75),(.38,.61),(.48,.74),(.59,.60),(.72,.75)], CYAN)
    line(d, [(.29,.80),(.71,.80)], GOLD, .026)


def rune_9(d):  # Arındırma — cleansing drop and radiant cut
    polygon(d, [(.50,.18),(.68,.47),(.65,.65),(.50,.76),(.35,.65),(.32,.47)], WHITE)
    polygon(d, [(.50,.31),(.57,.49),(.51,.62),(.42,.58),(.39,.49)], DARK)
    line(d, [(.24,.28),(.34,.37)], CYAN, .031)
    line(d, [(.68,.65),(.77,.74)], GOLD, .031)
    ellipse(d, (.70,.24,.76,.30), fill=WHITE)


def rune_10(d):  # Yansıma — mirrored shards around a seam
    polygon(d, [(.23,.50),(.43,.24),(.43,.76)], WHITE)
    polygon(d, [(.77,.50),(.57,.24),(.57,.76)], WHITE)
    polygon(d, [(.30,.50),(.39,.37),(.39,.63)], CYAN)
    polygon(d, [(.70,.50),(.61,.37),(.61,.63)], GOLD)
    line(d, [(.50,.22),(.50,.78)], SOFT, .018)


def rune_11(d):  # Çağırma — portal and emerging familiar
    arc(d, (.21,.18,.79,.76), 185, 535, WHITE, .052)
    ellipse(d, (.44,.34,.56,.46), fill=GOLD)
    polygon(d, [(.50,.43),(.65,.68),(.55,.65),(.50,.78),(.45,.65),(.35,.68)], WHITE)
    line(d, [(.27,.71),(.73,.71)], CYAN, .032)


def rune_12(d):  # Zaman — hourglass inside an orbital arrow
    polygon(d, [(.35,.25),(.65,.25),(.57,.43),(.50,.50),(.43,.43)], WHITE)
    polygon(d, [(.35,.75),(.65,.75),(.57,.57),(.50,.50),(.43,.57)], WHITE)
    line(d, [(.32,.22),(.68,.22)], GOLD, .024)
    line(d, [(.32,.78),(.68,.78)], GOLD, .024)
    arc(d, (.17,.17,.83,.83), 205, 445, CYAN, .027)
    polygon(d, [(.72,.18),(.83,.27),(.69,.30)], CYAN)


def fist(d):
    polygon(d, [(.25,.48),(.30,.30),(.40,.27),(.46,.35),(.48,.23),(.59,.22),(.64,.35),(.70,.28),(.79,.34),(.74,.57),(.61,.76),(.38,.73)], WHITE)
    line(d, [(.31,.45),(.69,.46)], CYAN, .025)
    line(d, [(.43,.35),(.42,.51)], DARK, .022)
    line(d, [(.58,.34),(.57,.50)], DARK, .022)


def dagger(d):
    polygon(d, [(.24,.72),(.38,.43),(.67,.22),(.59,.52),(.33,.78)], WHITE)
    polygon(d, [(.76,.72),(.62,.43),(.33,.22),(.41,.52),(.67,.78)], SOFT)
    line(d, [(.25,.61),(.39,.75)], GOLD, .035)
    line(d, [(.75,.61),(.61,.75)], CYAN, .035)


def spear(d):
    polygon(d, [(.75,.17),(.69,.39),(.59,.29)], WHITE)
    line(d, [(.25,.78),(.69,.34)], WHITE, .044)
    line(d, [(.21,.82),(.31,.72)], GOLD, .055)
    polygon(d, [(.57,.32),(.67,.42),(.51,.48)], CYAN)


def sword(d):
    polygon(d, [(.68,.17),(.64,.51),(.53,.62),(.43,.52),(.55,.40)], WHITE)
    line(d, [(.28,.75),(.58,.45)], WHITE, .048)
    line(d, [(.34,.57),(.48,.71)], GOLD, .040)
    polygon(d, [(.24,.79),(.32,.69),(.37,.74),(.29,.83)], CYAN)


def axe(d):
    line(d, [(.36,.80),(.59,.29)], WHITE, .050)
    polygon(d, [(.48,.25),(.66,.18),(.80,.29),(.70,.49),(.54,.42)], WHITE)
    polygon(d, [(.55,.30),(.67,.25),(.72,.30),(.66,.39)], DARK)
    line(d, [(.28,.75),(.42,.82)], GOLD, .040)


def hammer(d):
    line(d, [(.42,.80),(.55,.40)], WHITE, .060)
    polygon(d, [(.25,.25),(.69,.20),(.78,.34),(.70,.47),(.28,.48),(.19,.36)], WHITE)
    polygon(d, [(.29,.29),(.65,.26),(.69,.35),(.31,.39)], DARK)
    line(d, [(.35,.76),(.50,.82)], CYAN, .035)


def cannon(d):
    polygon(d, [(.22,.38),(.66,.25),(.79,.34),(.71,.58),(.32,.61)], WHITE)
    ellipse(d, (.54,.49,.78,.73), fill=SOFT)
    ellipse(d, (.60,.55,.72,.67), fill=DARK)
    polygon(d, [(.25,.57),(.40,.58),(.33,.79),(.23,.77)], GOLD)
    ellipse(d, (.74,.28,.83,.37), fill=CYAN)


def staff(d):
    line(d, [(.35,.80),(.57,.30)], WHITE, .050)
    arc(d, (.45,.16,.75,.44), 40, 300, WHITE, .047)
    ellipse(d, (.55,.23,.67,.35), fill=CYAN)
    polygon(d, [(.29,.74),(.42,.80),(.34,.86)], GOLD)


def talisman(d):
    polygon(d, [(.28,.20),(.70,.26),(.65,.80),(.23,.73)], WHITE)
    polygon(d, [(.34,.29),(.62,.33),(.58,.68),(.30,.65)], DARK)
    line(d, [(.46,.33),(.44,.62)], CYAN, .035)
    line(d, [(.34,.45),(.56,.49)], CYAN, .035)
    ellipse(d, (.67,.63,.78,.74), fill=GOLD)


def shield(d):
    polygon(d, [(.50,.17),(.76,.29),(.70,.62),(.50,.81),(.30,.62),(.24,.29)], WHITE)
    polygon(d, [(.50,.27),(.65,.34),(.61,.57),(.50,.68)], DARK)
    polygon(d, [(.50,.27),(.35,.34),(.39,.57),(.50,.68)], CYAN)
    line(d, [(.50,.27),(.50,.69)], GOLD, .020)


RUNES = [
    ("rune-01-saldiri", rune_1, CYAN),
    ("rune-02-iyilestirme", rune_2, (82, 235, 166, 255)),
    ("rune-03-hareket", rune_3, (91, 186, 255, 255)),
    ("rune-04-savunma", rune_4, (147, 145, 255, 255)),
    ("rune-05-patlama", rune_5, (255, 108, 82, 255)),
    ("rune-06-kontrol", rune_6, (91, 214, 236, 255)),
    ("rune-07-zayiflatma", rune_7, (202, 106, 255, 255)),
    ("rune-08-guclendirme", rune_8, (255, 191, 74, 255)),
    ("rune-09-arindirma", rune_9, (111, 236, 211, 255)),
    ("rune-10-yansima", rune_10, (187, 155, 255, 255)),
    ("rune-11-cagirma", rune_11, (120, 137, 255, 255)),
    ("rune-12-zaman", rune_12, (89, 218, 255, 255)),
]

WEAPONS = [
    ("weapon-01-yumruk", fist, GOLD),
    ("weapon-02-hancer", dagger, (208, 132, 255, 255)),
    ("weapon-03-mizrak", spear, CYAN),
    ("weapon-04-kilic", sword, (116, 211, 255, 255)),
    ("weapon-05-balta", axe, (255, 143, 76, 255)),
    ("weapon-06-cekic", hammer, GOLD),
    ("weapon-07-top", cannon, (255, 116, 83, 255)),
    ("weapon-08-asa", staff, (139, 121, 255, 255)),
    ("weapon-09-tilsim", talisman, (91, 228, 190, 255)),
    ("weapon-10-kalkan", shield, (107, 188, 255, 255)),
]


def save_set(folder, entries):
    target = OUT / folder
    target.mkdir(parents=True, exist_ok=True)
    for name, painter, accent in entries:
        image, draw = base_icon(accent)
        painter(draw)
        path = target / f"{name}.png"
        image.resize((FINAL, FINAL), Image.Resampling.LANCZOS).save(path, optimize=True)
        write_texture_meta(path)


def guid_for(path):
    relative = path.resolve().relative_to(ROOT).as_posix()
    return hashlib.md5(("dovus-hud:" + relative).encode("utf-8")).hexdigest()


def write_folder_meta(path):
    meta = Path(str(path) + ".meta")
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid_for(path)}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def write_texture_meta(path):
    meta = Path(str(path) + ".meta")
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid_for(path)}\n"
        "TextureImporter:\n"
        "  internalIDToNameTable: []\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 0\n"
        "    sRGBTexture: 1\n"
        "    linearTexture: 0\n"
        "    fadeOut: 0\n"
        "    borderMipMap: 0\n"
        "    mipMapsPreserveCoverage: 0\n"
        "    alphaTestReferenceValue: 0.5\n"
        "  isReadable: 0\n"
        "  streamingMipmaps: 0\n"
        "  streamingMipmapsPriority: 0\n"
        "  grayScaleToAlpha: 0\n"
        "  generateCubemap: 6\n"
        "  cubemapConvolution: 0\n"
        "  seamlessCubemap: 0\n"
        "  textureFormat: 1\n"
        "  maxTextureSize: 256\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 1\n"
        "    aniso: 1\n"
        "    mipBias: 0\n"
        "    wrapU: 1\n"
        "    wrapV: 1\n"
        "    wrapW: 1\n"
        "  nPOTScale: 1\n"
        "  lightmap: 0\n"
        "  compressionQuality: 50\n"
        "  spriteMode: 0\n"
        "  spriteExtrude: 1\n"
        "  spriteMeshType: 1\n"
        "  alignment: 0\n"
        "  spritePivot: {x: 0.5, y: 0.5}\n"
        "  spritePixelsToUnits: 100\n"
        "  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n"
        "  spriteGenerateFallbackPhysicsShape: 0\n"
        "  alphaUsage: 1\n"
        "  alphaIsTransparency: 1\n"
        "  spriteTessellationDetail: -1\n"
        "  textureType: 0\n"
        "  textureShape: 1\n"
        "  singleChannelComponent: 0\n"
        "  flipbookRows: 1\n"
        "  flipbookColumns: 1\n"
        "  maxTextureSizeSet: 0\n"
        "  compressionQualitySet: 0\n"
        "  textureFormatSet: 0\n"
        "  ignorePngGamma: 0\n"
        "  applyGammaDecoding: 0\n"
        "  platformSettings:\n"
        "  - serializedVersion: 4\n"
        "    buildTarget: DefaultTexturePlatform\n"
        "    maxTextureSize: 256\n"
        "    resizeAlgorithm: 0\n"
        "    textureFormat: -1\n"
        "    textureCompression: 1\n"
        "    compressionQuality: 50\n"
        "    crunchedCompression: 0\n"
        "    allowsAlphaSplitting: 0\n"
        "    overridden: 0\n"
        "  - serializedVersion: 4\n"
        "    buildTarget: Android\n"
        "    maxTextureSize: 256\n"
        "    resizeAlgorithm: 0\n"
        "    textureFormat: -1\n"
        "    textureCompression: 1\n"
        "    compressionQuality: 50\n"
        "    crunchedCompression: 0\n"
        "    allowsAlphaSplitting: 0\n"
        "    overridden: 0\n"
        "  spriteSheet:\n"
        "    serializedVersion: 2\n"
        "    sprites: []\n"
        "    outline: []\n"
        "    customData: \n"
        "    physicsShape: []\n"
        "    bones: []\n"
        "    spriteID: \n"
        "    internalID: 0\n"
        "    vertices: []\n"
        "    indices: \n"
        "    edges: []\n"
        "    weights: []\n"
        "    secondaryTextures: []\n"
        "    nameFileIdTable: {}\n"
        "  mipmapLimitGroupName: \n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    write_folder_meta(OUT)
    write_folder_meta(OUT / "Runes")
    write_folder_meta(OUT / "Weapons")
    save_set("Runes", RUNES)
    save_set("Weapons", WEAPONS)
    print(f"Wrote {len(RUNES) + len(WEAPONS)} HUD icons to {OUT}")
