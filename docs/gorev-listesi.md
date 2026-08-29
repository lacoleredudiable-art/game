# Ajan Görev Listesi

> **Görev tablosu boş.** Alfa prototipin T0–T14 görevleri bitti ve arşive alındı:
> `docs/arsiv/gorev-listesi-t0-t14.md` (prompt'ların yazım biçimi için örnek olarak
> bakılabilir, aksi hâlde okunmaz). Yeni görevler `docs/dovus-sistemi.md` §4 dolduktan
> sonra oradan doğar.
>
> Bu dosyada kalan şey **çalışma düzeni** — o kısım tasarımdan bağımsız ve geçerli.

---
    10|
## Nasıl çalıştırılır

1. **Her göreve ayrı ajan, ayrı dal.** Görev bitmeden sonrakini başlatma. Testler yeşilse
   ajan dalı kendi merge eder; PR yalnızca **karar** gereken bir şey çıktıysa açık kalır.
2. **Sırayı bozma.** Her görev kendinden öncekilerin bittiğini varsayar.
3. Ajan bitirdiğinde tek soruyu sor: **"kabul kriterlerinden hangisini doğrulayamadın?"**
   Doğrulanmamış kriter varsa görev bitmemiştir.
4. **Prompt'a ek bilgi yapıştırmaya gerek yok.** Değişmez kurallar `AGENTS.md`'de ve her
   ajanın bağlamına otomatik giriyor. Ajan nerede kaldığımızı `docs/durum.md`'den öğrenir.
    20|5. **Her prompt "ÖNCE OKU" satırıyla başlar** ve yalnızca gereken bölümleri sayar. Bu satır
   bağlamı korur; silinmez.
6. **Unity gerektiren görevlerde editör açık ve MCP bağlı olmalı.** Saf C# (Core) görevleri
   `dotnet test` ile doğrulanır, en hızlı kısım burasıdır — bir işin Core'da yapılabilecek
   kısmı varsa oraya ayrılır.
7. **Denetim turu ayrı bir görevdir** ve projeyi taşıyan şey odur. Denetçi **kod yazmaz**:
   bulgularını `docs/durum.md`'ye yazar, düzeltmeyi ayrı bir görev olarak yazan model yapar.
   Denetçi, denetlediği kodu yazan modelden farklı bir model olur.

## Hangi görev hangi modelle

    30|Ölçüt tek soru: görev `AGENTS.md`'deki **değişmez kurallara** dokunuyor mu (Core saflığı,
sayı uydurmama, kombo tablosu yasağı, silüet ≠ sayı), yoksa speci verilmiş bir yüzey mi?

| Görev tipi | Model |
|---|---|
| Core'a, gramere veya birden çok katmana aynı anda dokunan | Opus |
| Speci net, tek katmanlı yüzey (HUD, panel, görünüm) | Sonnet |
| Mekanik, saniyede doğrulanabilen (yeniden adlandırma, gömülü sayıyı veriye taşıma, test iskeleti) | Composer |
| Denetim turu | Yazan modelden farklı (Grok) |

Composer'ın hata kalıbı proje büyüdükçe sabit: **sayı uydurmak**, tablo/dizi yazmaya kaçmak,
    40|ve "ÖNCE OKU" listesinin dışına taşıp başka görevin dosyasına dokunmak. Dövüş mantığına
sokulmaz.

---

## Görevler

*(boş)*
