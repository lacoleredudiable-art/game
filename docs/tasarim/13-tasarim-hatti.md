# 13 — Tasarım hattı: iş sırası

Sürüm 1.0 · 12 Ekim 2026 · Kullanıcı: Mustafa

Boss, arena ve görsel dil için yapılacak işlerin sırası. Teknik onarım `11-onarim-plani.md`'dedir; bu belge onu
kapsamaz. **Mevcut arena ve boss (Ağların Kraliçesi, kaburgalar, köy duvarı, kozalar) temel alınmaz; her şey
sıfırdan tasarlanır.** Adımlar sırayla yapılır; bir adım bitmeden sonrakine geçilmez. Biten adım ✔ ile
işaretlenir ve tarihi yazılır.

## A. Kararlar (sohbette, kullanıcı verir)

| # | Karar | Neyi açar | Durum |
|---|---|---|---|
| A1 | Kül Ejderi yönü: 45 m yaşlı taş ejderha, krater, 3 faz (Uyanış / Doğruluş / Kalbin koru) | B2, B3 | ✔ 12 Ekim: olduğu gibi onaylı |
| A2 | Zorluk kademeleri ve takım görevi cezası (tur yanar mı, hangi zorlukta) | B2 | ✔ 12 Ekim: zorluğa göre (Cehennem'de tur yanar, kolay zorluklarda ağır hasar; antrenman modu ve sınırlı diriltme var) |
| A3 | Ağların Kraliçesi tamamen mi kalkıyor, ileride ikinci boss mu olur | B4 | ✔ 12 Ekim: tamamen kalkar (kod ve asset'ler silinir) |
| A4 | İlk oynanabilir hedef: tek oyuncu + 2 yapay zekâ dostu, yalnız faz 1, telefonda ~3 dk | E sırası | ✔ 12 Ekim: onaylı |

## B. Belgeler (Claude yazar, kullanıcı onaylar)

| # | Belge | İçerik | Bağlı | Durum |
|---|---|---|---|---|
| B1 | `12-sanat-incili.md` | Palet, ışık, sis, malzeme, silüet, ölçek, efekt dili, yapılmaz listesi | — | ✔ 12 Ekim |
| B2 | `14-kul-ejderi.md` (boss) | `10` §6.1 şablonunun bütün alanları: gövde ve çarpışma, her saldırı (ön işaret süresi, alan, hasar %, kesilebilirlik, açık kalma), tehdit ritmi (küçük / büyük / takım görevi), 3 faz ve geçişleri, can katmanları ve pul kırılması, takım görevleri (Kor Mührü, Kül Yağmuru), zorluk kademeleri, replikler, kamera notları (faz 2'de baş 15 m) | A1, A2 | ✔ 12 Ekim: onaylı |
| B3 | `15-krater-arenasi.md` (arena) | Bölgeler (oynanış 0–18 m, duvar 18–22 m, yamaç 22–60 m, uzak 60–250 m), her nesnenin koordinatı, ölçüsü, çarpışması ve hangi asset'ten yapılacağı; dikili taşlar ve siperler; ortam tehditlerinin yerleri; başlangıç noktaları; ışık, sis ve ses kaynaklarının yerleşimi | B2 | ✔ 12 Ekim |
| B4 | `10-olculer.md` güncellemesi | §6.3 ve §7 yeni belgelere bağlanır; eski satırlar kalkar; açık kalanlar `08`'e | B2, B3, A3 | ✔ 12 Ekim (yönlendirme notları; eski satırlar kod değişince silinir) |
| B5 | `16-asset-listesi.md` | Her asset için: ölçü, üçgen ve doku bütçesi, kaynak (elde / Tripo / Poly Haven / el yapımı), kabul kontrol sayfası, sıra | B3 | ✔ 12 Ekim |

## C. Gri kutu (blockout) — sanat yapılmadan önce

| # | İş | Neden | Bağlı |
|---|---|---|---|
| C1 | Arena ve ejderha kafası / pençeleri basit kutu ve kapsüllerle, gerçek ölçüde koddan kurulur (`GameBootstrapHost`) | Ölçek, kamera kadrajı, dikili taş mesafeleri ve takım görevleri model yapılmadan telefonda denenir. Burada yanlış çıkan şey modelde pahalıya düzelir | B3; teknik hat Adım 5 |
| C2 | Telefonda deneme: kadraj (SI-7.2), okunurluk (SI-1.5), taşlar arası koşu süreleri | Belgelerdeki `ÖNERİ` değerleri onaylanır ya da değişir | C1 |

## D. Asset üretimi (Mustafa: Tripo + Blender; Claude: iş kartı ve kabul kontrolü)

| # | Asset | Kaynak | Bağlı |
|---|---|---|---|
| D1 | Ortam kiti: zemin, çatlaklar, kenar sisi, ufuk (Deneme Sahnesi parçaları); krater duvarı ve uzak dağlar (Poly Haven kayaları); dikili taş (`runtasi`, 6–9 m'ye); basamak | Elde var, uyarlanır | C2 |
| D2 | Yere saplanmış eski silahlar (12–20) | Kılıç + Quaternius kılıç ve hançer, paslı doku | C2 |
| D3 | Ejderha kafası, boynu, iki ön pençesi (rigli) | Tripo → Blender; 40 bin üçgen bütçesi (O-15.6); iş kartı `16` §1 | Başladı 12 Ekim (onarım sürerken; C2'yi beklemez) |
| D4 | Ejderha gövdesi ve kanatlar: kuzey duvarını oluşturan kaya kütlesi; kanatlar Kanat Savuruşu için oynar | Tripo kaba modelinden; v0 ✔ 12 Ekim | D3 |
| D5 | Boss animasyonu: poz kütüphanesi (~26 poz) + 4 sinema klibi; zamanlama ve sürekli katmanlar kodda (karar 12 Ekim, `16` §1.5) | Blender (Claude) + kod (ajan, E2) | D3 |
| D6 | Atmosfer ve boss efektleri: kül, kıvılcım, yer sisi, darbe tozu, boss ateşi, pul kırılması, ortam tehditleri | Shader + parçacık | D1 |
| D7 | Ses: ejderha nefesi ve sesi, darbeler, ortam, faz müzik katmanları; **replik seslendirmesi sonra (12 Ekim kararı)**, o zamana kadar altyazı | — | D5 |

## E. Uygulama (teknik hat, ajanlar)

| # | İş | Bağlı |
|---|---|---|
| E1 | Arenayı gerçek asset'lerle koddan kur; eski arena ve örümcek koddan kalkar | D1, D2, D4 |
| E2 | Boss faz 1: veri JSON'u (B2'den), davranış, ön işaretler, tehdit ritmi | B2, D3, D5 |
| E3 | Görsel kontroller (`12` §13) ve Gözcü kuralları | E1, E2 |
| E4 | Ara hedef: tek oyuncu + 2 yapay zekâ dostu, faz 1, telefonda ~3 dk; his tutuyor mu | E3, A4 |
| E5 | Faz 2 ve 3, takım görevleri, can katmanları, replikler | E4 |
| E6 | Ağ (5 kişi) — ayrı karar, E4'te his tutarsa | E5 |
