# Prefab: Player_Melee (guia de criação)

Última revisão: 2026-05-22

## Resumo

Segundo personagem **corpo a corpo**: sem tiro, com dash, ataque em cone na direção do mouse e knockback definido pelo SO do jogador.

## Passo a passo no Unity

1. Duplique `Assets/Prefabs/Characters/Player.prefab` → `Player_Melee.prefab`.
2. No prefab duplicado:
  - **Desative ou remova** `PlayerShooting`, `PlayerAmmo`, `NetworkProjectileSpawner`.
  - **Adicione** `PlayerMeleeCombat` (Fire do Input System = ataque).
  - **Adicione** `PlayerGameplayModuleInstaller` (imunidade + UI de downed/revive).
3. Em `PlayerMeleeCombat`:
  - `Combat Stats` → `Assets/Data/Stats/Player/MeleeCombatStats.asset`
  - `Enemy Layers` → layer **Enemy**
  - `Attack Origin` → transform do corpo (ou filho na espada)
4. Mantenha: `PlayerDash`, `NetworkPlayerHealth`, `NetworkPlayerRevive`, NGO, `PlayerAim` (mira do cone).
5. Registre em **Default Network Prefabs** se for spawnar em rede.
6. Em `PlayerSpawnManager` / seleção de personagem, aponte para este prefab quando for o melee.

## Scripts novos


| Script                          | Função                                         |
| ------------------------------- | ---------------------------------------------- |
| `PlayerMeleeCombat`             | Cone + dano + knockback via `MeleeCombatStats` |
| `MeleeCombatStats`              | SO: dano, cone, range, knockback               |
| `KnockbackReceiver`             | `ApplyKnockback(direction, force, duration)`   |
| `PlayerDamageImmunity`          | I-frames + atravessar inimigos após dano       |
| `DownedPlayerWorldUI`           | Barra inconsciente + progresso revive          |
| `RevivePromptWorldUI`           | Texto "Interagir para Ressuscitar" + barra     |
| `PlayerGameplayModuleInstaller` | Instala módulos no `Awake`                     |


## Animação de espada

1. Crie um filho `MeleeVFX` (sprite da espada ou Animator).
2. Dispare animação em `PlayerMeleeCombat` (evento futuro) ou via `PlayerAnimationHandler` com trigger `OnMeleeAttack`.
3. O dano usa **cone matemático** na direção do mouse (`PlayerAim`), independente do sprite.

## Balanceamento (`MeleeCombatStats`)


| Campo               | Sugestão inicial                 |
| ------------------- | -------------------------------- |
| `damage`            | 2                                |
| `nearHalfWidth`       | 0.35 (perto do jogador)          |
| `farHalfWidth`        | 1.1 (no alcance máximo)          |
| `attackRange`         | 1.8 (profundidade)               |
| `knockbackDistance`   | 0.65 (servidor)                  |
| `knockbackDuration`   | 0.25                             |

## Debug visual

- `MeleeAttackDebugVisual` — trapézio após cada golpe (~0,35s).
- `drawDebugGizmos` no SO + Gizmos com prefab selecionado.

## Damage numbers

- Adicione **`DamageIndicatorPresenter`** em `MultiplayerManagers` ou na cena de gameplay.

## Rede

- Dano em inimigos com `NetworkEnemyController.TakeDamageRpc` (servidor).
- Knockback aplicado localmente no `KnockbackReceiver` do inimigo (visual imediato).

