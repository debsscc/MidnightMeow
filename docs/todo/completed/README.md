# Tarefas concluídas — índice

Tarefas finalizadas saíram de `docs/todo/` e ficam aqui com notas de implementação.

| Arquivo | Categoria |
|---------|-----------|
| [system.md](system.md) | Dash, defesa ranged, selamento, carruagem |
| [ux.md](ux.md) | Zoom, fluxo preparação, botões, câmera, dissolve |
| [balancing.md](balancing.md) | Projétil Cora, vida, attack speed Nixie, câmera Fase 1 |
| [gameplay.md](gameplay.md) | Dash sem colisão/dano |
| [ui.md](ui.md) | Feedback Forms, cooldown HUD, contador Fase 1 |
| [multiplayer.md](multiplayer.md) | Shader telegraph em build |
| [maintenance.md](maintenance.md) | Importação de telas via SO |

## Setup pendente no Editor (novas mecânicas)

1. **Selamento:** adicionar `RatHoleSpawnPoint` nos `SpawnPoint` da Fase-1; criar e atribuir SO `RatHoleSealConfig` no `NetworkRatHoleSealManager` do `WaveSystem`.
2. **Carruagem (Fase-2):** prefab com `NetworkObject`, `NetworkCarriage`, `HealthComponent`, tag `Structure`, `CarriagePath` com waypoints e zona de chegada.

Ver também [docs/gameplay/rat-hole-sealing.md](../gameplay/rat-hole-sealing.md) e [docs/gameplay/carriage.md](../gameplay/carriage.md).
