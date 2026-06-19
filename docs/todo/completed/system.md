# Concluídas — Sistema

## Dash dos personagens

**Implementação:** `NixieCoreStats` / `CoraCoreStats` (cooldown e distância); `PlayerStats.maxDashCharges`; `PlayerDash.SetDashChargeBonus(int)` para upgrades.

## Defesa ranged dos inimigos

**Implementação:** `EnemyStats.rangedDefense`; enum `DamageType`; `DamageDefenseUtility`; propagação em projéteis, melee e habilidades.

## Sistema de selamento

**Implementação (2026-06-19):**

| Peça | Caminho |
|------|---------|
| SO | Criar via **Create > MidnightMeow/Gameplay > Rat Hole Seal Config** em `Assets/Data/Gameplay/` |
| Buraco na cena | `RatHoleSpawnPoint` (`holeId`, sprite, trigger sem colisão com player) |
| Rede | `NetworkRatHoleSealManager` no `WaveSystem` (auto-instalado em `NetworkWaveManager.Awake`) |
| Tick servidor | `RatHoleSealZoneSystem` |
| Interação | `PlayerRatHoleSealInteraction` + `RatHoleSealPromptUI` (via `PlayerGameplayModuleInstaller`) |
| Visual zonas | `RatHoleSealZoneVisual` (bootstrap em `GameplaySceneBootstrap`) |
| Spawn filtrado | `RatHoleSpawnSelectionUtility` em `WaveGenerator` e `NetworkWaveManager` |

Fluxo: jogador perto do buraco → prompt F → servidor sorteia 1–2 áreas sem sobreposição (`CooperativeZonePlacementUtility`) → progresso cooperativo → buraco marcado selado → spawns ignoram o ponto.

## Mecânica de carruagem

**Implementação (2026-06-19):**

| Peça | Caminho |
|------|---------|
| SO | Criar via **Create > MidnightMeow/Gameplay > Carriage Config** em `Assets/Data/Gameplay/` |
| Trajeto | `CarriagePath` (waypoints na cena) |
| Rede | `NetworkCarriage` (vida, `pathProgress`, quebra, conserto) |
| Dano inimigo | Tag `Structure` + `PlayerCombatUtility` estendido |
| Conserto | Mesmo padrão cooperativo; `CarriageRepairPromptUI` / `CarriageRepairZoneVisual` |
| Eventos UI | `GameEvents.OnCarriagePathProgressChanged`, `OnCarriageArrived` |

**Editor:** montar prefab da carruagem na Fase-2 com `NetworkObject`, `HealthComponent`, collider, `CarriagePath` e waypoints.
