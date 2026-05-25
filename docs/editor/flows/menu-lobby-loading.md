# Fluxo Menu2 → Lobby (loading)

Última revisão: 2026-05-22

> Documentação completa: [screen-flow.md](./screen-flow.md)

## Comportamento

- Botão **PLAY** → `UIActionBridge.LoadLobby()` → rota `menu_lobby` no `ScreenFlowController` (fade + loading).
- Cliques extras ignorados enquanto `ScreenFlowController.IsTransitioning`.
- Visuais de fade/loading: objeto com `SceneTransition` na cena Menu2 (registra no controller persistente).

## Catálogo

Rota `menu_lobby` em `Assets/Data/UI/ScreenFlow/Route_Menu_Lobby.asset` (fade 3s, loading mín. 5s).
