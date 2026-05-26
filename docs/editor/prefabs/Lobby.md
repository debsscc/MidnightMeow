# Prefab: Lobby

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/Lobby.prefab`

## Resumo

UI da partida multiplayer em jogo: HUD, lobby in-game, sliders, indicadores. Contém **nested prefab** `PauseMenu`.

## Nested prefabs

| Nome | Asset | GUID |
|------|-------|------|
| PauseMenu | `Assets/Prefabs/UI/PauseMenu.prefab` | `2542914e2b637bb4b871cad284433d66` |

## Scripts principais (raiz / filhos)

| Script | Função |
|--------|--------|
| `MultiplayerLobbyUI` | Fluxo UI do lobby |
| `MultiplayerHUD` | HUD durante partida |
| `AdrenalineBarUi` | Barra de adrenalina |
| `HordeIndicator` | Indicador de horda |

## Valores a confirmar no Editor

| Objeto / script | Campo | Valor esperado | Valor atual |
|-----------------|-------|----------------|-------------|
| MultiplayerLobbyUI | *(listar refs de botões, textos, painéis)* | | |
| MultiplayerHUD | *(cards de jogador, etc.)* | | |
| Nested PauseMenu | Ativo por padrão? | | |
| Canvas | Render Mode / Sort Order | | |

## Notas

- Prefab muito grande (muitos elementos TMP/UI). Priorize validar referências quebradas no Inspector após reimport.
- Histórico: erro de GUID do PauseMenu corrigido restaurando `.meta` original.
