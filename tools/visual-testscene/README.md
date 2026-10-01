# Deneme sahnesi üreticileri (kutuda çalıştı)

- `process_models.py`: Poly Haven 1k glTF modellerini UV korunarak mobil üçgen sayısına indirir (trimesh + pymeshlab).
- `gen_layout.py`: zemin/çatlak/ufuk/sis OBJ'lerini ve `DenemeSahnesiLayout.json`'u deterministik üretir
  (numpy; tohum 20261001). Çıktılar `unity/Assets/Art/DenemeSahnesi/` altına kopyalanır.
Yollar kutuya göre yazılı; yeniden üretmek için `OUT`/`SRC` değişkenlerini düzelt.
