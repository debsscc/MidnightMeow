# Tarefas de UI pendentes

_Última revisão: 2026-07-05_

# Tela de Controles — TIME DE ARTE

- Adicionar Tela de controles, para os jogadores saberem quais botões apertar.

# Interface Visual — TIME DE ARTE

- Melhorar a interface do lobby, menu, preparação e escolha de personagens

# Projétil — TIME DE ARTE

- Melhorar a arte dos projéteis

---

## [TASK CONCLUÍDA] Tela de Pause — Camada da UI (Sorting Order)

- **O que foi feito:** `GameplayHudController` ganhou `PauseOverlayLayer` (sempre última camada em `GameplayHudLayers`). `BringOverlayToFront` reparenta o pause para essa camada; chamado em `GameManager2.ShowPauseOverlay`, `EnsurePauseOverlayVisible` e `SceneOverlayController.OpenOverlay`. `PlayerAbilityHud` passou a viver em `AbilityHudLayer` (sem `SetAsLastSibling` no canvas raiz). Scripts: `GameplayHudController.cs`, `GameManager2.cs`, `SceneOverlayController.cs`, `PlayerAbilityHud.cs`.

- **Como testar (Singleplayer):** Fase-1 ou Fase-2 → jogar → Esc (pause). Overlay de pause deve cobrir **toda** a HUD, inclusive slots Passiva/Dash/Q/R no canto inferior esquerdo.

- **Como testar (Multiplayer/Netcode):** Host ou Cliente pausa → ambos veem overlay acima da HUD de habilidades.

- **Resultado Esperado:** Pause sempre renderiza por cima de todos os widgets de gameplay no mesmo canvas.

---

## [TASK EM TESTE] Letterbox 16:9 v2 (Overlay only — URP-safe)

- **O que foi feito:** Removidas câmeras auxiliares / `Camera.rect` / remap Camera mode. Só barras Overlay + `LetterboxSafeArea` (`AspectRatioFitter` 16:9) em canvases Overlay. Fade continua exempt.
- **Causa da tela preta (v1):** URP 2D — câmeras Base DDOL limpavam o frame.
- **Como testar:** Play Mode em 1920×1080 (normal) e 2560×1080 (barras + UI 16:9). Mundo pode continuar ultrawide; UI não.
- **Docs:** `docs/editor/flows/screen-flow.md`

---

## [TASK CONCLUÍDA] UI Desconfigurada em Resoluções Ultrawide (ex: 2560×1080) — mitigação parcial (pré-letterbox)

- **O que foi feito:** `GameplayHudController.ApplyResponsiveCanvasScaler` define `matchWidthOrHeight = 0.5` e referência 1920×1080 em canvases `Scale With Screen Size` (gameplay). `MainMenuController` aplica a mesma política a todos os canvases de `Menu2`. `PlayerAbilityHud.ResolveHudAnchorPosition` respeita `Screen.safeArea` no canto inferior esquerdo. Scripts: `GameplayHudController.cs`, `MainMenuController.cs`, `PlayerAbilityHud.cs`.
- **Nota:** Insuficiente sozinho em ultrawide; sucedido pelo letterbox 16:9 acima.

- **Como testar:** Game View → aspect **21:9** (2560×1080 ou 3440×1440) → Fase-2 e Menu2. HUD de habilidades e objetivo devem permanecer legíveis, sem compressão extrema vertical; menu sem elementos cortados nas bordas.

- **Resultado Esperado:** Layout estável em 16:9, 21:9 e 4:3 sem depender só do match width (0) das cenas legadas.

---

## Verificação manual

- [x] Pause cobre HUD de habilidades (código — validar em Play)
- [x] Ultrawide 2560×1080 gameplay + menu via letterbox 16:9 (código — validar em Game View)
