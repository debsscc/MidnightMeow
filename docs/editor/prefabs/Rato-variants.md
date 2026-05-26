# Prefabs: Variantes de Rato

Última revisão: 2026-05-22

Variantes compartilham a mesma hierarquia base (`Enemy` / ranged): `NetworkObject`, `NetworkEnemyController`, `EnemyMovement`, `EnemyAttack_Ranged` (ou melee em tipos especiais), `EnemyDropHandler`, etc.

| Prefab | Caminho | EnemyStats asset |
|--------|---------|------------------|
| Rato_Padrao_Base | `Assets/Prefabs/Enemies/Rato_Padrao_Base.prefab` | `Rato_Padrao_Base.asset` |
| Rato_Padrao_Veloz | `Assets/Prefabs/Enemies/Rato_Padrao_Veloz.prefab` | `Rato_Padrao_Veloz 1.asset` |
| Rato_Padrao_Resistente | `Assets/Prefabs/Enemies/Rato_Padrao_Resistente.prefab` | `Rato_Padrao_Resistente.asset` |
| Rato_Eletrico | `Assets/Prefabs/Enemies/Rato_Eletrico.prefab` | `Rato_Eletrico.asset` |
| Rato_Acido | `Assets/Prefabs/Enemies/Rato_Acido.prefab` | `Rato_Acido.asset` |
| Enemy 1 | `Assets/Prefabs/Enemies/Enemy 1.prefab` | Legado — preferir `Rato_Padrao_Base` |

## GameObject raiz (comum)

| Propriedade | Valor |
|-------------|--------|
| Tag | `Enemy` |
| Layer | `Enemy` (11) |

## Ataque à distância

| Prefab | `projectilePrefab` |
|--------|----------------------|
| Rato_Padrao_Base, Veloz, Eletrico | `EnemyProjectile.prefab` (`guid: 6ededdb4f3eb7e143ae0a036319f5fd3`) via `EnemyAttack_Ranged` |
| Rato_Acido, Resistente | `EnemyAttack_Ranged` alternativo (`guid: 5be99fa3d9900c04f841392385f87d10`) + mesmo projétil |

## Ciência (drop MP)

`Rato_Padrao_Base.asset` define `minCienceDrop: 1` e referência ao prefab **Science** (`41457ddb`) para spawn via `NetworkWaveManager`.

## Ondas

Entradas em `WaveSettings` / instância `_GameLoop` em **Fase-1** referenciam estes prefabs — não o prefab `WaveSystem` isolado (campos `waveSettings` / `spawnPoints` vazios no asset).

## Relacionados

- [Enemy.md](Enemy.md) — template genérico `Enemy.prefab`
- [EnemyProjectile.md](EnemyProjectile.md)
