# Prefab: Enemy

Última revisão: 2026-06-28  
**Caminho:** `Assets/Prefabs/Enemies/Enemy.prefab`

## Resumo

Template de inimigo rato (ranged). Variantes de produção estão em `Rato_*.prefab` — ver [Rato-variants.md](Rato-variants.md).

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Tag | `Enemy` |
| Layer | `Enemy` (10) |

## Scripts principais

| Script | Responsabilidade |
|--------|------------------|
| `EnemyTargetFinder` | Alvo no `targetDetectionRange` |
| `EnemyMovement` | Perseguição / patrulha (NavMesh rígido; auto-adiciona `EnemyPhysicsBody`) |
| `EnemyPhysicsBody` | RB kinematic; **não colide com Player** (`excludeLayers`) |
| `EnemySwordHitFlash` | Flash dourado no hit melee (shader `MidnightMeow/EnemySwordHitFlash`) |
| `EnemyHitStun` | Parada após dano |
| `NetworkEnemyController` | IA servidor; vida; despawn; knockback RPC |
| `EnemyHealthConfig` | `stats` → `EnemyStats` SO |
| `HealthComponent` | Vida; `_allowDestroyOnDeath: false` em MP |
| `EnemyAnimationHandler` | Animações |
| `EnemyScaleConfig` | Escala visual |
| `EnemyDropHandler` | Drop ciência / itens |
| `EnemyTelegraphedAttacker` | Telegraph estilo Hades (SO `EnemyAttackPatternDefinition`) |
| `EnemyTelegraphZoneFactory` | Instancia zonas de perigo |
| `NetworkEnemyTelegraphRelay` | Réplica visual em clientes MP |
| `EnemyAttack_Ranged` | Legado — tiro instantâneo |
| `EnemyAttack_Melee` | Legado — dano instantâneo ao chegar perto |
| `EnemyAudioController` | SFX dano/morte |
| `DissolveEffect` | Death: anim `Dying` → dissolve + sparkle |
| `EnemySpawnPresentation` | Spawn: baforada de poeira + materialização (dissolve reverso). Auto-adicionado pelo `NetworkEnemyController`; roda local em solo e MP |
| `NetworkObject` + `NetworkTransform` | NGO |

## Apresentação de spawn

`EnemySpawnPresentation` (auto-adicionado em `NetworkEnemyController.Awake`) toca no `Start` de cada instância, em qualquer máquina:

- **Poeira**: `EnemySpawnVfx` — burst procedural no ponto de surgimento (sem prefab de arte).
- **Materialização**: dissolve reverso (amount 1→0, ~0,35s) reaproveitando o material do `DissolveEffect` (`DissolveTemplate`). Se não houver material de dissolve, só a poeira toca.

Sem sincronização de rede dedicada: como o prefab é instanciado em cada cliente, o efeito roda localmente. Não há trigger de animação de spawn no Animator — é tudo VFX/shader.

## Colisão e profundidade (2D)

- **Hits (dano)**: detectados pelo collider do inimigo na layer `Enemy` (raycast/colisão do `Projectile`, melee do player).
- **Empurrão**: Player↔Enemy e Projétil↔Player ficam desligados na matriz via `CombatLayerCollision`, então ninguém empurra ninguém (player atravessa os ratos e vice-versa).
- **Ratos entre si**: separação resolvida pelo `NavMeshAgent` (avoidance, raio ~0.5).
- **Sorting de profundidade**: `EnemyAnimationHandler` e `PlayerAnimationHandler` ordenam por `bounds.min.y` do **collider sólido (não-trigger)**. Para alinhar os "pés" de player e inimigo (se um ficar sempre na frente/atrás do outro), use o campo `sortingReferenceYOffset` no Inspector de cada um — ele desloca o Y de referência sem mexer no collider.

## Multiplayer

- Morte: `NetworkObject.Despawn` após `EnemyStats.deathDespawnDelay` (~0,4s)
- Dano ao jogador: servidor autoritativo

## Ataques com telegraph

Ver [enemy-telegraph-attacks.md](../../combat/enemy-telegraph-attacks.md). Ao atribuir um pattern SO, desligar ranged/melee legado automaticamente.

## Prefabs relacionados

- [Rato-variants.md](Rato-variants.md)
- `Enemy 1.prefab` — legado
