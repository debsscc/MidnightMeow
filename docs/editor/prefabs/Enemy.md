# Prefab: Enemy

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Enemies/Enemy.prefab`

## Resumo

Inimigo rato base (ranged). Variantes em `Rato_*.prefab` herdam ou duplicam esta estrutura.

## GameObject raiz

| Propriedade | *(confirmar no Editor)* |
|-------------|-------------------------|
| Tag | `Enemy` |
| Layer | `Enemy` |

## Scripts (Assembly-CSharp)

| Script | Responsabilidade |
|--------|------------------|
| `EnemyTargetFinder` | Alvo mais próximo no `targetDetectionRange` |
| `EnemyMovement` | Persegue ou random walk (patrulha) |
| `EnemyHitStun` | Parado após dano (`hitStunDuration` no EnemyStats) |
| `NetworkEnemyController` | Vida/morte em rede; IA só no servidor; após morte → `NetworkObject.Despawn(true)` (delay em `EnemyStats.deathDespawnDelay`) |
| `EnemyHealthConfig` | **Stats** → SO `EnemyStats` (`maxHealth`, etc.) — ver [diagnostics.md](../diagnostics.md) |
| `EnemyAnimationHandler` | Animações |
| `HealthComponent` | Vida e morte |
| `EnemyHealthConfig` | Config de vida (SO?) |
| `EnemyScaleConfig` | Escala visual |
| `EnemyDropHandler` | Drops ao morrer |
| `EnemyAttack_Ranged` | Ataque à distância |
| `EnemyAudioController` | SFX |
| `NetworkEnemyController` | Sincronização MP |

## Valores a confirmar no Editor

| Script | Campo | Valor esperado | Valor atual |
|--------|-------|----------------|-------------|
| EnemyMovement | stats / speed SO | `EnemyStats` em `Assets/Data/Stats/Enemies/` | |
| EnemyAttack_Ranged | projectilePrefab | `EnemyProjectile.prefab` | |
| EnemyHealthConfig | config asset | | |
| HealthComponent | _maxHealth | Vem do SO? | |
| EnemyStats | deathDespawnDelay | ~0,4s até remover o prefab | |
| HealthComponent | _allowDestroyOnDeath | **false** em MP (destruição via despawn) | |
| NetworkEnemyController | deathDespawnDelay | Fallback se SO não definir | 0,4 |

## Prefabs relacionados

- [Rato-variants.md](Rato-variants.md)
- `Enemy 1.prefab` — documentar diferença vs `Enemy.prefab`
