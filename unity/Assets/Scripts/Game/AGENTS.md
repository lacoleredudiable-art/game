# Game katmanı (ajan notu)

Kök `AGENTS.md` + `docs/MAP.md` + `docs/ARCHITECTURE.md`.

Unity katmanı: `Dovus.Game.<Konu>`. Tek hareket: `MotionTemplateRunner` + `MotionTemplateBodyHost`.
Sahne `GameBootstrapHost` ile kurulur. Takım: `TeamComboHost`, `TeamModifierHub` (eski portal border adları kalktı).

Derleme: `python tools/GameCompile/check.py`.
