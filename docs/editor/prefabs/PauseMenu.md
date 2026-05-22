# Prefab: PauseMenu

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/UI/PauseMenu.prefab`  
**GUID:** `2542914e2b637bb4b871cad284433d66`

## Resumo

Menu de pausa (UI). Usado como **nested prefab** em `Lobby.prefab` e em cenas de fase.

## Estrutura típica

- Botões: retomar, configurações, sair (confirmar nomes no Hierarchy)
- `EventTrigger` em botões para feedback
- Layout: `HorizontalLayoutGroup` / `VerticalLayoutGroup`

## Componentes

| Tipo | Uso |
|------|-----|
| `Image`, `Button`, TMP | UI |
| `EventTrigger` | Hover/click custom |

## Valores a confirmar no Editor

| Campo / objeto | Notas | Valor atual |
|----------------|-------|-------------|
| Canvas Group | Interactable / Blocks Raycasts quando pausado | |
| Botão Resume | OnClick → qual script/método? | |
| Botão Quit | Cena de destino | |
| Referências em `GameManager` / cena | `pauseMenuObject` na cena | |

## Referenciado por

- `Assets/Prefabs/Multiplayer/Lobby.prefab` (nested)
- `Assets/Scenes/Fases/Fase-1.unity` (overrides de instância)
- `Assets/Scenes/UI/Menu2.unity` (`pauseMenuObject`)
