# Prefab: Enemy

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Enemies/Enemy.prefab`

## Resumo

Template de inimigo rato (ranged). Variantes de produção estão em `Rato_*.prefab` — ver [Rato-variants.md](Rato-variants.md).

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Tag | `Enemy` |
| Layer | `Enemy` (11) |

## Scripts principais

| Script | Responsabilidade |
|--------|------------------|
| `EnemyTargetFinder` | Alvo no `targetDetectionRange` |
| `EnemyMovement` | Perseguição / patrulha |
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

## Multiplayer

- Morte: `NetworkObject.Despawn` após `EnemyStats.deathDespawnDelay` (~0,4s)
- Dano ao jogador: servidor autoritativo

## Ataques com telegraph

Ver [enemy-telegraph-attacks.md](../../combat/enemy-telegraph-attacks.md). Ao atribuir um pattern SO, desligar ranged/melee legado automaticamente.

## Prefabs relacionados

- [Rato-variants.md](Rato-variants.md)
- `Enemy 1.prefab` — legado
