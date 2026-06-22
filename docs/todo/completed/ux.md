# Concluídas — UX

## Reduzir zoom nos personagens

**Implementação:** `CameraConfig.asset` — `defaultOrthographicSize` 5 → 8; `FollowCamera` e MP unificados via `CameraConfig`.

## Fluxo de preparação

**Implementação:** `PreparationSessionManager.RequestConfirmContractRpc`; botão host "Confirmar Contrato!"; "Pronto" com contador 5→0; toggle de personagem em `TrySetCharacter`.

## Botões de retorno

**Implementação:** `PreparationScreenController` ("Voltar ao Menu", "Sair do Lobby"); `LobbyFlowController` ("Sair do Lobby").

## Câmera — efeito smooth nas bordas

**Implementação:** `CameraConfig.edgeDeadZoneX/Y` + `edgePanSmoothing`; `MultiplayerCameraController.ComputeEdgeFollowPosition()`.

## Efeito dissolve dos inimigos

**Sequência (2026-06-19, revisão 3):** morte → animação `Dying` completa → dissolve visível → despawn.

| Etapa | Responsável |
|-------|-------------|
| `OnDie` / estado `Dying` | `NetworkEnemyController` + `EnemyAnimationHandler` |
| Espera fim da animação | `DissolveEffect.WaitUntilDeathAnimationComplete` |
| Dissolve VOiD1 Fade 0→50 | `DissolveMaterialBinding` |
| Esconder renderers | `DissolveEffect.HideVisuals` |

**Não** congelar o animator antes do fim de `Dying` — isso deixava o sprite parado.

Ver [common-errors.md](../../troubleshooting/common-errors.md#gameplay--dissolve-de-inimigos-void1).

**Revisão 4 (2026-06-22):** sem `HideVisualsClientRpc` — dissolve local em cada peer antes do despawn.

## Vitória/derrota — entidades somem antes da transição

**Implementação:** `GameplayEndTransitionCoordinator` + `BeginEndGameScreenTransitionClientRpc` (fade em todos os peers); `GameplayTransitionCover` + `PlayerSpawnManager` aguardam overlay opaco antes de despawn; `DeathHordePresentation` sem fade de ratos pré-transição; `GameManager2` ignora fim de jogo quando NGO ativo.

## Hit melee da Nixie (onda shader)

**Implementação:** `MeleeHitWave.shader`, `MeleeAttackVisual`, `MeleeHitVisualConfig` (`Assets/Data/Combat/NixieMeleeHitVisual.asset`) referenciado em `NixieMeleeCombatStats.hitVisual`. Cores mudam com passiva via `PlayerPassiveHandler`. Gizmos desligados (`drawDebugGizmos: 0`).

## Animações Q/R (Nixie e Cora)

**Implementação:** estados `Ability1`/`Ability2` + triggers `OnAbility1`/`OnAbility2` em `AC_NIXIE` e `AC_CORA`; clips em `NixieAnimationProfile` / `CoraAnimationProfile` (`ability1Clip`, `ability2Clip`). `PlayerAnimationHandler.PlayAbilityAnimation` dispara os triggers.
