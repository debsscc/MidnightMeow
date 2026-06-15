# Prefab: MultiplayerCameraRig

Última revisão: 2026-06-10  
**Caminho:** `Assets/Prefabs/Multiplayer/MultiplayerCameraRig.prefab`

## Resumo

Rig de câmera com Cinemachine para follow multiplayer, shake e cutscenes.

## Componentes principais

| Script / componente | Função |
|---------------------|--------|
| `CinemachineBrain` | Brain na câmera principal |
| `CinemachineCamera` | VCams |
| `CinemachinePositionComposer` | Composição 2D |
| `MultiplayerCameraController` | Follow de jogadores |
| `CameraShakeController` | Shake |
| `CameraCutsceneController` | Cutscenes |
| `UniversalAdditionalCameraData` | URP |

## Diagnóstico `[CAM-DIAG]`

| Campo | Descrição |
|-------|-----------|
| **Diagnostic Config** | `Assets/Data/Debug/GameplayDiagnosticConfig.asset` |
| **Use Config Asset** | Se ligado, obedece `cameraDiagnostics` no SO |
| **Enable Diagnostics Logs** | Só usado se **Use Config Asset** estiver desligado |

Para **silenciar** os logs da câmera: no SO, desmarque **Camera Diagnostics** (ou desligue **Master Enabled**).

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| Target group / follow | Como escolhe jogador local | |
| CameraConfig SO | Se usado | `Assets/Data/Multiplayer/CameraConfig.asset` |
| **Intro zoom** | Zoom in ao iniciar fase | `playIntroZoom`, `introZoomInAmount` (2), `introZoomDuration` (2.5s) no SO |
| Bounds / confiner | Limites do mapa | |
| Prioridades Cinemachine | | |
| cameraDiagnostics | Logs CAM-DIAG | **false** (padrão) |

## Ligação com Player

- `NetworkPlayerController.TryBindCameraNow()` chama `MultiplayerCameraController.SetTarget` no jogador local (`IsOwner`).
- O campo `playerCamera` no prefab do personagem deve ficar **vazio** (câmera no prefab causa tela azul).
- `GameplayCameraRebindUtility` repete o bind em 0 / 0,35 / 0,75 / 1,5 s após `Fase-*` carregar (cliente NGO spawna o jogador depois do `SynchronizeComplete`).

## Display Error / tela azul no cliente

| Sintoma | Causa usual |
|---------|-------------|
| Tela azul + HUD ok | `MainCamera` ativa sem `Follow` no Cinemachine (câmera parada no rig, clear color azul) |
| Display 1 sem câmera | Nenhuma câmera habilitada durante transição |

Fluxo:

1. `GameplaySceneBootstrap.EnsureCameraRig()` — usa o rig da cena (`Fase-1` já tem instância na raiz) ou instancia via `GameplayPrefabCatalog`.
2. `EnsureActiveGameplayCamera()` — habilita `MainCamera` na cena de gameplay.
3. `TransitionCameraKeeper` (DDOL) — fallback preto só durante transições (sem câmera de gameplay ativa).
4. `TryBindCameraNow` usa `EnsureCameraRigPresent()` (sem rebind recursivo) e só retorna sucesso com `IsFollowingTarget`.
5. Rig da cena tem prioridade sobre `MultiplayerCameraRig(Clone)`; spawn via catalog é último recurso.
6. `MainCamera` em Z = -10; follow direto em `LateUpdate` (`useDirectCameraFollow`) com CinemachineBrain desligado após bind.
7. `GameplayCameraSceneUtility.TakeOverGameplayRendering()` desativa outras câmeras (menus/DDOL) e garante tag `MainCamera` só no rig da fase.
4. `MultiplayerCameraController.Resolve()` — singleton robusto (limpa `Instance` no `OnDestroy`, prefere rig da cena ativa sobre duplicatas).

**Prioridade Cinemachine:** `PlayerVirtualCamera` com `Priority.Enabled = 1`, valor `10`.
