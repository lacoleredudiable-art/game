# İsimlendirme (PLAN 2B.12)

## MonoBehaviour sonekleri (Game sahne bileşeni)

| Sonek | Rol |
|-------|-----|
| `*Director` | Oyun akışı orkestratörü |
| `*View` | Görsel sunum |
| `*Hud` | UI katmanı |
| `*Host` | Sahneye bağlanan tek örnek kabuk (Core sistem taşıyıcısı) |
| `*Controller` | Girdi / kontrol |

## Saf sınıf sonekleri

| Katman | Sonek | Durum |
|--------|-------|-------|
| App / Core (durumsuz) | `*Rules`, `*Math` | App: `BossDirectorRules`, `BossApproachRules`, … |
| Game (durumlu, MonoBehaviour değil) | `*Runtime`, `*Service` | `WeaponPassiveRuntime`, `TemplateDeliveryRuntime`, … |
| Veri | `*Catalog`, `*Table` | `EquipmentCatalog`, `TeamModifierTable`, … |

## Game `MonoBehaviour` dağılımı (2026-10-04)

Toplam **79** bileşen. Standart sonekle eşleşen birincil sınıflar: **Director 3**, **View 4**, **Hud 9**, **Host 3**, **Controller 2** → **21 / 79**.

### Standart dışı (bilinçli veya sonraki 2B.13 işi)

Örnekler: `HexagonInput`, `MoveInput`, `BossDirector` (Director soneki var), `ManifestationDirector`, `TeamComboHost`, `GameBootstrap`, `KinematicMotor`, `AllyDummy`, `CombatFeel`, `TuningPanel`, `GrammarDebugPanel`, `BuildSelectScreen`, …

İç içe yardımcı `MonoBehaviour` sınıfları (dosya adı ≠ sınıf adı, kasıtlı): `Targetable`, `HoldSurface`, `ScreenAnchoredCorner`, `Runner`, `FollowAnchor` — birincil bileşen dosya adıyla hizalı kalır.

## 2B.12 tip yeniden adları

| Eski | Yeni |
|------|------|
| `PortalBorderTeamHost` | `TeamComboHost` |
| `PortalBorderTeamAccess` | `TeamComboAccess` |
| `PortalBorderTeamDefaults` | `TeamComboDefaults` |
| `PrototypeBootstrap` | `GameBootstrap` |
| `PrototypeTuning` | `GameTuning` (düz `[Serializable]` alan: Unity alan adıyla bağlar → `MovedFrom` gerekmez; sahne/`tuning.json` alan adları aynı) |
| `V611DebugPanel` | `GrammarDebugPanel` |

`Weapons10` tip adı Scripts altında yok (2B.2f). `SweepV2` araç adı; kod tipi değil.

Bilinçli olarak DEĞİŞMEYENLER: sahne dosyası `Prototype.unity` (ve içindeki `m_EditorClassIdentifier` satırı — Unity GUID ile bağlar, bir sonraki kayıtta kendisi günceller), serileştirilmiş alanlar `_prototypeMainClassId`/`_prototypePassiveRuneIds`, `Resources/Bosses/*.json` içindeki belge amaçlı `"PrototypeTuning.PlayerMaxHp"` `maps_to` anahtarları (kod okumuyor). Görünür GameObject adları değişti: `TeamComboHost` (eski host adı), `GrammarSimpleControls` (eski `V611SimpleControls`); ada göre arayan kod yok.

**Başka dallar için:** eski tip adlarını (`PrototypeTuning`, `PrototypeBootstrap`, `PortalBorderTeam*`, `V611DebugPanel`) kullanan kod birleştirmede yukarıdaki tabloya göre yeniden adlandırılmalı.

## Yorum kodu sözlüğü

Eski PLAN / PR kısaltmaları; yeni yorumlarda tercih: kısa Türkçe cümle. Anlam belirsizse bu tabloya bak.

| Kod | Anlam | Belge |
|-----|-------|-------|
| O1 | Dodge: süre eşiği vs sürükleme eşiği | `Core/Tuning/DodgeTuning.cs` |
| O2 | Tek mükemmel dodge penceresi (PERFECT) | `DodgeTuning`, `ExchangeResolver` |
| O3 | Portal geri vuruş hasar çift sayım düzeltmesi | `TeamComboHost` |
| O4 | Diriliş zamanlayıcısı dünya saati | `PlayerVitals` |
| O5 | Eski `tuning.json` yedekleme | `TuningConfig` |
| O6 | %50 canla başlama debug bayrağı | `DebugConfig` |
| O7 | Tek kalıcı savaş RNG (kritik/sapma) | `CritSystem`, `ManifestationDirector.Damage` |
| O8 | Yay zırh delme bonusu tüketimi | `ManifestationDirector.Damage` |
| O10 | Kanallı skill sırasında silah swap kilidi | `ManifestationDirector.WeaponSwap` |
| O11 | GetComponent / Find tekrarını önbellekleme | çeşitli Game dosyaları |
| F1 | Stasis → yavaşlatma (oyuncu donması kaldırıldı) | `ManifestationDirector.VerbExecution` |
| T1 | İptal penceresi nokta sayısına bağlı | `SentenceEngine`, `ActorPose` |
| T4 | Kalıcı zemin izi / gölgesiz telefon | `GroundScarField`, `CombatAmbienceEnvironment` |
| T8.1 | Tuning sürüm yaması (bilinçli 0 korunur) | `GameTuning` |
| T9–T14 | Tuning/HUD/telemetri sürüm yamaları | `GameTuning.Migrate`, `HudSettings` |
| T10 | Canlı ayar paneli + JSON round-trip | `TuningConfig`, `TuningPanel`, Core `*Tuning` |
| T11 | Kare süresi HUD / ground scar cap | `FrameTimeHud`, `GameTuning` |
| T12 | Kapanış hasarı HUD aracı (varsayılan kapalı) | `HudSettings`, `ManifestationDirector` |
| A21–A23 | PLAN 2B.12 isimlendirme maddeleri | `docs/PLAN.md` |
| 2B.x | Mimari alt iş kodu | `docs/PLAN.md` |
