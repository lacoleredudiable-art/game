# Core katmanı (ajan notu)

Kök `AGENTS.md` bağlayıcıdır; burada yalnız Core'a özel hatırlatmalar.

- **Saf C#:** `using UnityEngine` yasak. Zaman `double`/`float` parametre olarak gelir (`TimeDirector`, `SentenceEngine`, `MotionTemplateRunner.Tick`).
- **Namespace:** `Dovus.Core.<Klasör>` — klasör adı konu sınırıdır (`Combat`, `Grammar`, `Motion`, …).
- **Veri:** Skill/rün sayıları `docs/element-sistemi.json`'dan; his sayıları `Core/Tuning/*.cs` varsayılanlarından. Sayı uydurma.
- **Hareket:** Kalıp mantığı `Motion/`; kalıp dışı konum yazımı `PositionOwnership` üzerinden kayıtlı olmalı.
- **Test:** Yeni Core dosyası → `tools/CoreTests/<Sistem>Tests.cs`, `namespace CoreTests;`, `[TestFixture]`. Koşum: `dotnet test tools/CoreTests`.
- **Harita:** Konu → dosya → `docs/MAP.md`.
