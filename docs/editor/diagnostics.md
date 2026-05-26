# Diagnóstico modular (combate / multiplayer)

Última revisão: 2026-05-22

## Objetivo

Testar fluxos (projétil → inimigo → vida em rede) **sem** `Debug.Log` espalhado no código.

## Setup no Editor (obrigatório para logs)

1. SO: `Assets/Data/Debug/GameplayDiagnosticConfig.asset` (canais `projectileHits`, `enemyDamage`, **`playerDash`**, **`meleeCombat`**, `cameraDiagnostics`, etc.).
2. Prefab **`MultiplayerManagers`**: componente **`GameplayDiagnosticListener`** com o SO em **Config** e **Use Config Asset** marcado.
3. Prefab **`MultiplayerCameraRig`**: **`MultiplayerCameraController`** → **Diagnostic Config** = mesmo SO; **Use Config Asset** ligado. Logs `[CAM-DIAG]` vêm daqui (desligados por padrão com `cameraDiagnostics = false`).

**Não é necessário** ligar UnityEvents no Inspector para dano/morte — o fluxo é 100% código + RPC.

---

## Checklist: layers e tags

| Objeto | Layer (índice) | Tag |
|--------|----------------|-----|
| Projétil (`Projectile.prefab`) | **Projectile** (7) | Untagged |
| Inimigo (raiz com collider) | **Enemy** (10) | **Enemy** (recomendado) |
| Jogador | **Player** (3) | **Player** |

Confirme em **Edit → Project Settings → Physics 2D** que **Projectile** colide com **Enemy** (o código também força isso no `Awake` do projétil).

---

## Checklist: prefab do inimigo (`Rato_Padrao_Base`, etc.)

| Componente | Obrigatório | Notas |
|------------|-------------|--------|
| `NetworkObject` | Sim | Spawn pela `NetworkWaveManager` |
| `NetworkEnemyController` | Sim | Autoridade de vida |
| `HealthComponent` | Sim | Não usar **Allow Destroy On Death** em MP (o script desliga no `Awake`) |
| `EnemyHealthConfig` | Sim | Campo **Stats** → SO `EnemyStats` (ex. `Rato_Padrao_Base.asset`) |
| `EnemyAnimationHandler` | Sim | Animator com triggers **`OnTakeDamage`** e **`OnDie`** |
| `CapsuleCollider2D` | Sim | **Is Trigger = off** no corpo que recebe tiro |
| `EnemyHitStun` | Opcional | Parada ao tomar dano |

**UnityEvents** em `HealthComponent` (OnHealthChanged / OnDied) podem ficar **vazios** — animação em rede usa `ClientRpc` do `NetworkEnemyController`.

---

## Checklist: prefab do projétil (`Assets/Prefabs/Combat/Projectile.prefab`)

| Componente | Notas |
|------------|--------|
| `NetworkObject` + `NetworkProjectileController` | Spawn pelo `NetworkProjectileSpawner` |
| `Projectile` | **Stats** → SO com campo **damage** |
| 2× `CircleCollider2D` | **Sólido** = dano + ricochete em inimigos e paredes; **trigger** = coleta de munição (Player). Sem Exclude Layers em Enemy |
| Comportamento esperado | 1º hit no inimigo: dano + hit stun (servidor) + ricochete se `currentBounces < maxBounces`; senão despawn. Logs: `[ProjectileHit] NetworkEnemy applied=True` + `[EnemyDamage] hp=X->Y` |
| `[Rejected_EnemyNotSpawned]` | Inimigo veio do **WaveGenerator** (`Instantiate` sem `Spawn`). Em MP desative **NightManager** automático — só **NetworkWaveManager** deve spawnar inimigos. |
| `[PlayerDash] stage=complete` | Dash terminou; se travar sem `complete`, ver `failsafe-timeout` |
| `[PlayerDash] rejected-cooldown` | Botão dentro do cooldown (após o fim do dash anterior) |
| `[PlayerDash] rejected-invalid-stats` | `dashDuration` ou `dashSpeed` zerados no SO |
| `[MeleeHit] ... applied` | Acerto no trapézio melee |
| `NetworkProjectileSpawner` → prefab | Deve ser **`Projectile.prefab`** (com colliders), não `NetworkProjectile.prefab` (sem colliders) |

---

## Valores de balanceamento (ScriptableObjects)

| Asset | Campo | Valor atual (exemplo rato base) |
|-------|--------|----------------------------------|
| `Assets/Data/Stats/Enemies/Rato_Padrao_Base.asset` | **maxHealth** | **2** |
| `Assets/Data/Stats/Projectiles/DefaultProjectileStats.asset` | **damage** | **1** |

Com esses valores o rato morre em **2 acertos** (2 → 1 → 0). Se parecer que “não morre”, pode ser despawn rápido (~0,35 s) ou vida repondo por spawn novo da wave.

Para testes mais visíveis, suba **maxHealth** no SO do inimigo ou **damage** no SO do projétil.

---

## Interpretação dos logs

| Log | Significado |
|-----|-------------|
| `[NetworkEnemy] applied=True ... hp=1/2` | Hit OK; vida no servidor após o tiro |
| `[EnemyDamage] hp=2->1 \| OK` | Dano autoritativo confirmado |
| `[EnemyDamage] ... \| REJECTED: ...` | Hit detectado mas vida não mudou — leia o motivo |
| `[Ignored_Client]` | Normal no cliente remoto |
| Sem `[EnemyDamage]` | Canal desligado no SO ou listener sem Config |

## Emissores

- `Projectile` → `ProjectileHits`
- `NetworkProjectileController` → `ProjectileNetwork`
- `NetworkEnemyController` → `EnemyDamage` (sucesso e rejeição)

---

## Ciência (drop ao matar)

1. **Create** → `Config/Ciencia Pickup Config` → salvar em `Assets/Data/Multiplayer/`
2. Prefab **`NetworkCiencia`**: adicionar `CienciaHoming` + referenciar SO em `NetworkCienciaController.Pickup Config`
3. **`NetworkWaveManager`** → campo **Network Ciencia Prefab** preenchido
4. **`Rato_Padrao_Base.asset`** (EnemyStats) → `dropChance`, `cienciaPrefab`, `minCienceDrop` / `maxCienceDrop`

Homing: dentro de `homingRadius`, o **servidor** move o pickup em direção ao jogador mais próximo (`homingSpeed`). Coleta em `collectRadius` (servidor, `FixedUpdate` + trigger).

**Prefab em produção:** `Science.prefab` (Fase-1 `networkCienciaPrefab`), não o `NetworkCiencia.prefab` vazio.
