# Oynanış sabit sayı raporu (PLAN 2B.11)

Kapsam: en yoğun 5 oynanış dosyası + `*Defaults` tek kaynak sınıfları. Görsel/HUD bu PR'da taşınmadı.

## Klasör bazlı float literal sayısı (`\d+\.\d+f` / `\d+f`)

| Klasör | Önce | Sonra | Not |
|--------|------|-------|-----|
| Skills | 1051 | 1013 | Closing + MechanicWorld |
| Boss | 392 | 387 | AttackTelegraph + BossTelegraph |
| Team | 70 | 63 | TeamComboHost |
| Actors | 287 | 287 | gelecek iş |
| Combat | 0 | 0 | — |
| **Oynanış toplam** | **1800** | **1750** | ratchet: `GameplayLiteralRatchetTests` |

Görsel-öncelikli klasörler (bu PR dokunmadı): Hud, Feel, Audio, Casting, Cameras, Arena, Weapons, Vfx, Config, Composition, Data — yaklaşık **3596** float literal (BuildSelectScreen, HudTheme, WeaponHandProps, …).

## Taşınan adlandırılmış sabitler (const, JSON yok)

| Anahtar / alan | Değer | Dosya |
|----------------|-------|-------|
| `ClosingDamageDefaults.*` | eski literal'ler 1:1 | `Game/Skills/Closing/ClosingDamageDefaults.cs` |
| `TeamComboDefaults.*` | eski literal'ler 1:1 | `Game/Team/TeamComboDefaults.cs` |
| `MechanicWorldDefaults.*` | eski literal'ler 1:1 | `Game/Skills/Mechanics/MechanicWorldDefaults.cs` |
| `AttackTelegraphDefaults.*` | eski literal'ler 1:1 | `Game/Boss/AttackTelegraphDefaults.cs` |
| `BossTelegraphDefaults.*` | eski literal'ler 1:1 | `Game/Boss/BossTelegraphDefaults.cs` |

Mevcut `JsonParam` anahtarları değişmedi; yeni JSON anahtarı eklenmedi (değerler zaten gömülü fallback'te).

## Gelecek iş

- `LivingEffectView`, `ProceduralChunkMesh`, `JsonEffectRuntime`, `ManifestationDirector.*`, `KinematicMotor`, `ActorVisual`, HUD tema dosyaları.
- `element-sistemi.json` parametreleri: yalnız zaten `JsonParam` ile okunan kurallar.
- Actors + kalan Skills/Boss dosyaları.
