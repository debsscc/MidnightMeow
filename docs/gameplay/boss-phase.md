# Fase 3 — Boss

Última revisão: 2026-07-16

## Comportamento

- Contrato 3 carrega `Fase-3.unity`
- Uma onda com um único `Rato_Boss` (200 HP, escala 2×)
- Sem selamento de buracos nem carruagem
- Vitória ao eliminar o boss (`GameEvents.OnNightEnded`)
- Solo e multiplayer usam a mesma apresentação

## IA do Rei Rato (`RatKingController`)

FSM **somente no servidor**. Clientes recebem telegraphs (`ClientRpc`) e animações (`NetworkVariable`).

| Estado | Fluxo |
|--------|--------|
| Decision | Alvo = jogador vivo mais próximo → roleta por pesos do SO |
| Ranged | Foge até `maxRangedDistance * fleeDistanceThreshold` (ou timeout) → 5 faixas (±angle2, ±angle1, 0) |
| Charge | Aproxima com buff de speed até `chargeRange * 0.6` → telegraph + dash com overlap → melee `ConeFrustum` |

Config: `RatKingBehaviorConfig` (`MidnightMeow/Combat/Rat King Behavior Config`).

Guia de setup no Editor: [rat-king-boss-setup.md](../editor/guides/rat-king-boss-setup.md).

## UI (só Fase-3 / `KillBoss`)

| Elemento | Comportamento |
|----------|----------------|
| `BossHealthBarHud` | Banner **Objetivo** (“DERROTE O REI RATO”) + barra larga (moldura dourada + fill vermelho) no topo |
| `EnemyHealthBarDisplay` | Fallback world-space no boss até a HUD de tela vincular; depois some |
| `PhaseObjectiveHud` | **Oculta** (sem texto de buracos/poças seladas) |
| `SpriteBlink` | Só em hits **marcantes** (`dano > 1` ou `≥ 5%` da vida máx.) |

Helpers: `BossPhaseUtility`, criação via `GameplayHudController.EnsureBossHealthBarHud()`.

**Nota (2026-07-17):** bind via `BossEnemyMarker.OnBossAvailable` + poll; `EnsureConfigured()` reconstrói UI/CanvasGroup se o objeto na cena estiver incompleto ou desligado. Canvas da Fase-3 renomeado para `Gameplay_UI` com `GameplayHudController` bakeado. HUD re-assegurada em `GameState.Playing` e no spawn do boss. Sem bind, world-space permanece como fallback.

## Prefab

| Campo | Valor |
|-------|-------|
| Caminho | `Assets/Prefabs/Enemies/Rato_Boss.prefab` |
| Base | `Rato_Padrao_Resistente` |
| HP | 200 (`HealthComponent`) |
| Componentes | `BossEnemyMarker` + **`RatKingController`** + telegraph factory/relay |
| Animator | `AC_Rato_Rei.controller` |
| Rede | Registrado em `DefaultNetworkPrefabs.asset` |

## Animação (`AC_Rato_Rei`)

| Item | Caminho |
|------|---------|
| Controller | `Assets/Data/Animacoes/Enemy_AC/AC_Rato_Rei.controller` |
| Clips | `Assets/Art/Sprites/Animations/Enemies/Rato_Rei/` |
| Sprites | `Assets/Art/Sprites/Enemies/Rato rei/` |

Estados: `Idle`, `Running`, `Attacking`, `Spell` (projétil/faixas), `Charging` (investida), `TakingDamage`, `Dying`.

| Parâmetro | Tipo | Uso |
|-----------|------|-----|
| `OnSpell` | Trigger | Cast do ataque a distância |
| `OnCharge` | Trigger | Início da investida |
| `IsCharging` | Bool | Mantém `Charging` até a investida acabar |
| `OnAttack` | Trigger | Melee pós-dash |

Disparo de rede: `NetworkEnemyController.ServerNotifySpellCast` / `ServerNotifyChargeStart` / `ServerNotifyChargeEnd` / `ServerNotifyMeleeAttack`.

## Dados

- `Assets/Data/Stats/Game/Fase3.asset` — 1 inimigo, 1 onda
- Catálogo: `Resources/PhaseWaveSettingsCatalog.asset` → entrada `Fase-3`
- Behavior SO: criar via menu (ver guia) e atribuir em `RatKingController.config`
