# Prefab: NetworkCiencia

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/NetworkCiencia.prefab`

## Resumo

Pickup de ciência com sincronização de rede.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `NetworkTransform` | Posição sync |
| `CienciaHoming` | Ímã (servidor) |
| `NetworkCienciaController` | Coleta + RPC + SO `CienciaPickupConfig` |

**Produção:** Fase-1 usa `Science.prefab` (visual + collider). Este prefab é alternativo — deve espelhar os mesmos componentes.

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| Valor de ciência | Int no script ou SO | |
| Collider (trigger) | Layer Collectable | |
| VFX / som na coleta | | |

## Relação com Science.prefab

- `Science.prefab` — versão mais simples (também tem `Ciencia` + rede). Documentar diferença de uso.
