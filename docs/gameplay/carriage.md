# Carruagem (Fase 2)

Última revisão: 2026-06-19

## Comportamento

- Objeto com tag **Structure**, vida configurável, movimento ao longo de `CarriagePath`.
- Inimigos priorizam Player mas atacam Structure (`EnemyTargetFinder`); telegraphs aplicam dano via `PlayerCombatUtility`.
- Vida = 0 → para (`NetworkCarriage.IsBroken`); popup **"Aperte F para consertar"**.
- Conserto cooperativo (mesmo padrão do selamento).
- Chegada ao fim do trajeto → `GameEvents.OnCarriageArrived`; progresso em `OnCarriagePathProgressChanged`.

## Configuração

`Assets/Data/Gameplay/CarriageConfig.asset` (criar pelo menu do Unity)

## Prefab sugerido

| Componente | Notas |
|------------|--------|
| `NetworkObject` | Spawn na Fase-2 pelo host |
| `NetworkCarriage` | Referência a `CarriageConfig` e `CarriagePath` |
| `HealthComponent` | `SetAllowDestroyOnDeath(false)` |
| `CarriagePath` | Array de waypoints + zona de chegada no último ponto |
| Collider2D | Para dano melee/projétil |
| Tag | `Structure` |

## Código

- `NetworkCarriage`, `CarriagePath`
- `CarriageRepairPromptUI`, `CarriageRepairZoneVisual` (instalados no jogador via `PlayerGameplayModuleInstaller`)
