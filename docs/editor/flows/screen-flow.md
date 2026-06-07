# Fluxo de telas unificado

Última revisão: 2026-06-07

> Requisitos completos: [screen-flow.md](../screen-flow.md)

## Visão geral

| Responsabilidade | Componente / asset |
|------------------|-------------------|
| Troca de **cena** | `ScreenFlowController` + `SceneFlowCatalog` |
| Orquestração (pause / corrida) | `GameFlowOrchestrator` |
| Persistência (magículas, tiers, host) | `SaveProfileStore` + `GameSaveData` |
| Contexto volátil da sessão | `GameSessionContext` |
| **Overlay** (pause) | `SceneOverlayController` + `PauseMenuActions` |
| Bootstrap por cena | `ScreenFlowSceneBootstrap` (auto via `RuntimeInitializeOnLoadMethod`) |
| UI placeholder 1920×1080 | `ScreenFlowPlaceholderFactory` |

## Fluxo completo (implementado)

```
BootstrapScene → Menu2 (MainMenuController)
  ├─ Novo Jogo → Lobby (LobbyFlowController: mode_select)
  │    ├─ Hostear → host_waiting → [2 jogadores] → Loading1
  │    ├─ Entrar → client_join → [conectou] → (host carrega) Loading1
  │    └─ Personagens → Characters (upgrades only) → return_lobby
  └─ Continuar (host + save) → Lobby (auto-host) → host_waiting → ...

Loading1 → Preparation (PreparationScreenController + PreparationSessionManager)
  ├─ Escolher Personagem → Characters (selection) → preparation_hub
  └─ [2× Pronto] → Loading2 → Fase-1 (gameplay)

Fase-1 → [vitória/derrota] → Preparation (loop)
```

## Rotas (`Assets/Data/UI/ScreenFlow/`)

| ID | Cena | Load |
|----|------|------|
| `bootstrap_menu` | Menu2 | Single |
| `menu_lobby` | Lobby | Single + loading |
| `lobby_loading1` | Loading1 | **NetcodeHost** |
| `loading1_preparation` | Preparation | **NetcodeHost** |
| `lobby_characters` | Characters | Single |
| `return_lobby` | Lobby | Single |
| `preparation_characters` | Characters | Single |
| `preparation_hub` | Preparation | Single |
| `preparation_loading2` | Loading2 | **NetcodeHost** |
| `loading2_gameplay` | Fase-1 | **NetcodeHost** |
| `gameplay_preparation` | Preparation | **NetcodeHost** |
| `return_menu` | Menu2 | Single |

## Persistência

- Arquivo: `{persistentDataPath}/MidnightMeow/saves/save_slot_0.json`
- **Continuar** habilitado só se `wasHost == true` no save
- Magículas e tiers por personagem (Nix/Cora) via `CharacterSaveData`

## Contratos

SOs em `Assets/Data/Contracts/Contract_1..3.asset` (`ContractDefinition`).

## Pause

- Single-player / fase: `GameManager2` + `GameFlowOrchestrator.RequestPause/Resume`
- Multiplayer: `MultiplayerGameManager.RequestPauseRpc/RequestResumeRpc`
- Prefab `PauseMenu` → `PauseMenuActions` (substitui classe `Buttons` removida)

## Para artistas

Placeholders usam:
- Canvas Scaler **1920×1080**, `matchWidthOrHeight = 0.5`
- Retângulos coloridos para Nix (azul) e Cora (vermelho) na loading
- `CursorManager` sprites existentes via `ScreenFlowPlaceholderFactory.ApplyMenuCursor()`

Substituir placeholders mantendo **ancoragens** dos botões gerados pelos controllers.
