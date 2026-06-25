# Carruagem (Fase 2)

Última revisão: 2026-06-25

## Comportamento

- Objeto com tag **Structure**, vida configurável, movimento ao longo de `CarriagePath`.
- Inimigos priorizam Player mas atacam Structure (`EnemyTargetFinder`); telegraphs aplicam dano via `PlayerCombatUtility`.
- Vida = 0 → para (`NetworkCarriage.IsBroken`); popup **"Aperte E para consertar"**.
- Conserto cooperativo (mesmo padrão do selamento).
- Chegada ao fim do trajeto → `GameEvents.OnCarriageArrived`; progresso em `OnCarriagePathProgressChanged` (replicado via `NetworkVariable` para clientes).
- **HUD Fase 2:** `PhaseObjectiveHud` mostra `Carruagem: X%` junto com buracos e inimigos.
- **Barra de vida:** `EnemyHealthBarDisplay` na carruagem; vida sincronizada por `NetworkCarriage`.
- **Escala visual:** `CarriageConfig.visualScale` (padrão 3× no filho `Visual`).

## Configuração

`Assets/Data/Gameplay/CarriageConfig.asset`

Setup de cena: **MidnightMeow → Phases → Setup Active Phase Scene** (Fase-2).

## Prefab sugerido

| Componente | Notas |
|------------|--------|
| `NetworkObject` | Spawn na Fase-2 pelo host |
| `NetworkCarriage` | Referência a `CarriageConfig` e `CarriagePath` |
| `HealthComponent` | `SetAllowDestroyOnDeath(false)` |
| `EnemyHealthBarDisplay` | Barra world-space acima do sprite |
| `CarriagePath` | Array de waypoints + zona de chegada no último ponto |
| Collider2D | Para dano melee/projétil |
| Tag | `Structure` |

## Código

- `NetworkCarriage`, `CarriagePath`
- `CarriageRepairPromptUI`, `CarriageRepairZoneVisual` (instalados no jogador via `PlayerGameplayModuleInstaller`)
