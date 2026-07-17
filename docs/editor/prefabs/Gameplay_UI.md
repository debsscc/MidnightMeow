# Prefab: Gameplay_UI

Última revisão: 2026-07-17  
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
| `healthBarUi` | Barra de vida do jogador local — tremor leve abaixo de ~50% HP (junto com a vinheta) |
| `ScienceIndicator` | Contador de magículas coletadas na fase (`RoundMagiculaTracker`) |
| `GameplayHudController` | Orquestra widgets de HUD no `Awake` (cooldowns, wave, feedback, indicadores) |
| `HordeIndicator` | Wave atual, inimigos restantes e kills — topo central |
| `PlayerAbilityHud` | Cooldowns Passiva / Dash / Q / R — canto inferior esquerdo |
| `PlaytestFeedbackButton` | Botão de feedback — canto inferior **direito** (280×72) |

## Correção 2026-06-16

- **Escala do root:** `Gameplay_UI` estava com `localScale (0,0,0)` — corrigido para `(1,1,1)`.
- **`GameplayHudController`:** camadas `GameplayHudLayers` com fallback **apenas** para widgets ausentes; reutiliza `HordeIndicator` da cena quando presente.

## Correção 2026-07-05

- **`PauseOverlayLayer`:** overlays (pause, baú) reparentados para camada acima da HUD de habilidades via `GameplayHudController.BringOverlayToFront`.
- **`AbilityHudLayer`:** `PlayerAbilityHud` deixa de usar `SetAsLastSibling` no canvas raiz.
- **`CanvasScaler`:** `matchWidthOrHeight = 0.5` aplicado em runtime no gameplay (`GameplayHudController`) e no menu (`MainMenuController`).

## HUD de habilidades (`PlayerAbilityHud`)

- Posição padrão: **canto inferior esquerdo** (fallback procedural).
- Slots: Passiva, Dash, Q, R — cada um com overlay de cooldown e timer.
- Ícones por personagem: campos `passiveHudIcon` / `dashHudIcon` / `ability1HudIcon` / `ability2HudIcon` em `CharacterAbilitySet` (`CoraAbilitySet`, `NixAbilitySet`).
- Arte: `Assets/Art/Sprites/New_UI/HUD_ ability/Habilidades Cora|Nyx/`.
- Fallback: sprites do theme / campos opcionais do componente; senão quadrados coloridos.
- SP e MP: vincula ao jogador local (`NetworkPlayerController.IsOwner` ou tag `Player`) e troca a arte no bind.
- **Escopo:** só em cenas `Fase-*`. Ao ir para Victory/GameOver/Menu, `GameplaySessionTeardown.HideGameplayHud` + `GameplaySceneBootstrap.CleanupGameplayHudOutsidePhases` ocultam/destroem placeholders — não devem aparecer fora da fase.

## Magículas na fase

- Coleta via `Ciencia` / `NetworkCienciaController` → `RoundMagiculaTracker` (por jogador local).
- HUD (`ScienceIndicator`) mostra só o número. A tela de personagens (painel de skills Nyxie/Cora) exibe `{count} magículas` / `{count} magicules` via `UiLocalization.FormatMagiculaCount` (`hud.magiculas_count`) — sempre visível ao abrir skills, inclusive no modo browse do Lobby/Menu.
- Ao vencer ou perder: `CommitToSave()` grava em `SaveProfileStore.Active.magiculas`.
- Multiplayer: `sharedSciencePool` desligado por padrão em `MultiplayerConfig`.

## Objetivo Fase 1 (`PhaseObjectiveHud`)

- Layout: banner `Objetivo.png` + título localizado (`objective.seal_holes.title`) e frame `Obj_Buracos.png` com contador `{sealed}/{total}` (`objective.holes_count`).
- Sprites: `Assets/Art/Sprites/id visual/UI/HUD/Fase 1/` via `Resources/PhaseObjectiveHudVisuals.asset`.
- Dados: `GameEvents.OnPhaseObjectiveStatusChanged` + `PhaseObjectiveStatusUtility.CountSealedHoles` (buracos locais na cena — igual em SP e MP).
- Fase 2 (carruagem) / boss: ainda usam texto legado condensado; Fase 3 oculta este HUD.

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
