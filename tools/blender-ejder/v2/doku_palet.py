"""Albedo -> sanat incili paleti (SI-2, SI-5.3–5.5) + kor maskesi (R faz1, G göz, B faz2+).
Kullanım: python doku_palet.py <doku_klasörü> <ad> <albedo.png> <boyut> <çıkış_albedo> <çıkış_kor>
Yüz verisi: <doku_klasörü>/<ad>_yuz.npz (yuzey_disa.py)."""
import sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

D, NAME, SRC, SIZE, OUT_A, OUT_E = sys.argv[1:7]; SIZE = int(SIZE)

def srgb2lin(c): return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def lin2srgb(c): return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.clip(c, 0, None) ** (1 / 2.4) - 0.055)
M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]]); Mi = np.linalg.inv(M)
WP = np.array([0.95047, 1.0, 1.08883])
def f(t): return np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16 / 116)
def fi(t): return np.where(t > 0.206893, t ** 3, (t - 16 / 116) / 7.787)
def rgb2lab(rgb):
    xyz = srgb2lin(rgb) @ M.T / WP; fx, fy, fz = f(xyz[..., 0]), f(xyz[..., 1]), f(xyz[..., 2])
    return np.stack([116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)], -1)
def lab2rgb(lab):
    fy = (lab[..., 0] + 16) / 116; fx = fy + lab[..., 1] / 500; fz = fy - lab[..., 2] / 200
    xyz = np.stack([fi(fx), fi(fy), fi(fz)], -1) * WP
    return np.clip(lin2srgb(xyz @ Mi.T), 0, 1)
def hexlab(h): return rgb2lab(np.array([int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)]))

TAS = hexlab("#3A4048"); KEMIK = hexlab("#5B5A57"); KUL = hexlab("#5B5A57")

d = np.load(f"{D}/{NAME}_yuz.npz"); uv, N, C = d["uv"], d["n"], d["c"]
img = Image.new("I", (SIZE, SIZE), 0); dr = ImageDraw.Draw(img)
for i, t in enumerate(uv):
    dr.polygon([(float(u * SIZE), float((1 - v) * SIZE)) for u, v in t], fill=i + 1)
fm = np.asarray(img).astype(np.int64) - 1
cov = fm >= 0
# boşlukları en yakın yüzle doldur (dikiş kanaması olmasın)
idx = ndimage.distance_transform_edt(~cov, return_distances=False, return_indices=True)
fmf = fm[idx[0], idx[1]]
Cn, Nn = C[fmf], N[fmf]                       # piksel başına konum ve normal (arena: X, Z, H)

src = np.asarray(Image.open(SRC).convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)).astype(float) / 255
lab = rgb2lab(src)
Lm, Ls = lab[cov, 0].mean(), lab[cov, 0].std()
out = np.empty_like(lab)
out[..., 0] = np.clip(TAS[0] + (lab[..., 0] - Lm) / max(Ls, 1e-3) * 5.5, 12, 45)
am, bm = lab[cov, 1].mean(), lab[cov, 2].mean()
out[..., 1] = TAS[1] + (lab[..., 1] - am) * 0.15
out[..., 2] = TAS[2] + (lab[..., 2] - bm) * 0.15

X, Z, H = Cn[..., 0], Cn[..., 1], Cn[..., 2]
w_kemik = np.zeros(lab.shape[:2])
if NAME == "ejder_bas":
    w_kemik = np.clip((H - 7.2) / 0.8, 0, 1) * (Z < 18.5)            # boynuzlar ve tepe dikenleri
elif NAME.startswith("ejder_bacak"):
    w_kemik = np.clip((1.3 - H) / 0.4, 0, 1) * np.clip((13.6 - Z) / 0.8, 0, 1)   # tırnaklar
elif NAME == "ejder_govde":
    w_kemik = np.clip((H - 15.5) / 1.5, 0, 1) * 0.7                    # sırt dikenleri
# kül: yukarı bakan yüzler, düşük frekanslı gürültüyle
rng = np.random.default_rng(7)
noise = ndimage.gaussian_filter(rng.random((SIZE // 8, SIZE // 8)), 2.0)
noise = (noise - noise.min()) / (noise.max() - noise.min())
noise = np.asarray(Image.fromarray((noise * 255).astype(np.uint8)).resize((SIZE, SIZE), Image.BICUBIC)).astype(float) / 255
w_kul = np.clip((Nn[..., 2] - 0.55) / 0.35, 0, 1) * (0.35 + 0.5 * noise)
for w, tgt, keepL in ((w_kemik, KEMIK, 0.45), (w_kul, KUL, 0.35)):
    w = w[..., None]
    t = tgt.copy(); tL = tgt[0] + (out[..., 0] - TAS[0]) * keepL
    blend = np.stack([tL, np.full_like(tL, t[1]), np.full_like(tL, t[2])], -1)
    out = out * (1 - w) + blend * w

alb = lab2rgb(out)
Image.fromarray((alb * 255 + 0.5).astype(np.uint8)).save(OUT_A)

# --- 3B pul çatlakları: piksel başına 3B konum (ağırlık merkezi), Voronoi kenarları ---
Pf = d["p"]
py, px = np.mgrid[0:SIZE, 0:SIZE]
pu = (px + 0.5) / SIZE; pv = 1 - (py + 0.5) / SIZE
T = uv[fmf]; A_, B_, C_ = T[..., 0, :], T[..., 1, :], T[..., 2, :]
v0 = B_ - A_; v1 = C_ - A_; v2 = np.stack([pu - A_[..., 0], pv - A_[..., 1]], -1)
den = v0[..., 0] * v1[..., 1] - v1[..., 0] * v0[..., 1]; den = np.where(np.abs(den) < 1e-12, 1e-12, den)
b1 = (v2[..., 0] * v1[..., 1] - v1[..., 0] * v2[..., 1]) / den
b2 = (v0[..., 0] * v2[..., 1] - v2[..., 0] * v0[..., 1]) / den
b1 = np.clip(b1, 0, 1); b2 = np.clip(b2, 0, 1 - b1); b0 = 1 - b1 - b2
PP = Pf[fmf]; P3 = PP[..., 0, :] * b0[..., None] + PP[..., 1, :] * b1[..., None] + PP[..., 2, :] * b2[..., None]
from scipy.spatial import cKDTree
lo, hi = Pf.reshape(-1, 3).min(0) - 2, Pf.reshape(-1, 3).max(0) + 2
a3 = 0.5 * np.linalg.norm(np.cross(Pf[:, 1] - Pf[:, 0], Pf[:, 2] - Pf[:, 0]), axis=1).sum()
e1 = uv[:, 1] - uv[:, 0]; e2 = uv[:, 2] - uv[:, 0]
a2 = 0.5 * np.abs(e1[:, 0] * e2[:, 1] - e1[:, 1] * e2[:, 0]).sum()
MPX = np.sqrt(a3 / (a2 * SIZE * SIZE))              # bir doku pikseli kaç metre
WIDTH = max(0.09, 2.4 * MPX)
CELL = max(2.2, WIDTH * 24)
print(f"DOKU {NAME}: piksel {MPX*100:.1f} cm, çatlak {WIDTH*100:.0f} cm, hücre {CELL:.1f} m")
nseed = int(np.prod((hi - lo) / CELL)) + 50
seeds = lo + np.random.default_rng(11).random((nseed, 3)) * (hi - lo)
dd, _ = cKDTree(seeds).query(P3.reshape(-1, 3), k=2)
edge = (dd[:, 1] - dd[:, 0]).reshape(SIZE, SIZE)
g = np.random.default_rng(5).random((24, 24, 24))
gc = (P3 - lo) / (hi - lo) * 23
brk = ndimage.map_coordinates(g, [gc[..., 0], gc[..., 1], gc[..., 2]], order=1)
crack3d = np.clip((WIDTH - edge) / (WIDTH * 0.5), 0, 1) * np.clip((brk - 0.42) / 0.12, 0, 1)
crack3d = crack3d * (1 - np.clip(w_kemik, 0, 1))     # boynuz ve tırnakta kor yok
# dokuda aşırı gerilmiş üçgenler (seyrek piksel) kor almaz: tek çatlak koca leke olur
fa3 = 0.5 * np.linalg.norm(np.cross(Pf[:, 1] - Pf[:, 0], Pf[:, 2] - Pf[:, 0]), axis=1)
fa2 = 0.5 * np.abs(e1[:, 0] * e2[:, 1] - e1[:, 1] * e2[:, 0]) * SIZE * SIZE
dens = np.sqrt(fa3 / np.maximum(fa2, 1e-9))                  # metre / piksel
med = np.median(dens[fa2 > 1])
stretch_ok = np.clip((2.5 * med - dens) / (1.0 * med), 0, 1)
crack3d = crack3d * stretch_ok[fmf]
print(f"GERILME {NAME}: medyan {med*100:.1f} cm/piksel, kor kapatılan üçgen %{(stretch_ok < 0.5).mean()*100:.1f}")
crack3d = ndimage.gaussian_filter(crack3d, 0.5)
# albedoda çatlak içi koyulaşır (kor sönükken de okunur)
alb = alb * (1 - 0.45 * crack3d[..., None])
Image.fromarray((alb * 255 + 0.5).astype(np.uint8)).save(OUT_A)

# --- kor maskesi ---
v = src.max(-1)
blur = ndimage.gaussian_filter(v, SIZE / 700)
crev = np.clip((blur - v - 0.025) / 0.08, 0, 1) * (v < 0.42)
cov_in = ndimage.binary_erosion(cov, iterations=3)
crev = crack3d   # yalnız 3B pul çatlakları (2B doku çizgileri gürültülü)
hsv = np.asarray(Image.open(SRC).convert("HSV").resize((SIZE, SIZE))).astype(float) / 255
warm = (hsv[..., 1] > 0.35) & (hsv[..., 2] > 0.3) & ((hsv[..., 0] * 360 < 45) | (hsv[..., 0] * 360 > 330))
R = np.zeros_like(v); G = np.zeros_like(v); B = np.zeros_like(v)
if NAME == "ejder_bas":
    for ex in (2.0, -2.0):
        dist = np.sqrt((X - ex) ** 2 + (Z - 12.0) ** 2 + (H - 5.0) ** 2)
        G = np.maximum(G, np.clip((0.75 - dist) / 0.25, 0, 1) * warm)          # gözün kendi sıcak pikselleri
        G = np.maximum(G, np.clip((0.32 - dist) / 0.12, 0, 1))                 # küçük çekirdek
    G = ndimage.gaussian_filter(G * cov, 0.6)
    neck = np.clip((Z - 17.0) / 1.5, 0, 1)
    R = crev * neck
    B = crev * (1 - neck) * 0.6
elif NAME.startswith("ejder_bacak"):
    B = crev * 0.8
elif NAME == "ejder_kanatlar":
    vein = np.maximum(crev * 0.5, warm.astype(float) * 0.9)
    B = ndimage.gaussian_filter(vein * cov, 0.6)             # zar damarları + kemik çatlakları: faz 2+
elif NAME == "ejder_gogus":
    low = np.clip((21.0 - Z) / 3.0, 0, 1) * np.clip((9.0 - H) / 3.0, 0, 1)
    R = crev * low * 0.85                      # alt göğüs önü faz 1'de yanar
    B = crev * (1 - low) * 0.6                 # omuzlar faz 2+
elif NAME == "ejder_govde":
    crev = np.maximum(crev, warm.astype(float) * 0.7)
    chest = np.clip((26.0 - Z) / 6.0, 0, 1) * np.clip((11.0 - H) / 4.0 + 0.5, 0, 1)
    R = crev * chest
    B = crev * (1 - chest) * 0.7
E = np.stack([R, G, B, np.ones_like(R)], -1)
Image.fromarray((np.clip(E, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(OUT_E)

Lout = out[..., 0][cov]
print(f"PALET {NAME}: L* ort {Lout.mean():.1f} (hedef 27), %5–%95 {np.percentile(Lout,5):.1f}–{np.percentile(Lout,95):.1f}; "
      f"kemik payı %{(w_kemik[cov]>0.5).mean()*100:.1f}, kül payı %{(w_kul[cov]>0.3).mean()*100:.1f}; "
      f"kor R %{(R[cov]>0.3).mean()*100:.2f} G %{(G[cov]>0.3).mean()*100:.2f} B %{(B[cov]>0.3).mean()*100:.2f}")
