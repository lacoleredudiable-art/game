# Core katmanı (ajan notu)

Kök `AGENTS.md` bağlayıcıdır; burada yalnız Core'a özel hatırlatmalar.

- **Saf C#:** `using UnityEngine` yasak. Zaman `double`/`float` parametre olarak gelir (`TimeDirector`, `SentenceEngine`, `MotionTemplateRunner.Tick`).
- **Namespace:** `Dovus.Core.<Klasör>` — klasör adı konu sınırıdır (`Boss`, `Casting`, `Damage`, `Dodge`, `Element`, `Grammar`, `Hud`, `Input`, `Motion`, `Passives`, `Status`, `Tuning`, …). `Core/Combat/` kaldırıldı (2B.6b).
- **Veri:** Tek JSON `kural-motoru-v4.json`; SkillMotor prose `SliceGrammarDefaults` (bellek). Sayı uydurma.
- **Hareket:** Kalıp mantığı `Motion/`; kalıp dışı konum yazımı `PositionOwnership` üzerinden kayıtlı olmalı.
- **Test:** Yeni Core dosyası → `tools/CoreTests/<Sistem>Tests.cs`, `namespace CoreTests;`, `[TestFixture]`. Koşum: `dotnet test tools/CoreTests`.
- **Harita:** Konu → dosya → `docs/MAP.md`.
