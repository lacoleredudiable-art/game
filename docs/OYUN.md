# OYUN.md — tek oyun özeti

Alfa **dilim** (Aşama A): tek boss dövüşü, altıgen kombo, dodge ve CD/mana kapıları. Tam oyun vizyonu `docs/tasarim/13-vizyon-ve-cerceve.md`; hedef boss/arena `18`/`19`.
Sayılar aşağıdaki üretilmiş blokta; bu bölüm akış özetidir.

## Bir dövüş nasıl akar

Sol joystick hareket. Sağ altıgende ilk nokta fiil, ikinci nokta (en fazla bir) sıfat; cümle tamamlanınca `CastPipeline` skill'i çözer, yürütme yalnız `RuleEngineV4SentenceHost` + `kural-motoru-v4.json` (v4.5 tek yol). Merkeze kısa dokunuş düz vuruş. Dodge ayrı düğme: **2 hak**, **6 sn**/hak dolum; çift basış birleşik kaçış (zincir takla). İki faz: patlama + süzülme; i-frame boyunca kor turuncusu parlama; engel, minyon ve dost yüzeyinde kayma, duvara yapışmama (O-4.9). **Mükemmel dodge yoktur** (kaldırıldı; `13` §3). Boss saldırıları `BossBrain` + `BossDirector`; can `PlayerHealth`, ölçek `CombatScale`. Aktif dilim boss: Ağların Kraliçesi; sonraki hedef: Kül Ejderi (`17`–`20`).

<!-- gen:begin -->
## Sayılar ve veri (üretilmiş)

| Konu | Değer | Kaynak |
|------|-------|--------|
| Kombo üst sınırı (rün) | **2** (fiil + 1 sıfat) | `kural-motoru-v4.json` (`slice_runes` + `scale`) → `SliceGrammarBuilder` → `combo_system.current_max_length` → `SkillMotor` / `HexagonInputController` |
| Hasar/can ölçeği | ×4000 (`CombatScale.DamageAndHp`) | `global_rules.*.max_hp` ham → oyun canı |
| Oyuncu max can | **4,000,000** (ham 1000 × 4000) | `BossCombatProfile` / dilim grameri |
| Boss max can | **60,000,000** (ham 15000 × 4000) | aynı |
| Arena yarıçapı | **50 m** (çap 100 m) | `Prototype.unity` `ArenaHalfSizeM` (kod yedeği 25 m) |
| Dodge hakları | **2** | `DodgeTuning.MaxCharges` |
| Dodge dolum | **6 sn**/hak | `DodgeTuning.ChargeRechargeMs` |
| Mana maliyeti zorunlu | **true** | `CombatTuning.EnforceResourceCost` |
| Bekleme (CD) zorunlu | **true** | `CombatTuning.EnforceCooldown` |
| Kritik | **5%** / ×**2** | `crit_system` JSON + `CritSystem` |
| Dev HP (debug) | **1,000,000,000** | `DevPlayerHp.Pool` (`DebugConfig` açık build) |

### Aktif boss: `aglarin_kralicesi` — Ağların Kraliçesi

Sahne `ActiveBossId`; saldırı türleri `BossAttackKind` ile eşlenir.

**Saldırılar:**
- `leg_slam` (Slam): Bacak Çakması
- `web_spit` (Volley): Ağ Tükürüğü
- `poison_breath` (FireCone): Zehir Nefesi
- `web_field` (WebField): Ağ Örme
- `pounce` (Pounce): Sıçrayış

**Fazlar:**
- Faz 1 (Örücü): can %100→%50 — `leg_slam`, `web_spit`, `web_field`
- Faz 2 (Avcı): can %50→%0 — `leg_slam`, `web_spit`, `web_field`, `poison_breath`, `pounce`

### Rünler (6)

**Zarar**, **Şifa**, **Hareket**, **Koruma**, **Kontrol**, **Arındırma**

### Silahlar (2)

**Yay**, **Kılıç**

### Elementler (0)

—
 (oynanış çarpanı `element_mult` = 1.0; ağırlıklı VFX/boya)

### Okunmayan boss JSON alanları

Boss JSON `vitals.total_hp` / `player_hp` (120/22) **kod tarafından okunmuyor**; yalnız `phases` uygulanır.
<!-- gen:end -->
