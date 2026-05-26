# Single Responsibility Principle (SRP)

## Objetivo

Cada classe/script deve ter **uma razão para mudar**. Facilita manutenção, testes e trabalho paralelo (design × código × rede).

## Exemplos corretos no projeto

| Script | Responsabilidade |
|--------|------------------|
| `PlayerMovement` | Locomoção e física 2D |
| `PlayerShooting` | Cadência, spawn de projétil |
| `PlayerInitializer` | Aplicar progression/stats nos componentes |
| `HealthComponent` | Vida, dano, morte |
| `NetworkPlayerController` | Autoridade/replicação do input no multiplayer |
| `WaveGenerator` | Lógica de geração de ondas (dados em `WaveSettings`) |

## Sinais de violação

- Script com `Update` fazendo movimento, tiro, UI, áudio e save.
- `GameManager` crescendo com lógica de onda, pause, upgrades e cena.
- Prefab com 15 scripts que todos leem input diretamente.

## Refatoração esperada

Dividir em componentes menores ou extrair para `Systems/` (ex.: `DayManager`, `NightManager`, `WaveGenerator`).

## Para agentes de IA

Se a tarefa pede “adicionar X ao Player”, avalie um **novo componente** ou extensão de SO antes de inflar `NetworkPlayerController` ou `PlayerShooting`.
