# Prefab: Projectile

Última revisão: 2026-07-17  
**Caminho:** `Assets/Prefabs/Combat/Projectile.prefab`  
**GUID:** `eadee2043abe1c540b4356dff9dbd9a7`

## Resumo

Projétil do **jogador** (Cora): física local + sincronização NGO no mesmo prefab.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Nome | `Projectile` |
| Layer | `Projectile` (7) |
| Scale | `0.5` (proporcional) |

## Componentes

| Componente | Notas |
|------------|--------|
| `Rigidbody2D` | gravity 0 |
| 2× `CircleCollider2D` | Sólido (paredes) + trigger (inimigos); offset centrado |
| `Projectile` | `stats` → `DefaultProjectileStats.asset`; `_spriteFacingOffsetDegrees: 0` (arte aponta para +X) |
| `ProjectileSparkTrail` | Faíscas magenta em world space; para no hit |
| `NetworkObject` | Registrado em Default Network Prefabs |
| `NetworkProjectileController` | |
| `NetworkTransform` | |
| `Animator` | Hit / expire |

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `stats` | `DefaultProjectileStats.asset` — `maxDistance: 80` (fallback); destrói em parede/inimigo |

## Campos (YAML)

| Campo | Valor |
|-------|--------|
| `_hitAnimDuration` | ~0.5 (só clip Hit) |
| `_vanishAnimDuration` | ~0.6 (só clip Vanish / expire por `maxDistance`) |
| `_spriteFacingOffsetDegrees` | `0` (fireball Cora aponta para a direita; use `-90` se a arte apontar para cima) |

## Sprite / animação

Arte: `Assets/Art/Sprites/VFX/Fire ball Cora/` (import **Single** — frame inteiro; slices automáticos quebravam splash/vanish em pedaços ~13px).

| Fase | Frames | Clip / estado |
|------|--------|----------------|
| Voo (loop) | `Base_fire_ball_cora_01`…`08` | `Player_flying.anim` → Flying |
| Hit / splash | `splash_fire_ball_cora_01`…`06` | `Player_projec_hit.anim` → Hit |
| Vanish | `vanish_fire_ball_cora_01`…`07` | `Player_projec_vanish.anim` → Vanish |
| Animator | `Assets/Data/Animacoes/Projectiles/Projectile Player.controller` | Spawn → Flying; impacto → Hit; expire → Vanish |

Fluxo:
- **Acerta algo** (inimigo / parede esgotando bounces): Spawn → Flying → **Hit** → despawn.
- **Não acerta nada** até `ProjectileStats.maxDistance`: Spawn → Flying → **Vanish** → despawn.

Hit e Vanish são mutuamente exclusivos (sem encadear Hit→Vanish). Em rede: `NotifyHitAndDespawn` / `NotifyVanishAndDespawn` via ClientRpc. Owner colliders são ignorados no spawn.

## Colisões (código)

- Layer `Projectile` ignora `Player` (projétil da Cora não acerta Nixie/aliado).
- Dano em inimigos: servidor via `NetworkProjectileController.ServerApplyEnemyHit`.
- **Passiva Cora (splash):** no impacto, o servidor busca alvos com `Physics2D.OverlapCircle` e faz `Instantiate` + `NetworkObject.Spawn` do prefab em `NetworkProjectileSpawner.networkSplashProjectilePrefab` (ver [guia](../guides/nixie-cora-passive-refactor.md)).

## Referenciado por

- [Cora.md](Cora.md) → `PlayerShooting.projectilePrefab`, `NetworkProjectileSpawner`

## Não confundir com

- [NetworkProjectile.md](NetworkProjectile.md) — prefab mínimo **sem colliders**; não usar em produção
- [EnemyProjectile.md](EnemyProjectile.md) — projétil inimigo
