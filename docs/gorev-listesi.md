# Ajan Görev Listesi — Alfa Prototip

> Sırayla çalıştırılacak, birbirinden bağımsız doğrulanabilir görevler. Her görevin altında
> **kopyala-yapıştır hazır prompt** var; Composer gibi hızlı modellerle çalışacak şekilde
> yazıldı: dar kapsam, net dosya yolları, sayılabilir kabul kriteri, açık yasaklar.
>
> Kaynaklar: [Element Sistemi](element-sistemi.md) · `element-sistemi.json`

> **16 Eylül 2026:** T0-T14 (beşgen prototip) görev prompt'ları buradan silindi — hepsi
> tamamlandı, git geçmişinde duruyor, güncel altıgen/element sistemiyle işleri kalmadı.
> Aşağıdaki tek güncel bölüm **Backlog**. Yeni görevler
> [Element Sistemi](element-sistemi.md) + `element-sistemi.json`'a referans vermeli.

---

## Nasıl çalıştırılır

1. Her göreve ayrı ajan, ayrı dal. Görev bitmeden bağımlı olduğu görevi başlatma (her
   görevin başındaki "paralel/sıralı" notuna bak).
2. Ajan testler yeşilse dalı kendi merge eder; PR yalnızca karar gereken bir şey çıktıysa
   açık kalır (AGENTS.md).
3. Ajan bitirdiğinde tek soruyu sor: **"kabul kriterlerinden hangisini doğrulayamadın?"**
4. Prompt'a ek bilgi yapıştırmana gerek yok — "ÖNCE OKU" satırı ajanın bağlamı, silme.

## Backlog — Element sistemi motor tam uyumu (v5.3, 16 Eylül 4. tur)

**Durum:** `SkillMotor` şu an `verbs`/`adjectives`/`elements`/`scaling_economy.lengths`/
`active_modes`'u okuyor ve dünyaya işliyor (ulti sistemi uçtan uca çalışıyor). Geri kalan
JSON kök bölümleri (`formulas`, `crit_system`, `global_rules`, `state_machine`, `passives`,
`chain_mechanics`, `manipulation_layers`, `status_interaction_table`'ın canlı okunması,
verb'lerin `crit_eligible`/`element_origin`/`engine_base_stats.damage_type` alanları) ya hiç
okunmuyor ya da (durum tablosu gibi) elle senkronlanmış bir C# kopyası olarak yaşıyor.
Detaylı döküm: `docs/durum.md` → "Bilinen açıklar" ikinci maddesi.

**Sırala:** Görev 0 **önce** ve **tek başına** biter (hepsi `SkillMotor.cs`'e dokunuyor,
paralel çalışırsa çakışır). Görev 0 bitince 1-7 **paralel** verilebilir — her biri kendi YENİ
dosyasını yazar, `SkillMotor.cs`'e bir daha dokunmaz, sadece `Skills.XxxYyy` gibi zaten
genişletilmiş bir property'yi okur.

---

### Görev 0 — SkillMotor'a eksik tüm parse'ları ekle (sıralı, tek ajan)

```
Rolün: element-sistemi.json'ı okuyan Core motor geliştiricisi.

ÖNCE OKU: docs/durum.md üstündeki "master ayrışması" notu (context için) + docs/durum.md
"Bilinen açıklar" 2. madde + unity/Assets/Scripts/Core/Grammar/SkillMotor.cs (TAMAMI —
ActiveModeNode + ParseActiveModes'u ÖRNEK AL, aynı MiniJson deseniyle yaz) +
docs/element-sistemi.json şu alanlar: verbs[].crit_eligible / element_origin /
engine_base_stats.damage_type, passives, chain_mechanics, status_interaction_table,
manipulation_layers.zone_layer.

GÖREV
1. VerbNode'a CritEligible (bool), ElementOrigin (string), DamageType (string) alanlarını
   ekle; ParseVerbs'te doldur; SkillResolution'a da taşı (ActiveModeNode'daki gibi ctor'a
   opsiyonel parametre ekle, mevcut çağrıları KIRMA).
2. Yeni PassiveNode struct'ı (Id, Element, TriggerCombo int[], DurationSec, Effects
   JsonValue + GetEffect/GetEffectBool) + ParsePassives(root, dst) — passives.list'i okur.
   SkillMotor'a `IReadOnlyList<PassiveNode> Passives` property'si ekle.
3. Yeni ChainNode struct'ı (Element, Pattern, Effect, Links float[], Finisher) +
   ParseChains(root, dst) — chain_mechanics.chains'i okur. `IReadOnlyList<ChainNode> Chains`.
4. Yeni StatusInteractionNode struct'ı (A, B, Name, Effect, ReadAs) + ParseStatusInteractions
   — status_interaction_table'ın TÜM kategorilerini (debuff_debuff, cc_debuff, cc_cc,
   buff_debuff — gerçek anahtar adlarını JSON'dan oku, uydurma) düz bir listeye toplar.
   `IReadOnlyList<StatusInteractionNode> StatusInteractions`.
5. Yeni ZoneNode struct'ı (Id, Element, Movement, DurationSec, opsiyonel Manipulation
   JsonValue) + ParseZones — manipulation_layers.zone_layer.zones'u okur.
   `IReadOnlyList<ZoneNode> Zones`, `int MaxActiveZones` (max_active_zones).

KABUL KRİTERLERİ
- `dotnet test` yeşil (mevcut 155 test kırılmadı)
- Yeni bir test: `motor.Passives.Count`, `.Chains.Count`, `.StatusInteractions.Count`,
  `.Zones.Count` gerçek JSON'daki sayılarla birebir eşleşiyor (uydurma sayı yazma, JSON'u
  say)
- Mevcut hiçbir public API imzası kırılmadı (ManifestationDirector vb. derlenmeye devam eder)

YASAKLAR
- Yeni bir JSON parser yazma — MiniJson zaten var, onu kullan
- Bu alanları herhangi bir yere UYGULAMA (director yazma) — bu görev sadece OKUMA katmanı,
  uygulama Görev 1-7'nin işi
- docs/element-sistemi.json'ı değiştirme

Bitirince dotnet test çıktısını özetle, hangi kabul kriterini doğrulayamadığını söyle.
```

---

### Görev 1 — DamageCalculator + crit sistemi (paralel, Görev 0 sonrası)

```
Rolün: hasar formülü yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json (formulas, crit_system, global_rules.weakness) +
unity/Assets/Scripts/Core/Combat/ClosingDamageMath.cs (MEVCUT hasar yolu — SİLME,
DamageCalculator ONUN YERİNE geçmiyor, PARALEL yazılıyor) + SkillMotor.cs'teki yeni
CritEligible/ElementOrigin/DamageType alanları (Görev 0).

GÖREV
1. Core/Combat/DamageCalculator.cs: formulas.damage'ı BİREBİR uygula:
   base_damage_value × adjective.damage_mult × length.damage_mult × (1-resistance) ×
   weakness_bonus, ardından crit_system (base_crit_chance, adjective_crit_bonus[adj.id],
   max_crit_chance ile sınırlı) × crit_multiplier. Deterministik test için Random ctor'da
   seed alsın.
2. `docs/durum.md`'ye net bir not düş: bu hesap ŞU AN hiçbir yerden çağrılmıyor
   (ManifestationDirector hâlâ ClosingDamageMath kullanıyor) — ikisini nasıl birleştireceği
   ayrı bir karar (sahibine sorulacak), bu görevin kapsamı değil.

KABUL KRİTERLERİ
- dotnet test yeşil, yeni testler: crit_system'deki 3 sıfat bonusunun (keskinlik,
  saflastirma, berraklik id'lerini JSON'dan oku) doğru toplandığını, max_crit_chance
  tavanının çalıştığını, weakness_bonus>1 durumunda hasarın arttığını kanıtlar.

YASAKLAR
- ClosingDamageMath.cs'e veya ManifestationDirector.cs'e dokunma
- SkillMotor.cs'e dokunma
```

---

### Görev 2 — ResourceTracker + CooldownTracker (paralel, Görev 0 sonrası)

```
Rolün: kaynak/soğuma sistemi yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json global_rules (resource_system, cooldown_rules,
status_durations, player_stats, boss_stats) + unity/Assets/Scripts/Core/Tuning/StatusTuning.cs
+ unity/Assets/Scripts/Game/PlayerVitals.cs (mevcut MaxHp nereden geliyor, KARŞILAŞTIR).

GÖREV
1. Core/Combat/ResourceTracker.cs: max_mana, regen_per_sec, regen_delay_after_cast_sec'i
   uygulayan basit bir havuz (Spend/Tick/CanAfford). Oyunda ŞU AN hiçbir yerde mana yok —
   bu görev sadece sınıfı yazar, PlayerVitals'a bağlamaz (ayrı bir karar, ManifestationDirector
   zaten base_resource_cost'u hiç düşmüyor, aynı gerekçe).
2. Core/Combat/CooldownTracker.cs: global_cooldown_sec + max_concurrent_casts'i uygulayan
   basit bir sınıf (verb id → soğuma bitiş zamanı).
3. StatusTuning.cs'teki sabit süre/magnitude değerlerini (StunMs, RootMs, vb.)
   global_rules.status_durations'daki karşılığıyla KARŞILAŞTIR — aynıysa dokunma, farklıysa
   docs/durum.md'ye fark listesi yaz (hangisi otorite olacağına sahibi karar verir, kendin
   değiştirme).

KABUL KRİTERLERİ
- dotnet test yeşil, ResourceTracker/CooldownTracker için ayrı testler
- docs/durum.md'de StatusTuning karşılaştırma tablosu (fark varsa)

YASAKLAR
- StatusTuning.cs'in sayılarını DEĞİŞTİRME, sadece karşılaştır ve raporla
- PlayerVitals.cs'e veya ManifestationDirector.cs'e bağlama
```

---

### Görev 3 — PlayerStateMachine (paralel, Görev 0 sonrası)

```
Rolün: durum makinesi yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json state_machine (player_states, boss_states) +
unity/Assets/Scripts/Core/Grammar/SentenceState.cs + SentenceEngine.cs (mevcut
SentencePhase enum'u state_machine'in player_states'iyle KARŞILAŞTIR, birebir örtüşmüyor).

GÖREV
1. Core/Combat/PlayerStateMachine.cs: state_machine.player_states'teki her state için
   (idle/drawing/casting/recovering/dodging/stunned/rooted/channeling/dead) can_draw/
   can_move/can_dodge/i_frames okuyan bir sınıf (MiniJson ile parse — SkillMotor'a
   ParseStateMachine ekle, motor.PlayerStates/BossStates property'si; bu görev SkillMotor'a
   dokunuyor AMA Görev 0'ın alanlarından FARKLI, çakışma riski düşük — yine de Görev 0
   bittikten SONRA başla).
2. SentencePhase ile state_machine.player_states arasındaki farkı docs/durum.md'ye yaz
   (ör. "drawing" state'i can_draw="partial" diyor, SentencePhase.Building'de tam açık).

KABUL KRİTERLERİ
- dotnet test yeşil, her state için can_draw/can_move/can_dodge/i_frames doğru okunuyor
- SentencePhase'e bağlama YAPILMADI (bu ayrı bir karar) — sadece yeni sınıf + fark raporu

YASAKLAR
- SentenceEngine.cs / SentencePhase enum'una dokunma
```

---

### Görev 4 — PassiveDirector (paralel, Görev 0 sonrası, Görev 0'ın Passives'ine bağımlı)

```
Rolün: pasif tetikleme sistemi yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json passives + unity/Assets/Scripts/Core/Combat/
ActiveModeDirector.cs (AYNI DESENİ TAKİP ET — trigger_combo eşleşmesi, cooldown yok ama
duration_sec var, effects JsonValue).

GÖREV
Core/Combat/PassiveDirector.cs: motor.Passives listesini alır, TryTrigger(dot dizisi,
worldMs) ile trigger_combo eşleşen pasifi bulur, DurationSec kadar aktif tutar (birden
fazla pasif AYNI ANDA aktif olabilir — ActiveModeDirector'dan farkı bu, tek mod değil liste).

KABUL KRİTERLERİ
- dotnet test yeşil: 12 pasifin hepsi doğru trigger_combo ile eşleşiyor, süresi dolunca
  düşüyor, iki pasif aynı anda aktif olabiliyor.

YASAKLAR
- ManifestationDirector.cs'e bağlama (bu görev sadece Core sınıfı)
- SkillMotor.cs'e dokunma (Passives zaten Görev 0'da geldi)
```

---

### Görev 5 — ChainDirector (paralel, Görev 0 sonrası, Görev 0'ın Chains'ine bağımlı)

```
Rolün: zincir mekaniği yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json chain_mechanics.

GÖREV
Core/Combat/ChainDirector.cs: motor.Chains listesini alır, son N cast'in pattern'iyle
(chain_mechanics'teki "pattern" alanının formatını JSON'dan çöz, uydurma) eşleşeni bulur,
Links dizisindeki artan bonusu ve son eşleşmede Finisher'ı tetikler.

KABUL KRİTERLERİ
- dotnet test yeşil: 6 zincirin hepsi doğru pattern ile eşleşiyor, Links sırası doğru
  uygulanıyor, Finisher yalnızca tam zincirde tetikleniyor.

YASAKLAR
- Kombo tablosu yazma — pattern eşleştirme JSON'daki veriden türer, elle dizi yazılmaz
- ManifestationDirector.cs'e bağlama
```

---

### Görev 6 — StatusReactionTable'ı JSON'dan canlı oku (paralel, Görev 0 sonrası)

```
Rolün: durum etkileşim tablosu geliştiricisi.

ÖNCE OKU: unity/Assets/Scripts/Core/Status/StatusReactionTable.cs (MEVCUT — elle
kopyalanmış 14 kural) + docs/element-sistemi.json status_interaction_table + Görev 0'ın
motor.StatusInteractions'ı.

GÖREV
StatusReactionTable.cs'in statik elle-yazılmış listesini, motor.StatusInteractions'tan
türeyen bir listeye çevir (TryGetRule aynı imzada kalsın, StatusApplicator/StatusBoard
DEĞİŞMESİN). "a"/"b" alanlarındaki durum id'lerini StatusKindUtil.TryParse ile StatusKind'a
çevir; eşleşmeyen id varsa (yeni bir mechanic) docs/durum.md'ye yaz, sessizce atlama.

KABUL KRİTERLERİ
- dotnet test yeşil, TÜM StatusBoardTests kırılmadan geçiyor (mevcut testler JSON'dan
  gelen kurallarla da aynı sonucu vermeli — bu regresyon testidir)
- JSON'daki kural sayısı ile TryGetRule'un tanıdığı kural sayısı test edilir

YASAKLAR
- StatusBoard.cs / StatusApplicator.cs'e dokunma (imza aynı kalıyor, içerik kaynağı değişiyor)
- Yeni bir mekanik/kural İCAT ETME — sadece JSON'da olanı oku
```

---

### Görev 7 — ZoneDirector (paralel, Görev 0 sonrası, Görev 0'ın Zones'una bağımlı)

```
Rolün: kalıcı alan (zone) sistemi yazan geliştirici.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.zone_layer + docs/durum.md
"Toprak kalıcı alan" sorusunun cevabı (bu sohbette sorulmuştu) + unity/Assets/Scripts/Core/
Combat/StateBridgeBoard.cs (BENZER bir "dünyada yaşayan obje" deseni — örnek al).

GÖREV
1. Core/Layers/IZoneDirector.cs arayüzü (ZoneInstance struct: Id, Element, X, Y, Z,
   RadiusM, RemainingSec, Movement) + Core/Combat/ZoneDirector.cs (saf C#, TrySpawn/Tick/
   Remove/MoveZone, max_active_zones sınırı).
2. Bu SADECE Core mantığı — Unity'de görsel spawn ETMEZ (o ayrı bir görev, Game katmanı).

KABUL KRİTERLERİ
- dotnet test yeşil: max_active_zones dolunca en eskisi düşüyor, RemainingSec sıfırlanınca
  zone kayboluyor, "player_directed"/"follow_target"/"static" movement tipleri ayrı ayrı
  test edilir (davranış farkı en azından bir konum-güncelleme metodunda var olmalı).

YASAKLAR
- unity/Assets/Scripts/Game/ klasörüne dosya ekleme (bu görev saf Core)
- ManifestationDirector.cs'e bağlama
```

---

---

### Görev 8 — SpaceDirector: mevcut hardcoded geçişleri space_layer'a bağla (paralel, Görev 0 sonrası)

```
Rolün: uzay/geçiş mekaniği geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.space_layer + unity/Assets/Scripts/
Core/Combat/SkillMotionMotor.cs (MEVCUT — Hava+Su Zenitsu kesişi, Hava+Su+Toprak işaret
mekaniği ŞU AN ELLE rün kontrolüyle yazılı, space_layer JSON'undan HİÇ okumuyor) +
StateBridgeBoard.cs.

GÖREV
1. SkillMotor'a space_layer.effects'i okuyan SpaceEffectNode + ParseSpaceEffects ekle
   (Id, Element, Type, DistanceM, IFrameMs, DamageOnPass bool, DamageOnCross float,
   DurationSec — alanlar effect tipine göre değişiyor, JsonValue ile opsiyonel oku).
2. SkillMotionMotor.cs'teki mevcut sabit sayıları (distance_m, i_frame_ms vb.) motor.
   SpaceEffects'ten gelen değerlerle KARŞILAŞTIR; aynıysa dokunma, farklıysa
   docs/durum.md'ye yaz (hangisi otorite sahibi karar verir).
3. "Karabasan"/"Hiçlik" gibi henüz karşılığı olmayan space effect'leri (invisible_link,
   tear) için YENİ mekanik YAZMA — sadece motor.SpaceEffects listesinde veri olarak
   dursunlar, docs/durum.md "Bilinen açıklar"a "şu N effect JSON'da var, oyunda karşılığı
   yok" diye ekle.

KABUL KRİTERLERİ
- dotnet test yeşil, motor.SpaceEffects.Count JSON'daki sayıyla eşleşiyor
- SkillMotionMotor'un MEVCUT davranışı (Zenitsu/işaret) hiç değişmedi (regresyon yok)

YASAKLAR
- SkillMotionMotor.cs'in mevcut çalışan mantığını SİLME/yeniden yazma — sadece karşılaştır
- StateBridgeBoard.cs'e dokunma
```

---

### Görev 9 — TimeDirector: gecikme/yankı/kalıcılık (paralel, Görev 0 sonrası)

```
Rolün: zaman katmanı geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.time_layer.

GÖREV
Core/Combat/TimeEffectDirector.cs (saf C#, mevcut Core/Time/ klasöründeki yavaş-çekim
yönetmeninden AYRI — o dünya saatini yönetiyor, bu belirli bir cast'in gecikmeli/yankılı
davranışını): time_layer.effects'teki 4 tipi uygula:
- delayed_detonation (Karabasan): cast X saniye sonra patlar
- echo (Alev): cast'ten damage_ratio kadar bir "yankı" hasarı delay_sec sonra tekrar gelir
- extend_lifetime (Lav): bir zone/effect'in RemainingSec'i multiplier ile çarpılır
- death_delay (Cehennem): ölüm anı delay_sec ertelenir (PlayerVitals/BossVitals'a
  BAĞLAMA — bu görev sadece "ne zaman tetikleneceğini" hesaplayan saf mantık, gerçek
  ölüm/patlama uygulaması Faz 6 "Bağlama" turunun işi)

KABUL KRİTERLERİ
- dotnet test yeşil: 4 tip için de zamanlama hesabı (worldMs bazlı) doğru test edilir.

YASAKLAR
- PlayerVitals.cs / BossVitals.cs / Core/Time/*.cs'e dokunma
```

---

### Görev 10 — RealityDirector: dirilme engeli + silme (paralel, Görev 0 sonrası)

```
Rolün: gerçeklik katmanı geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.reality_layer.

GÖREV
Core/Combat/RealityEffectDirector.cs: revive_block (Cehennem, dirilmeyi N sn engeller —
PlayerVitals'ın respawn akışına BAĞLAMA, sadece "engelli mi" bayrağını hesapla),
partial_erase (Karabasan: shield/haste/damage_reduction hedeflerini siler — bu targets
listesi zaten StatusKind'ta var, StatusBoard üzerinde çalışacak bir yardımcı yaz AMA
StatusBoard.cs'e DOKUNMA, dışarıdan Board.CleanseHostile benzeri yeni bir public metod
gerekiyorsa StatusBoard'a EKLE sadece, mevcut metodu değiştirme), full_erase (Hiçlik:
targets = minions/summons/shields — **oyunda minion/summon sistemi YOK**, bu görevin
kapsamı yalnızca "shields" kısmını uygular, diğer ikisini docs/durum.md'ye "sistem yok,
uygulanamaz" diye açık olarak yazar; UYDURMA minion sistemi kurma).

KABUL KRİTERLERİ
- dotnet test yeşil: partial_erase şeffaf/haste/damage_reduction'ı doğru temizliyor,
  full_erase shield'ı temizliyor, revive_block bayrağı süresi doğru hesaplanıyor.

YASAKLAR
- Minion/summon sistemi icat etme
- PlayerVitals.cs'in respawn akışına bağlama (Faz 6 işi)
```

---

### Görev 11 — Equipment sistemi: veri modeli + element eşleşme bonusu (paralel, Görev 0 sonrası)

```
Rolün: ekipman veri modeli geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json equipment_system (TAMAMI — küçük bölüm).

GÖREV
1. Core/Equipment/EquipmentSlot.cs (enum: Weapon, Armor, Accessory) +
   Core/Equipment/EquipmentItem.cs (Id, Name, Slot, Element) — equipment_system.examples'
   taki 6×3=18 örneği SkillMotor deseniyle JSON'dan okuyan bir parser (yeni
   Core/Equipment/EquipmentCatalog.cs, MiniJson kullan).
2. Core/Equipment/EquipmentBonusResolver.cs: element_match_bonus ("+%10 etki") — silahın
   Element'i cast edilen skill'in element'iyle eşleşirse %10 bonus döner (JSON'daki
   "+%10" metninden yüzdeyi türet, SkillMotor.ParseDefenseDropMult'taki sayı-çıkarma
   desenini örnek al, elle "10" yazma).

KABUL KRİTERLERİ
- dotnet test yeşil: 18 örnek item doğru parse ediliyor, eşleşen/eşleşmeyen element için
  bonus doğru (1.1 / 1.0) dönüyor.

YASAKLAR
- Envanter UI'ı yazma (oyuncunun ekipman SEÇMESİ ayrı, çok daha büyük bir görev — bu görev
  sadece veri + hesap katmanı; oyuncunun şu an sabit/tek bir ekipmanı olduğunu VARSAY)
- PlayerVitals.cs veya ManifestationDirector.cs'e bağlama
```

---

### Görev 12 — UI Rules hizalaması (paralel, Görev 0 sonrası, Game katmanı)

```
Rolün: HUD geliştiricisi (bu görev Core değil Game — Unity gerekli).

ÖNCE OKU: docs/element-sistemi.json ui_rules + unity/Assets/Scripts/Game/PentagonView.cs
+ ReactionReadout.cs + Core/Tuning/FeelTuning.cs (ReadoutHoldMs — read_as_display.
duration_ms=1500 ile KARŞILAŞTIR).

GÖREV
1. `read_as_display.duration_ms` (1500) ile `FeelTuning.ReadoutHoldMs`'in mevcut değerini
   karşılaştır; farklıysa docs/durum.md'ye yaz (değiştirme, sahibi karar versin).
2. `cooldown_display` (radial_overlay, her rünün etrafında, sayı göster): PentagonView'daki
   her nokta için `base_cooldown_sec` dolana kadar dairesel bir dolum efekti + kalan saniye
   sayısı çiz. Şu an hiçbir cooldown UI'da görünmüyor — bu görev sadece GÖRSEL, cooldown'un
   GERÇEKTEN engellemesi Görev 2'nin (CooldownTracker) Faz 6'da bağlanmasına bağımlı; bağlı
   değilken bile dolum animasyonu kozmetik olarak (her cast'te base_cooldown_sec kadar
   dolar, hiçbir şeyi engellemez) eklenebilir.
3. `zone_display` (in_world, transparency 0.6): Görev 7 (ZoneDirector) henüz Game'e
   bağlanmadığı için bu maddeyi sadece docs/durum.md'ye not düş, kod yazma.

KABUL KRİTERLERİ
- Unity Play mode'da her rün noktasının etrafında dairesel cooldown dolumu görünüyor
  (Unity MCP ile canlı doğrula)
- dotnet test yeşil (bu görev Game katmanı olsa da Core testleri kırılmamalı)

YASAKLAR
- CooldownTracker'ı gerçekten cast'i ENGELLEYECEK şekilde bağlama (Faz 6 işi)
- ZoneDirector'ı Unity'ye bağlama (Görev 7 henüz Core'da, Game'e geçmedi)
```

---

**Not (auto mode kullanan ajanlara):** Görev 0 bitip merge olmadan 1-12'yi başlatma — hepsi
`Skills.Passives`/`Skills.Chains`/`Skills.StatusInteractions`/`Skills.Zones`/
`Skills.SpaceEffects`/yeni Verb alanlarını okuyor, onlar yoksa derlenmezler. Her görev kendi
dalında, `dotnet test` yeşilse kendi merge eder (AGENTS.md). **Hiçbiri henüz gerçek oynanışa
BAĞLANMIYOR** — bu bilinçli: 13 görevin bağlama sırası birbirine karışırsa (ör. CooldownTracker
cast'i engellemeye başlarken ResourceTracker henüz yokken oyun kilitlenebilir) hata ayıklaması
imkânsızlaşır. Hepsi (Görev 0-12) bitip birleşince **"Faz 6 — Bağlama"** turu yazılacak: her
birini tek tek, sırayla, `ManifestationDirector`/`PrototypeBootstrap`'a gerçekten bağlayıp
Unity Play mode'da canlı doğrulayan görevler. O zaman JSON'un `equipment_system`/`ui_rules`/
`manipulation_layers`'ının TAMAMI da dahil olmak üzere gerçekten oyunda çalışıyor olacak —
sahibinin talebi budur, "motor değil ama JSON'da var, atlanır" diye bir kategori artık yok.
