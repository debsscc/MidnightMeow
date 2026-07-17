# Guia Editor — Rei Rato (Boss FSM + Telegraph Tronco de Cone)

Última revisão: 2026-07-17

Setup manual após o código das Fases 1–4. **Não** instancia prefabs por script: tudo abaixo é no Unity Inspector.

> **Anti-tunneling (Fuga / Investida):** após pular o código de CircleCast + MovePosition, siga também [rat-king-anti-tunneling.md](rat-king-anti-tunneling.md) (`obstacleLayer`, Continuous, raio do Cast).

## Checklist rápido

1. Criar asset `RatKingBehaviorConfig`
2. Adicionar `RatKingController` ao prefab `Rato_Boss`
3. Confirmar parâmetros do Animator `AC_Rato_Rei`
4. (Opcional) Prefab de zona telegraph se quiser override visual
5. Smoke test em `Fase-3` (solo + host/cliente)

---

## 1. ScriptableObject — `RatKingBehaviorConfig`

### Criar o asset

1. Project: pasta sugerida `Assets/Data/Combat/` (ou `Assets/Data/Combat/Boss/`)
2. RMB → **Create → MidnightMeow → Combat → Rat King Behavior Config**
3. Nome sugerido: `RatKing_ReiRato_Behavior`

### Campos e defaults recomendados

| Grupo | Campo | Default | Função |
|-------|--------|---------|--------|
| Roleta | `rangedWeight` | **70** | Chance relativa do ataque a distância |
| Roleta | `chargeWeight` | **30** | Chance relativa da investida |
| Roleta | `decisionPause` | 0.35 | Pausa entre ataques (após terminar um) |
| Fuga | `minFleeTime` / `maxFleeTime` | **1 / 4** | Fallback de tempo; early-exit pode cortar antes |
| Fuga | `maxRangedDistance` | **8** | Alcance máx. do ataque a distância (casar com o telegraph) |
| Fuga | `fleeDistanceThreshold` | **0.75** | Interrompe fuga ao atingir 75% de `maxRangedDistance` |
| 5 faixas | `rangedAngle1` / `rangedAngle2` | **30 / 60** | Ângulos ± → disparos em -60, -30, 0, +30, +60 |
| 5 faixas | `rangedLaneWidth` / `rangedLaneLength` | 0.85 / 8 | Retângulos do telegraph (`rangedLaneLength` ≈ `maxRangedDistance`) |
| 5 faixas | `rangedFillDuration` | 0.9 | Fill Time (janela de esquiva) |
| 5 faixas | `rangedDamage` | 12 | Dano ao completar o fill |
| 5 faixas | `rangedDamageLayers` | **Player** | Layer mask |
| 5 faixas | `rangedVisualStyle` | (opcional) | `EnemyTelegraphVisualStyle` |
| Investida | `chargeRange` | 7 | Comprimento do dash / telegraph |
| Investida | Aproximação para em `chargeRange * 0.6` | — | Hardcoded no controller |
| Investida | `chargeApproachSpeedMultiplier` | 1.75 | Buff de velocidade na aproximação |
| Investida | `chargeDashSpeed` | 18 | Velocidade do dash |
| Investida | `chargeWindupDuration` | **1** | Charge-up = fill do telegraph da trajetória |
| Investida | `chargeLaneWidth` / `chargeDashDamage` | 1.1 / 15 | Hitbox e dano no trajeto |
| Melee | `meleeInnerRadius` | 0.35 | Raio menor (base no boss) |
| Melee | `meleeOuterRadius` | 1.8 | Raio maior (ponta) |
| Melee | `meleeLength` | 2.4 | Altura / alcance |
| Melee | `meleeOpeningAngleDegrees` | 40 | Meio-ângulo (usado se outer ≤ 0) |
| Melee | `meleeFillDuration` / `meleeDamage` | 0.45 / 18 | Fill + dano |

A roleta normaliza pelos pesos: `70/(70+30)` = 70% ranged.

### Prefab `Rato_Boss`

1. Abrir `Assets/Prefabs/Enemies/Rato_Boss.prefab`
2. **Add Component → `RatKingController`**
3. Arrastar o SO em `Config`
4. `Attack Origin`: leave empty (usa o transform) ou um child `FirePoint`
5. `Telegraph Factory` / `Telegraphed Attacker`: auto via `GetComponent` se já existirem no prefab
6. Deixe o pattern genérico do `EnemyTelegraphedAttacker` **vazio ou desabilitado** — o boss desliga o attacker automático

Componentes que já devem existir no boss:

- `BossEnemyMarker`
- `NetworkEnemyController`
- `EnemyMovement` + `NavMeshAgent`
- `EnemyTelegraphZoneFactory`
- `NetworkEnemyTelegraphRelay`
- `HealthComponent` (200 HP)

### Calibrar early-exit da fuga (`maxRangedDistance` / `fleeDistanceThreshold`)

1. Abra o asset **RatKingBehaviorConfig** referenciado em `Rato_Boss` → `RatKingController.config` (não o prefab em si, a menos que o SO esteja inline).
2. No header **Ataque a distância — fuga**:
   - `maxRangedDistance` → mesmo valor de `rangedLaneLength` (comprimento do telegraph das 5 faixas). Se as faixas medem 8 unidades, use **8**.
   - `fleeDistanceThreshold` → **0.75** (para em ~6u se o alcance for 8).
3. Como validar o tamanho visual: em Play Mode, observe o comprimento amarelo/vermelho das faixas; meça aproximado na Scene View (ou use o valor já configurado em `rangedLaneLength`) e copie para `maxRangedDistance`.
4. Se o boss ainda foge demais: baixe o threshold (ex.: 0.6). Se atacar cedo demais: suba para 0.85–0.9.

---

## 2. Animator — `AC_Rato_Rei`

Controller: `Assets/Data/Animacoes/Enemy_AC/AC_Rato_Rei.controller`

### Parâmetros (criar se faltar)

| Nome | Tipo | Quem dispara |
|------|------|----------------|
| `MoveSpeed` | Float | Já existe (movimento) |
| `IsAttacking` | Bool | Já existe / boss busy |
| `OnAttack` | Trigger | Já existe — follow-up melee |
| `OnTakeDamage` / `OnDie` | Trigger | Já existem |
| `OnSpell` | **Trigger** | Ataque a distância (5 faixas) |
| `OnCharge` | **Trigger** | Início do charge-up da investida |
| `IsCharging` | **Bool** | `true` no charge-up/dash; `false` ao terminar |

### Transições sugeridas

| De | Para | Condição |
|----|------|----------|
| Any State / Idle/Run | `Spell` | `OnSpell` |
| Any State | `Charging` | `OnCharge` **ou** `IsCharging == true` |
| `Charging` | Idle/Run | `IsCharging == false` |
| Any State | `Attacking` | `OnAttack` (melee pós-dash) |

O servidor replica via `NetworkVariable` em `NetworkEnemyController` (`ServerNotifySpellCast` / `ChargeStart` / `ChargeEnd` / `MeleeAttack`). Clientes remotos **não** precisam de RPC extra se os parâmetros existirem no controller.

---

## 3. Telegraph — Tronco de Cone (`ConeFrustum`)

### O que o código já faz

- Shape enum: `TelegraphShapeType.ConeFrustum`
- Shader `MidnightMeow/TelegraphFill` com `_Shape = 2`, `_ConeInnerRatio`, `_ConeOuterRatio`
- Overlap: AABB + filtro `TelegraphConeFrustumUtility.ContainsPoint` (mesma silhueta do visual)
- Snapshot NGO inclui raios/ângulo do cone

### Prefab de zona (opcional)

Se quiser um prefab dedicado em vez do runtime da factory:

1. Criar GO vazio `TelegraphZone_ConeFrustum`
2. Add:
   - `SpriteRenderer` (sprite branco 1×1 ou o mesmo da factory)
   - `EnemyTelegraphZoneView`
   - `EnemyTelegraphZoneInstance` (opcional — factory adiciona se faltar)
3. Material: usar template `Resources/TelegraphZoneMaterial` (shader `TelegraphFill`) — o `EnemyTelegraphZoneView` instancia material em runtime; **não** precisa configurar shader no prefab se o Resources existir
4. Atribuir em `EnemyTelegraphZoneFactory.zonePrefab` no boss (ou deixar null para criar em runtime)

### Configurar um strike `ConeFrustum` em um Pattern SO (opcional)

Para usar fora do boss (ou override):

1. Create `Enemy Attack Pattern`
2. Strike:
   - `shape` = **ConeFrustum**
   - `size.y` = comprimento
   - `coneInnerRadius` = raio menor (base)
   - `coneOuterRadius` = raio maior (ponta); se 0, deriva de `coneOpeningAngleDegrees`
   - `coneOpeningAngleDegrees` = meio-ângulo (graus)
   - `fillDuration`, `damage`, `damageLayers`
   - `anchorToTargetOnStart` = false (sai do boss)
   - `aimAtTarget` = true (ou false se a rotação for setada no código)

Gizmos: selecione a zona no Scene view (Edit Mode) — `OnDrawGizmosSelected` desenha o trapézio.

---

## 4. Teste de regressão

- [ ] Solo Fase-3: boss foge e dispara 5 faixas amarelo→vermelho
- [ ] Roleta ~70/30 ao longo de vários ciclos
- [ ] Investida: aproximação rápida → telegraph retangular (encurtado se houver parede) → dash via MovePosition → cone melee
- [ ] Fuga contra parede: não atravessa; early-attack ou deslize tangencial ([anti-tunneling](rat-king-anti-tunneling.md))
- [ ] Dash do player durante o dash do boss: **sem dano** (i-frames)
- [ ] Cliente remoto: vê telegraphs e animações Spell/Charging
- [ ] Morte do boss encerra o brain (sem erro no Console)

## Docs relacionadas

- [boss-phase.md](../../gameplay/boss-phase.md)
- [enemy-telegraph-attacks.md](../../combat/enemy-telegraph-attacks.md)
- Prefab: `Assets/Prefabs/Enemies/Rato_Boss.prefab`
