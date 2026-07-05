# Concluídas — Multiplayer

## Mecânica de Reviver (downed vs morte final)

**Implementação:** `BeginDownedPresentation`, timer servidor com pausa na zona, bleed-out → dissolve. Ver `docs/todo/multiplayer.md` (TASK CONCLUÍDA).

## Carruagem Fase 2 (sync Cliente)

**Implementação:** `ConfigureCarriage` em todos os peers no `OnNetworkSpawn`; retry de path local; HUD via NV `_pathProgress`. Ver `docs/gameplay/carriage.md`.

## Pausa multiplayer (countdown 3→1)

**Implementação:** `ResumeCountdownRoutine` no servidor; `GameEvents.IsPaused` para congelar carruagem, inimigos e spawn; UI de countdown no pause menu. Ver `docs/todo/multiplayer.md` (TASK CONCLUÍDA).

## Shader de zona de ataque (build)

**Implementação:** `TelegraphFill.shader` em Always Included Shaders; `Resources/TelegraphZoneMaterial.mat`; `EnemyTelegraphZoneView` carrega via Resources.
