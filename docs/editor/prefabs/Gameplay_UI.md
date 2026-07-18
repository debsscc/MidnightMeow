# Prefab: Gameplay_UI (legado)

Última revisão: 2026-07-18  
**Caminho:** `Assets/Prefabs/UI/Gameplay_UI.prefab`

> **Legado:** este prefab **não é mais a fonte de verdade** da HUD de combate. A UI atual vive **nas cenas de fase**, sob `---- UI ----` → `Canvas` (Fase-3: o Canvas chama-se `Gameplay_UI` na hierarquia, mas é objeto de cena — não uma instância obrigatória deste prefab).

Para tutorial / novas peças de HUD, edite a cena: ver [tutorial-tips-hud-setup.md](../guides/tutorial-tips-hud-setup.md) e a seção abaixo.

## Hierarquia atual (Fase-1 / 2 / 3)

```
---- UI ----
├── UIManager
└── Canvas                    # Fase-3: nomeado Gameplay_UI
    ├── Button
    ├── PlayerUI
    ├── HordeUI
    ├── HealthBar             # vida (healthBarUi)
    ├── AdrenalineBar
    ├── Indicator / ScienceIndicator
    ├── HordeIndicator
    └── (runtime) GameplayHudLayers → Ability / Objective / Feedback…
```

### Autorado na cena

| Widget | Função |
|--------|--------|
| `HealthBar` | Vida do jogador local |
| `AdrenalineBar` | Adrenalina / frenzy |
| `ScienceIndicator` | Magículas da fase (`RoundMagiculaTracker`) |
| `HordeIndicator` | Wave (desliga quando a fase usa objetivo) |

### Criado em runtime (`GameplayHudController` via `GameplaySceneBootstrap`)

| Widget | Fase | Função |
|--------|------|--------|
| `PlayerAbilityHud` | 1–3 | Skills — canto inferior esquerdo |
| `PhaseObjectiveHud` | 1 | Sele buracos |
| `PhaseObjectiveHud` | 2 | Proteja a carruagem + barra de trajeto |
| `BossHealthBarHud` | 3 | Objetivo + vida do Rei Rato |
| `PlaytestFeedbackButton` | 1–3 | Feedback |

O bootstrap prefere Canvas com nome `Gameplay_UI`; senão pontua Canvas filhos de um pai com `"UI"` no nome (ex. `---- UI ----`).

## Prefab legado — componentes (referência histórica)

| Script | Função |
|--------|--------|
| `healthBarUi` | Barra de vida |
| `ScienceIndicator` | Magículas |
| `GameplayHudController` | Orquestra widgets / camadas |
| `HordeIndicator` | Wave |
| `PlayerAbilityHud` | Skills |
| `PlaytestFeedbackButton` | Feedback |

## Tutorial (dicas HUD)

- Setup nas **cenas** sob `---- UI ----` → Canvas: [tutorial-tips-hud-setup.md](../guides/tutorial-tips-hud-setup.md).
- Prefab: `Assets/Prefabs/UI/TutorialTipPanel.prefab` — Pause 1 (rot. Z 90°, alpha 0.28), painel `320×221` à direita, texto preto em negrito; fonte ≤22.
- `TutorialManager` + `TutorialUIController`; dados em `Assets/Data/Tutorial/`.
- **Não** configure o tutorial neste prefab legado.

## Objetivo Fase 1 (`PhaseObjectiveHud`)

- Banner + contador `{sealed}/{total}`; sprites em `Resources/PhaseObjectiveHudVisuals.asset`.
- Dados: `GameEvents.OnPhaseObjectiveStatusChanged`.

## Objetivo Fase 2 (`PhaseObjectiveHud` — carruagem)

- Banner “Proteja a carruagem” + barra de trajeto (`1 - PathProgress`) + follower `Carriage_Reference`.
- Progresso: `CarriageController.PathProgress`.

## Fase 3 (`BossHealthBarHud`)

- Banner objetivo + barra de vida do boss; criado no `ObjectiveHudLayer` quando a fase é KillBoss.

## Magículas

- `ScienceIndicator` na cena; tracker `RoundMagiculaTracker`. Persistência via `CommitToSave()` ao fim da partida.

## Correções históricas (prefab)

- 2026-06-16: scale root `(0,0,0)` → `(1,1,1)`.
- 2026-07-05: `PauseOverlayLayer`, `CanvasScaler` 1920×1080 match 0.5.
