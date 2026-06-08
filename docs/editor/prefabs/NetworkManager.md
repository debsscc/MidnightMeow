# Prefab: NetworkManager

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/NetworkManager.prefab`

## Resumo

`Unity.Netcode.NetworkManager` + `UnityTransport` (UTP / Relay).

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkManager` | Listas de prefabs NGO |
| `UnityTransport` | Conexão / relay |

## Campos (YAML)

| Campo | Valor |
|-------|--------|
| `PlayerPrefab` | **None** (`fileID: 0`) — spawn via `PlayerSpawnManager`, não auto-spawn NGO default |
| `AutoSpawnPlayerPrefabClientSide` | `true` *(sem PlayerPrefab efetivo)* |
| `NetworkPrefabsLists` | Asset de lista default — deve incluir Cora, Nixie, Projectile, inimigos, Science, etc. |

## Checklist de prefabs na lista

- [Cora.md](Cora.md) / [Nixie.md](Nixie.md)
- [Projectile.md](Projectile.md)
- [Rato-variants.md](Rato-variants.md) — ratos usados nas ondas
- [Science.md](Science.md)
- [EnemyProjectile.md](EnemyProjectile.md)
- `PreparationSessionManager.prefab` / `CharactersSessionManager.prefab` (hub Preparation ↔ Characters)

## Crítico

Tudo que chama `NetworkObject.Spawn` precisa estar registrado. O jogador **não** é `Player.prefab` (removido).
