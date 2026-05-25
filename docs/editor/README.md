# Documentação do Editor Unity

Agentes de IA **não veem o Inspector**. Esta pasta descreve prefabs, cenas e assets críticos com valores lidos dos YAMLs do projeto (última varredura: 2026-05-22).

## Índice de prefabs

### Personagens jogáveis

| Prefab | Doc | Papel |
|--------|-----|--------|
| Cora | [prefabs/Cora.md](prefabs/Cora.md) | Ranged (tiro + projétil rede) — **GUID spawn MP** `b18ed4e45e4d20a4dbdac339b666e689` |
| Nixie | [prefabs/Nixie.md](prefabs/Nixie.md) | Melee (trapézio + knockback servidor) |
| Player (legado) | [prefabs/Player.md](prefabs/Player.md) | Índice; prefab `Player.prefab` removido — usar Cora/Nixie |

### Inimigos e combate

| Prefab | Doc |
|--------|-----|
| Enemy (ranged) | [prefabs/Enemy.md](prefabs/Enemy.md) |
| Rato (variantes) | [prefabs/Rato-variants.md](prefabs/Rato-variants.md) |
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
| Science (ciência MP) | [prefabs/Science.md](prefabs/Science.md) |
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

## Layers (referência)

| Índice | Nome |
|--------|------|
| 0 | Default |
| 3 | Player |
| 5 | UI |
| 6 | Wall |
| 7 | Projectile |
| 8 | Structure |
| 10 | Collectable (tag `Drop` na Science) |
| 11 | Enemy |
| 12 | DashableWall |

## Template

[_template-prefab.md](_template-prefab.md)

## Contexto global

- [project-context.md](project-context.md)
- [scenes.md](scenes.md)
- [diagnostics.md](diagnostics.md)
