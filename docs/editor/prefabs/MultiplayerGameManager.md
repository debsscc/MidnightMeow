# Prefab: MultiplayerGameManager

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/MultiplayerGameManager.prefab`

## Resumo

Estado global da sessão MP: fase de jogo, vitória/derrota, transição para gameplay (`ServerBeginGameplaySession` ao carregar Fase-1).

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | Persiste na sessão |
| `MultiplayerGameManager` | `NetworkVariable` de estado; cena de gameplay configurável no script |

## Instanciação

- Uma instância por sessão (Lobby ou bootstrap).
- `MultiplayerBootstrapper.gameManager` — referência **0** no prefab `MultiplayerManagers`; ligar na cena.

## Comportamento documentado (código)

- Ao entrar em cena com nome = `gameplaySceneName` (ex. **Fase-1**), servidor pode forçar estado `Playing` para ondas iniciarem.
- Ver [scenes.md](../scenes.md)

## ScriptableObjects (YAML)

| Campo | Asset |
|-------|--------|
| `multiplayerConfig` | `Assets/Data/Multiplayer/MultiplayerConfig.asset` |
| `gameConfig` | `Assets/Data/Stats/Game/defaultGameConfig.asset` |
