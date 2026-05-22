# Documentação do Editor Unity

Agentes de IA **não veem o Inspector**. Esta pasta descreve prefabs, cenas e assets críticos.

**Status da documentação:** tabelas marcadas com *(confirmar no Editor)* devem ser preenchidas por você após revisar o Inspector.

## Índice de prefabs

### Personagens e combate

| Prefab | Doc |
|--------|-----|
| Player | [prefabs/Player.md](prefabs/Player.md) |
| Enemy | [prefabs/Enemy.md](prefabs/Enemy.md) |
| Ratos (variantes) | [prefabs/Rato-variants.md](prefabs/Rato-variants.md) |
| Projectile | [prefabs/Projectile.md](prefabs/Projectile.md) |
| NetworkProjectile | [prefabs/NetworkProjectile.md](prefabs/NetworkProjectile.md) |
| EnemyProjectile | [prefabs/EnemyProjectile.md](prefabs/EnemyProjectile.md) |

### Multiplayer

| Prefab | Doc |
|--------|-----|
| Lobby | [prefabs/Lobby.md](prefabs/Lobby.md) |
| MultiplayerManagers | [prefabs/MultiplayerManagers.md](prefabs/MultiplayerManagers.md) |
| MultiplayerGameManager | [prefabs/MultiplayerGameManager.md](prefabs/MultiplayerGameManager.md) |
| NetworkManager | [prefabs/NetworkManager.md](prefabs/NetworkManager.md) |
| PlayerSpawnManager | [prefabs/PlayerSpawnManager.md](prefabs/PlayerSpawnManager.md) |
| WaveSystem | [prefabs/WaveSystem.md](prefabs/WaveSystem.md) |
| MultiplayerCameraRig | [prefabs/MultiplayerCameraRig.md](prefabs/MultiplayerCameraRig.md) |
| NetworkCiencia | [prefabs/NetworkCiencia.md](prefabs/NetworkCiencia.md) |

### UI e coletáveis

| Prefab | Doc |
|--------|-----|
| PauseMenu | [prefabs/PauseMenu.md](prefabs/PauseMenu.md) |
| Gameplay_UI | [prefabs/Gameplay_UI.md](prefabs/Gameplay_UI.md) |
| Science | [prefabs/Science.md](prefabs/Science.md) |
| Defeat / Victory / Controls / Shadow | [prefabs/UI-misc.md](prefabs/UI-misc.md) |

### Ambiente

| Prefab | Doc |
|--------|-----|
| House | [prefabs/House.md](prefabs/House.md) |
| NavMesh / Wall / Walkable | [prefabs/Environment.md](prefabs/Environment.md) |

### Legado (não usar em produção)

| Prefab | Caminho |
|--------|---------|
| Player Variant | `Assets/Prefabs/_Legacy/oLD/Player Variant.prefab` |
| Shadow Variant, câmeras antigas | `Assets/Prefabs/_Legacy/oLD/` |

## Template

[_template-prefab.md](_template-prefab.md)

## Contexto global

- [project-context.md](project-context.md)
- [scenes.md](scenes.md)
