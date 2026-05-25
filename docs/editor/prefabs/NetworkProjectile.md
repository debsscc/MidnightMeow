# Prefab: NetworkProjectile

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Combat/NetworkProjectile.prefab`

## Resumo

Prefabs **legado / incompleto**: apenas `NetworkObject`, `NetworkTransform`, `Projectile`, `NetworkProjectileController` — **sem colliders nem Rigidbody2D**.

## Estado atual

| Presente | Ausente |
|----------|---------|
| NGO + transform sync | `CircleCollider2D`, `Rigidbody2D`, Animator |

## Uso recomendado

**Não usar em produção.** O jogador (Cora) dispara `Projectile.prefab`, que já inclui rede + física + dano.

## Se reativar este prefab

Espelhar `Projectile.prefab`: dois colliders, layer Projectile (7), `DefaultProjectileStats`, registro em Default Network Prefabs.

## Relacionados

- [Projectile.md](Projectile.md)
