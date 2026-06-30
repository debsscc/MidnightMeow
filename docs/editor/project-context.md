# Contexto do projeto (Editor)

Última revisão: 2026-05-22

## Unity

- **Versão:** 6000.3.13f1
- **Render:** 2D (URP conforme cenas)
- **Networking:** Unity Netcode for GameObjects (prefabs com `NetworkObject`)

## Tags (`ProjectSettings/TagManager.asset`)

| Tag | Uso típico |
|-----|------------|
| `Player` | Jogador local/rede |
| `Enemy` | Ratos / inimigos |
| `Structure` | Casa / estruturas defendidas |
| `Drop` | Coletáveis dropados |
| `GameController` | Managers de sessão |
| `MainCamera` | Câmera principal |
| `Respawn`, `Finish`, `EditorOnly` | Padrão Unity / legado |

## Layers

| Layer | Índice aprox. | Uso |
|-------|----------------|-----|
| Default | 0 | Geral |
| Player | 3 | Corpo do jogador |
| Wall | 6 | Paredes |
| Projectile | 7 | Projéteis do jogador |
| Structure | 8 | Casa / estruturas |
| Enemy | 11 | Inimigos |
| Collectable | 10 | Ciência (`Science.prefab`, tag `Drop`) |
| DashableWall | 12 | Paredes atravessáveis no dash |
| Shadow | 13 | Sombra projetada do personagem |
| Barrier | 14 | Barreira da Cora (colide só com Enemy / ProjectileEnemy) |
| UI | 5 | Interface |

## Input

- **Input System** package (`PlayerInput` nos prefabs [Cora](prefabs/Cora.md) / [Nixie](prefabs/Nixie.md))
- Action Map: `Gameplay` (Move, Fire, Ability, Frenzy, Dash)

## Pastas de código

Scripts em `Assets/Scripts/` (antigo `_Scripts`). Ver [STRUCTURE.md](../assets/STRUCTURE.md).

## Serviços em runtime

- `ServiceLocator` — registro de serviços (ex.: `PlayerProgressionData`)
- `Bootstrapper` / `MultiplayerBootstrapper` — cenas iniciais

## Eventos globais

Ver `GameEvents` em `Assets/Scripts/Core/GameEvents.cs` e [02-event-driven.md](../practices/02-event-driven.md).

## Nota para agentes

O bloco dinâmico em [AGENTS.md](../../AGENTS.md) (Unity Code Assist) pode listar **objeto ativo na cena** e tags/layers — use-o como snapshot, mas prefira este arquivo para regras estáveis.
