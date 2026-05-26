# Prefab: WaveSystem

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/WaveSystem.prefab`

## Resumo

Ondas autoritativas no servidor (`NetworkWaveManager`). Instanciado em Fase-1 sob `---- Sistemas ----` / `_GameLoop`.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `NetworkWaveManager` | Spawns inimigos + ciência |

## Campos no prefab asset (YAML)

| Campo | Valor no prefab | Notas |
|-------|-----------------|--------|
| `waveSettings` | `null` | **Sobrescrito na cena** Fase-1 |
| `spawnPoints` | `null` | Pontos na hierarquia da cena |
| `networkCienciaPrefab` | `NetworkCiencia.prefab` (`d39f5e5c`) | |

## Override em Fase-1

Na cena `Assets/Scenes/Fases/Fase-1.unity`, `_GameLoop` / `NetworkWaveManager`:

| Campo | Valor em produção |
|-------|-------------------|
| `networkCienciaPrefab` | **Science.prefab** (`41457ddb`) |

Usar **Science** (visual + collider Collectable), não o prefab mínimo `NetworkCiencia`, salvo testes.

## Ligação

- `MultiplayerBootstrapper.waveManager` → instância na cena
- Estado `Playing` necessário para iniciar ondas — ver [scenes.md](../scenes.md)
