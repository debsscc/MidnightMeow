# Prefab: MultiplayerCameraRig

Última revisão: 2026-05-22  
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
| CameraConfig SO | Se usado | |
| Bounds / confiner | Limites do mapa | |
| Prioridades Cinemachine | | |
| cameraDiagnostics | Logs CAM-DIAG | **false** (padrão) |

## Ligação com Player

`NetworkPlayerController` tenta bind de `playerCamera` em runtime — confirmar que este rig está na cena MP.
