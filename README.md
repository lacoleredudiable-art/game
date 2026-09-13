# game

Mobil, kooperatif, aksiyon tabanlı boss dövüşü oyunu. Referans: Dragon Nest.

20-30 kişilik bir arkadaş grubu için yapılıyor. Ticari hedef yok — amaç, teknik ve görsel
olarak gerçekten etkileyici bir oyun çıkarmak.

**Durum:** 13 Eylül 2026 — dövüş tasarımı **sıfırdan**. Bağlayıcı kaynak
`docs/element-sistemi.json` (v4.2). Eski pentagon-cümle spec arşivde. Prototip kodu
repoda duruyor ama yeni dil ona uydurulmaz; JSON'dan yeniden kurulur.

## Belgeler

| Dosya | Ne için |
|---|---|
| [Durum](docs/durum.md) | Nerede kaldık, ne açık. **İlk buraya bak.** |
| [Element Sistemi](docs/element-sistemi.json) | **Bağlayıcı dövüş sistemi** (rün / fiil / sıfat / ekonomi) |
| [Teknoloji Kararları](docs/teknoloji-kararlari.md) | Stack, mimari, kapsam dışı |
| [Unity Notları](docs/unity-notlari.md) | Editör/build/telefon tuzakları |
| [Görev Listesi](docs/gorev-listesi.md) | Ajan çalışma düzeni; görev tablosu boş |
| [Tasarım Özeti](docs/tasarim-ozeti.md) | Ürün bağlamı (nadiren) |
| [AGENTS.md](AGENTS.md) | Ajan kuralları |

Eski turlar [`docs/arsiv/`](docs/arsiv/README.md) — bağlayıcı değil.

## Dövüş sistemi, tek paragrafta

Altı ana element rünü. 2'li çizim bileşik **kimlik skill**'i üretir. 3'lü aynı bileşigin
**fiil kapısını** + ana element **sıfatını** açar (2'linin yapamadığı iş). 4'lü dar şekil /
yüksek commit anahtarıdır — hasar ultisi değil. Uzunluk hasarı/poise'u çarpmaz; sıfat yeni
işlev ekliyorsa hasar vergisi alır (`damage_mult ≤ 0.9`).

## Nasıl çalıştırılır

Dövüş mantığı Unity'den ayrı, saf C#:

```bash
cd tools/CoreTests && dotnet test
```

Unity tarafı: `unity/` projesini Unity 6 ile aç. Telefona build: mevcut Android menüsü.
Yeni element dilinin preview/tooling'i görevlerle gelecek.
