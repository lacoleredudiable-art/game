# Element Sistemi — Spesifikasyon (Altıgen)

> **16 Eylül 2026'da yazıldı.** Eski dokümanlar (`dovus-sistemi.md`, `tasarim-ozeti.md`,
> `teknoloji-kararlari.md`, `his-kontrol-listesi.md`, `t0-kurulum.md`, `alis-sepeti.md`,
> `animasyon-omurgasi.md`) **silindi** — hepsi beşgen/3-rün alfa prototipine aitti, sistem
> altıgen/6-elemente geçtiğinden beri güncel değildi ve kafa karıştırıyordu. Git geçmişinde
> hâlâ duruyorlar, gerekirse kazılabilir; ama bağlayıcı doküman artık **bu dosya + veri
> kaynağı `docs/element-sistemi.json`**.
>
> **İş bölümü:** sayılar/mekanik JSON'da yaşar, motor (`SkillMotor.cs`/`MiniJson.cs`) onu
> okur. Bu MD dosyası "ne var, neden böyle, ne eksik" anlatır — kendi başına veri kaynağı
> değildir. Bir sayı/mekanik değiştirmek istiyorsan JSON'u değiştir (+ `Resources` kopyasını
> senkron tut), burayı değil.

---

## 1. Tek paragrafta sistem

Sol başparmak karakteri yürütür. Sağ başparmak, **altıgen dizilmiş 6 nokta** üzerinde
sürüklenerek cümle kurar: ilk dokunulan nokta **fiil** (ne yapıyorum), sonrakiler **sıfat**
(nasıl yapıyorum). Altı nokta altı elemente karşılık gelir — Ateş / Su / Hava / Toprak /
Aydınlık / Karanlık (`docs/element-sistemi.json` çekirdek id 1-6). Cümle çizilirken sonuç
zaten dünyada olur. Merkeze kısa dokunma düz vuruş, dodge ekrana sabit ayrı düğme. Bu girdi
katmanının detayları (mesafe/kısa-sıçrama, dwell, iptal pencereleri) eski `dovus-sistemi.md`
§2-3'te yazılıydı ve **hâlâ geçerli** — sadece nokta sayısı 5'ten 6'ya çıktı, mekanik aynı.

## 2. Girdi — kod durumu ve bilinen isim borcu

Kontrol mantığı zaten 6 noktaya geçmiş:

- `Core/Grammar/PentagonLayout.cs`: `DotCount = 6`, komşuluk `±1 mod 6` (altıgen).
- `Core/Grammar/Rune.cs`: `Rune` enum 1-6, `RuneInfo.DisplayName` doğru eşliyor
  (1=Ateş … 6=Karanlık).

**Ama isimlendirme borcu var** — kod çalışıyor, isimler yanıltıcı:

- Dosya/sınıf adları hâlâ `Pentagon*`: `PentagonLayout.cs`, `PentagonInput.cs`,
  `PentagonView.cs`, `PentagonLayoutScreen.cs`. Altıgen mantığı bu dosyaların içinde ama adları
  beşgen diyor.
- `Rune` enum'unun **üye adları** eski 5-rün isimlerini taşıyor ve artık yanlış elementi
  karşılıyor: `Rune.Zehir = 4` ama pozisyon 4 = **Toprak**; `Rune.Toprak = 6` ama pozisyon 6 =
  **Karanlık**. Kod `RuneInfo.DisplayName()` ile doğru string'i döndürüyor, yani **çalışıyor**,
  ama enum'u okuyan biri "Toprak" görüp "Karanlık" olduğunu bilemez.

Bu bir davranış hatası değil, isimlendirme hatası — düzeltmesi mekanik (rename + kullanım
yerlerini güncelle) ama henüz yapılmadı. `docs/durum.md` "Bilinen açıklar"da da kayıtlı.

## 3. Gramer (değişmeden taşındı)

Üç kural, hepsi karşılık — ezberlenmez, türetilir:

- **K1 — Konum rolü belirler.** İlk dokunulan nokta fiil, sonrakiler sıfat. `5-1` ile `1-5`
  aynı şey değildir.
- **K2 — Mesafe büyüklüğü belirler.** Komşu noktaya kısa sıçrama hafif/hızlı, karşıya uzun
  sıçrama ağır/yavaş bir sıfattır.
- **K3 — Tekrar yoğunlaştırır.** Noktada bekleme (`dwellMs`) veya noktaya geri dönme (bir
  sıfat yuvası harcar) — ikisi de zaman öder, karıştırılmaz.

**Cümle = 1 fiil + en fazla 3 sıfat** (`maxSentenceDots = 4`). Katlama (fold) kuralı —
`SkillMotor.Resolve()` bunu uygular:

| Uzunluk | Çözümleme |
|---|---|
| 1 | Kök skill (fiil+sıfat, o elementin kendi kombinasyonu) |
| 2 | Bileşik elementin `skill_name` kartı (36 bileşikten biri) |
| 3 | (1-2 bileşiği) + 3. kökün sıfatı — 2'li skill kartı **taşınmaz**, yeni isim `Bileşik · Sıfat` |
| 4 | (1-2 bileşiği) + (3-4 bileşiği) — sol fiil, sağ sıfat; `Bileşik1+Bileşik2` |

Uzunluk ekonomisi (`scaling_economy.lengths`, JSON'da):

| Uzunluk | Rol | cast_time_mult | resource_cost_mult | mobility |
|---|---|---|---|---|
| 2 | Temel | 1.0 | 1.0 | free_move |
| 3 | Durumsal | 1.4 | 1.5 | slowed_move |
| 4 | Dar Cevap | 2.0 | 2.2 | rooted |

**Anti-ladder kuralı değişmedi:** uzunluk hasar/poise çarpmaz (`damage_mult`/`poise_damage_mult`
her uzunlukta 1.0) — uzunluk güç değil durumsal araçtır (cast_time + kaynak + mobilite cezası).

## 4. Altı Element Ailesi

`docs/element-sistemi.json` → `element_families` (Ateş hariç; kaynak onu vermedi, aşağıda not).

| Aile | Class | Kimlik | His |
|---|---|---|---|
| Ateş | *(veri yok)* | — | — |
| Su | Support | Şifa, akış, kabullenme | Sakinleştirici, iyileştirici, arındırıcı |
| Hava | DPS | Özgürlük, hız, kaçış | Hızlı, kaotik, mobil |
| Toprak | Tank | Dayanıklılık, sabır, sınır | Ağır, koruyucu, sabit |
| Aydınlık | Support | Arınma, hakikat, düzen | Parlak, arındırıcı, ifşa edici |
| Karanlık | Tank | Gizlilik, belirsizlik, sır | Sinsi, emici, gizleyen |

Her aile **7 fiil** taşır (1 kök + 6 bileşik) ve **7 sıfat** (1 kök + 6 bileşik) — toplam
6×7 = **42 fiil, 42 sıfat, 42 element** (6 kök + 36 bileşik). Tam liste `element-sistemi.json`
→ `verbs` / `adjectives` / `elements`; burada tekrarlanmıyor (tek doğruluk kaynağı JSON'da
kalsın diye).

**Class dağılımı Sütun 2'nin (5 kişinin uyumu) veri temeli** — ama şu an sadece veri: oyuncunun
hangi elementi/aileyi oynadığını seçtiği bir sistem yok, tek karakter var. Bkz. §7.

## 5. Fiil alanları (motor artık hepsini okuyor)

Her fiilde: `animation_type`, `target_mode`, `base_cooldown_sec`, `engine_base_stats`
(`action`, `base_damage_value`, `base_poise_damage`, `base_hitbox`, `cast_mobility`),
`mechanics[]`. 16 Eylül'den beri ayrıca (35/42 fiilde):

- `base_resource_cost` — kaynak/mana bedeli
- `special` — fiile özel serbest-form veri (`heal_value`, `shield_amount`, `dash_distance_m`…)
- `zone_effect` — yerde kalan alan (varsa): `type`, `duration_sec`, `effects{}`
- `target_behaviors` — `selective` fiillerde self/enemy/ally davranış metni

`SkillResolution` bunların hepsini taşıyor (`SkillMotor.cs`), ama **hiçbir gameplay sistemi
henüz tüketmiyor** — mana harcanmıyor, cooldown zorlanmıyor, zone spawn olmuyor. Bkz. §8.

## 6. Sıfat alanları

`engine_modifiers` artık üç sabit alanla sınırlı değil (`damage_mult`, `hitbox_scale_mult`,
`poise_damage_mult`) — 20'den fazla farklı alan kullanılıyor (`trajectory_override`,
`apply_slow`, `cooldown_mult`, `max_targets`, `hitbox_override`, `lifetime_add`…). Motor
şimdi tamamını genel bir sözlük olarak okuyor (`AdjectiveNode.EngineModifiers`,
`SkillResolution.EngineModifiers["alan_adı"]`).

**Sıfat vergisi kuralı (değişmeden geçerli):** sıfat yeni işlev/şekil ekliyorsa
`damage_mult` **her zaman ≤ 0.9** — taban hasarı 1.0 üstüne çıkaran sıfat yok. Sıfat silüeti
değiştirir, sayıyı değil (AGENTS.md kural 5).

## 7. Mekanikler / Status

`element-sistemi.json` → `mechanics`: `hard_cc` (stun, root, silence, knockback),
`soft_cc` (slow, blind, disarm, taunt), `debuff` (burn, armor_break, grievous_wounds,
weaken, **poison**), `buff` (shield, haste, cleanse, damage_reduction, regen),
`special` (stasis, fear).

`Core/Status/StatusKind.cs` bunların hepsini bir enum + `StatusBoard` (süre/magnitude
bookkeeping, kalkan emme, hard/soft CC blokları) ile karşılıyor — **`poison` hariç**, o
JSON'da var ama enum'da karşılığı yok (bilinen açık).

**Durum etkileşim tablosu (zehir+ateş=patlama, ıslak+yıldırım=zincir gibi çapraz kurallar)
hiç yok** — ne kodda ne JSON'da. Eski `tasarim-ozeti.md` §5 bunu "asıl sihir" diye
işaretlemişti; hâlâ en büyük eksik. Bkz. §8.

## 8. Motor durumu — okunan / okunmayan

**Okunuyor (`SkillMotor.cs`, 16 Eylül genişlemesinden sonra):** `elements`, `verbs`
(`animation_type`/`target_mode`/`base_cooldown_sec`/`base_resource_cost`/`target_behaviors`/
`special`/`zone_effect`/`mechanics`/`engine_base_stats`), `adjectives`
(`engine_modifiers` tamamı), `scaling_economy.lengths`.

**Bilerek okunmuyor:**
- `atoms_catalog` / `all_verbs_atoms` / `atom_kombinasyonlari` — motor değil, VFX/animasyon
  ekibi için referans (JSON'un kendi tanımı öyle: *"motor okumaz"*).
- `three_runes_examples` — 6 aile için örnek 3'lü kombo hasar/heal değerleri; gerçek bir
  `DamageCalculator` yokken referans/tasarım örneği.

**Hiç yok (JSON'da bile planlanmış ama boş):** `formulas`, `global_rules`, `crit_system` —
`element-sistemi.json` → `motor_parse_extension.new_root_sections` bunları listeliyor ama
içerikleri yok. Bunlar gelmeden gerçek hasar formülü / kritik sistemi yazılamaz (sayı
uydurmak olur).

## 9. Ne tüketiyor, ne tüketmiyor (Play mode'da fark görmek için)

Kısa cevap: **hiçbir yeni sistem henüz JSON'un yeni alanlarını tüketmiyor.** Sırayla
gerekenler (`docs/gorev-listesi.md` "Backlog"):

1. `ResourceTracker` — `base_resource_cost` harcansın, yetmezse skill iptal
2. `CooldownTracker` — `base_cooldown_sec` dolmadan fiil tekrar kullanılamasın
3. `DamageCalculator` — `formulas`/`global_rules` gelince gerçek hasar formülü
4. `StatusApplicator` / durum etkileşim tablosu (§7)
5. `ZoneDirector` — `zone_effect`'i olan fiiller yerde gerçekten alan bıraksın
6. Diğerleri (`CompositionResolver`, `ValidationRules`, `ModeDirector`, `PassiveDirector`,
   `SpaceDirector`/`TimeDirector`/`RealityDirector`, `ChainDirector`)

## 10. Bilinen açıklar (öncelik sırasına yakın)

1. **~~Pentagon→Hexagon isim borcu~~ (§2) — hâlâ açık.** Kod doğru çalışıyor
   (`PentagonLayout.DotCount=6`), ama dosya/sınıf adları (`PentagonInput.cs` vb.) ve `Rune`
   enum üyeleri (`Rune.Toprak=6=Karanlık`) hâlâ yanıltıcı — henüz yeniden adlandırılmadı.
2. **Element/sınıf seçimi yok** — tek karakter, oyuncu hangi aileyi oynadığını seçemiyor.
3. **~~Durum etkileşim tablosu yok~~ — 16 Eylül'de kapandı.** `status_interaction_table`
   (17 kural) `Core/Status/StatusReactionTable.cs` + `StatusBoard.Apply/Tick` +
   `StatusApplicator` içinde uygulanıyor (8 test). JSON'a da belge olarak eklendi ama motor
   onu okumuyor — sayılar C#'ta sabit (StatusTuning deseniyle aynı).
4. **Co-op / network hiç yok** — `AllyDummy` yapay kukla, gerçek 2. oyuncu/senkron yok.
5. **`base_resource_cost` Ateş ailesinin 7 fiilinde yok** — v5.2 kaynağı Ateş'i vermedi,
   uydurulmadı.
6. **2-5 (Zehir/`aktif_zehirlenme`) ve 3-2 (Pus/`kisisel_isinlanma`) kilitli tutuldu** —
   sohbetten gelen v5.2 verisi bunları İksir/`tam_arinma` ve hız+görünmezliğe geri almak
   istiyordu ama JSON'da zaten kasıtlı dönüşüm notu vardı; sahibi kararı kilitli hali korudu.
7. **~~`StatusKind` enum'ında `poison` yok~~ — 16 Eylül'de eklendi** (`Core/Status/StatusKind.cs`).
8. **Boss çeşitliliği düşük ama arttı (16 Eylül).** İkinci saldırı eklendi:
   `fire_cone`/Cehennem Nefesi (`BossAttackKind.FireCone`, `docs/bosses/karadul.json`
   `implemented:true` oldu) — boss can %50'nin altına düşünce (Faz 2/Öfke) slam ile
   dönüşümlü seçiliyor, dar bir koni (40°), ayrı windup/hasar/mekanik (burn+grievous_wounds).
   Hâlâ tek boss (Karadul); görsel olarak aynı "bite" animasyonunu tekrar kullanıyor (ayrı
   clip yok — Demon Watcher paketiyle `Combat_Spell_*` eşlenince düzelir).
9. **Dodge/hitstop/kamera "his" numaraları artık sadece kodda** — eski `dovus-sistemi.md`
   §6-9'un anlattığı sayılar (`hitstopPerfectMs`, `impactFrameMs`, dodge derece eşikleri vb.)
   `Core/Tuning/{FeelTuning,DodgeTuning,SlowmoTuning,SentenceTuning}.cs` içinde varsayılan
   olarak yaşıyor — doküman silindi ama sayılar kaybolmadı, sadece gerekçe prose'u gitti.
   Yeni bir "his değeri" gerekirse artık kaynak bu Tuning sınıflarının mevcut değerleri +
   bu dosyadır, `dovus-sistemi.md` değil (AGENTS.md güncellendi).
10. **16 Eylül bug turu kapatıldı** (durum.md'de detay): düz vuruşun heal basması
    (sahnede donmuş `BasicStrikeDot=5` idi), hasar sayısı görünmemesi (`ShowDamageNumbers`
    sahnede donmuş `0`'dı), sol joystick'in görseli olmaması, kamera 360° dönemiyor olması,
    duvarların içine girilebilmesi. Hepsi Play mode'da Unity MCP ile doğrulandı.

## 11. Kaynak dosyalar

| Dosya | Ne için |
|---|---|
| `docs/element-sistemi.json` | Tek veri kaynağı — motor bunu okur, sayı/mekanik burada değişir |
| `docs/element-sistemi.md` | Bu dosya — insan-okunur anlatım, gramer, bilinen açıklar |
| `unity/Assets/Scripts/Core/Grammar/SkillMotor.cs` | JSON → `SkillResolution` çözümleyici |
| `unity/Assets/Scripts/Core/Grammar/MiniJson.cs` | Bağımsız minimal JSON parser (Core saf C#) |
| `unity/Assets/Scripts/Core/Status/StatusKind.cs` + `StatusBoard.cs` | Mekanik/durum uygulaması |
| `docs/durum.md` | Nerede kaldık — ilk buraya bak |
| `docs/gorev-listesi.md` | Görevler ve prompt'lar |
