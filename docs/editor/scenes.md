# Cenas

Última revisão: 2026-07-18

## EventSystem

Cenas de UI (Menu2, Lobby, Loading, Preparation, Characters) e Fase-1 mantêm um `EventSystem` **desativado** na hierarquia (legado). Em runtime, `EventSystemGlobalBootstrap` garante um único `GlobalEventSystem` (DDOL) e remove duplicatas ao carregar cenas.

Clientes em rotas `NetcodeHost` aguardam a cena via `NetworkSceneSyncUtility` (NGO Scene Management do host) — não carregam localmente.

Gameplay: `GameplayPrefabCatalog` em `Resources/` instancia `MultiplayerCameraRig` se a Fase-* carregar sem o rig. Jogadores só spawnam em cenas `Fase-*` (após `SynchronizeComplete`).

**Setup de fases (MP + mecânicas):** menu **MidnightMeow → Phases → Setup All Phase Scenes**. Ver [phases-implementation.md](../todo/phases-implementation.md).

## Bootstrap e fluxo


| Cena              | Caminho                                             | Função                                  |
| ----------------- | --------------------------------------------------- | --------------------------------------- |
| Bootstrap         | `Assets/Scenes/BootstrapScene/BootstrapScene.unity` | Inicialização, carregamento de serviços |
| Menu principal    | `Assets/Scenes/UI/Menu2.unity`                      | `MainMenuController` + `ContinueSavePanelController` — Novo Jogo (slot 0), Continuar (painel Save), Opções, Sair |
| Lobby             | `Assets/Scenes/UI/Lobby.unity`                      | `LobbySceneUIController` — host/entrar/solo; `Btn_Back` → Menu2 |
| Personagens       | `Assets/Scenes/UI/Characters.unity`                  | `CharactersScreenController` — hub (livro) + painéis `Skils_Nyxie` / `Skils_Cora` via `CharacterSkillsPanel`. Retratos `Nyxie_Images` / `Cora_Images` (`CharacterPortraitVisual`): idle `Nyx/Cora_Personagem_Aguardando_Selecao`, hover `Nix_Selecionado_Personagem` / `Cora_Selecionada_Personagem`, selecionado (local ou outro no MP) `*_OutroPlayer_Personagem`; trocar de personagem devolve o anterior ao idle. Clique no retrato seleciona e volta à Preparation; `Btn_Voltar` também. `magiculasText` (nome na cena) auto-bind. Retorno Preparation ↔ Characters sem loading (aditivo solo + MP). |
| Preparação        | `Assets/Scenes/UI/Preparation.unity`                | `PreparationScreenController` — fase (Fase 1–3), hint itálico `"Fase Selecionada"` (Inknut, preto, size 20) só após clique explícito no botão (não no default automático da fase 1), `Selected_Badge` por conclusão, **Escolher Personagem** → Characters (aditivo), ícones em `Icons_Characters` (Cora à esquerda / Nyxie à direita): padrão `Cora_Selecionada` / `Nix_Selecionado`; quando escolhido (solo ou qualquer jogador no MP) → `Cora_Selecionada (1)` / `Nix_Selecionado (1)`, **Pronto** inicia partida, **Voltar** (`Btn_Back`) → Lobby. Clique na imagem do contrato (`Contract_images`) abre zoom (fundo escuro) via `UiSimpleImageZoomOverlay`. Par Preparation ↔ Characters sem loading. |
| Loading 1         | `Assets/Scenes/UI/Loading1.unity`                   | Transição Lobby → Preparação |
| Loading 2         | `Assets/Scenes/UI/Loading2.unity`                   | Transição para gameplay                 |
| Jogo (UI wrapper) | `Assets/Scenes/UI/Game.unity`                       | Fluxo de partida com UI                 |
| Game (legado?)    | `Assets/Scenes/Game.unity`                          | Verificar Build Settings antes de usar  |
| Fase 1            | `Assets/Scenes/Fases/Fase-1.unity`                  | Selamento de buracos. HUD sob `---- UI ----` → `Canvas` |
| Fase 2            | `Assets/Scenes/Fases/Fase-2.unity`                  | Carruagem. HUD sob `---- UI ----` → `Canvas`. **Sem** prefabs Cora/Nixie na hierarquia — jogador só via `PlayerSpawnManager`. 3 buracos (`SpawnPoint`, `SpawnPoint (1)`, `SpawnPoint (5)`). |
| Fase 3            | `Assets/Scenes/Fases/Fase-3.unity`                  | Boss (`Rato_Boss`). HUD sob `---- UI ----` → Canvas nomeado `Gameplay_UI` |

**Fase-3 — fogo ambiente:** prefab `Assets/Prefabs/VFX/AmbientFire2D.prefab` (ver [AmbientFire2D.md](prefabs/AmbientFire2D.md)). Arrastar na cena; presets Ember/Torch/Bonfire.

**Fase-3 — iluminação / tela escura fora de 1080p:** luzes estavam sob Canvas Overlay (`Enviroment/Canvas` — “letterbox”). Ver guia [fase3-lighting-letterbox-fix.md](guides/fase3-lighting-letterbox-fix.md). Runtime: `PhaseLightingHierarchyFix` + `OrthographicSpriteLightFitter`.
| Game Over         | `Assets/Scenes/UI/GameOver.unity`                   | `EndGameScreenController` — derrota (Continuar / Sair) |
| Vitória           | `Assets/Scenes/UI/VictoryScene.unity`               | `EndGameScreenController` — vitória (Prosseguir → próxima fase / créditos na Fase-3) |


## Sandbox (não produção)

Cenas de teste em `Assets/_Sandbox/` (ex.: `Teste_MultiplayerFase-1.unity`). **Não** incluir em build de release sem revisão.

## NavMesh

Assets de NavMesh baked por cena em subpastas (`NavMesh-*.asset`). Prefabs: `NavMesh.prefab`, `NavMesh Surface.prefab`.

## Multiplayer (Fase-1 / sandbox)

- **Ondas:** apenas **`NetworkWaveManager`** (com `NetworkObject` no mesmo GameObject). Não usar **NightManager** / **WaveGenerator** na mesma cena MP — inimigos precisam de `NetworkObject.Spawn()`.
- **Bootstrap:** `MultiplayerBootstrapper` valida `NetworkWaveManager` na cena de gameplay.
- **Câmera:** `MultiplayerCameraRig` na raiz da Fase-1 (posição ~1.7, 8.58). Clientes aguardam spawn via `GameplayCameraRebindUtility` após `NetworkSceneSyncUtility.WaitForActiveScene`. Logs `[CAM-DIAG]` em `GameplayDiagnosticConfig.cameraDiagnostics` (ver [diagnostics.md](diagnostics.md)).
- **Shake ao tomar dano:** `PlayerCameraFeedback` → `CameraShakeController` (preset Medium em `CameraConfig`); rede dispara no `PlayTakeDamageVisualClientRpc` só para `IsOwner`; cenas offline/legado usam `HealthComponent`.
- **Juice (eventos importantes):** `PlayerCameraJuice` no jogador local (SP e MP) — dash/habilidades = micro-shake + zoom punch; kill = micro-shake (ClientRpc p/ killer); morte = shake médio-forte. Lean + breathing (camera bounce) via `enableCameraBounce`. Shake usa Perlin + decay. **Tiro normal não treme.**
- **Acessibilidade (motion sickness):** desligar bounce em `CameraConfig.enableCameraBounce` e/ou `MultiplayerCameraController.enableCameraBounce` e/ou `PlayerCameraJuice.enableCameraBounce`.
- **Pouca vida:** vinheta vermelha + tremor da barra a partir de ~50% HP (mesmo fluxo em SP/MP via `NetworkPlayerHealth`).
- **Tune:** `Assets/Data/Multiplayer/CameraConfig.asset` — `enableCameraBounce`, `zoomPunch*`, `moveLean*` (amplitude ao andar), `breathing*` (amplitude/frequência idle), `shakePerlinFrequency`.
- **Limites da câmera:** adicione um GameObject com `CameraBoundsVolume` + `PolygonCollider2D` na Fase-1; o `MultiplayerCameraController` liga ao `CinemachineConfiner2D` e aplica clamp manual via `CameraBoundsClampUtility` quando `useDirectCameraFollow` está ativo (Brain desligado).
- **HUD habilidades:** `PlayerAbilityHud` é criado automaticamente no Canvas da fase (`---- UI ----`) ao entrar em Fase-* (cooldowns + passiva, canto inferior esquerdo).
- **Magículas na fase:** `ScienceIndicator` já está no Canvas da cena (`Indicator` → `ScienceIndicator`); o script só atualiza o texto via `RoundMagiculaTracker`.
- **Objetivo por fase:** `PhaseObjectiveHud` (Fase-1 buracos / Fase-2 carruagem) e `BossHealthBarHud` (Fase-3) via `GameplayHudController` no mesmo Canvas. Título do objetivo usa Fira Sans (`GameplayUiFonts`).
- **Tipografia nas fases:** textos serializados em Fase-1/2/3 usam Fira Sans Medium (TMP + UI.Text). Prompts de interação (selar / reviver / consertar / escolta da carruagem) compartilham tamanho `0.9`, sorting `450` e opacidade ~0.78 via `GameplayUiFonts.ApplyWorldInteraction` (Fase-1 e Fase-2). Tutorial (`TutorialTipPanel`) também usa Fira Sans.
- **Tutorial (dicas):** painel manual no Canvas da cena — [tutorial-tips-hud-setup.md](guides/tutorial-tips-hud-setup.md). Não usar o prefab legado `Gameplay_UI.prefab`.
- **Estado da partida:** ao carregar Fase-1 como servidor, `MultiplayerGameManager` passa automaticamente para `Playing` (campo `gameplaySceneName`). Sem isso, `NetworkWaveManager` fica em espera e nenhum inimigo spawna.
- **Trilha:** objeto raiz `Soundtrack` em Fase-1/2/3 define o clip (`Fase 1.wav`, etc.) para o `MusicCrossfadeController`; `Play On Awake` desligado — só o crossfade persistente toca (evita duplo start ao entrar na fase). Os créditos não devem `enabled=false` nas fontes `MusicA`/`MusicB` do crossfade (isso cortava a trilha das fases após fechar créditos).

### Hierarquia recomendada (Fase-1)

| Objeto | Pai | Componentes-chave |
|--------|-----|-------------------|
| `---- Sistemas ----` | raiz | organização |
| `_GameLoop` | `---- Sistemas ----` | `NetworkObject`, `NetworkWaveManager` |
| `---- Spawn Points Inimigos ----` | raiz | 6+ `Transform` referenciados em `spawnPoints` |
| `MultiplayerGameManager` | raiz (prefab) | `NetworkObject`, estado `Playing` |
| `MultiplayerManagers` | raiz (prefab) | `MultiplayerBootstrapper` → `waveManager` = `_GameLoop` |
| `MultiplayerCameraRig` | raiz (~1.7, 8.58) | `MultiplayerCameraController`, `MainCamera` (Z=-10) + `CinemachineBrain`, `PlayerVirtualCamera` |

`waveSettings` em `_GameLoop` → `Assets/Data/Stats/Game/Fase 1.asset`. Prefabs de inimigo devem estar em **Default Network Prefabs**.

**Ciência:** `NetworkWaveManager.networkCienciaPrefab` → `Science.prefab` (com `CienciaHoming` + `CienciaPickupConfig`). Ver [diagnostics.md](diagnostics.md#ciência-drop-ao-matar).

## Ao alterar uma cena

1. Documentar prefabs instanciados novos aqui ou em `prefabs/`.
2. Confirmar em **File → Build Settings** se a cena permanece no build.
3. Atualizar `Última revisão` neste arquivo.

