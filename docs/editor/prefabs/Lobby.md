# Prefab: Lobby

Última revisão: 2026-07-17  
**Caminho:** `Assets/Prefabs/Multiplayer/Lobby.prefab`

## Resumo

UI da partida multiplayer em jogo: HUD, lobby in-game, sliders, indicadores. Contém **nested prefab** `PauseMenu`.

Botão **CopyCode**: Image usa `Caixa_Menu_2.png`, tamanho 300×100, texto `COPY CODE`. Página direita alinhada ao Desconectar (anchor bottom-right, pos visual do print ≈ Pos X -504 / Y 380). **Pivot (0.5, 0.5)** para o hover (`Button_Juiceness`) escalar pelo centro do sprite; `anchoredPosition` equivalente com pivot central ≈ (-654, 430).
Botão **Disconnect**: idle com `NormalColor` alpha 0.55 (fade); hover volta a opacidade total. Texto na mesma cor dos outros botões (`#323232`). Em modo Singleplayer vira **VOLTAR** com `soloDisconnectAnchoredPos` ≈ (309, -109) (anchor/pivot middle-center no override da cena). **Jogar** (Toggle): root 360×164.3 com Background stretch e pivot centro — hover escala pelo meio da arte.

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
| Canvas (`Lobby` raiz) | Render Mode | **Screen Space - Camera** (override na `Lobby.unity` → Main Camera) | |
| Panel (fundo) | Image Material | **Sprite Lit Default** (`Assets/Art/Materials/Sprite Lit Default.mat`) | |
| `Lobby.unity` (cena) | Light + ParticleSystem | Na **raiz da cena** (não no prefab) | |
| `Lobby.unity` | Main Camera | Post Processing **ligado** (Global Volume nos filhos de `Light`) | |

## Ambience (luz + partículas)

Igual ao Menu2: `Light` e `ParticleSystem` ficam na **cena** `Lobby.unity`, não dentro do prefab.

- Fundo do `Panel` precisa de **Sprite Lit Default** para as `Light2D` aparecerem.
- Canvas do lobby em **Screen Space - Camera** (não Overlay), senão o UI cobre partículas e ignora profundidade da cena.
- `TorchLight` na layer **UI** (não Collectable).

O mesmo checklist foi aplicado nas cenas **Loading1**, **Loading2**, **Preparation** e **Characters** (objetos na raiz da cena, sem prefab compartilhado).


- Prefab muito grande (muitos elementos TMP/UI). Priorize validar referências quebradas no Inspector após reimport.
- Histórico: erro de GUID do PauseMenu corrigido restaurando `.meta` original.
