# game

Unity C# ile geliştirilen mobil, kooperatif, aksiyon tabanlı boss dövüşü prototipi.

## Güncel dövüş girdisi

Sol başparmak karakteri yürütür. Sağ başparmak, altıgen dizilmiş **6 nokta** üzerinde
sürüklenerek cümle kurar: ilk nokta fiil, sonraki en fazla 3 nokta sıfattır. Merkeze kısa
dokunma düz vuruş yapar. Dodge, altıgenin dışında ayrı bir düğmedir.

Mükemmel dodge derecelendirmesi hitstop, kamera ve görsel geri bildirim üretir; cümle süresini
uzatan bir yavaş çekim penceresi yoktur.

## Belgeler

- [Durum](docs/durum.md) — en son tamamlanan işler ve bilinen açıklar
- [Element Sistemi](docs/element-sistemi.md) — güncel, insan-okur tasarım özeti
- [Element verisi](docs/element-sistemi.json) — motorun okuduğu sayılar ve mekanikler
- [Unity notları](docs/unity-notlari.md) — sahne, build ve cihaz operasyonları
- [AGENTS.md](AGENTS.md) — katkı kuralları

Eski beşgen prototip belgeleri 16 Eylül 2026'da kaldırıldı; güncel tasarım için git
geçmişindeki eski belgeler kaynak kabul edilmez.
