# Deneme sahnesi (görsel test, $0)

Kurulum: **Dovus → Visual → Deneme Sahnesi - Build (sahneyi kur)**. Sahne (`Assets/Scenes/DenemeSahnesi.unity`) bu
menüyle baştan üretilir; `.unity` dosyası elle düzenlenmez. Işık: **Deneme Sahnesi - Bake Lighting (async)**.

- `DenemeSahnesiLayout.json`: tüm yerleşim ve görünüm sayıları (sis, ortam ışığı, renk derecelendirme, bloom,
  kayalar, ejderha, kamera, lav ışıkları). Değiştir → Build menüsünü tekrar çalıştır.
- `Meshes/`: kutuda üretilmiş zemin (yakın/uzak), lav çatlakları, ufuk silüeti, kenar sisi (OBJ, x ekseni Unity
  OBJ içe aktarımı için ters yazıldı). Üretici: `tools/visual-testscene/gen_layout.py`.
- `Placeholder/DragonHead_AI_placeholder.obj`: kutuda Hunyuan3D-2mini ile üretilmiş kaba ejderha başı (yer tutucu).
- `Shaders/`: `LavaEmissive` (çatlak + göz), `FogSilhouette` (ufuk), `EdgeMist` (kenar sisi), `SoftHero` (kahraman).
- `Generated/`: Build menüsünün yazdığı materyaller, post-process profili, ışık ayarı.

Ejderha değişimi (Dungeon Mason "Dragon for Boss Monster: PBR"): Asset Store'dan ekle → Build'i tekrar çalıştır
(otomatik bulur) ya da prefab'ı seçip **Deneme Sahnesi - Ejderhayi secili prefab ile degistir**. Yer tutucu silinmez,
kapatılır. Asset Store paketi public repoya commit edilmez (klasörünü `.gitignore`'a ekle).

Kahraman sırası: `Assets/Art/Mixamo/Characters/` içindeki Mixamo karakteri (git'e girmez) → Synty Hero Knight (yerel)
→ Quaternius (repoda). Kahraman materyalleri `SoftHero` kopyalarına çevrilir (orijinallere dokunulmaz).
