# Diagrama — Fluxo de Telas

Baseado em [screen-flow.md](./screen-flow.md) e [screen-flow.md (requisitos)](../screen-flow.md).

## Diagrama principal

```mermaid
flowchart TD
    subgraph init [1. Inicialização]
        Bootstrap[BootstrapScene]
        Menu[Menu Principal]
        Saves[Painel Saves]
        Options[Painel Opções]
        Bootstrap --> Menu
        Menu --> Saves
        Menu --> Options
    end

    subgraph lobby [2. Lobby]
        LobbyMode[Lobby — Solo / Host / Entrar]
        Menu -->|Novo Jogo| LobbyMode
        Saves -->|Continuar save| LobbyMode
        LobbyMode -->|2 jogadores ou Solo| Load1[Loading1]
        LobbyMode -->|Personagens| CharsBrowse[Characters — consulta]
        CharsBrowse --> LobbyMode
    end

    subgraph prep [3. Preparação]
        Preparation[Preparation — contrato + personagem + pronto]
        Load1 --> Preparation
        Preparation -->|Escolher Personagem| CharsSelect[Characters — seleção]
        CharsSelect --> Preparation
        Preparation -->|todos prontos| Load2[Loading2]
    end

    subgraph loop [4. Loop de Gameplay]
        Gameplay[Fase-1 / Fase-2 / Fase-3]
        Victory[VictoryScene]
        Defeat[GameOver]
        Credits[Créditos]
        Load2 --> Gameplay
        Gameplay -->|vitória| Victory
        Gameplay -->|derrota| Defeat
        Victory -->|Prosseguir Fase-1/2| Load2
        Victory -->|Prosseguir Fase-3| Credits
        Defeat -->|Reiniciar| Load2
        Victory -->|Sair| Menu
        Defeat -->|Sair| Menu
    end

    Menu -->|Sair| Quit[Application.Quit]
```

## Componentes

| Papel | Script |
|-------|--------|
| Máquina de estados | `ScreenFlowStateMachine` + `GameSessionContext` |
| Troca de cena | `ScreenFlowController` + `SceneFlowCatalog` |
| Menu | `MainMenuController` |
| Lobby | `LobbyFlowController` + `LobbySessionManager` |
| Preparação | `PreparationScreenController` + `PreparationSessionManager` |
| Personagens | `CharactersScreenController` + `CharactersSessionManager` |
| Loading | `LoadingScreenController` |
| Fim de partida | `EndGameScreenController` |
| Contrato | `Contract_1.asset` → `Fase-1` |
