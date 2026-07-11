# Fluxo Menu → Lobby → Loading

Última revisão: 2026-07-04

> Documentação completa: [screen-flow.md](./screen-flow.md)

## Menu (Menu2)

| Botão | Ação |
|-------|------|
| Novo Jogo | `GameSessionContext.BeginNewGame()` → slot 0 → rota `menu_lobby` |
| Continuar | Abre painel **Save** (`ContinueSavePanelController`) — só se `SaveProfileStore.HasAnyHostSave()` |
| Opções | Painel Opções (gráficos, áudio, controles, geral) |
| Feedback Playtest | Abre o [formulário de playtest](https://docs.google.com/forms/d/e/1FAIpQLScqrERAjHtXbsp-kTXYh86otM1uvqKOICOwL0JFGYLe5203aw/viewform) no navegador (`Application.OpenURL`) |
| Sair | `Application.Quit()` |

### Tela Continuar (Save)

Fluxo em dois passos, separado das abas do `MenuTabController` (Levels / Settings / Upgrades):

1. **Continuar** (bookmark) → ativa `Canvas/Save` (inicia oculto); bookmarks do hub trocam para sprites **tucked** (encolhidos na lateral do livro).
2. **Arquivo 1/2/3** → preview do contrato + data/hora; habilita **Carregar** / **Deletar**. Slots vazios ficam cinza e não clicáveis.
3. **Carregar** → `GameSessionContext.BeginContinue(slot)` + rota `menu_lobby`.
4. **Deletar** → modal `SaveDeleteConfirmation` (Confirmar / Cancelar).
5. **ESC** ou bookmark **Sair** (vira **Voltar** na tela de save) → fecha Save e volta para **Levels**.

Preview de contrato: `MenuContractVisualConfig` em `Resources/`.

Bookmarks na tela Continuar: permanecem visíveis com sprites **tucked** (`MenuBookmarkVisualConfig`); **Sair** vira **Voltar** (sprite + label) e fecha o painel. ESC também volta.

**Música:** objeto raiz `Sound Track` (`menu.wav`, grupo **Music**). O `MusicCrossfadeController` lê o clip, desliga o `AudioSource` da cena e toca via crossfade persistente **no mesmo grupo Music**, controlável pelo slider Música (`GameAudioSettings` → `MusicVolume`).

**SFX de botões:** global via `UiSfxPlayer` / `UiButtonSfx` (hover + click; auto-inject). Clips em `UIAudioConfig.buttonHover` / `buttonClick`.

Layout: botões canto inferior esquerdo com offset (ref. `docs/reference_imgs/menu.png`).

## Lobby

| Botão | Ação |
|-------|------|
| Hostear | Painel aguardando + `ConnectionManager.StartHostAsync()` |
| Entrar | Painel código + `ConnectionManager.StartClientAsync()` |
| Jogar Solo | `BeginSinglePlayer()` → `lobby_loading1` |
| Personagens | `Characters` modo consulta → `return_lobby` |

**Multiplayer:** quando 2 jogadores conectam, host dispara `lobby_loading1` automaticamente.

## Loading1 / Loading2 — sincronia imediata

- O overlay de loading aparece **no mesmo frame** do clique (antes do fade) via `ScreenFlowController.RequestScene`.
- Rotas para `Loading1`/`Loading2` pulam o fade gradual, zeram `minLoadingTime` no overlay e usam apenas o overlay built-in (sem loading legado da cena de origem).
- `LoadingScreenController` faz **handoff** do overlay (`HandoffToDedicatedLoadingScene`) no `Awake`, sem `ClearTransitionOverlay` no início da rotina — evita flash entre overlay e UI da cena.
- Clientes em rede recebem feedback imediato via `NetworkSceneLoadingFeedback` quando o host inicia carga NGO.

## Loading1

- Arte Nix + Cora (placeholders coloridos)
- Rota pendente: `loading1_preparation`
- Tempo mínimo padrão: **2,5 s** em `LoadingScreenController` (antes 7 s)
- Barra de progresso usa `LoadingProgressUtility` (sprite UI builtin + `fillAmount`) e reinicia em 0% a cada transição

## Catálogo

| Rota | Asset |
|------|-------|
| `menu_lobby` | `Route_Menu_Lobby.asset` |
| `lobby_loading1` | `Route_Lobby_Loading1.asset` |
| `loading1_preparation` | `Route_Loading1_Preparation.asset` |

## Wiring no Editor

Para ligar hierarquias e eventos de forma permanente:

1. Abra a cena desejada (ou use **BootstrapScene** para testar o fluxo completo).
2. Menu **MidnightMeow → Screen Flow → Setup Active Scene** (cena aberta) ou **Setup All Flow Scenes** (todas de uma vez).
3. O script cria `---- ScreenFlow ----` com `ScreenFlowSceneBootstrap` e o controller da cena.
4. Em **Lobby**, o `LobbySceneUIController` em `LobbyManager` recebe refs dos botões do prefab `Lobby`.
5. Cenas legadas (`Loading1/2`, `Preparation`, `Characters`) têm `Button_Menu` desativado para não chamar `GameFlowManager.LoadMenu`.
6. **Victory/GameOver**: `EndGameScreenController` religa `Button_Menu` para sair ao menu.

Em runtime, se refs faltarem, `ScreenFlowUiLookup` resolve por nome (`Host`, `Join`, `StartGame`, `Back`, etc.) e o bootstrap cria controllers ausentes.

## Template legado (Defeat / Sound Track)

Cenas copiadas do template de fim de jogo traziam o prefab `Defeat` e `Sound Track` (`game over.wav`) ativos em Loading, Preparation e Characters. Isso fazia a tela/música de derrota aparecer no meio do fluxo.

- **Runtime:** `ScreenFlowLegacySceneCleanup` desativa `Defeat` e `Sound Track` em Loading, Preparation e Characters (não em **Menu2** — trilha do menu via crossfade).
- **Editor:** o setup de Screen Flow também desativa esses objetos nas cenas afetadas.
- **Transições:** `ScreenFlowController` usa overlay persistente (fade + loading) para o menu não ficar visível por cima do carregamento.

## Gameplay solo (Fase-1)

O modo solo inicia um **host local** (`ConnectionManager.TryStartLocalSoloHost`) antes de carregar Fase-1. Sem isso, `PlayerSpawnManager` não spawna o jogador e a câmera fica na cor de fundo azul (só a HUD aparece).
