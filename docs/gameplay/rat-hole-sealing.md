# Selamento de buracos de spawn

Última revisão: 2026-06-30

## Spawn data-driven por buraco

Cada `RatHoleSpawnPoint` pode referenciar um **`RatHoleSpawnProfile`** (SO):

| Campo | Descrição |
|-------|-----------|
| `enemyTable` | Lista de prefabs + `spawnWeight` (ex.: Elétrico 0.7, Padrão 0.3) |
| `minSpawnTime` / `maxSpawnTime` | Intervalo sorteado entre spawns |

O buraco (`RatHoleSpawnController`) sorteia o delay, aguarda, sorteia o rato e pede spawn ao orquestrador.

- **Multiplayer:** `NetworkWaveManager` + `RatHoleSpawnOrchestrator` (servidor)
- **Single player:** `LocalRatHoleSpawnService` + mesmo orquestrador
- **Fallback:** se o buraco não tiver SO, o servidor monta perfil temporário a partir do `WaveSettings` legado da fase

Perfil padrão opcional por fase: `PhaseWaveSettingsCatalog.PhaseEntry.defaultHoleSpawnProfile`.

Criar perfis em `Assets/Data/Gameplay/` → menu **MidnightMeow/Gameplay/Rat Hole Spawn Profile**.

**Valores padrão (`RatHoleSpawnProfile.asset`):**

| Rato | Peso |
|------|------|
| Rato Padrão | 0.65 |
| Rato Elétrico | 0.25 |
| Rato Resistente | 0.10 |

`minSpawnTime`: 8s · `maxSpawnTime`: 14s

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

| Campo | Descrição |
|-------|-----------|
| `sealCompleteClip` | SFX ao concluir selamento (`Buraco Selado.wav`). Grupo **SFX** do `NewAudioMixer` — controlado pelo slider **SFX** nas opções. |

### Textos (pt-BR / en-US)

Chaves na tabela **UI** (`UiLocalization`):

| Chave | PT | EN |
|-------|----|----|
| `seal.prompt` | Aperte E para selar | Press E to seal |
| `seal.progress` | Fique na Área para selar — {0}% | Stay in the Area to seal — {0}% |
| `seal.complete` | Área selada | Area sealed |
| `objective.holes_status` | Buracos: … | Holes: … |

## Código principal

- `RatHoleSpawnPoint`, `NetworkRatHoleSealManager`, `RatHoleSealZoneSystem`
- `PlayerRatHoleSealInteraction`, `RatHoleSealPromptUI`, `RatHoleSealZoneVisual`, `RatHoleSealStatusUI`
- `PhaseObjectiveManager`, `PhaseObjectiveStatusUtility`
- Visual das zonas: `SealZoneRingVisual` em `_GameLoop/SealZoneVisuals/SealZonePool/`; tamanho = `zoneRadius × 2 × zoneVisualScaleMultiplier`

## Cena (Fase-1)

Em cada `SpawnPoint` usado pelo `NetworkWaveManager`:

1. Adicionar `RatHoleSpawnPoint` com `holeId` único (1, 2, 3…).
2. Atribuir `RatHoleSpawnProfile` no buraco (ou usar `defaultHoleSpawnProfile` no catálogo da fase).
3. `RatHoleSpawnController` é adicionado automaticamente em runtime.
4. Sprite do buraco (sem colisão sólida com player); `CircleCollider2D` trigger opcional para debug.
5. No `WaveSystem`, referenciar `RatHoleSealConfig` no `NetworkRatHoleSealManager`.
