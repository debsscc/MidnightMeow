# Fluxo Menu2 → Lobby (loading)

Última revisão: 2026-05-22

## Comportamento

- Botão **PLAY** (`Level1`) → `UIActionBridge.LoadLobby()` → `SceneTransition.TryBeginTransition("Lobby")`.
- Cliques extras são **ignorados** enquanto `_sceneLoadPending` ou `SceneTransition.IsTransitioning`.
- Transição usa `Time.unscaledDeltaTime` (fade/loading não travam com `timeScale`).

## Arquivos

- `Assets/Scripts/UI/SceneTransition.cs` — guard `IsTransitioning`
- `Assets/Scripts/UI/Buttons/UIActionBridge.cs` — debounce

## Ajuste opcional no Editor

No botão PLAY, pode desmarcar **Interactable** via animação; o código já impede loads duplicados.
