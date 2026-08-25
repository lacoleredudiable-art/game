# Skill Preview — cümle anlatıcı

Beş rünü JSON'da tarif et; gramerle bir cümle yaz; motor **ne olacağını** anlatır.
Unity'ye bağlı değil — tasarım masası aracı.

## Çalıştır

```bash
cd tools/skill-preview
python3 preview.py 5-1-2-4
python3 preview.py 5-1-2 --target ally
python3 preview.py --list
python3 preview.py -i
```

## Rünleri yaz

`runes.json` içinde her nokta (1–5):

| Alan | Anlam |
|---|---|
| `id` | İsim (ODAK, YAY, …) |
| `function` | Tek cümlelik işlev |
| `verb` | Fiil rolünde ne doğar |
| `adjective` | Sıfat rolünde ne bozar |
| `closing` | Son rünse kapanış türü |
| `seedRegime` | Fiil tohum rejimi |
| `visualHint` | Ucuz VFX ipucu |

**Nitel fark** için `transitions[]` doldur:

```json
{ "from": "Wave", "adj": "1", "to": "Fissure",
  "beat": "Halka fay hattına dönüşür (mutate)." }
```

Geçiş yoksa motor uyarır — `5-1` ile `5-1-2` “biraz daha X”e düşmesin diye.

## Gramer (motorun varsaydığı)

- İlk nokta **fiil**, sonrakiler **sıfat**
- En fazla 4 nokta; fazlası yeni cümle
- Kapanış **türü** son rün; ödül miktarı uzunluk tablosundan
- Muhatap (`--target`) anlamı boyar; ODAK tek başına heal değildir

## Örnek

```text
python3 preview.py 5-1-2-4
```

`IT → ODAK → YAY → BIRIK`: şok halkası → fay → çatlaktan sürü → kalıcı iz.
