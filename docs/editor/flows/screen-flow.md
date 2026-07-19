# Fluxo de telas unificado

Última revisão: 2026-07-18

> Requisitos completos: [screen-flow.md](../screen-flow.md)  
> Diagrama visual: [screen-flow-diagram.md](./screen-flow-diagram.md)  
> Referências visuais: [docs/reference_imgs/](../../reference_imgs/)

## Cenas e responsabilidades

| Cena | Responsabilidade |
|------|------------------|
| **Menu2** | Novo jogo, continuar (saves de host), opções, sair |
| **Lobby** | Hostear, entrar, jogar solo, consultar personagens |
| **Loading1** | Progresso de carregamento (Lobby → Preparação) — ambience Menu2/Lobby (Light + partículas, Canvas Camera) |
| **Loading2** | Progresso de carregamento (Preparação → gameplay) — mesmo ambience |
| **Preparation** | Contrato + personagem + pronto (sem ordem obrigatória) — mesmo ambience |
| **Characters** | Skills/upgrades (em save) ou consulta (menu/lobby) — mesmo ambience |
| **Fase-1** | Gameplay principal |
| **VictoryScene** | Vitória — Prosseguir (próxima fase / créditos na Fase-3) ou sair |
| **GameOver** | Derrota — continuar ou sair |

## Visão geral (arquitetura)

| Responsabilidade | Componente / asset |
|------------------|-------------------|
| Troca de **cena** | `ScreenFlowController` + `SceneFlowCatalog` |
| Orquestração (pause / corrida) | `GameFlowOrchestrator` |
| Persistência (magículas, tiers, host) | `SaveProfileStore` + `GameSaveData` |
| Contexto volátil da sessão | `GameSessionContext` |
| **Overlay** (pause) | `SceneOverlayController` + `PauseMenuActions` |
| Bootstrap por cena | `ScreenFlowSceneBootstrap` |
| Overlay (fade DDOL) | `TransitionFadeOverlay` — **sem** painel de loading; Loading1/2 são oficiais |
| Letterbox 16:9 (DDOL) | `AspectLetterboxController` + `LetterboxAspectMath` — força viewport 16:9 em todas as cenas |
| UI placeholder 1920×1080 | `ScreenFlowPlaceholderFactory` (menus/hub; não usar para loading) |
| Seleção UI (teclado/gamepad) | `UiSelectionUtility` + `GamepadUiAutoSelect` + `UiSelectOnEnable` |

### Navegação de UI sem mouse

O `GlobalEventSystem` (DDOL) substitui o EventSystem das cenas — `firstSelected` da cena **não** é usado. Ao abrir menu/painel/popup, o código chama `EventSystem.SetSelectedGameObject` via `UiSelectionUtility` (Menu2, pause, Preparation, GameOver/Victory, Controles, abas). `GamepadUiAutoSelect` completa a navegação com stick/setas e limpa seleção inválida (objeto desativado).

No **Menu2**, `UiSelectableFocusVisual` (adicionado por `MainMenuController`) copia/ajusta `Highlighted`/`Selected` do ColorTint para o foco de gamepad e o hover do mouse ficarem visíveis. Cursor de hover usa o sprite de gameplay quando o hover estava igual ao default.

## Fluxo completo

```
BootstrapScene → Menu2
  ├─ Novo Jogo → Loading2 → Lobby
  ├─ Continuar (se host) → Painel Saves → Lobby (auto-host)
  └─ Opções → Painel Opções (na mesma cena)

Lobby
  ├─ Hostear / Entrar → sincronização (2 jogadores) → Loading1 → Preparation
  ├─ Jogar Solo → Loading1 → Preparation
  └─ Voltar (`Btn_Back`) → Menu2 (`ExitToMainMenu`)

Preparation
  ├─ Escolher Personagem → Characters (seleção + upgrades) → Voltar
  ├─ Contrato 1 (ativo) / 2 e 3 (bloqueados)
  ├─ Voltar (`Btn_Back`) → Lobby (`return_lobby`)
  └─ [contrato + personagem + todos prontos] → Loading2 → Fase-1

Fase-1 / Fase-2 / Fase-3 → [vitória/derrota] → VictoryScene / GameOver
  ├─ Vitória → Prosseguir → próxima fase via NGO (MP) / Loading2 (solo) — ou créditos (Fase-3, todos os peers)
  ├─ Personagens: preservados (`LobbySelectionStore` + Preparation/Characters session)
  ├─ Derrota → Reiniciar fase
  └─ Sair → Menu2 (desconecta rede)
```

## Rotas (`Assets/Data/UI/ScreenFlow/`)

| ID | Cena | Load |
|----|------|------|
| `bootstrap_menu` | Menu2 | Single |
| `menu_lobby` | Loading2 | Single + fade |
| `loading2_lobby` | Lobby | Single + fade |
| `lobby_loading1` | Loading1 | NetcodeHost / Single |
| `loading1_preparation` | Preparation | NetcodeHost / Single |
| `lobby_characters` | Characters | Single |
| `preparation_characters` | Characters | Single (navegação local por jogador) |
| `characters_preparation` | Preparation | Single (navegação local por jogador) |
| `return_lobby` | Lobby | Single |
| `preparation_loading2` | Loading2 | NetcodeHost / Single |
| `loading2_gameplay` | Fase-1 | NetcodeHost / Single |
| `gameplay_victory` | VictoryScene | NetcodeHost / Single |
| `gameplay_defeat` | GameOver | NetcodeHost / Single |
| `victory_preparation` | Preparation | NetcodeHost / Single |
| `defeat_preparation` | Preparation | NetcodeHost / Single |
| `return_menu` | Menu2 | Single |

## Regras de negócio

### Menu
- **Novo Jogo** reinicia o slot 0 (`ResetActive`): magículas iniciais, mask de contratos zerado → só Contrato 1 liberado (salvo se `ContractProgressionConfig.unlockAllContractsForTesting` estiver ligado).
- **Continuar** visível apenas se existir save onde `wasHost == true`.
- **Continuar** abre painel de saves (partidas como host), não vai direto ao lobby.
- Botões no canto inferior esquerdo (ref. `menu.png`).

### Lobby
- **Voltar** (`Btn_Back`): desconecta e volta ao Menu2 via `ExitToMainMenu()`.
- **Multiplayer**: ao conectar o 2º jogador, transição automática para Loading1.
- **Solo**: botão dedicado, sem exigir sincronização.
- **Música:** `Sound Track` na cena Lobby; persiste em Loading1/2, Preparation e Characters (`CarriesMusicAcross`).

### Preparação
- Sem ordem obrigatória entre contrato, personagem e pronto.
- **Voltar** (`Btn_Back`): rota `return_lobby` → Lobby.
- Mensagens de erro ao apertar pronto sem requisitos (ex.: personagem, contrato, outro jogador).
- Hover no contrato exibe tooltip (ref. `hover_contract.png`).
- Ícones sob **Selecionar Personagens** (`Icons_Characters`): padrão `Cora_Selecionada` / `Nix_Selecionado`; ao escolher (solo = local; MP = qualquer jogador da sessão) → `Cora_Selecionada (1)` / `Nix_Selecionado (1)`. Se ambos estiverem escolhidos no MP, os dois ícones ficam na variante `(1)`.
- **Multiplayer:** apenas o **host** seleciona o contrato; o cliente vê tooltip e o contrato escolhido pelo host.
- **Multiplayer:** cada jogador navega livremente entre Preparation e Characters (rotas `preparation_characters` / `characters_preparation` com **carga aditiva**; `HubSceneNavigator` alterna visibilidade da UI sem descarregar cenas).
- Quando **todos** estão prontos (contrato + personagem), o host dispara `preparation_loading2` (NetcodeHost) para todos.

### Personagens
- **Menu/Lobby**: somente descrição das skills (modo `UpgradesOnly`).
- **Preparação**: seleção sincronizada (Nix/Cora exclusivos) + upgrades com magículas.
- **Multiplayer:** ambos escolhem personagem; escolha replicada via `PreparationSessionManager` + `CharactersSessionManager` (prefabs em `Assets/Prefabs/Multiplayer/`, catálogo `Resources/HubSessionPrefabCatalog`, registrados em `DefaultNetworkPrefabs`, spawn DDOL no servidor em Loading1).
- Personagem já escolhido por outro jogador fica **bloqueado** e exibe rótulo `Jogador N`.
- Retratos na Characters (`CharacterPortraitVisual` em `Nyxie_Images` / `Cora_Images`): idle `*_Personagem_Aguardando_Selecao`, hover `Nix_Selecionado_Personagem` / `Cora_Selecionada_Personagem`, selecionado (local ou taken no MP) `*_OutroPlayer_Personagem`. Trocar de personagem devolve o anterior ao idle (solo e MP).
- 6 botões de skill (3 Nix + 3 Cora); popup de upgrade (ref. `levelupskill.png`).
- Visual das barras (`SkillBarEntry`): State1 = tier 1, State3 = tier 2, State4 = tier 3; State2 = skill **selecionada** e com magículas para o próximo nível (não aplicar State2 às 3 skills só porque dá para comprar).

### Vitória / Derrota
- Botão **Continuar** → Preparation (reset de rodada, mantém MP).
- Botão **Sair** → Menu2 + desconexão.

## Persistência

- Arquivo: `{persistentDataPath}/MidnightMeow/saves/save_slot_{N}.json` (N = 0..2)
- Magículas e tiers por personagem via `CharacterSaveData`
- **Apagar save (slot):** painel Saves → botão **Apagar Save N** → confirmação (data, magículas)
- **Apagar todos:** painel Saves (Continuar) → **Apagar todos os saves** → confirmação
- **Áudio:** Opções → sliders Volume geral / Música / SFX → `GameAudioSettings` + mixer único `MidnightMeowAudioMixer` (`Assets/Resources/MidnightMeowAudioMixer.mixer`); botão **Restaurar padrões de áudio** (75% em cada canal).
- **SFX de botões (global):** `UiSfxPlayer` + `UiButtonSfx` (auto-inject em `Button`/`Toggle` via `UiButtonSfxBootstrap`). Clips `Hover.wav` / `Click.wav` via `UIAudioConfig` (`buttonHover` / `buttonClick`). Menu2, Lobby, Preparation, Characters, Victory/GameOver, Pause e o botão Fechar dos créditos aplicam `Button_Juiceness` + tint reforçado via `UiButtonFeedbackUtility` (`MainMenuController`, `LobbySceneUIController` / `LobbyFlowController`, `PreparationScreenController`, `CharactersScreenController`, `EndGameScreenController`, `PauseMenuActions`, `CreditsOverlayController`). UnityEvents legados em `UIButtonInteractionEvents` → `MenuAudioManager.PlayHoverSound` delegam ao `UiSfxPlayer`. Opt-out: `UiSfxIgnore` ou `playSfx = false`.
- API: `SaveProfileStore.DeleteSlot(int)`, `DeleteAllSlots()` — só no menu (`GameFlowOrchestrator.CanRequestTransition`)

## Contratos

| Asset | Status | Missão |
|-------|--------|--------|
| `Contract_1.asset` | Ativo | Sobreviva 3 ondas → `Fase-1` |
| Contrato 2 | Bloqueado | — |
| Contrato 3 | Bloqueado | — |

## Para artistas

- Canvas Scaler **1920×1080**, `matchWidthOrHeight = 0.5`
- Placeholders em `ScreenFlowPlaceholderFactory`; substituir mantendo **ancoragens**
- Refs: `menu.png`, `hostear_lobby.png`, `entrar_lobby.png`, `prep_screen.png`, `contract.png`, `select_char.png`, `char_from_lobby.png`

## Letterbox 16:9 (resolução / ultrawide)

> **Status (2026-07-18):** v3 URP-safe. `Camera.rect` em câmeras existentes; barras Overlay + **OnGUI**; no Editor o tamanho vem de `Handles.GetMainGameViewSize()` (evitar `Screen` colapsar após letterbox).

| Camada | Comportamento |
|--------|----------------|
| Câmeras de cena | `camera.rect` → viewport 16:9 (sem criar câmeras novas) |
| Barras | Overlay UGUI (sprite sólido) + `OnGUI` (garantia visual) |
| Overlay UI | `LetterboxSafeArea` + `AspectRatioFitter` |
| Fade | `LetterboxExempt` / sort ≥ 32000 — tela cheia |

**Como testar:** Game → Free Aspect → alargar bem. Console deve logar `[AspectLetterbox] Output=… showBars=True` e laterais pretas (não azul do céu).

**Nota:** 2560×1440 = 16:9 → sem barras (correto). Ultrawide real = 2560×1080 / Free Aspect largo.
