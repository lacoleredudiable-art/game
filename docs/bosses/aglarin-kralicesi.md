# Ağların Kraliçesi — taslak tasarım (Faz 3.0)

Durum: **ONAYLANDI (2026-10-02).** Karar 1 = A (sıçrayış hareket kalıbıyla), Karar 2 = B (CC0 örümcek modeli), Karar 3 = karadul varsayılan.
Kaynak: `unity/Assets/Resources/Bosses/karadul.json` şeması, `BossAttackKind` / `BossAttack` / `BossAttackControl` / `BossAttackKindPicker`, `BossDirector.SelectNextAttack`.
Sayı kuralı: karadul'dan alınan sayılar **(karadul)**, uydurulmuş varsayılanlar **(spec'te yok)** diye işaretli.

## 1. Kimlik

| Alan | Değer |
|---|---|
| boss_id | `aglarin_kralicesi` |
| Ad / alt başlık | Ağların Kraliçesi / Yuvanın Örücüsü |
| Element | Karanlık, Zehir (spec'te yok) |
| Tema | Dev örümcek; arenayı ağla daraltır, sonra avlanır |

Karadul'dan farkı: karadul yerinde durup vurur (tüm saldırılar `Standing`). Kraliçe **alanı değiştirir** (kalıcı ağ alanları) ve 2. fazda **yer değiştirerek** saldırır (sıçrayış). Oyuncunun öğrendiği şey: "nerede durduğun" ve "ne zaman kaçtığın".

## 2. Fazlar

| Faz | Ad | Can aralığı | Saldırılar |
|---|---|---|---|
| 1 | Örücü | 100 → 50 | `leg_slam`, `web_spit`, `web_field` |
| 2 | Avcı | 50 → 0 | `leg_slam`, `web_spit` (5 mermi), `web_field`, `poison_breath`, `pounce` |

- total_hp 120, player_hp 22, phase_transition_sec 2, stagger_duration_sec 0.4 **(karadul)**.
- Faz geçişinde: mevcut ağ alanları kalır; boss 2 sn kükrer (karadul geçiş süresi).
- Aynı saldırı üst üste en fazla `MaxSameAttackKindStreak` kez (mevcut kural, değişmez).

## 3. Saldırılar

| id | Ad | Tür (BossAttackKind) | Hareket | Windup ms | Hacim | Hasar | Etki |
|---|---|---|---|---|---|---|---|
| `leg_slam` | Bacak Çakması | `Slam` (mevcut) | Standing | 640 / 900 / 640 | daire 5.4 / 5.4 / 8.0 m | 22 | — |
| `web_spit` | Ağ Tükürüğü | `Volley` (mevcut) | Standing | 700 | 3 / 5 mermi, 30°, 7 m/s, r 0.35, 3 sn | 6 | isabette `Slow` 1.5 sn |
| `poison_breath` | Zehir Nefesi | `FireCone` (mevcut) | Standing | 800 | koni (FireCone tuning) | 18 | `Poison` (burn yerine) |
| `web_field` | Ağ Örme | **yeni: `WebField`** | Standing | 900 | hedefin konumunda disk r 3.0 m | 0 | alanda duran: `Slow` |
| `pounce` | Sıçrayış | **yeni: `Pounce`** | **Leap** | 1000 | iniş dairesi r 3.0 m | 14 | iniş noktasında |

Kaynaklar: `leg_slam`, `web_spit` hız/sayı/hasar ve `poison_breath` windup/hasar **(karadul)**. Slow 1.5 sn, ağ ve sıçrayış sayıları **(spec'te yok)**.

### 3.1 Bacak Çakması (`Slam`)
Karadul'un çakmasıyla birebir: üç ritim (YAKIN / GEÇ / GENİŞ), aynı telegraph diski. Yalnız görsel/ses farklı.

### 3.2 Ağ Tükürüğü (`Volley`)
Karadul'un mermisi + isabette `Slow` (süre 1.5 sn, spec'te yok). Dodge i-frame ve `mermi_sil` kuralları aynı. Veri: `on_hit_status`.

### 3.3 Zehir Nefesi (`FireCone`)
Karadul'un konisi; etki listesi veriden gelir: `["poison"]`. Bugün `BossDirector` FireCone'a `grievous_wounds` + `burn` etkisini **sabit kodla** uyguluyor (L820 çevresi). Bu, veriden okunacak şekilde değişmeli (karadul davranışı aynı kalır).

### 3.4 Ağ Örme (`WebField`, yeni)
- Windup 900 ms: hedefin o anki konumunda beyaz halka büyür (telegraph).
- Vuruşta hasar yok; yerde **kalıcı ağ diski** kalır: r 3.0 m, ömür 8 sn, aynı anda en fazla 3 (en eskisi söner).
- Diskin içindeki oyuncu/dost: `Slow` (her 0.25 sn yenilenir, diskten çıkınca 0.5 sn içinde biter).
- Disk arena merkezinden **en az 4 m** uzağa iner (25 m'lik arenada merkez her zaman temiz kalır).
- Karşı oyun: windup'ta yer değiştir; `ates`/yakıcı fiillerin ağı yakması **gelecek** (bkz. bölüm 6).
- Tüm sayılar spec'te yok.

### 3.5 Sıçrayış (`Pounce`, yeni, yalnız faz 2)
- Seçilme şartı: hedef 4–12 m arasında (spec'te yok). Daha yakınsa çakma, daha uzaksa tükürük seçilir.
- Windup 1000 ms: boss çömelir, **iniş noktasında** kırmızı daire (r 3.0 m) belirir. İniş noktası windup başında kilitlenir (oyuncuyu takip etmez; dodge ile kaçılır).
- Aktif: 0.45 sn havada (spec'te yok), iner, iniş dairesinde 14 hasar.
- `BossAttackMotion.Leap`: enum'da zaten var; `Root` olan boss bu saldırıyı başlatamaz (mevcut `BossAttackControl.Evaluate` kuralı, ekstra kod yok).
- **Hareket yolu (AGENTS kural 3):** boss'u yalnız hareket kalıbı taşıyabilir. Bugün boss'ta `MotionTemplateBody` yok (yalnız oyuncuda, `PrototypeBootstrap` L205). Bkz. **Karar 1**.

## 4. Arena (25 m yarıçap)

- Boss başlangıçta merkezden ~5 m ileride (mevcut bootstrap).
- En fazla 3 ağ diski × π·3² ≈ 85 m² — 25 m'lik arenanın (~1960 m²) %4'ü; alanı kapatmaz, rota değiştirir.
- Sıçrayış en fazla 12 m: arena çapının (50 m) çeyreği; iniş noktası duvardan en az 2 m içeri kırpılır (`ArenaClamp`).

## 5. Veri şeması (`Resources/Bosses/aglarin-kralicesi.json`)

Karadul şemasının aynısı + saldırı başına isteğe bağlı yeni alanlar:

```json
{
  "id": "web_spit",
  "kind": "volley",
  "on_hit_status": { "id": "slow", "duration_sec": 1.5 }
}
{
  "id": "poison_breath",
  "kind": "fire_cone",
  "mechanics": ["poison"]
}
{
  "id": "web_field",
  "kind": "web_field",
  "windup_ms": 900,
  "field": { "radius_m": 3.0, "life_sec": 8, "max_count": 3, "status": "slow",
             "refresh_sec": 0.25, "min_center_dist_m": 4 }
}
{
  "id": "pounce",
  "kind": "pounce",
  "windup_ms": 1000,
  "damage": 14,
  "leap": { "min_range_m": 4, "max_range_m": 12, "air_sec": 0.45, "land_radius_m": 3.0,
            "wall_margin_m": 2 }
}
```

- `kind`: saldırının hangi `BossAttackKind` ile oynadığı. Karadul'da yok; yoksa `id`'den eşlenir (`slam`/`fire_cone`/`volley`), böylece karadul.json değişmez.
- `vitals.phases[].attacks`: faz başına izinli saldırı id'leri. `BossAttackKindPicker`'ın sabit Calm/Enraged dizileri bu listeden beslenir.
- Etiket kimliği yok: davranış `kind` + alanlardan doğar (AGENTS kural 5).

## 6. Kapsam dışı / gelecek

- Yavru örümcekler (ek düşman sistemi yok).
- Ağın yakıcı/ateş fiilleriyle yakılması (gramerden doğmalı; ayrı iş).
- Co-op zorunlu mekanikler (karadul'daki chain/rift/lock gibi stub).
- Yeni status türü yok: yalnız mevcut `Slow`, `Poison`.

## 7. Onay için kararlar

**Karar 1 — Sıçrayışın hareket yolu**
- A (önerilen): Boss'a `MotionTemplateBody` eklenir; sıçrayış tek fazlı bir hareket kalıbıyla (`leap`) oynar. AGENTS kural 3'e tam uyar, ama boss için ilk kalıp kullanımı (3.4 işi büyür).
- B: İlk sürümde sıçrayış yok; faz 2 = karadul gibi yerinde saldırılar + ağ. Sıçrayış ayrı PR.

**Karar 2 — Görsel model**
- A (önerilen): İlk sürümde mevcut boss modeli + renk/ağ efekti; örümcek modeli ayrı iş.
- B: CC0 örümcek modeli şimdi bulunup bağlanır (Mixamo insansı animasyonları örümceğe uymaz; prosedürel/basit animasyon gerekir).

**Karar 3 — Varsayılan boss**
- `ActiveBossId` varsayılanı `karadul` kalır (CI/tarama karadul üstünde, kapı değişmez). Kraliçe tuning'den seçilir.

## 8. Faz 3 kod parçaları (onay sonrası)

| Parça | İçerik | Bağlı karar |
|---|---|---|
| 3.1 | JSON + `ActiveBossId` + yükleyici parametresi | 3 |
| 3.2 | Core: `WebField`, `Pounce` türleri, faz listesiyle seçici, veriden etki listesi | 1 (B ise `Pounce` yok) |
| 3.3 | `BossDirector`: yeni türlerin çözümü, `on_hit_status`, FireCone etkilerinin veriden okunması | — |
| 3.4 | Telegraph + ağ alanı + (Karar 1A ise) boss `MotionTemplateBody` ile sıçrayış | 1, 2 |
| 3.5 | Bootstrap'ta boss seçimi + HUD | 2, 3 |
| 3.6 | Doğrulama: CoreTests, AtomSim, SweepV2 kapısı | — |
| 3.7 | CC0 örümcek modeli: bul, içe aktar, Editor bind ile animator, `BossVisual` eşlemesi | 2 |

Model gereksinimi (Karar 2B): CC0 lisanslı, kendi animasyonları olan örümcek (en az idle, yürüme, saldırı, ölüm; varsa sıçrama ve vurulma). Mixamo insansı klipleri kullanılmaz. Eksik klip, mevcut en yakın klibe düşer.
