# Kül Ejderi v2 — çalışma kaynağı (teslim DEĞİL)

Durum: kullanıcı onayladı ("başarılı bir ejderha oldu"). Henüz Unity'ye teslim edilmedi; `unity/Assets` içine konmadı.

## İçerik
- `ejder.blend` — güncel ana dosya: Tripo v2 gövde (sfenks pozu), v1 baş + 34 diş, omurga kemikleri
  (Kok → Omurga_1 → Omurga_2 → Gogus), kalp cebi, kanatlar (Kanat_{L,R}_01–06, Omurga_2'ye bağlı), v2 dokular.
- `kemik.json` — gövde uydurma iskeletinin kemik konumları (govde2.py çıktısı).
- `kanat3.json` — kanat eklem/kemik verisi (kanat3.py çıktısı).
- `doku/` — gövde ve kanat albedo + kor (emisyon RGBA: R faz 1, G gözler, B faz 2+).
- `tripo/` — ham Tripo kaynakları (gövde, kanat).
- `onizleme/` — son dokulu renderlar ve v1/v2 karşılaştırması.
- Betikler: `tools/blender-ejder/v2/` (Blender 5.2, `python3 <betik>.py -- <argümanlar>`).

## Üretim sırası
govde2 → govde2_birlestir → pozlar2 (`-- ejder.blend 2.5`) → kapak (ZCUT 28.5) → kalp_yer → omurga (DMAX 4)
→ omurga_kemik → kanat3 → kanat3_birlestir → doku_palet / malzeme2.

## Açık işler (teslimden önce)
1. Kanat pozları: `Poz_Kanat_*_Supurme_Sonu` toprağa giriyor; `Acik` yandan bakınca kenardan görünüyor.
2. Dinlenme kanatları önden biraz dik; indirilecek.
3. Üçgen bütçesi: gövde ~57k → ~13k, kanatlar 80k (çift yüz dahil) → bütçeye. Toplam hedef 36k + LOD1.
4. Plaka ve kalp geçici.
5. `teslim.py` yeni nesnelere uyarlanıp FBX alınacak; 14-kul-ejderi.md kemik listesi/bütçe güncellenecek.
