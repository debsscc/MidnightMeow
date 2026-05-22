# Prefab: MultiplayerManagers

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/MultiplayerManagers.prefab`

## Resumo

Bootstrap de rede: Relay, conexão, orquestração e log MP.

## GameObject raiz

| Propriedade | Valor no YAML |
|-------------|---------------|
| Nome | `MultiplayerManagers` |
| Tag | Untagged |
| Layer | Default |

## Componentes

| Script | Campos serializados (snapshot YAML) |
|--------|-------------------------------------|
| `RelayManager` | `config: {fileID: 0}` *(confirmar SO)* |
| `ConnectionManager` | `config` → GUID `50a79734eaf520a409e26b037cab7b62` |
| `MultiplayerBootstrapper` | `relayManager`, `connectionManager` wired; `gameManager: 0`, `waveManager: 0` |
| `MultiplayerLogger` | `ativo: 1`, `prefixo: '[MP]'`, flags de log `1` |
| `GameplayDiagnosticListener` *(adicionar)* | SO `GameplayDiagnosticConfig` — ver [diagnostics.md](../diagnostics.md) |

## Valores a confirmar no Editor

| Script | Campo | Valor esperado | Valor atual |
|--------|-------|----------------|-------------|
| RelayManager | config | `MultiplayerConfig` asset | |
| ConnectionManager | config | mesmo SO acima | |
| MultiplayerBootstrapper | gameManager | ref `MultiplayerGameManager` na cena/prefab | |
| MultiplayerBootstrapper | waveManager | ref `WaveSystem` / `NetworkWaveManager` | |
| MultiplayerLogger | ativo | só dev? | |

## SO relacionado

- `MultiplayerConfig` — localizar asset com GUID `50a79734eaf520a409e26b037cab7b62` em `Assets/Data/`
