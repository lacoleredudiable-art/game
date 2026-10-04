# Game katmanı (ajan notu)

Kök `AGENTS.md` + `docs/MAP.md` + `docs/ARCHITECTURE.md`.

Unity katmanı: namespace = klasör (`Dovus.Game.<Klasör>`, alt klasörler nokta ile; `Cameras`/`Arena` adları Unity çakışması yüzünden). Partial sınıf taşınırken tüm partial'lar + `.meta`'lar birlikte. Tek hareket: `MotionTemplateRunner` + `MotionTemplateBodyHost`.
Sahne `GameBootstrapHost` ile kurulur. Takım: `TeamComboHost`, `TeamModifierHub` (eski portal border adları kalktı).

Derleme: `python tools/GameCompile/check.py`.
