# Prefab: Science

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/UI/Science.prefab`

## Resumo

Pickup de ciência **multiplayer** usado em Fase-1 (`NetworkWaveManager.networkCienciaPrefab`) e nos `EnemyStats.cienciaPrefab`. Registrado em **Default Network Prefabs**.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | Spawn via servidor |
| `NetworkTransform` | Authority servidor (homing) |
| `Rigidbody2D` | Kinematic — triggers 2D confiáveis |
| `CircleCollider2D` | Trigger, layer **Collectable** |
| `Ciencia` | Valor da moeda (`SetValue` antes do `Spawn`) |
| `CienciaHoming` | Ímã no raio do SO |
| `NetworkCienciaController` | Coleta servidor + `GameEvents.OnCienciaCollected` |

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `pickupConfig` | `Assets/Data/Multiplayer/CienciaPickupConfig.asset` |
| `config` | `Assets/Data/Multiplayer/MultiplayerConfig.asset` (`sharedSciencePool`) |

## Comportamento

1. Inimigo morre → `EnemyDropHandler` → `NetworkWaveManager.SpawnNetworkCiencia` (valor → `Spawn`).
2. Dentro de `homingRadius`, o servidor move o pickup em direção ao jogador.
3. Dentro de `collectRadius`, coleta, `Despawn`, evento de ciência (pool compartilhado se configurado).
