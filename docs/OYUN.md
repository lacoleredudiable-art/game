# OYUN.md — tek oyun özeti

Alfa prototip: tek boss dövüşü, altıgen kombo girdisi, dodge ve CD/mana kapıları.
Sayılar aşağıdaki üretilmiş blokta; elle yazılan kısım yalnız akış.

## Bir dövüş nasıl akar

Sol joystick hareket. Sağ altıgende ilk nokta fiil, ikinci nokta (en fazla bir) sıfat; cümle tamamlanınca `CastPipeline` skill'i çözer ve `ManifestationDirector` yürütür. Merkeze kısa dokunuş düz vuruş. Dodge ayrı düğme: iki hak, 6 sn dolum; çift basış birleşik kaçış. Mükemmel dodge yalnız **görsel donma** (dünya/boss/CD akmaya devam). Boss saldırıları `BossBrain` + `BossDirector`; oyuncu canı `PlayerHealth`, ölçek `CombatScale`.

<!-- gen:begin -->
## Sayılar ve veri (üretilmiş)

| Konu | Değer | Kaynak |
|------|-------|--------|
| Kombo üst sınırı (rün) | **2** (fiil + 1 sıfat) | `element-sistemi.json` → `combo_system.current_max_length` → `SkillMotor` / `HexagonInputController` |
| Hasar/can ölçeği | ×4000 (`CombatScale.DamageAndHp`) | `global_rules.*.max_hp` ham → oyun canı |
| Oyuncu max can | **400,000** (ham 100 × 4000) | `BossCombatProfile` / `element-sistemi.json` |
| Boss max can | **88,000,000** (ham 22000 × 4000) | aynı |
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

### Rünler (12)

**Saldırı**, **İyileştirme**, **Hareket**, **Savunma**, **Patlama**, **Kontrol**, **Zayıflatma**, **Güçlendirme**, **Arındırma**, **Yansıma**, **Çağırma**, **Zaman**

### Silahlar (10)

**Yumruk**, **Yay**, **Büyü Kitabı**, **Kılıç**, **Küre**, **Çekiç**, **Top**, **Asa**, **Tılsım**, **Kalkan**

### Elementler (6)

**Ateş**, **Su**, **Hava**, **Toprak**, **Aydınlık**, **Karanlık**
 (oynanış çarpanı `element_mult` = 1.0; ağırlıklı VFX/boya)

### Okunmayan boss JSON alanları

Boss JSON `vitals.total_hp` / `player_hp` (120/22) **kod tarafından okunmuyor**; yalnız `phases` uygulanır.
<!-- gen:end -->
