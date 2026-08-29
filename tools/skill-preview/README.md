# Skill Preview — cümle anlatıcı

Rünleri JSON'da tarif et; gramerle bir cümle yaz; motor **ne olacağını** anlatır.
Unity'ye bağlı değil — tasarım masası aracı.

## Çalıştır

```bash
cd tools/skill-preview
python3 preview.py 3-6            # ATEŞ → SU
python3 preview.py 6-3            # SU → ATEŞ (aynı ikili, ters sıra, başka silüet)
python3 preview.py 3-6-5 --target ground
python3 preview.py --list
python3 preview.py --check        # fiil × sıfat kapsama tablosu
python3 preview.py -i
python3 preview.py 5-1-2-4 --runes runes-operator5.json   # eski beşgen set
```

## Setler

| Dosya | Set |
|---|---|
| `runes.json` | **Element 6'lısı** (varsayılan): AYDINLIK, HAVA, ATEŞ, KARANLIK, TOPRAK, SU |
| `runes-operator5.json` | Eski operatör 5'lisi: ODAK, YAY, TUT, BIRIK, IT |

Nokta sayısı `layout.dotCount`'tan gelir; beşgen ve altıgen aynı motorla çalışır.

## Altıgen ve karşıtlık

Sıra: `1 AYDINLIK · 2 HAVA · 3 ATEŞ · 4 KARANLIK · 5 TOPRAK · 6 SU`

- **Mesafe 1 (komşu)** — fiziksel yakınlık: ışık-hava, hava-ateş, ateş-karanlık,
 karanlık-toprak, toprak-su, su-ışık. Birbirini besler.
- **Mesafe 3 (karşıt)** — `1↔4`, `2↔5`, `3↔6`. Karşıt sıfat, fiilin **tanımlayıcı
 özelliğini siler**: su alevin yakmasını, ateş akışın sönmezliğini, toprak havanın
 hareketini, aydınlık karanlığın yutmasını. Nitel kırılmanın kaynağı bu.

Beşgende tam karşıt yoktu; altıgenin tek yapısal kazancı bu.

## Rünleri yaz

`runes.json` içinde her nokta:

| Alan | Anlam |
|---|---|
| `id` | İsim (ATEŞ, SU, …) |
| `function` | Tek cümlelik işlev |
| `verb` | Fiil rolünde ne doğar |
| `adjective` | Sıfat rolünde ne bozar |
| `closing` | Son rünse kapanış türü |
| `seedRegime` | Fiil tohum rejimi |
| `visualHint` | Ucuz VFX ipucu |
| `roleLean` | Tank/dps/support eğilimi (sınıf değil, eğilim) |

**Nitel fark** için `transitions[]` doldur:

```json
{ "from": "Blaze", "adj": "6", "to": "Steam",
  "beat": "Karşıt: alev söner ama hacim patlar — yakma gider, buhar cephesi gelir." }
```

`--check` boş kalan hücreleri gösterir; geçiş yoksa o ikili "biraz daha X"e düşer.

> `transitions[]` bir **anlatım** tablosudur, kombo tablosu değil. Motora taşınmaz:
> Unity tarafında karşılığı silüet parametreleridir (AGENTS.md kural 7).

## Gramer (motorun varsaydığı)

- İlk nokta **fiil**, sonrakiler **sıfat**
- En fazla 4 nokta; fazlası yeni cümle
- Kapanış **türü** son rün; ödül miktarı uzunluk tablosundan
- Muhatap (`--target`) anlamı boyar; AYDINLIK tek başına heal değildir

## Örnek

```text
python3 preview.py 3-6     → Blaze → Steam   (alev söner, hacim patlar)
python3 preview.py 6-3     → Flow  → Boil    (akış sönmez, ısınır ve erir)
python3 preview.py 3-6-5   → Blaze → Steam → Geyser
python3 preview.py 6-3-2   → Flow  → Boil  → Steam  (aynı varış, farklı yol ve kapanış)
```
