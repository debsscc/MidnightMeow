# Prefab: NetworkProjectile

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Combat/NetworkProjectile.prefab`

## Resumo

**Obsoleto para spawn:** prefab mínimo sem `CircleCollider2D`. O `NetworkProjectileSpawner` no Player deve apontar para **`Projectile.prefab`** (GUID `eadee2043abe1c540b4356dff9dbd9a7`).

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `NetworkTransform` | |
| `Projectile` | Lógica de impacto |
| `NetworkProjectileController` | Autoridade servidor |

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| Diferença vs `Projectile.prefab` | Por que dois prefabs? | |
| stats SO | Mesmo asset que Projectile? | |
| Spawn apenas no servidor? | | |

## Referenciado por

- `Player` → `NetworkProjectileSpawner.networkProjectilePrefab`
