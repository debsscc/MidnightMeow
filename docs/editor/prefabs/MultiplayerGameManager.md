# Prefab: MultiplayerGameManager

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/Multiplayer/MultiplayerGameManager.prefab`

## Resumo

Estado global da sessão multiplayer (rede).

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | Objeto de rede |
| `MultiplayerGameManager` | Regras de sessão, spawn, fim de partida |

## Valores a confirmar no Editor

| Campo | Descrição | Valor atual |
|-------|-----------|-------------|
| *(todos os SerializeField de MultiplayerGameManager)* | Preencher após abrir script | |

## Instanciação

- Deve existir uma instância por sessão (bootstrap ou cena MP).
- Verificar ligação em `MultiplayerBootstrapper.gameManager`.
