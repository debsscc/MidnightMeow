# Prefab: Science

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/UI/Science.prefab`  
**GUID:** `41457ddb74133c14884342c60f3aa8ba`

## Resumo

Pickup de ciência **multiplayer** usado em **Fase-1** (`NetworkWaveManager.networkCienciaPrefab` na cena) e em `EnemyStats.cienciaPrefab`.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Tag | `Drop` |
| Layer | `Collectable` (10) |

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | Spawn servidor |
| `NetworkTransform` | Authority servidor (homing) |
| `Rigidbody2D` | Kinematic |
| `CircleCollider2D` | Trigger |
| `Ciencia` | Valor (`SetValue` antes do `Spawn`) |
| `CienciaHoming` | Ímã no raio do SO |
| `NetworkCienciaController` | Coleta + `GameEvents.OnCienciaCollected` |

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `pickupConfig` | `Assets/Resources/CienciaPickupConfig.asset` |
| `config` | `Assets/Data/Multiplayer/MultiplayerConfig.asset` |

## Comportamento

1. Inimigo morre → `EnemyDropHandler` → `NetworkWaveManager.SpawnNetworkCiencia`.
2. Servidor aplica valor, depois `Spawn`.
3. Dentro de `homingRadius` → move em direção ao jogador.
4. Dentro de `collectRadius` → `Despawn` + pool compartilhado se configurado.

## Relacionados

- [NetworkCiencia.md](NetworkCiencia.md) — alternativo no prefab WaveSystem asset
