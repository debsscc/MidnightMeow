# Prefab: Projectile

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Combat/Projectile.prefab`

## Resumo

Projétil do jogador (local + componentes de rede no mesmo prefab).

## GameObject raiz

| Propriedade | Valor no YAML |
|-------------|---------------|
| Nome | `Projectile` |
| Layer | `Projectile` (7) |

## Componentes

| Componente | Notas |
|------------|--------|
| `Rigidbody2D` | massa baixa, gravity 0 |
| 2× `CircleCollider2D` | Sólido (paredes) + trigger (dano em inimigos); **sem Exclude Layers em Enemy** |
| `Projectile` | `stats` → GUID `caabe74a63e4a0b489c486933be32f4c` |
| `NetworkObject` | |
| `NetworkProjectileController` | |
| `NetworkTransform` | |
| `Animator` | `_projectileAnimator` |

## Campos snapshot (YAML)

| Campo | Valor |
|-------|--------|
| stats | `caabe74a63e4a0b489c486933be32f4c` |
| _hitAnimDuration | 0.3 |
| _playHitOnExpire | 0 |

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| stats | Asset `ProjectileStats` em `Assets/Data/Stats/Projectiles/` | |
| Layer collision matrix | Colide com Enemy, Wall, etc. | |
| Network prefab registration | Na lista do NetworkManager | |
| Velocidade / lifetime | No SO ou script | |

## Referenciado por

- `Player.prefab` → `PlayerShooting.projectilePrefab`
