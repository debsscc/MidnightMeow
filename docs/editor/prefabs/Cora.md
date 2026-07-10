# Prefab: Cora

Última revisão: 2026-07-09  
**Caminho:** `Assets/Prefabs/Characters/Cora.prefab`  
**GUID:** `b18ed4e45e4d20a4dbdac339b666e689`

## Resumo

Jogador **à distância**: tiro, munição, spawn de `Projectile` em rede. Prefab padrão do `PlayerSpawnManager`.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Nome | `Cora` |
| Tag | `Player` |
| Layer | `Player` (3) |

## Hierarquia (filhos)

```
Cora
├── Shadow
├── firePoint
├── Audios
├── DustParticiple
└── Particle System
```

## Scripts (raiz)

| Script | Notas |
|--------|--------|
| `PlayerInputHandler` | Input System |
| `PlayerMovement` | `stats` → DefaultPlayerStats |
| `PlayerAmmo` | |
| `PlayerShooting` | `projectilePrefab` → Projectile.prefab |
| `PlayerAim` | `firePoint` |
| `PlayerAbilityHandler` | `abilitySet` → CoraAbilitySet; prefabs barreira/poça |
| `CoraBarrierAbilityExecutor` | Habilidade Q — posição do mouse + rotação pela mira (`AbilityPlacementUtility.RotationFromDirection`) |
| `CoraPoolAbilityExecutor` | Habilidade R |
| `PlayerPassiveHandler` | Kill streak / ricochete |
| `PlayerAbilityStatScaler` | Tiers do ataque normal |
| `NetworkPlayerAbilityRelay` | Sync animações MP |
| `NetworkAbilityObjectSpawner` | Spawn barreira/poça em rede |
| `PlayerAnimationHandler` | |
| `PlayerDeathPresentation` | Morte: anim `Dying` completa → hold 5s (tempo real) → derrota; flip travado; dissolve só se aliado vivo (MP) |
| `HealthComponent` | `_maxHealth: 100`, `_destroyDelay: 4`; `OnDied` sem dissolve |
| `PlayerAdrenaline` | |
| `SpriteBlink` | |
| `PlayerInitializer` | Progressão + upgrades |
| `PlayerAudioController` | SFX via `CoraPlayerAudioConfig` (aplicado pelo profile) → mixer **SFX** |
| `PlayerDash` | `passThroughLayer` = DashableWall + Player + ProjectileEnemy (`m_Bits: 4360`); dash **não** atravessa Enemy |
| `KnockbackReceiver` | |
| `OwnerNetworkTransform` | |
| `NetworkPlayerController` | Refs wired (shooting, ammo, etc.) |
| `NetworkPlayerHealth` | |
| `NetworkPlayerAdrenaline` | |
| `NetworkPlayerSpectator` | |
| `NetworkProjectileSpawner` | |
| `MultiplayerCombatIntegrityLogger` | |
| `NetworkPlayerRevive` | |
| `Shadow` | Ghosting de dash (`Sombra` → `Assets/Prefabs/UI/Shadow.prefab`); filho **Shadow** = elipse no chão |
| `DissolveEffect` | |
| `PlayerGameplayModuleInstaller` | Imunidade + UI downed/revive + `installAbilityDebugVisual` |
| `AbilityDebugVisualHost` | Instalado em runtime; shader `AbilityZoneFill`; gizmos ON |

**Ausente vs Nixie:** `PlayerMeleeCombat`, `MeleeAttackDebugVisual`.

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| **`CharacterProfileApplier.profile`** | `Assets/Data/Characters/CoraGameplayProfile.asset` |
| `AnimatorProfileBinder.profile` | `Assets/Data/Characters/CoraAnimationProfile.asset` → `AC_CORA.controller` |
| `stats` / `baseStats` (legado, espelhado pelo profile) | `Assets/Data/Stats/Player/PlayerCoreStats.asset` |
| `CoraRangedCombatStats` (via profile) | `Assets/Data/Characters/CoraRangedCombatStats.asset` — **attackRange**, fireRate, **attackAnimClipLength** (0,517) |
| `audioConfig` (via profile) | `Assets/Data/Audio/Player/Cora/CoraPlayerAudioConfig.asset` |

## SFX (Cora)

Config em `Assets/Data/Audio/Player/Cora/CoraPlayerAudioConfig.asset` — todos roteados ao grupo **SFX** via `GameAudioSettings.BindSfxOutput`. Sem SFX de morte por enquanto.

| Evento | Clip | Disparo |
|--------|------|---------|
| Ataque normal (tiro) | `Cora Ataque Normal.wav` | `PlayerShooting.OnProjectileInstantiated` |
| Dash | `Cora Dash.wav` | `PlayerDash.OnDashStarted` |
| Barreira (Q) | `Cora Barreira.wav` | `PlayerAbilityHandler` → `CoraBarrier` |
| Poça (R) | `Cora Poca.wav` | `PlayerAbilityHandler` → `CoraPool` |
| Tomando dano | `Cora Tomando Dano.wav` | `HealthComponent` (solo) / `NetworkPlayerHealth` ClientRpc (MP) |

## Timing do tiro (sync com animação)

| Item | Função |
|------|--------|
| `CoraAnimationProfile.attackClip` | `Cora_Base_Attack` — **fonte única** do timing via evento `PerformFire` |
| `attackAnimClipLength` | **0,517** — espelha duração do clip; alimenta `AttackSpeed` |
| Animation Event `PerformFire()` | No clip (~0,233 s) em **`PlayerAnimationHandler`** (mesmo GO do Animator) |
| Fallback | Só após o frame do evento, via `normalizedTime`; timeout = fim do clip |

**Cadência vs animação:** `Animator.speed = clipLength × fireRate` no estado `Shooting` — acelera o clip (não o float `AttackSpeed`).

**Facing ao atacar:** snap para a mira no início; flip **travado até o clip terminar**; depois mouse/movimento.

**Ajustar sync:** mover o evento `PerformFire` na timeline de `Cora_Base_Attack` (função no `PlayerAnimationHandler`).

| Campo | Asset |
|-------|--------|
| `PlayerAbilityHandler.abilitySet` | `Assets/Data/Abilities/CoraAbilitySet.asset` |
| `barrierPrefab` | `CoraBarrier.prefab` — `NetworkCoraBarrier`, `BoxCollider2D` (layer `Barrier`), `NavMeshObstacle.carveOnlyStationary: 0`, rotação Z via `NetworkTransform` |
| `poolPrefab` | `Assets/Prefabs/Combat/CoraDamagePool.prefab` |
| `progressionData` | Instância no prefab (GUID `b87f7c79296088641991071b4e517b5c`) |
| `NetworkPlayerHealth` / revive | `MultiplayerConfig`, `DownedPlayerConfig` |

## Prefabs referenciados

| Campo | Prefab |
|-------|--------|
| `PlayerShooting.projectilePrefab` | `Assets/Prefabs/Combat/Projectile.prefab` |
| `NetworkProjectileSpawner` | Mesmo Projectile |

## Multiplayer

- [x] `NetworkObject` + `OwnerNetworkTransform`
- Dono: movimento, mira, tiro; servidor valida projéteis via `NetworkProjectileSpawner`
- **Morte:** mesmo fluxo que Nixie (`PlayerDeathPresentation`: anim + 5s → fade derrota; flip travado na queda)
- **Ratos na derrota:** combo via `DeathHordePresentation` (ver Nixie.md)

## Histórico

| Data | Alteração |
|------|-----------|
| 2026-06-21 | Tiro sincronizado via Animation Event `PerformFire` em `Cora_Base_Attack`; `attackAnimClipLength` 0,517 |
| 2026-06-14 | Animador dedicado `AC_CORA.controller` + clips em `Assets/Art/Sprites/Animations/Cora/` via `CoraAnimationProfile` |
| 2026-06-14 | `CharacterProfileApplier` + `AnimatorProfileBinder`; SOs em `Assets/Data/Characters/` |
| 2026-05-22 | Doc criada a partir do YAML (substitui Player.prefab legado) |
