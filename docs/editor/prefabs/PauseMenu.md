# Prefab: PauseMenu

Última revisão: 2026-06-08  
**Caminho:** `Assets/Prefabs/UI/PauseMenu.prefab`  
**GUID:** `2542914e2b637bb4b871cad284433d66`

## Resumo

Menu de pausa (UI). Usado como **nested prefab** em `Lobby.prefab` e em cenas de fase.

## Estrutura típica

- Botões: retomar, configurações, sair (confirmar nomes no Hierarchy)
- `EventTrigger` em botões para feedback
- Layout: `HorizontalLayoutGroup` / `VerticalLayoutGroup`

## Componentes

| Tipo | Uso |
|------|-----|
| `Image`, `Button`, TMP | UI |
| `EventTrigger` | Hover/click custom |
| `PauseMenuActions` | Continuar, Reiniciar fase (solo), Abandonar, Controles, Sair do jogo |

## Comportamento (playtest)

| Botão | Solo | Multiplayer |
|-------|------|-------------|
| **Continuar** | Retoma (`timeScale = 1`) | Pause global retoma para todos |
| **Reiniciar fase** | Recarrega Fase-1 via NGO + reset de round | Oculto |
| **Abandonar** | `ScreenFlowStateMachine.ExitToMainMenu()` | Igual (desconecta NGO) |
| **Controles** | Abre painel `Controls` (esconde botões do pause) | Igual |
| **Voltar** (`Controls/Back`) | Volta ao painel principal do pause | Igual |
| **Sair** (Buttons2) | Abre confirmação → fecha o aplicativo | Igual |

`PauseMenuActions` religa botões no `Awake` (refs legadas do prefab estavam nulas).

## Valores a confirmar no Editor

| Campo / objeto | Notas | Valor atual |
|----------------|-------|-------------|
| Canvas Group | Interactable / Blocks Raycasts quando pausado | |
| `PauseMenuActions` no root | `quitConfirmationRoot` → `Background_PopUp` | Ligado |
| Botões | Rewire automático por nome (`Resume`, `Replay`, `Menu`, `Config`, `Quit`, `Don'tQuit`, `Back` em `Controls`) | |
| MP pause UI | `MultiplayerGameManager.ApplyPauseClientRpc` → `GameManager2.ShowPauseOverlay` | |

## Referenciado por

- `Assets/Prefabs/Multiplayer/Lobby.prefab` (nested)
- `Assets/Scenes/Fases/Fase-1.unity` (overrides de instância)
- `Assets/Scenes/UI/Menu2.unity` (`pauseMenuObject`)
