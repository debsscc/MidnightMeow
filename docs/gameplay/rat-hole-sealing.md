# Selamento de buracos de spawn

Última revisão: 2026-06-19

## Comportamento

1. Jogador aproxima-se de um buraco **não selado** (`RatHoleSpawnPoint`).
2. Surge prompt **"Aperte F para selar"** (`RatHoleSealPromptUI`).
3. Host/servidor cria 1 ou 2 áreas circulares (`CooperativeZonePlacementUtility`) conforme jogadores vivos.
4. Jogadores permanecem nas áreas → barra de progresso sobe (`RatHoleSealZoneSystem`).
5. Dois jogadores em duas áreas → `dualPlayerSpeedMultiplier`.
6. Ninguém nas áreas por `abandonTimeout` → cancela.
7. Concluído → buraco deixa de ser escolhido por `RatHoleSpawnSelectionUtility`.

## Configuração

`Assets/Data/Gameplay/RatHoleSealConfig.asset` (criar pelo menu do Unity)

## Código principal

- `RatHoleSpawnPoint`, `NetworkRatHoleSealManager`, `RatHoleSealZoneSystem`
- `PlayerRatHoleSealInteraction`, `RatHoleSealPromptUI`, `RatHoleSealZoneVisual`

## Cena (Fase-1)

Em cada `SpawnPoint` usado pelo `NetworkWaveManager`:

1. Adicionar `RatHoleSpawnPoint` com `holeId` único (1, 2, 3…).
2. Sprite do buraco (sem colisão sólida com player); `CircleCollider2D` trigger opcional para debug.
3. No `WaveSystem`, referenciar `RatHoleSealConfig` no `NetworkRatHoleSealManager`.
