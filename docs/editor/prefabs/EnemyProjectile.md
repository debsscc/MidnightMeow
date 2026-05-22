# Prefab: EnemyProjectile

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Combat/EnemyProjectile.prefab`

## Resumo

Projétil disparado por inimigos ranged.

## Componentes

| Componente | Notas |
|------------|--------|
| `EnemyProjectile` | Dano, velocidade (SO?) |
| `NetworkObject` | |
| `NetworkEnemyProjectileController` | Sync rede |

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| stats | `EnemyProjectileStats` SO | |
| Layer | `ProjectileEnemy` | |
| Prefab registrado no NetworkManager | | |

## Referenciado por

- `EnemyAttack_Ranged` nos prefabs de rato
