# Prefab: Nixie

Última revisão: 2026-06-14  
**Caminho:** `Assets/Prefabs/Characters/Nixie.prefab`  
**GUID:** `7b87bef79bcba89408883a628d686c78`

## Resumo

Jogador **corpo a corpo**: ataque em trapézio (`PlayerMeleeCombat`), sem tiro. Dash e stack MP iguais à Cora.

## GameObject raiz

| Propriedade | Valor |
|-------------|--------|
| Nome | `Nixie` |
| Tag | `Player` |
| Layer | `Player` (3) |

## Hierarquia (filhos)

```
Nixie
├── Shadow
├── firePoint
├── Audios
├── DustParticiple
└── Particle System
```

## Scripts (raiz)

| Script | Notas |
|--------|--------|
| `PlayerInputHandler` | Fire = ataque melee |
| `PlayerMovement` | |
| `PlayerAim` | Direção do trapézio |
| `PlayerAbilityHandler` | `abilitySet` → NixAbilitySet; `unlockAllAbilitySlotsOnStart: false` (bloqueio por wave) |
| `NixPushAbilityExecutor` | Habilidade Q |
| `NixChargeAbilityExecutor` | Habilidade R — dano no corredor desde `Rigidbody2D.position` no início |
| `PlayerPassiveHandler` | Kill streak / cleave |
| `PlayerAbilityStatScaler` | Tiers do ataque normal |
| `NetworkPlayerAbilityRelay` | Sync animações MP |
| `NetworkAbilityObjectSpawner` | — (Nix não usa spawn) |
| `PlayerAnimationHandler` | |
| `PlayerDeathPresentation` | Morte: anim `Dying` completa → hold 5s (tempo real) → derrota; flip travado; dissolve só se aliado vivo (MP) |
| `HealthComponent` | `_maxHealth: 100`; `OnDied` sem dissolve (VFX via presentation) |
| `PlayerAdrenaline` | |
| `PlayerInitializer` | `playerShooting` vazio |
| `PlayerAudioController` | |
| `PlayerDash` | `passThroughLayer` = DashableWall + Player + ProjectileEnemy (`m_Bits: 4360`); dash **não** atravessa Enemy; `dashFailsafeExtraSeconds: 0.35` |
| `KnockbackReceiver` | |
| `PlayerMeleeCombat` | `combatStats` → MeleeCombatStats |
| `OwnerNetworkTransform` | |
| `NetworkPlayerController` | `shooting`/`ammo`/`meleeCombat` — **rewire no Inspector** se MP desativar melee em remotos |
| `NetworkPlayerHealth` | |
| `NetworkPlayerAdrenaline` | |
| `NetworkPlayerSpectator` | |
| `NetworkPlayerRevive` | |
| `Shadow`, `DissolveEffect` | `Shadow.Sombra` → `Assets/Prefabs/UI/Shadow.prefab` (ghosting dash); filho **Shadow** = elipse no chão |
| `PlayerGameplayModuleInstaller` | `installMeleeDebugVisual` + `installAbilityDebugVisual` |
| `AbilityDebugVisualHost` | Instalado em runtime; shader `AbilityZoneFill`; gizmos ON |

**Ausente vs Cora:** `PlayerShooting`, `PlayerAmmo`, `NetworkProjectileSpawner`.

## ScriptableObjects

| Campo | Asset |
|-------|--------|
| **`CharacterProfileApplier.profile`** | `Assets/Data/Characters/NixieGameplayProfile.asset` |
| `AnimatorProfileBinder.profile` | `Assets/Data/Characters/NixieAnimationProfile.asset` → `AC_NIXIE.controller` |
| `stats` (legado, espelhado pelo profile) | `Assets/Data/Stats/Player/PlayerCoreStats.asset` |
| `PlayerMeleeCombat.combatStats` (via profile) | `Assets/Data/Stats/Player/NixieMeleeCombatStats.asset` |
| `PlayerAbilityHandler.abilitySet` | `Assets/Data/Abilities/NixAbilitySet.asset` |
| `enemyLayers` | Layer Enemy (1024) |

## Debug / combate

- `MeleeAttackDebugVisual` — trapézio após cada golpe (auto via installer)
- Knockback: `NetworkEnemyController.ApplyKnockbackRpc` no servidor
- Ver [Player_Melee.md](Player_Melee.md) (guia de setup) e [diagnostics.md](../diagnostics.md)

## Multiplayer

- [x] `NetworkObject` + `OwnerNetworkTransform`
- Atribuir `meleeCombat` em `NetworkPlayerController` no prefab
- **Morte:** `NetworkPlayerHealth` → `PlayerDeathPresentation` (anim completa + 5s em tempo real; `PlayerFacingController` desliga). Game over após esse tempo → fade para derrota (`Route_Gameplay_Defeat`, 1s). Com aliado vivo: dissolve + câmera no parceiro.
- **Ratos na derrota (combo):** para spawn + slow-mo ~2s + fade dos ratos (3–8s) + vinheta/zoom no corpo (`DeathHordePresentation`). MP com aliado vivo: só slow-mo + foco no corpo até o dissolve.

## Histórico

| Data | Alteração |
|------|-----------|
| 2026-06-14 | Combo derrota: `DeathHordePresentation` (spawn off, slow-mo, fade ratos, vinheta/zoom) |
| 2026-06-12 | Fluxo de morte B: presentation + dissolve MP; bleed-out pausado |
| 2026-06-14 | Animador dedicado `AC_NIXIE.controller` + clips em `Assets/Art/Sprites/Animations/Nyxie/` via `NixieAnimationProfile` |
| 2026-06-14 | `CharacterProfileApplier` + `AnimatorProfileBinder`; SOs em `Assets/Data/Characters/` |
| 2026-05-22 | Doc completa a partir do YAML |
