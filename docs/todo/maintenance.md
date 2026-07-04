# Tarefas de manutenção pendentes

_Última revisão: 2026-07-04_

---

### AJUSTES - Remoção de Gizmos de Depuração (Dash)
- **Comportamento Atual vs Desejado:** Retângulo de debug do Dash visível em Play Mode (build inclusa). Desejado: desativado em runtime ou restrito ao Editor.
- **Arquivos Investigados:** `Assets/Scripts/Components/Player/PlayerDash.cs`, `Assets/Scripts/Combat/AbilityDebugVisualHost.cs`, `Assets/Scripts/Components/Player/PlayerGameplayModuleInstaller.cs`, `Assets/Scripts/Combat/MeleeAttackDebugVisual.cs`
- **Causas Prováveis Identificadas:**
  1. **`AbilityDebugVisualHost` com `showInPlayMode = true` e `drawDebugGizmos = true` por padrão:** `PlayerDash.TryStartDash()` chama `_debugHost.ShowDash()` em toda execução; `LateUpdate` mantém `SpriteRenderer` ativo com shader `MidnightMeow/AbilityZoneFill`.
  2. **Instalação automática em produção:** `PlayerGameplayModuleInstaller` adiciona `AbilityDebugVisualHost` a todo jogador spawnado se ausente — prefabs de personagem herdam o componente de debug.
  3. **`OnDrawGizmos` sem guarda de editor:** gizmos do host e de `CoraBarrierAbilityExecutor` desenham em Play Mode no Editor; em build o retângulo runtime vem do `SpriteRenderer`, não dos gizmos.
- **Plano de Ação Recomendado:**
  1. Em `AbilityDebugVisualHost`: default `showInPlayMode = false`; guarda `#if UNITY_EDITOR` em `OnDrawGizmos` / `DrawPreviewGizmo`.
  2. Em `PlayerDash`: só chamar `ShowDash` quando `#if UNITY_EDITOR` ou `Debug.isDebugBuild` (alinhado a `MeleeAttackDebugVisual.drawDebugGizmos`).
  3. Em `PlayerGameplayModuleInstaller`: não adicionar `AbilityDebugVisualHost` em builds release (ou usar `[SerializeField] bool installDebugVisuals` default false).

---

### BUILD E SHADERS - Shaders de Ataque/Habilidades do Nixie quebrados
- **Comportamento Atual vs Desejado:** VFX corretos no Editor; retângulos brancos (fallback) na build. Desejado: materiais com shader correto em player build.
- **Arquivos Investigados:** `Assets/Art/Shaders/AbilityZoneFill.shader`, `Assets/Art/Shaders/TelegraphFill.shader`, `Assets/Art/Shaders/MeleeHitWave.shader`, `Assets/Scripts/Combat/AbilityDebugVisualHost.cs`, `Assets/Scripts/Combat/MeleeAttackDebugVisual.cs`, `ProjectSettings/GraphicsSettings.asset`
- **Causas Prováveis Identificadas:**
  1. **`AbilityZoneFill` ausente de Always Included Shaders:** guid `145668f0260a513479cb20e8653b8418` não listado em `GraphicsSettings.m_AlwaysIncludedShaders`; `TelegraphFill` (`32088765482028e4487ac3ed1ef2cfa1`) está incluído — Nixie usa `AbilityZoneFill` via `Shader.Find("MidnightMeow/AbilityZoneFill")` em runtime.
  2. **`Shader.Find` em runtime falha na build:** `AbilityDebugVisualHost.EnsureRenderer()` e `MeleeAttackDebugVisual` criam `new Material(shader)` após `Shader.Find`; se stripped, cai em `Sprites/Default` (retângulo branco sólido).
  3. **Variant stripping URP:** shaders custom em `Assets/Art/Shaders/` podem perder variantes usadas por `SpriteRenderer` / `LineRenderer` quando não referenciados por material em cena nem pré-carregados.
- **Plano de Ação Recomendado:**
  1. Adicionar `AbilityZoneFill.shader` e `MeleeHitWave.shader` a **Project Settings → Graphics → Always Included Shaders** (seguir precedente documentado em `docs/todo/completed/multiplayer.md` para `TelegraphFill`).
  2. Criar materiais estáticos em `Assets/Resources/` (ex.: `AbilityZoneFill.mat`) e referenciá-los em código em vez de `Shader.Find` + `new Material` em runtime.
  3. Rebuild e validar ataque melee, Dash debug (se habilitado) e habilidades Nix no cliente build.

---

### BUILD E SHADERS - Sliders de Áudio Inoperantes na Build
- **Comportamento Atual vs Desejado:** Sliders de opções não alteram volume na build; funcionam no Editor. Desejado: `AudioMixer` responde e `PlayerPrefs` persiste.
- **Arquivos Investigados:** `Assets/Scripts/Audio/GameAudioSettings.cs`, `Assets/Scripts/UI/ScreenFlow/MainMenuController.cs`, `Assets/Audio/NewAudioMixer.mixer`, `Assets/Scripts/UI/ScreenFlow/ScreenFlowSceneBootstrap.cs`
- **Causas Prováveis Identificadas:**
  1. **`GameAudioSettings` sem referência serializada ao mixer na build:** `EnsureExists()` cria GO em runtime sem `[SerializeField] audioMixer`; `FindProjectMixer()` usa `Resources.FindObjectsOfTypeAll<AudioMixer>()` — na build o mixer em `Assets/Audio/` pode não estar carregado, retornando `null` e silenciando `SetFloat`.
  2. **Parâmetros expostos corretos mas mixer inacessível:** `NewAudioMixer` expõe `MasterVolume`, `MusicVolume`, `SfxVolume` — nomes batem com `GameAudioSettings`; falha é de resolução do asset, não de string.
  3. **Sliders gravam prefs mas mixer não aplica:** `SetVolume` persiste em `PlayerPrefs` mesmo com `audioMixer == null`; na próxima abertura o valor parece “salvo” mas sem efeito audível.
- **Plano de Ação Recomendado:**
  1. Colocar referência direta a `NewAudioMixer` em prefab/singleton persistente (`BootstrapScene` ou `GameAudioSettings` na cena de boot) — evitar `FindObjectsOfTypeAll` na build.
  2. Alternativa mínima: mover cópia do mixer para `Resources/NewAudioMixer.mixer` e carregar com `Resources.Load<AudioMixer>`.
  3. Logar warning `[GameAudioSettings] Parâmetro 'X' não encontrado` na build para confirmar; testar sliders no Menu2 build após fix.

---

### BUILD E SHADERS - Shader da Tela Inicial muito escuro na Build
- **Comportamento Atual vs Desejado:** Menu normal no Editor; excessivamente escuro no executável. Desejado: mesma luminosidade percebida.
- **Arquivos Investigados:** `Assets/Scenes/UI/Menu2.unity`, `ProjectSettings/ProjectSettings.asset` (`m_ActiveColorSpace: 1` = Linear), `Assets/Art/Shaders/URP Sprites/BlinkingSprite.shadergraph`, `Assets/Scenes/Fases/Fase-2.unity` (comparação `m_VertexColorAlwaysGammaSpace`)
- **Causas Prováveis Identificadas:**
  1. **Color Space Linear sem gamma no Canvas do menu:** Canvas em `Menu2` com `m_VertexColorAlwaysGammaSpace: 0`; em projeto Linear, UI pode renderizar ~2× mais escura na build do que no Editor (comportamento documentado Unity 6 UI).
  2. **Shader Graph / materiais de fundo com cálculo em gamma:** `BlinkingSprite.shadergraph` (guid `0a66134f4be3fed458efd5493b8e9371`) incluído em Always Included Shaders — variantes ou propriedades de cor podem diferir entre Editor DX11 e build player.
  3. **URP/post-processing na câmera do menu:** diferença de `Volume` profile ou exposição entre Editor (Scene view iluminada) e build standalone sem ajuste de tonemapping.
- **Plano de Ação Recomendado:**
  1. No Canvas raiz de `Menu2`: ativar **Vertex Color Always Gamma Space** (igual ao HUD de Fase-2 que usa `1`).
  2. Revisar materiais de background do menu: preferir cores em sRGB / materiais URP UI Lit com conversão correta.
  3. Comparar build Development vs Release com Frame Debugger; ajustar multiplicador de cor no material ou Volume de pós-processo da câmera do menu se necessário.
