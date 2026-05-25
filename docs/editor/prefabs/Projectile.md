# Prefab: Projectile

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Combat/Projectile.prefab`  
**GUID:** `eadee2043abe1c540b4356dff9dbd9a7`

## Resumo

Projétil do **jogador** (Cora): física local + sincronização NGO no mesmo prefab.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Nome | `Projectile` |
| Layer | `Projectile` (7) |

## Componentes

| Componente | Notas |
|------------|--------|
| `Rigidbody2D` | gravity 0 |
| 2× `CircleCollider2D` | Sólido (paredes) + trigger (inimigos) |
| `Projectile` | `stats` → `DefaultProjectileStats.asset` |
| `NetworkObject` | Registrado em Default Network Prefabs |
| `NetworkProjectileController` | |
| `NetworkTransform` | |
| `Animator` | Hit / expire |

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `stats` | `Assets/Data/Stats/Projectiles/DefaultProjectileStats.asset` |

## Campos (YAML)

| Campo | Valor |
|-------|--------|
| `_hitAnimDuration` | 0.3 |
| `_playHitOnExpire` | false |

## Referenciado por

- [Cora.md](Cora.md) → `PlayerShooting.projectilePrefab`, `NetworkProjectileSpawner`

## Não confundir com

- [NetworkProjectile.md](NetworkProjectile.md) — prefab mínimo **sem colliders**; não usar em produção
- [EnemyProjectile.md](EnemyProjectile.md) — projétil inimigo
