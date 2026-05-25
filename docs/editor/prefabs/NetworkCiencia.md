# Prefab: NetworkCiencia

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/NetworkCiencia.prefab`  
**GUID:** `d39f5e5c0794eaa47bf264d3a36f51da`

## Resumo

Pickup de ciência **minimalista** (rede). Referenciado pelo prefab `WaveSystem` em asset; **produção em Fase-1 usa `Science.prefab`**.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `NetworkTransform` | |
| `CienciaHoming` | Servidor move em direção ao jogador |
| `NetworkCienciaController` | Coleta + evento ciência |
| `CircleCollider2D` | Trigger |

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `pickupConfig` | `CienciaPickupConfig.asset` |
| `config` | `MultiplayerConfig.asset` |

## Science vs NetworkCiencia

| | Science.prefab | NetworkCiencia.prefab |
|--|----------------|----------------------|
| Uso Fase-1 | **Sim** (override cena) | Não (só default WaveSystem asset) |
| Visual / tag | Sprite, tag `Drop`, layer Collectable | Minimal |
| `Ciencia` + valor | Sim | Verificar no Inspector |

Manter comportamento alinhado: homing servidor, `SetValue` antes do `Spawn`, coleta no servidor.

## Relacionados

- [Science.md](Science.md)
