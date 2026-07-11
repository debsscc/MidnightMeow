# Concluídas — Manutenção

## Importação de assets de telas e HUD

**Implementação:** `ScreenVisualTheme` (seções MainMenu, Lobby, Preparation, Characters, GameplayHud); `PlayerAbilityHudTheme`; `ScreenThemeApplier` com fallback de placeholders.

## Debug Dash / shaders build / áudio / menu escuro

**Implementação:** Debug visuals off por padrão; `CombatVisualMaterials` + Resources; mixer único `MidnightMeowAudioMixer` em Resources; gamma space no Menu2. Ver `docs/todo/maintenance.md` (TASK CONCLUÍDA).
