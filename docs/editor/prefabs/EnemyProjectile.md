# Prefab: EnemyProjectile

Última revisão: 2026-06-10  
**Caminho:** `Assets/Prefabs/Combat/EnemyProjectile.prefab`  
**GUID:** `6ededdb4f3eb7e143ae0a036319f5fd3`

## Resumo

Projétil inimigo: legado (`EnemyAttack_Ranged`) e **visual de voo** dos telegraphs `ProjectileToZone` (clientes MP via `GameplayPrefabCatalog.enemyTelegraphTravelPrefab`).

## GameObject raiz

| Propriedade | Valor no YAML |
|-------------|---------------|
| Layer | Default (0) — *revisar se colisão com Player falhar* |
| Tag | Untagged |

## Componentes

| Componente | Notas |
|------------|--------|
| `EnemyProjectile` | `stats` → `EnemyProjectie.asset` *(typo no nome do asset)* |
| `NetworkObject` | |
| `NetworkEnemyProjectileController` | `DespawnAfterHit` no servidor; dano ao player via `PlayerCombatUtility` |
| `Rigidbody2D` + collider | Confirmar trigger vs sólido no Inspector |

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `stats` | `Assets/Data/Stats/Projectiles/EnemyProjectie.asset` |

## Referenciado por

- [Rato-variants.md](Rato-variants.md) — `EnemyAttack_Ranged.projectilePrefab`
