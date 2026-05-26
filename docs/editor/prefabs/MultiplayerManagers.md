# Prefab: MultiplayerManagers

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/MultiplayerManagers.prefab`

## Resumo

Bootstrap de rede: Relay, conexão, orquestração, log MP e apresentação de dano flutuante.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Nome | `MultiplayerManagers` |
| Tag | Untagged |
| Layer | Default (0) |

## Componentes

| Script | Campos (YAML) |
|--------|----------------|
| `RelayManager` | `config` — confirmar `MultiplayerConfig` no Inspector |
| `ConnectionManager` | `config` → `MultiplayerConfig.asset` (`50a79734`) |
| `MultiplayerBootstrapper` | `relayManager`, `connectionManager` wired; `gameManager` / `waveManager` = **0** (resolver na cena) |
| `MultiplayerLogger` | `ativo: true`, `prefixo: '[MP]'` |
| `GameplayDiagnosticListener` | SO `GameplayDiagnosticConfig` — ver [diagnostics.md](../diagnostics.md) |
| `DamageIndicatorPresenter` | Escuta `GameEvents.OnDamageShown`; números world-space |

## ScriptableObjects

| Asset | Caminho |
|-------|---------|
| `MultiplayerConfig` | `Assets/Data/Multiplayer/MultiplayerConfig.asset` |

## Cena

Referências a `MultiplayerGameManager` e `WaveSystem` são preenchidas na instância da cena (Lobby / Fase-1), não no prefab isolado.
