# Concluídas — UI

## Feedback Forms

**Implementação:** `PlaytestFeedbackButton` canto inferior esquerdo (menu + gameplay); botão maior no menu (220×80).

## Cooldown das skills

**Implementação:** `PlayerAbilityHud` + `PlayerAbilityHudTheme` SO; bootstrap em `GameplaySceneBootstrap`.

## Pause acima da HUD + ultrawide

**Implementação:** `PauseOverlayLayer` / `BringOverlayToFront`; `CanvasScaler` match 0.5; safe area na HUD de habilidades. Ver `docs/todo/ui.md` (TASK CONCLUÍDA).

## Contador Fase 1

**Implementação:** `HordeIndicator` (`GameEvents.OnWaveStatusChanged`); `OffscreenEnemyIndicator` (setas na borda).
