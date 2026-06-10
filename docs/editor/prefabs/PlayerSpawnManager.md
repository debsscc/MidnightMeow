# Prefab: PlayerSpawnManager

Última revisão: 2026-06-10  
**Caminho:** `Assets/Prefabs/Multiplayer/PlayerSpawnManager.prefab`

## Resumo

Spawn de jogadores na rede: pontos no mapa e prefab NGO.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `PlayerSpawnManager` | Lista de spawn points + prefab |

## Campos (YAML)

| Campo | Valor atual |
|-------|-------------|
| `playerNetworkPrefab` | **Cora** — `guid: b18ed4e45e4d20a4dbdac339b666e689` |
| `characterPrefabs` | `[]` *(vazio — preencher Cora + Nixie quando Lobby selecionar personagem)* |
| `spawnPoints` | Array de Transforms *(preencher na cena ou filhos)* |

## Campos adicionais (2026-06-10)

| Campo | Valor / notas |
|-------|----------------|
| `gameplaySpawnDelaySeconds` | `0.35` — fallback rápido se `SynchronizeComplete` não disparar |
| `coSpawnSeparation` | `1.35` — jogadores no mesmo spawn point surgem próximos, mas separados (~180°) |

## Fluxo esperado

1. Lobby escolhe Cora ou Nixie.
2. Servidor spawna o prefab correspondente em um `spawnPoint` livre.
3. Vários clientes no **mesmo** spawn point recebem offset circular (`coSpawnSeparation`).
4. Pós-carga de cena: **reposiciona** jogadores existentes (`ApplySpawnTransformToPlayer`) em vez de despawn/respawn (evita teleporte visual).
4. Até `characterPrefabs` estar populado, todos recebem **Cora** por padrão.

## Relacionados

- [Cora.md](Cora.md), [Nixie.md](Nixie.md)
- [Lobby.md](Lobby.md)
