# Carruagem (Fase 2)

Última revisão: 2026-07-14

## Comportamento

- Objeto com tag **Structure**, vida em `CarriageConfig.maxHealth`, movimento ao longo de `CarriagePath`.
- Inimigos priorizam Player mas atacam Structure (`EnemyTargetFinder`).
- Vida = 0 → para (`NetworkCarriageHealth.IsBroken`); label **"Aperte E para consertar"**.
- Conserto cooperativo (mesmo padrão do selamento).
- Chegada ao fim → `PhaseObjectiveManager.NotifyCarriageArrived()` + `GameEvents.OnCarriageArrived` → vitória Fase 2.
- **HUD:** `PhaseObjectiveHud` mostra `Carruagem: X%` (lê `_pathProgress`).
- **Arte oficial:** hierarquia `VisualRoot` com 3 camadas (Body / Wheels / Back). Ver [prefabs/Carriage.md](../editor/prefabs/Carriage.md).
- **Placeholder legado:** só se `CarriageConfig.useOfficialArt = false`.

## Visual / rodas

| Peça | Responsável |
|------|-------------|
| Escala | `CarriageConfig.visualRootScale` aplicado em `VisualRoot` |
| Giro das rodas | `CarriageWheelSpinner` (local; pausa / quebrada / chegada param) |
| Raios | `frontWheelRadius` / `backWheelRadius` no config |
| Collider | `colliderSize` / `colliderOffset` no config |
| Label conserto | `repairLabelOffset` |

Não sincronizar ângulo de roda na rede — deriva do progresso/movimento já replicado.

## Trajeto

Em `CarriageConfig`: `pathStartX` (-42), `pathEndX` (18), `moveSpeed`, `arrivalZoneRadius`.

1. **`PhaseGameplayContentInstaller`** — cria/atualiza `CarriagePath` em todos os peers.
2. **`CarriageSpawner`** (servidor) — instancia o prefab, `NetworkObject.Spawn()`.
3. **`CarriageController`** — servidor avança `_pathProgress`; clientes seguem via `NetworkTransform`.

**Solo:** host inicia em Loading2 antes da fase. Não abrir `Fase-2` sem host.

## Configuração

- `Assets/Data/Gameplay/CarriageConfig.asset`
- Prefab: `Assets/Prefabs/Gameplay/Carriage.prefab`
- Sprites: `Assets/Art/Sprites/Carriage/`
- Catálogo: `GameplayPrefabCatalog.carriagePrefab`

Setup Editor: **MidnightMeow → Phases → Setup Active Phase Scene** (Fase-2).

## Código ativo

- `CarriageController`, `CarriageSpawner`, `CarriagePath`, `CarriageWheelSpinner`
- `NetworkCarriageHealth`, `NetworkCarriageRepairManager`, `CarriageDamageFilter`
- `CarriageRepairWorldUI`, `CarriageRepairZoneVisualHost`
- `PhaseGameplayContentInstaller.ConfigureCarriage()` / `EnsureCarriageSetup()`
