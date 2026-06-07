# Implementação — Habilidades de Personagem

Última revisão: 2026-06-07

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
- **Habilidades:** owner executa → `ReportAbilityActivatedServerRpc` → `ClientRpc` para animação remota
- **Barreira/Poça:** `NetworkAbilityObjectSpawner` com `INetworkSerializable` compacto
- **CC (slow/stun):** `ApplySlowRpc` / `ApplyStunRpc` no servidor

## Setup no Editor

### Prefabs configurados (2026-06-07)

| Prefab | Componentes wired |
|--------|-------------------|
| `Nixie.prefab` | Executores Nix, passiva, scaler, relay, spawner, `NixAbilitySet` |
| `Cora.prefab` | Executores Cora, passiva, scaler, relay, spawner, `CoraAbilitySet`, barreira/poça |
| `CoraBarrier.prefab` | `NetworkObject`, `NavMeshObstacle`, stun trigger |
| `CoraDamagePool.prefab` | `NetworkObject`, dano em área |

`DefaultNetworkPrefabs.asset` inclui `CoraBarrier` e `CoraDamagePool`.

### Debug visual (Play Mode + Gizmos)

- Shader `MidnightMeow/AbilityZoneFill` em `Assets/Art/Shaders/AbilityZoneFill.shader`
- `AbilityDebugVisualHost` instalado via `PlayerGameplayModuleInstaller` nos prefabs Nixie/Cora
- Gizmos ligados por padrão (`drawDebugGizmos = true`) em dash, executores e melee debug
- Sandbox: `unlockAllAbilitySlotsOnStart = true` no `PlayerAbilityHandler` até existir UI de progressão

### Animator (ainda manual)

Adicionar triggers no controller de cada personagem: `OnAbility1`, `OnAbility2`, `OnDash`
