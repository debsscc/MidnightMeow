# Prefab: NetworkManager

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/NetworkManager.prefab`

## Resumo

`Unity.Netcode.NetworkManager` + transporte UTP para host/client.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkManager` | Player prefab list, protocolo |
| `UnityTransport` | Endereço, porta, relay |

## Valores a confirmar no Editor

| Campo (NetworkManager) | Valor esperado | Valor atual |
|------------------------|----------------|-------------|
| Network Prefabs Lists | Inclui `Player`, projéteis, inimigos? | |
| Player Prefab | `Assets/Prefabs/Characters/Player.prefab` | |
| Run In Background | | |
| UnityTransport | Connection data / relay | |

## Crítico

Lista de prefabs registrados deve bater com tudo que spawna via `NetworkObject.Spawn`.
