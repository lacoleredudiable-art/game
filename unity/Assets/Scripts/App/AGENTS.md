# App katmanı (Dovus.App)

Kök `AGENTS.md` bağlayıcıdır. Klasör: `unity/Assets/Scripts/App/` — **`Application` adı kullanılmaz** (`Dovus.Application` ile çakışır).

- Saf C#: `using UnityEngine` yasak.
- Namespace: `Dovus.App.<Konu>` (ör. `Dovus.App.Time`).
- Assembly: `Dovus.App.asmdef`, yalnız `Dovus.Core` referansı, `noEngineReferences: true`.
- `Actors/` — `PlayerHealth` (saf can, ölüm/diriliş zamanlayıcısı); `PlayerVitals` Game adaptörü.
- İleride CastPipeline, BossBrain ve simülasyon komutları buraya taşınacak.
- Unity saat/zar adaptörleri `Game/Platform/` (`Dovus.Game.Platform`).
