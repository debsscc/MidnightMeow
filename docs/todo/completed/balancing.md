# Concluídas — Balanceamento

## Velocidade do projétil da Cora

**Implementação:** `DefaultProjectileStats.asset` — `moveSpeed` 20 → 14.

## Vida de Cora

**Implementação:** `CoraCoreStats.asset` (maxHealth 5) vs `NixieCoreStats.asset` (maxHealth 7); perfis `CoraGameplayProfile` / `NixieGameplayProfile`.

## Attack speed do Nixie

**Implementação:** `NixieMeleeCombatStats.asset` — `attackCooldown` 0.45 → 0.32.

## Câmera durante a Fase 1

**Implementação (2026-06-19):** `CameraConfig.asset` + `MultiplayerCameraController.ComputeEdgeFollowPosition()`.

**Ajuste correto da dead zone:** valores **maiores** em `edgeDeadZoneX/Y` fazem a câmera panear **antes** (margem menor). Valores baixos pioram o problema.

| Campo | Valor atual (teste drástico) |
|-------|------------------------------|
| `edgeDeadZoneX` | 0.42 |
| `edgeDeadZoneY` | 0.40 |
| `edgePanSmoothing` | 28 |

Ver também [common-errors.md](../../troubleshooting/common-errors.md#gameplay--câmera-dead-zone-invertida).
