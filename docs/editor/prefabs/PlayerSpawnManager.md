# Prefab: PlayerSpawnManager

Última revisão: 2026-06-28  
**Caminho:** `Assets/Prefabs/Multiplayer/PlayerSpawnManager.prefab`

## Resumo

Spawn de jogadores na rede: pontos no mapa e prefab NGO.

## Componentes

| Componente | Notas |
|------------|--------|
| `NetworkObject` | |
| `PlayerSpawnManager` | Lista de spawn points + prefab |

## Campos (YAML)

| Campo | Valor atual |
|-------|-------------|
| `playerNetworkPrefab` | **Cora** (fallback legado) — `guid: b18ed4e45e4d20a4dbdac339b666e689` |
| `characterPrefabs` | **Obrigatório:** Element 0 = Nixie (`CharacterA`), Element 1 = Cora (`CharacterB`) |
| `spawnPoints` | Array de Transforms na **cena de gameplay** (um por jogador ou mais) |

## Como configurar spawn em locais diferentes

1. Abra a cena de fase (`Fase-1`, `Fase-2`, …).
2. Crie vazios (ou use `---- Spawn Points Jogadores ----`):
   - `SP1`, `SP2`, … — só `Transform`, posicionados no mapa onde cada jogador deve surgir.
3. Selecione **`PlayerSpawnManager`** na Hierarchy.
4. No Inspector, em **Spawn Points**, defina o **Size** = número de jogadores (ex.: `2`).
5. Arraste `SP1` → Element 0, `SP2` → Element 1, etc.
6. **Ordem importa:** o 1º cliente conectado usa índice 0, o 2º usa índice 1 (`ClientId` por ordem de chegada).
7. Se faltar ponto, o sistema reutiliza o mesmo (offset `coSpawnSeparation` ≈ 1,35 unidades).

### Fase-1 (atual)

| Elemento | Objeto | Posição aprox. |
|----------|--------|----------------|
| 0 | `SP1` | (-36.6, 4.5) |
| 1 | `SP2` | (-39.7, 2.5) |

**Não confundir** com `spawnPoints` do `NetworkWaveManager` — aqueles são para **inimigos**, não jogadores.

## Campos adicionais (2026-06-10)

| Campo | Valor / notas |
|-------|----------------|
| `gameplaySpawnDelaySeconds` | `0.35` — fallback rápido se `SynchronizeComplete` não disparar |
| `coSpawnSeparation` | `1.35` — jogadores no mesmo spawn point surgem próximos, mas separados (~180°) |

## Fluxo esperado

1. Preparação → Characters: jogador escolhe Nixie (`CharacterA`) ou Cora (`CharacterB`).
2. Seleção persistida em `LobbySelectionStore`, `SaveProfileStore` e (MP) `PreparationSessionManager`.
3. Servidor spawna o prefab correspondente via `GetPrefabForClient` → `ResolveCharacterPrefab`.
4. **Todas as cenas Fase-* devem ter `characterPrefabs` preenchido** (Fase-2 estava vazio e sempre spawnava Cora).
5. Fallback em runtime: `CharacterPrefabLibrary` + menu **Setup All Phase Scenes**.

## Relacionados

- [Cora.md](Cora.md), [Nixie.md](Nixie.md)
- [Lobby.md](Lobby.md)
