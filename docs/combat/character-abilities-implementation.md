# Implementação — Habilidades de Personagem

Última revisão: 2026-06-30

## Arquitetura

| Componente | Responsabilidade |
|-----------|------------------|
| `PlayerAbilityHandler` | Orquestra inputs Q/R/Dash; bloqueio mútuo; cooldowns |
| `PlayerPassiveHandler` | Kill streak + timer da passiva |
| `PlayerAbilityStatScaler` | Escala ataque normal por tier |
| `IAbilityExecutor` + executores | Lógica por habilidade (Nix/Cora) |
| `NetworkPlayerAbilityRelay` | ClientRpc para animações em clientes remotos |
| `NetworkAbilityObjectSpawner` | Spawn servidor de barreira/poça |

## Inputs

| Ação | Tecla | Slot |
|------|-------|------|
| Ataque normal | LMB | `PrimaryAttack` |
| Habilidade 1 | Q | `Ability1` |
| Habilidade 2 | R | `Ability2` |
| Dash | Shift / Space | `Dash` (todos os personagens desde o início) |

## Dados (ScriptableObjects)

- `Assets/Data/Abilities/NixAbilitySet.asset`
- `Assets/Data/Abilities/CoraAbilitySet.asset`
- Definições individuais em `Assets/Data/Abilities/Definitions/`

## Rede

- **Dash:** predição local + `OwnerNetworkTransform`
- **Habilidades:** owner executa → `ReportAbilityActivatedServerRpc` → `ClientRpc` para animação + VFX remoto (`AbilityDebugVisualHost`)
- **Animação jogador (MP):** `NetworkPlayerAbilityRelay` replica `MoveSpeed`, sequência de ataque (`OnShoot`/melee) e `PlayRemoteShootImpactClientRpc` no disparo confirmado
- **Animação inimigo (MP):** `NetworkEnemyController` replica `MoveSpeed`, `flipX` e `OnAttack` via `NetworkVariable` (servidor publica)
- **Investida (Nix):** dano autoritativo no servidor via `ReportChargeDamageServerRpc` (funciona com owner host ou client)
- **Barreira/Poça:** `NetworkAbilityObjectSpawner` + `NetworkCoraBarrier.SyncInitializeClientRpc`; barreira usa layer **`Barrier`** + `BoxCollider2D` retangular; colisão 2D só com **Enemy** e **ProjectileEnemy** (matriz em Project Settings → Physics 2D); jogadores e projéteis do jogador atravessam; stun via overlap no `CoraBarrierNavMeshBlocker`
- **CC (slow/stun):** `ApplySlowRpc` / `ApplyStunRpc` no servidor

## Setup no Editor

### Prefabs configurados (2026-06-07)

| Prefab | Componentes wired |
|--------|-------------------|
| `Nixie.prefab` | Executores Nix, passiva, scaler, relay, spawner, `NixAbilitySet` |
| `Cora.prefab` | Executores Cora, passiva, scaler, relay, spawner, `CoraAbilitySet`, barreira/poça |
| `CoraBarrier.prefab` | `NetworkObject`, `NavMeshObstacle`, `BoxCollider2D` (layer `Barrier`) |
| `CoraDamagePool.prefab` | `NetworkObject`, dano em área |

`DefaultNetworkPrefabs.asset` inclui `CoraBarrier` e `CoraDamagePool`.

### Debug visual (Play Mode + Gizmos)

- Shader `MidnightMeow/AbilityZoneFill` em `Assets/Art/Shaders/AbilityZoneFill.shader`
- `AbilityDebugVisualHost` instalado via `PlayerGameplayModuleInstaller` nos prefabs Nixie/Cora
- Gizmos de dash desativados por padrão em `PlayerDash`
- Habilidades Q/R desbloqueadas no início da partida (`AbilityProgressionState` + `PlayerAbilityHandler.UnlockAllAbilitiesAtMatchStart`)
- Fire rate (Cora): `PlayerShooting` aplica cooldown rígido; animação de tiro só em `OnProjectileInstantiated`
- Gizmo de Poça/Barreira: duração do overlay = `effectDuration` do tier (`AbilityDebugVisualHost`)
- Investida (R): `NixChargeAbilityExecutor` avança o jogador; dano/gizmo ancorados na posição do corpo (`Rigidbody2D`) no início do avanço
- Empurrão (Q): knockback em `NixPushAbility.asset` (tiers 3.5 / 4.5 / 5.5 de distância)
- Dash: `DefaultPlayerStats` — `dashSpeed: 11`, `dashDuration: 0.35`; ignora colisão com `DashableWall`, `Enemy`, `Player`, `ProjectileEnemy` (`passThroughLayer` + merge em código)
- Projétil Cora: `DefaultProjectileStats.maxDistance: 80` (fallback fora do mapa); ignora layer `Player` (sem colidir com Nixie/aliado)

### Animator (ainda manual)

Adicionar triggers no controller de cada personagem: `OnAbility1`, `OnAbility2`, `OnDash`
