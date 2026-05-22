# Prefab: WaveSystem

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/WaveSystem.prefab`

## Resumo

Ondas de inimigos autoritativas no servidor (`NetworkWaveManager`).

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `NetworkWaveManager` | Lê `WaveSettings` SO |

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| waveSettings | Asset em `Assets/Data/` | |
| Prefabs de inimigos por tipo | Rato_Base, Veloz, etc. | |
| Intervalos / quantidades | Balanceamento | |

## Ligação

- `MultiplayerBootstrapper.waveManager` deve referenciar esta instância.
