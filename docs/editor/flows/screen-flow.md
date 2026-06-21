# Fluxo de telas unificado

Última revisão: 2026-06-08

> Requisitos completos: [screen-flow.md](../screen-flow.md)  
> Diagrama visual: [screen-flow-diagram.md](./screen-flow-diagram.md)  
> Referências visuais: [docs/reference_imgs/](../../reference_imgs/)

## Cenas e responsabilidades

| Cena | Responsabilidade |
|------|------------------|
| **Menu2** | Novo jogo, continuar (saves de host), opções, sair |
| **Lobby** | Hostear, entrar, jogar solo, consultar personagens |
| **Loading1** | Progresso de carregamento (Lobby → Preparação) |
| **Preparation** | Contrato + personagem + pronto (sem ordem obrigatória) |
| **Characters** | Skills/upgrades (em save) ou consulta (menu/lobby) |
| **Loading2** | Progresso de carregamento (Preparação → Gameplay) |
| **Fase-1** | Gameplay principal |
| **VictoryScene** | Vitória — continuar ou sair |
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
| UI placeholder 1920×1080 | `ScreenFlowPlaceholderFactory` |

## Fluxo completo

```
BootstrapScene → Menu2
  ├─ Novo Jogo → Lobby
  ├─ Continuar (se host) → Painel Saves → Lobby (auto-host)
  ├─ Opções → Painel Opções (na mesma cena)
  └─ Personagens (via Lobby) → Characters (somente consulta)

Lobby
  ├─ Hostear / Entrar → sincronização (2 jogadores) → Loading1 → Preparation
  ├─ Jogar Solo → Loading1 → Preparation
  └─ Personagens → Characters (consulta) → Voltar ao Lobby

Preparation
  ├─ Escolher Personagem → Characters (seleção + upgrades) → Voltar
  ├─ Contrato 1 (ativo) / 2 e 3 (bloqueados)
  └─ [contrato + personagem + todos prontos] → Loading2 → Fase-1

Fase-1 → [vitória/derrota] → VictoryScene / GameOver
  ├─ Continuar → Preparation (mantém sincronização MP)
  └─ Sair → Menu2 (desconecta rede)
```

## Rotas (`Assets/Data/UI/ScreenFlow/`)

| ID | Cena | Load |
|----|------|------|
| `bootstrap_menu` | Menu2 | Single |
| `menu_lobby` | Lobby | Single + loading |
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
- **Continuar** visível apenas se existir save onde `wasHost == true`.
- **Continuar** abre painel de saves (partidas como host), não vai direto ao lobby.
- Botões no canto inferior esquerdo (ref. `menu.png`).

### Lobby
- **Personagens**: consulta de skills, sem níveis nem compras.
- **Multiplayer**: ao conectar o 2º jogador, transição automática para Loading1.
- **Solo**: botão dedicado, sem exigir sincronização.

### Preparação
- Sem ordem obrigatória entre contrato, personagem e pronto.
- Mensagens de erro ao apertar pronto sem requisitos (ex.: personagem, contrato, outro jogador).
- Hover no contrato exibe tooltip (ref. `hover_contract.png`).
- **Multiplayer:** apenas o **host** seleciona o contrato; o cliente vê tooltip e o contrato escolhido pelo host.
- **Multiplayer:** cada jogador navega livremente entre Preparation e Characters (rotas `preparation_characters` / `characters_preparation` com **carga aditiva**; `HubSceneNavigator` alterna visibilidade da UI sem descarregar cenas).
- Quando **todos** estão prontos (contrato + personagem), o host dispara `preparation_loading2` (NetcodeHost) para todos.

### Personagens
- **Menu/Lobby**: somente descrição das skills (modo `UpgradesOnly`).
- **Preparação**: seleção sincronizada (Nix/Cora exclusivos) + upgrades com magículas.
- **Multiplayer:** ambos escolhem personagem; escolha replicada via `PreparationSessionManager` + `CharactersSessionManager` (prefabs em `Assets/Prefabs/Multiplayer/`, catálogo `Resources/HubSessionPrefabCatalog`, registrados em `DefaultNetworkPrefabs`, spawn DDOL no servidor em Loading1).
- Personagem já escolhido por outro jogador fica **bloqueado** e exibe rótulo `Jogador N`.
- 6 botões de skill (3 Nix + 3 Cora); popup de upgrade (ref. `levelupskill.png`).

### Vitória / Derrota
- Botão **Continuar** → Preparation (reset de rodada, mantém MP).
- Botão **Sair** → Menu2 + desconexão.

## Persistência

- Arquivo: `{persistentDataPath}/MidnightMeow/saves/save_slot_{N}.json` (N = 0..2)
- Magículas e tiers por personagem via `CharacterSaveData`
- **Apagar save (slot):** painel Saves → botão **Apagar Save N** → confirmação (data, magículas)
- **Apagar todos:** painel Saves (Continuar) → **Apagar todos os saves** → confirmação
- **Áudio (placeholder):** Opções → sliders Volume geral / Música / SFX → `GameAudioSettings` + `NewAudioMixer`; botão **Restaurar padrões de áudio** (75% em cada canal).
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
