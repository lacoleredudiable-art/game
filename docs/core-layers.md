# Core katman sırası (2B.6c)

Üst klasör grafiği: `tools/CoreTests/CoreLayeringTests.cs` (Tarjan SCC kapısı).

## Hedef bağımlılık yönü (alttan üste)

1. **Shared** — JSON (`MiniJson`), kimlik enum'ları (`PortalOp`, `TeamOp`, `SlamVariant`), `ResourceTracker`, paylaşılan yardımcılar. Core içi çıkış yok.
2. **Border, Time, Hud** — sahne/çerçeve; Core içi çıkış yok veya yalnız Shared.
3. **Data** — `element-sistemi.json` ve diğer ham belge kökleri; yalnız Shared'a bağımlı.
4. **Element, Input** — rün kimliği (`Rune`, `RuneLoadout`), altıgen geometri (`HexagonLayout`, `JumpKind`).
5. **Tuning** — tüm `*Tuning` serileştirme tipleri + `CombatTuning` kökü; Shared (ve gerektiğinde Manifestation sayıları).
6. **Presentation, Mechanic, Manifestation** — görsel/mechanic planlama; Data + Grammar + Motion'a doğru tek yön.
7. **Grammar** — `SkillMotor`, `SentenceEngine`, `SkillEngineModifiers`; Data + Element + Input.
8. **Motion, Portal, Team** — dünya hareketi ve takım/portal kuralları; Grammar ve alt katmanlar.
9. **Dövüş konu klasörleri** — `Boss`, `Casting`, `Damage`, `Dodge`, `Equipment`, `Passives`, `Status`: oyun döngüsü; birbirine sıkı bağlı kalan SCC (2B.6c allowlist). Yeni çapraz `using` eklemeden önce alt katmana tip çekin.

## 2B.6c sonrası ölçüm

- **Kırılan döngüler:** Data↔Grammar/Team/Portal; Element↔Grammar; Input↔Grammar; Tuning↔dövüş; Equipment↔Grammar (#106); Combat↔Status (2B.6a).
- **Kalan SCC (1):** Boss, Casting, Damage, Dodge, Equipment, Grammar, Passives, Status — kapı testinde açık izinli; sayı ratchet ile yeni SCC yasak.
