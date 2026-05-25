# Personagens jogáveis (índice)

Última revisão: 2026-05-22

O prefab único `Assets/Prefabs/Characters/Player.prefab` **não existe mais**. O multiplayer usa dois prefabs selecionáveis no Lobby:

| Personagem | Prefab | Doc |
|------------|--------|-----|
| **Cora** (ranged) | `Assets/Prefabs/Characters/Cora.prefab` | [Cora.md](Cora.md) |
| **Nixie** (melee) | `Assets/Prefabs/Characters/Nixie.prefab` | [Nixie.md](Nixie.md) |

## Spawn em rede

- `PlayerSpawnManager.playerNetworkPrefab` → **Cora** (`guid: b18ed4e45e4d20a4dbdac339b666e689`)
- Lobby deve permitir escolher Cora ou Nixie; preencher `PlayerSpawnManager.characterPrefabs` quando a seleção por personagem estiver ativa.

## Componentes comuns (ambos)

`NetworkObject`, `OwnerNetworkTransform`, `NetworkPlayerController`, `NetworkPlayerHealth`, `NetworkPlayerRevive`, `NetworkPlayerSpectator`, `PlayerDash`, `PlayerGameplayModuleInstaller` (imunidade + UI downed/revive), `PlayerDamageImmunity` (via installer), input/movement/health/audio compartilhados.

## Histórico

| Data | Alteração |
|------|-----------|
| 2026-05-22 | Doc dividida em Cora.md + Nixie.md; GUID `b18ed4e45e4d20a4dbdac339b666e689` = Cora |
