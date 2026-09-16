CC0 / free VFX notes
====================

Combat impact/trail currently uses PlaceholderFactory (procedural primitives) when
Resources/Vfx/Trail and Resources/Vfx/Impact prefabs are missing.

To drop in Kenney Particle Pack (or any CC0 pack):
1. Place prefabs under unity/Assets/Resources/Vfx/Trail/<styleId>.prefab
2. And/or unity/Assets/Resources/Vfx/Impact/<styleId>.prefab
3. Style ids must match docs/prezentasyon-katmani.json vfx_binding styles
   (e.g. burst_soft, slash_arc).

PlaceholderFactory.TryLoadPrefab already prefers these assets over primitives.
