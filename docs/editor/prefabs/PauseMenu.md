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
| `PauseMenuActions` | Continuar, Reiniciar fase (solo), Abandonar, Créditos, Sair do jogo |

## Comportamento (playtest)

| Botão | Solo | Multiplayer |
|-------|------|-------------|
| **Continuar** | Retoma (`timeScale = 1`) | Pause global retoma para todos |
| **Reiniciar fase** | Recarrega fase via NGO + reset de round | Oculto |
| **Abandonar** | `ScreenFlowStateMachine.ExitToMainMenu()` | Igual (desconecta NGO) |
| **Créditos** | `CreditsOverlayController.OpenFromPause()` — rola, escurece, fecha; volta ao pause | Igual (UI local; jogo continua pausado) |
| **Sair** (Buttons2) | Abre confirmação → fecha o aplicativo | Igual |

Painel de **Controles** no pause: removido por enquanto (sem `controlsPanel` em `PauseMenuActions`).

Vitória/derrota: `GameplaySessionTeardown` congela gameplay sem abrir o pause (`GameEvents.InvokeGameplayFreeze`).

## Navegação teclado / gamepad

O EventSystem de cena é substituído em runtime pelo `GlobalEventSystem` (`EventSystemGlobalBootstrap`). Por isso **First Selected da cena não vale** — a seleção é feita em código:

| Momento | Comportamento |
|---------|----------------|
| Abrir pause (`SceneOverlayController` / `PauseMenuActions`) | Seleciona **Resume** (ou primeiro Selectable) |
| Popup sair (`Background_PopUp`) | Seleciona **Don'tQuit** (cancelar) |
| Fechar popup | Volta seleção para Resume |
| Stick/setas sem seleção | `GamepadUiAutoSelect` escolhe o Selectable do canvas mais alto |

Scripts: `UiSelectionUtility`, `UiSelectOnEnable` (opcional em painéis), `GamepadUiAutoSelect`.

## Valores a confirmar no Editor

| Campo / objeto | Notas | Valor atual |
|----------------|-------|-------------|
| Canvas Group | Interactable / Blocks Raycasts quando pausado | |
| `PauseMenuActions` no root | `quitConfirmationRoot` → `Background_PopUp` | Ligado |
| Botões | Rewire automático por nome (`Resume`, `Replay`, `Menu`, `Credits`, `Quit`, `Don'tQuit`) | |
| `SceneOverlayController` (cenas Fase-*) | overlay `pause` → root `PauseMenu` | Fase-1 e Fase-2 |
| MP pause UI | `MultiplayerGameManager.ApplyPauseClientRpc` → `GameManager2.ShowPauseOverlay` | |

## Referenciado por

- `Assets/Prefabs/Multiplayer/Lobby.prefab` (nested)
- `Assets/Scenes/Fases/Fase-1.unity` (overrides de instância)
- `Assets/Scenes/UI/Menu2.unity` (`pauseMenuObject`)
