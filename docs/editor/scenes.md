# Cenas

Última revisão: 2026-05-22

## Bootstrap e fluxo


| Cena              | Caminho                                             | Função                                  |
| ----------------- | --------------------------------------------------- | --------------------------------------- |
| Bootstrap         | `Assets/Scenes/BootstrapScene/BootstrapScene.unity` | Inicialização, carregamento de serviços |
| Menu principal    | `Assets/Scenes/UI/Menu2.unity`                      | Menu / tabs                             |
| Lobby             | `Assets/Scenes/UI/Lobby.unity`                      | Multiplayer lobby                       |
| Jogo (UI wrapper) | `Assets/Scenes/UI/Game.unity`                       | Fluxo de partida com UI                 |
| Game (legado?)    | `Assets/Scenes/Game.unity`                          | Verificar Build Settings antes de usar  |
| Fase 1            | `Assets/Scenes/Fases/Fase-1.unity`                  | Level principal                         |
| Fase 2            | `Assets/Scenes/Fases/Fase-2.unity`                  | Segundo level                           |
| Game Over         | `Assets/Scenes/UI/GameOver.unity`                   | Derrota                                 |
| Vitória           | `Assets/Scenes/UI/VictoryScene.unity`               | Vitória                                 |


## Sandbox (não produção)

Cenas de teste em `Assets/_Sandbox/` (ex.: `Teste_MultiplayerFase-1.unity`). **Não** incluir em build de release sem revisão.

## NavMesh

Assets de NavMesh baked por cena em subpastas (`NavMesh-*.asset`). Prefabs: `NavMesh.prefab`, `NavMesh Surface.prefab`.

## Multiplayer (Fase-1 / sandbox)

- **Ondas:** apenas **`NetworkWaveManager`** (com `NetworkObject` no mesmo GameObject). Não usar **NightManager** / **WaveGenerator** na mesma cena MP — inimigos precisam de `NetworkObject.Spawn()`.
- **Bootstrap:** `MultiplayerBootstrapper` valida `NetworkWaveManager` na cena de gameplay.
- **Câmera:** prefab `MultiplayerCameraRig`; logs `[CAM-DIAG]` controlados por `GameplayDiagnosticConfig.cameraDiagnostics` (ver [diagnostics.md](diagnostics.md)).
- **Estado da partida:** ao carregar Fase-1 como servidor, `MultiplayerGameManager` passa automaticamente para `Playing` (campo `gameplaySceneName`). Sem isso, `NetworkWaveManager` fica em espera e nenhum inimigo spawna.

### Hierarquia recomendada (Fase-1)

| Objeto | Pai | Componentes-chave |
|--------|-----|-------------------|
| `---- Sistemas ----` | raiz | organização |
| `_GameLoop` | `---- Sistemas ----` | `NetworkObject`, `NetworkWaveManager` |
| `---- Spawn Points Inimigos ----` | raiz | 6+ `Transform` referenciados em `spawnPoints` |
| `MultiplayerGameManager` | raiz (prefab) | `NetworkObject`, estado `Playing` |
| `MultiplayerManagers` | raiz (prefab) | `MultiplayerBootstrapper` → `waveManager` = `_GameLoop` |

`waveSettings` em `_GameLoop` → `Assets/Data/Stats/Game/Fase 1.asset`. Prefabs de inimigo devem estar em **Default Network Prefabs**.

**Ciência:** `NetworkWaveManager.networkCienciaPrefab` → `Science.prefab` (com `CienciaHoming` + `CienciaPickupConfig`). Ver [diagnostics.md](diagnostics.md#ciência-drop-ao-matar).

## Ao alterar uma cena

1. Documentar prefabs instanciados novos aqui ou em `prefabs/`.
2. Confirmar em **File → Build Settings** se a cena permanece no build.
3. Atualizar `Última revisão` neste arquivo.

