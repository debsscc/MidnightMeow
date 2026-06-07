# Prefab: Cora

Última revisão: 2026-06-07  
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
| `CoraBarrierAbilityExecutor` | Habilidade Q |
| `CoraPoolAbilityExecutor` | Habilidade R |
| `PlayerPassiveHandler` | Kill streak / ricochete |
| `PlayerAbilityStatScaler` | Tiers do ataque normal |
| `NetworkPlayerAbilityRelay` | Sync animações MP |
| `NetworkAbilityObjectSpawner` | Spawn barreira/poça em rede |
| `PlayerAnimationHandler` | |
| `HealthComponent` | `_maxHealth: 100`, `_destroyDelay: 4` |
| `PlayerAdrenaline` | |
| `SpriteBlink` | |
| `PlayerInitializer` | Progressão + upgrades |
| `PlayerAudioController` | |
| `PlayerDash` | `passThroughLayer` = DashableWall (2048) |
| `KnockbackReceiver` | |
| `OwnerNetworkTransform` | |
| `NetworkPlayerController` | Refs wired (shooting, ammo, etc.) |
| `NetworkPlayerHealth` | |
| `NetworkPlayerAdrenaline` | |
| `NetworkPlayerSpectator` | |
| `NetworkProjectileSpawner` | |
| `MultiplayerCombatIntegrityLogger` | |
| `NetworkPlayerRevive` | |
| `Shadow` | Sombra local |
| `DissolveEffect` | |
| `PlayerGameplayModuleInstaller` | Imunidade + UI downed/revive + `installAbilityDebugVisual` |
| `AbilityDebugVisualHost` | Instalado em runtime; shader `AbilityZoneFill`; gizmos ON |

**Ausente vs Nixie:** `PlayerMeleeCombat`, `MeleeAttackDebugVisual`.

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| `stats` / `baseStats` | `Assets/Data/Stats/Player/DefaultPlayerStats.asset` |
| `PlayerAbilityHandler.abilitySet` | `Assets/Data/Abilities/CoraAbilitySet.asset` |
| `barrierPrefab` | `Assets/Prefabs/Combat/CoraBarrier.prefab` |
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

## Histórico

| Data | Alteração |
|------|-----------|
| 2026-05-22 | Doc criada a partir do YAML (substitui Player.prefab legado) |
