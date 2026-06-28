# Selamento de buracos de spawn

Última revisão: 2026-06-28

## Fluxo de selamento (resumo)

1. Aproximar do buraco → prompt **"Aperte E para selar"** (`RatHoleSealPromptUI`).
2. Pressionar **Interact (E)** → surgem **áreas circulares** grandes e opacas (`SealZoneRingVisual` via `RatHoleSealZoneVisual`), posicionadas em direção à câmera.
3. Entrar na área → texto **"Fique na Área para selar — X%"** + barra (`RatHoleSealStatusUI`).
4. 100% → **"Área selada"**; buraco para de spawnar; HUD atualiza (`PhaseObjectiveHud` / win Fase 1).
5. **Todos os buracos selados** → `PhaseObjectiveManager.TryEvaluateSealVictory()` → vitória → `VictoryScene`.

## Vitória (Fase 1)

- Condição: `PhaseWaveSettingsCatalog.PhaseWinCondition.SealAllHoles`.
- Contagem: `PhaseObjectiveStatusUtility.CountSealedHoles` usa **`RatHoleSpawnPoint.All`** como fonte de verdade (alinhado ao spawn).
- Disparo imediato ao selar o último buraco via `NetworkRatHoleSealManager` + polling em `PhaseObjectiveManager`.
- Transição: `GameEvents.OnNightEnded` + fallback `MultiplayerVictoryCoordinator` (caso `MultiplayerGameManager` ainda não tenha spawn de rede).

## Configuração

`Assets/Data/Gameplay/RatHoleSealConfig.asset` — instância em produção: `Assets/Resources/RatHoleSealConfig.asset`

## Código principal

- `RatHoleSpawnPoint`, `NetworkRatHoleSealManager`, `RatHoleSealZoneSystem`
- `PlayerRatHoleSealInteraction`, `RatHoleSealPromptUI`, `RatHoleSealZoneVisual`, `RatHoleSealStatusUI`
- `PhaseObjectiveManager`, `PhaseObjectiveStatusUtility`
- Visual das zonas: `SealZoneRingVisual` em `_GameLoop/SealZoneVisuals/SealZonePool/`; tamanho = `zoneRadius × 2 × zoneVisualScaleMultiplier`

## Cena (Fase-1)

Em cada `SpawnPoint` usado pelo `NetworkWaveManager`:

1. Adicionar `RatHoleSpawnPoint` com `holeId` único (1, 2, 3…).
2. Sprite do buraco (sem colisão sólida com player); `CircleCollider2D` trigger opcional para debug.
3. No `WaveSystem`, referenciar `RatHoleSealConfig` no `NetworkRatHoleSealManager`.
