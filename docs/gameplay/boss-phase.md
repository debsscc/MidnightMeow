# Fase 3 — Boss

Última revisão: 2026-07-10

## Comportamento

- Contrato 3 carrega `Fase-3.unity`
- Uma onda com um único `Rato_Boss` (200 HP, escala 2×)
- Sem selamento de buracos nem carruagem
- Vitória ao eliminar o boss (`GameEvents.OnNightEnded`)
- Solo e multiplayer usam a mesma apresentação

## UI (só Fase-3 / `KillBoss`)

| Elemento | Comportamento |
|----------|----------------|
| `BossHealthBarHud` | Barra grande no **topo** da tela (nome + fill + trail) |
| `EnemyHealthBarDisplay` | **Desligada** no boss (evita barra duplicada na cabeça) |
| `PhaseObjectiveHud` | **Oculta** (sem texto de buracos/poças seladas) |
| `SpriteBlink` | Só em hits **marcantes** (`dano > 1` ou `≥ 5%` da vida máx.) |

Helpers: `BossPhaseUtility`, criação via `GameplayHudController.EnsureBossHealthBarHud()`.

## Prefab

| Campo | Valor |
|-------|-------|
| Caminho | `Assets/Prefabs/Enemies/Rato_Boss.prefab` |
| Base | `Rato_Padrao_Resistente` |
| HP | 200 (`HealthComponent`) |
| Componente extra | `BossEnemyMarker` (`displayName`: Rei Rato) |
| Animator | `AC_Rato_Rei.controller` |
| Rede | Registrado em `DefaultNetworkPrefabs.asset` |

## Animação (`AC_Rato_Rei`)

| Item | Caminho |
|------|---------|
| Controller | `Assets/Data/Animacoes/Enemy_AC/AC_Rato_Rei.controller` |
| Clips | `Assets/Art/Sprites/Animations/Enemies/Rato_Rei/` |
| Sprites | `Assets/Art/Sprites/Enemies/Rato rei/` |

Estados: `Idle`, `Running`, `Attacking`, `Spell` (projétil), `Charging` (investida), `TakingDamage`, `Dying`.

Parâmetros (além do padrão de inimigo):

| Parâmetro | Tipo | Uso |
|-----------|------|-----|
| `OnSpell` | Trigger | Cast do projétil |
| `OnCharge` | Trigger | Início da investida |
| `IsCharging` | Bool | Mantém `Charging` até a investida acabar |

`EnemyAnimationHandler` já dispara `OnAttack` / `OnTakeDamage` / `OnDie` / `MoveSpeed` / `IsAttacking`. `OnSpell` e `OnCharge` ficam para o código das habilidades do boss.

## Dados

- `Assets/Data/Stats/Game/Fase3.asset` — 1 inimigo, 1 onda
- Catálogo: `Resources/PhaseWaveSettingsCatalog.asset` → entrada `Fase-3`

## Habilidades futuras

`BossEnemyMarker` e `EnemyTelegraphedAttacker` estão prontos para extensão. Wire de `OnSpell` (projétil) e `OnCharge`/`IsCharging` (investida) no gameplay ainda pendente.
