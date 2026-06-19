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
