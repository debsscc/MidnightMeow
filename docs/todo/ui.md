# Tarefas de UI pendentes

_Última revisão: 2026-07-04_

# Tela de Controles — TIME DE ARTE

- Adicionar Tela de controles, para os jogadores saberem quais botões apertar.

# Interface Visual — TIME DE ARTE

- Melhorar a interface do lobby, menu, preparação e escolha de personagens

# Projétil — TIME DE ARTE

- Melhorar a arte dos projéteis

---

### TELA DE PAUSE - Camada da UI (Sorting Order)
- **Comportamento Atual vs Desejado:** Pause renderiza abaixo da HUD de habilidades (canto inferior esquerdo). Desejado: pause sobre todos os elementos de HUD.
- **Arquivos Investigados:** `Assets/Prefabs/UI/PauseMenu.prefab`, `Assets/Scripts/UI/GameplayHudController.cs`, `Assets/Scripts/UI/PlayerAbilityHud.cs`, `Assets/Scenes/Fases/Fase-2.unity`
- **Causas Prováveis Identificadas:**
  1. **Mesmo Canvas, ordem de hierarquia:** `PauseMenu` é instanciado como filho do canvas de gameplay (`m_TransformParent` → mesmo root que `GameplayHudController`); `PauseMenu` não possui Canvas próprio — é overlay Image dentro do canvas compartilhado (`sortingOrder: 3`).
  2. **`PlayerAbilityHud` forçado ao topo:** `GameplayHudController.EnsureAbilityHud()` chama `abilityHud.transform.SetAsLastSibling()` no `Awake` — coloca a HUD de habilidades **depois** do `PauseMenu` na hierarquia, desenhando por cima do overlay de pause.
  3. **Sem camada dedicada de overlay:** não existe `PauseOverlayLayer` com `SetAsLastSibling` ao abrir pause; `GameManager2.ShowPauseOverlay()` apenas ativa o GO sem reordenar siblings.
- **Plano de Ação Recomendado:**
  1. Em `GameManager2.ShowPauseOverlay()` / `MultiplayerGameManager.ApplyPauseClientRpc`: mover `pauseMenuObject` para `SetAsLastSibling()` no canvas de gameplay.
  2. Alternativa estrutural mínima: criar filho `PauseLayer` em `GameplayHudController` (como `AbilityHudLayer`) e reparentar `PauseMenu` para ele, sempre por último na hierarquia.
  3. Se necessário, subir `Canvas.sortingOrder` de um canvas filho só para pause (evitar segundo canvas fullscreen desnecessário).

---

### BUILD E UI - UI Desconfigurada em Resoluções Ultrawide (ex: 2560×1080)
- **Comportamento Atual vs Desejado:** Layout quebra fora da resolução de referência do Editor. Desejado: HUD estável em ultrawide e outras proporções.
- **Arquivos Investigados:** `Assets/Scenes/Fases/Fase-2.unity`, `Assets/Scenes/UI/Menu2.unity`, `Assets/Scripts/UI/PlayerAbilityHud.cs`, `Assets/Scripts/UI/GameplayHudController.cs`, `Assets/Scripts/UI/ScreenFlow/ScreenFlowPlaceholderFactory.cs`
- **Causas Prováveis Identificadas:**
  1. **`CanvasScaler` com `Match Width Or Height = 0` (match width):** referência `1920×1080` e `m_MatchWidthOrHeight: 0` em Fase-2 e Menu2 — em ultrawide 21:9 a escala segue largura e elementos ancorados verticalmente comprimem/deslocam.
  2. **HUD de habilidades com anchors fixos / fallback procedural:** `PlayerAbilityHud.BuildUi()` usa posições absolutas no canto inferior esquerdo sem safe area; `SetAsLastSibling` não corrige escala.
  3. **Múltiplos Canvas sem política unificada:** gameplay usa `Screen Space - Overlay` (sorting 3) enquanto Menu2 usa `Screen Space - Camera` — comportamento de scaler difere entre cenas.
- **Plano de Ação Recomendado:**
  1. Ajustar `CanvasScaler` do HUD de gameplay: `Match Width Or Height` ≈ **0.5** (ou 1.0 para priorizar altura em ultrawide) e validar em 2560×1080 / 3440×1440.
  2. Revisar anchors dos widgets em `PlayerAbilityHud` e `PhaseObjectiveHud` (top-center / bottom-left com offsets proporcionais, não pixels fixos de 1920p).
  3. Testar com **Game View** em aspect ratios 16:9, 21:9 e 4:3 antes de nova build.
