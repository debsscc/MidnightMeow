# Prefab: Gameplay_UI

Última revisão: 2026-06-14  
**Caminho:** `Assets/Prefabs/UI/Gameplay_UI.prefab`

## Resumo

Canvas de HUD principal (vida, upgrades, magículas da fase) durante gameplay single-player ou multiplayer.

## Componentes Unity

| Tipo | Presente |
|------|----------|
| `Canvas` + `CanvasScaler` + `GraphicRaycaster` | Sim |
| TMP / Image / Button | Sim |

## Scripts custom

| Script | Função |
|--------|--------|
| `healthBarUi` | Barra de vida do jogador local |
| `ScienceIndicator` | Contador de magículas coletadas na fase (`RoundMagiculaTracker`) |
| `PlayerAbilityHud` | Cooldowns Passiva / Dash / Q / R — canto inferior esquerdo (criado por `GameplaySceneBootstrap` se ausente) |

## HUD de habilidades (`PlayerAbilityHud`)

- Posição padrão: **canto inferior esquerdo** (fallback procedural).
- Slots: Passiva, Dash, Q, R — cada um com overlay de cooldown e timer.
- Sprites opcionais no Inspector: `passiveIcon`, `dashIcon`, `ability1Icon`, `ability2Icon`.
- SP e MP: vincula ao jogador local (`NetworkPlayerController.IsOwner` ou tag `Player`).

## Magículas na fase

- Coleta via `Ciencia` / `NetworkCienciaController` → `RoundMagiculaTracker` (por jogador local).
- Ao vencer ou perder: `CommitToSave()` grava em `SaveProfileStore.Active.magiculas`.
- Multiplayer: `sharedSciencePool` desligado por padrão em `MultiplayerConfig`.

## Inimigos — barra de vida

- `EnemyHealthBarDisplay` é adicionado automaticamente em inimigos (`HealthComponent` + tag `Enemy`).
- Barra world-space dimensionada pelo `SpriteRenderer.bounds`.
- Sprites opcionais: `backgroundSprite`, `fillSprite` no componente.

## Valores a confirmar no Editor

| Objeto | Campo | Valor atual |
|--------|-------|-------------|
| Canvas | Sort Order / Render Mode | |
| Barras de vida jogador | Eventos `GameEvents.OnPlayerHealthChanged` | |
| `PlayerAbilityHud` | Ícones de arte (opcional) | |
| EventSystem na cena | Não duplicar com Lobby | |
