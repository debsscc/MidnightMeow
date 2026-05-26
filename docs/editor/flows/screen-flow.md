# Fluxo de telas unificado

Última revisão: 2026-05-22

## Visão geral

| Responsabilidade | Componente / asset |
|------------------|-------------------|
| Troca de **cena** (Menu → Lobby → Fase) | `ScreenFlowController` + `SceneFlowCatalog` |
| Evento do Inspector → cena | `ScreenFlowRequest` |
| **Overlay** na mesma cena (pause, baú) | `SceneOverlayController` + `SceneOverlayRequest` |
| SFX/VFX no clique | `FlowEventRelay` (UnityEvents + AudioClip opcional) |
| Fade / loading visuais (Menu2) | `SceneTransition` (registra imagens no controller) |

## Fluxo inicial do jogo

```
BootstrapScene  --(bootstrap_menu, Instant)-->
Menu2           --(menu_lobby, LoadingScreen)-->
Lobby           --(lobby_gameplay, NetcodeHost)-->
Fase-1
```

Rotas em `Assets/Data/UI/ScreenFlow/`. IDs em `SceneFlowRouteIds`.

## Para game designers

### Botão → outra cena

1. No botão, **On Click** → componente com `ScreenFlowRequest.Execute()`.
2. Preencher **Route Id** (ex.: `menu_lobby`) ou arrastar um asset `Scene Flow Route`.
3. Em **Flow Events**, ligar `On Before` / `On After` a sons ou partículas.

Alternativa legada: `UIActionBridge.LoadLobby()` (já usa as rotas se o `ScreenFlowController` existir).

### Pause / painel na mesma cena

1. Na cena, criar `SceneOverlayController` no Canvas.
2. Lista **Overlays**: id `pause`, referência ao GameObject do menu, marcar **Pause Game Time** se necessário.
3. Botão → `SceneOverlayRequest.Open()` / `Close()`.

### Nova rota (ex.: tela de upgrades)

1. **Create → MidnightMeow → Screen Flow → Scene Route**
2. Adicionar o asset ao array `routes` em `SceneFlowCatalog.asset`
3. Usar o novo `routeId` em `ScreenFlowRequest`

### Modos de transição (`ScreenTransitionMode`)

| Valor | Efeito |
|-------|--------|
| Instant | Carga imediata |
| Fade | Fade out → carga → fade in (`Time.unscaledDeltaTime`) |
| LoadingScreen | Fade + tela de loading + tempo mínimo (barra usa `LoadingBar` + `CurrentAsyncLoad`) |

`NetcodeHost` na rota: só o **host** chama `NetworkManager.SceneManager.LoadScene` (lobby → gameplay).

## Arquivos principais

- `Assets/Scripts/UI/ScreenFlow/ScreenFlowController.cs`
- `Assets/Scripts/UI/ScreenFlow/ScreenFlowRequest.cs`
- `Assets/Scripts/UI/ScreenFlow/SceneOverlayController.cs`
- `Assets/Data/UI/ScreenFlow/SceneFlowCatalog.asset`
- `Assets/Scripts/Core/Bootstrapper.cs` — inicia com `bootstrap_menu`

## Migração

- `GameFlowManager.LoadMenu/Lobby` → delegam ao catálogo.
- `SceneTransition` deixou de ser Singleton; visuais do Menu2 continuam no mesmo objeto.
- Game Over / Victory: podem trocar botões para `ScreenFlowRequest` com `return_menu`.
