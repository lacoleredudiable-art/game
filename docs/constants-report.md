# Oynanış sabit sayı raporu (PLAN 2B.11)

Kapsam: en yoğun 5 oynanış dosyası + `*Defaults` tek kaynak sınıfları. Görsel/HUD bu PR'da taşınmadı.

## Klasör bazlı float literal sayısı (`\d+\.\d+f` / `\d+f`)

| Klasör | Önce | Sonra | Not |
|--------|------|-------|-----|
| Skills | 1051 | 1013 | Closing + MechanicWorld |
| Boss | 392 | 387 | AttackTelegraphView + BossTelegraphView |
| Team | 70 | 63 | TeamComboHost |
| Actors | 287 | 287 | gelecek iş |
| Combat | 0 | 0 | — |
| **Oynanış toplam** | **1800** | **1750** | ratchet: `GameplayLiteralRatchetTests` |

Görsel-öncelikli klasörler (bu PR dokunmadı): Hud, Feel, Audio, Casting, Cameras, Arena, Weapons, Vfx, Config, Composition, Data — yaklaşık **3596** float literal (BuildSelectHud, HudTheme, WeaponHandPropsView, …).

## Taşınan adlandırılmış sabitler (const, JSON yok)

| Anahtar / alan | Değer | Dosya |
|----------------|-------|-------|
| `ClosingDamageDefaults.*` | eski literal'ler 1:1 | `Game/Skills/Closing/ClosingDamageDefaults.cs` |
| `TeamComboDefaults.*` | eski literal'ler 1:1 | `Game/Team/TeamComboDefaults.cs` |
| `MechanicWorldDefaults.*` | eski literal'ler 1:1 | `Game/Skills/Mechanics/MechanicWorldDefaults.cs` |
| `AttackTelegraphViewDefaults.*` | eski literal'ler 1:1 | `Game/Boss/AttackTelegraphViewDefaults.cs` |
| `BossTelegraphViewDefaults.*` | eski literal'ler 1:1 | `Game/Boss/BossTelegraphViewDefaults.cs` |

Mevcut `JsonParam` anahtarları değişmedi; yeni JSON anahtarı eklenmedi (değerler zaten gömülü fallback'te).

## Gelecek iş

- `LivingEffectView`, `ProceduralChunkMesh`, `JsonEffectRuntime`, `ManifestationDirector.*`, `KinematicMotorController`, `ActorView`, HUD tema dosyaları.
- `element-sistemi.json` parametreleri: yalnız zaten `JsonParam` ile okunan kurallar.
- Actors + kalan Skills/Boss dosyaları.

---

## 2B.11b–g: tüm oynanış/ayar sayıları (A17 tam kapanış)

Kural: Core/App/Game kodundaki her oynanış/ayar sayısı (hasar, süre, menzil, hız, bekleme, zamanlama, sayaç) adlandırılmış bir kaynakta durur:
`*Defaults` statik `const` sınıfları (değer birebir aynı literal, aynı tip), `Core/Tuning/*Tuning`, `Game/Config` (tuning.json) ya da `Resources` JSON.
Bu aşamada taşıma yalnız `const` ile yapılır (derleme anında aynı değer → sweep hash bayt bayt aynı). Bir değeri çalışma anı ayarına/JSON'a
yükseltmek ayrı bir tasarım kararıdır (sabit katlama sırası değişebilir), burada yapılmaz.

**Sayaç** (`tools/CoreTests/MagicNumberRatchetTests.cs`): yorum/string dışındaki sayı literal'leri. Muaf (önemsiz ya da ayar kaynağı):
- değerler: 0–16 son eksiz tamsayı (indeks, segment, mesh), `0/1/2` (f/d dahil), `0.5`, `10`, `60`, `90`, `100`, `180`, `255`, `360`, `1000`, `1e-3…1e-6`;
- satırlar: `const`, enum değeri, `[Attribute]`, `case`, `#if`, dizi indeksi `[n]`, renk kurucuları (`Color`/`Color32`/`HSVToRGB`), `Rect`, UI yerleşim
  (anchor/size/font/padding/Place/CreateText/GUI…);
- dosyalar: `*Defaults.cs`, `Core/Tuning/**`, `Game/Config/**`, `HudTheme.cs` (tema verisi), `ProceduralChunkMesh.cs` (mesh köşe verisi), `Editor/`, `DevTools/`, `Tests/`.

| Ölçüm | Toplam |
|-------|--------|
| 2B.11 öncesi (#113 master) | **1615** |
| #114 sonrası (en yoğun 5 dosya) | **1499** |

| Alan | #114 sonrası |
|------|------|
| Core | 336 |
| Game/Skills | 261 |
| Game/Vfx | 135 |
| Game/Weapons | 127 |
| Game/Arena | 121 |
| Game/Casting | 109 |
| Game/Actors | 84 |
| Game/Hud | 77 |
| Game/Audio | 72 |
| Game/Boss | 69 |
| Game/Cameras | 37 |
| Game/Composition | 35 |
| App | 15 |
| Game/Feel | 12 |
| Game/Team | 5 |
| Game/Platform | 4 |

### İlerleme
<!-- 2B.11 ilerleme satırları -->
2B.11c: Core+App 351 → 0, ~190 yeni const, 37 yeni Defaults dosyası (+2 const `StatusDefaults`).
2B.11d: Game/Skills+Game/Team 266 → 0, ~266 yeni const, 24 yeni Defaults dosyası (+TeamComboDefaults/MechanicWorldDefaults genişletme).
2B.11e: Game/Boss+Actors+Composition+Cameras+Feel+Platform 241→0, 182 const, 37 yeni Defaults dosyası (+AttackTelegraphViewDefaults/BossTelegraphViewDefaults genişletme).
2B.11f: Game/Weapons+Game/Audio+Game/Casting 308→0, 257 yeni const, 10 yeni Defaults dosyası.
2B.11g: Game/Vfx+Game/Arena+Game/Hud 333→0, ~310 yeni const, 22 yeni Defaults dosyası.
