# Prefabs: Variantes de Rato

Última revisão: 2026-07-10

Variantes compartilham a mesma hierarquia base (`Enemy` / ranged): `NetworkObject`, `NetworkEnemyController`, `EnemyMovement`, `EnemyAttack_Ranged` (ou melee em tipos especiais), `EnemyDropHandler`, etc.

## Animação (horde)

Sprites desenhados olhando para a **direita**; `EnemyAnimationHandler` / `NetworkEnemyController` aplicam `flipX = !facingRight` (mesma convenção dos players).

Controllers em `Assets/Data/Animacoes/Enemy_AC/` (`AC_Enemy`, `AC_Enemy_Acid`, `AC_Enemy_Helmet`, `AC_Rato_Rei`):

- **Default state (horde):** `Running` (não `Idle`)
- **`IsAttacking` (bool):** telegraph/pattern ativo (`EnemyTelegraphedAttacker.IsExecuting`)
- **`Attacking` → locomotion:** só quando `IsAttacking == false`
- Profile: `EnemyDefaultAnimationProfile.asset` → `isAttackingParameter: IsAttacking`
- **Boss (`Rato_Boss`):** `AC_Rato_Rei` — default `Idle`; extras `Spell`/`OnSpell` (projétil) e `Charging`/`OnCharge`+`IsCharging` (investida). Ver [boss-phase.md](../../gameplay/boss-phase.md).

**Morte:** `DissolveEffect` + material `EnemyDeathFade.mat` — animação `Dying` completa, depois fade out (sem reaparecer).

## Telegraph (MP)

Cada `Rato_*` inclui `EnemyTelegraphModuleInstaller` com pattern SO em `Assets/Data/Combat/Patterns/`. Em runtime o instalador adiciona `EnemyTelegraphedAttacker`, `EnemyTelegraphZoneFactory` e `NetworkEnemyTelegraphRelay`; o `ClientRpc` de visual fica em `NetworkEnemyController` (ver [enemy-telegraph-attacks.md](../../combat/enemy-telegraph-attacks.md)).

| Prefab | Caminho | EnemyStats asset |
|--------|---------|------------------|
| Rato_Padrao_Base | `Assets/Prefabs/Enemies/Rato_Padrao_Base.prefab` | `Rato_Padrao_Base.asset` |
| Rato_Padrao_Veloz | `Assets/Prefabs/Enemies/Rato_Padrao_Veloz.prefab` | `Rato_Padrao_Veloz 1.asset` |
| Rato_Padrao_Resistente | `Assets/Prefabs/Enemies/Rato_Padrao_Resistente.prefab` | `Rato_Padrao_Resistente.asset` |
| Rato_Eletrico | `Assets/Prefabs/Enemies/Rato_Eletrico.prefab` | `Rato_Eletrico.asset` |
| Rato_Acido | `Assets/Prefabs/Enemies/Rato_Acido.prefab` | `Rato_Acido.asset` |
| Enemy 1 | `Assets/Prefabs/Enemies/Enemy 1.prefab` | Legado — preferir `Rato_Padrao_Base` |

### Velocidade (`moveSpeed` em EnemyStats)

| Asset | moveSpeed (2026-06-28) |
|-------|------------------------|
| Rato_Padrao_Base | 5,5 |
| Rato_Padrao_Veloz | 7 |
| Rato_Padrao_Resistente | 6,2 |
| Rato_Acido | 3,2 |
| Rato_Eletrico | 8,5 |

Ajuste fino em `Assets/Data/Stats/Enemies/*.asset` — não hardcodar no `EnemyMovement`.

## Física vs player

- `EnemyPhysicsBody`: `Rigidbody2D` **Kinematic** — bloqueia o player (não entra dentro do rato) sem ser empurrado pelo motor do jogador.
- NavMesh continua movendo o `transform`; o RB kinematic sincroniza em `FixedUpdate`.
- Colisão **Player–Enemy permanece ativa** (não é ignore layer).

## GameObject raiz (comum)

| Propriedade | Valor |
|-------------|--------|
| Tag | `Enemy` |
| Layer | `Enemy` (11) |

## Ataque à distância

| Prefab | `projectilePrefab` |
|--------|----------------------|
| Rato_Padrao_Base, Veloz, Eletrico | `EnemyProjectile.prefab` (`guid: 6ededdb4f3eb7e143ae0a036319f5fd3`) via `EnemyAttack_Ranged` |
| Rato_Acido, Resistente | `EnemyAttack_Ranged` alternativo (`guid: 5be99fa3d9900c04f841392385f87d10`) + mesmo projétil |

## Ciência (drop MP)

`Rato_Padrao_Base.asset` define `minCienceDrop: 1` e referência ao prefab **Science** (`41457ddb`) para spawn via `NetworkWaveManager`.

## Ondas

Entradas em `WaveSettings` / instância `_GameLoop` em **Fase-1** referenciam estes prefabs — não o prefab `WaveSystem` isolado (campos `waveSettings` / `spawnPoints` vazios no asset).

## SFX (ratos comuns)

Clips em `Assets/Audio/AUDIOS ATUALIZADOS/SFXS/Ratos/`; config central em `Assets/Resources/EnemyCommonSfxConfig.asset`. Prefabs `Rato_*` deixam `damageClip`/`deathClip` vazios no Inspector — o `EnemyAudioController` resolve em runtime. Todos os SFX passam pelo bus global `EnemySfxBus` (grupo **SFX** do `MidnightMeowAudioMixer`).

## Relacionados

- [Enemy.md](Enemy.md) — template genérico `Enemy.prefab`
- [EnemyProjectile.md](EnemyProjectile.md)
