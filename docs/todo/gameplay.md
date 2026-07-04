# Gameplay — Fases 1–3

Última revisão: 2026-07-04

## Status

| Fase | Contrato | Cena | Mecânicas |
|------|----------|------|-----------|
| 1 | 1 | `Fase-1` | Spawn por buraco (SO) + selamento |
| 2 | 2 | `Fase-2` | Spawn por buraco + selamento + carruagem horizontal |
| 3 | 3 | `Fase-3` | Boss (`Rato_Boss`) — sem selamento/carruagem |

## Spawn por buraco (GDX)

Ver [rat-hole-sealing.md](../gameplay/rat-hole-sealing.md). O sistema legado de ondas (`WaveGenerator` / `WaveSettings`) permanece apenas para fases que ainda usam `useWaveSpawning`.

## Setup obrigatório no Editor

Após atualizar o código, execute:

1. **MidnightMeow → Phases → Setup All Phase Scenes**
2. **MidnightMeow → Phases → Register Network Prefabs (Boss + Carriage)**

## Desbloqueio de contratos

- Testes: `Assets/Resources/ContractProgressionConfig.asset` → `unlockAllContractsForTesting = true`
- Produção: desligar flag; progressão linear via `completedContractMask` no save

## Barras de vida inimigos

`EnemyHealthBarDisplay` é adicionada automaticamente em inimigos com tag `Enemy`. Boss usa `BossEnemyMarker` para barra sempre visível.

Ver plano completo: [phases-implementation.md](phases-implementation.md)

---

### HABILIDADE DA CORA - Duplicação de áreas na Barreira da Cora
- **Comportamento Atual vs Desejado:** Dois retângulos visuais (debug + barreira) simultâneos. Desejado: um único retângulo como barreira física/lógica — bloqueia inimigos, libera jogadores aliados e projéteis aliados.
- **Arquivos Investigados:** `Assets/Scripts/Components/Ability/CoraBarrierAbilityExecutor.cs`, `Assets/Scripts/Components/Ability/CoraBarrier.cs`, `Assets/Scripts/Multiplayer/Combat/NetworkCoraBarrier.cs`, `Assets/Scripts/Combat/AbilityDebugVisualHost.cs`, `Assets/Scripts/Components/Player/PlayerAbilityHandler.cs`, `Assets/Scripts/Multiplayer/Player/NetworkPlayerAbilityRelay.cs`, `Assets/Scripts/Combat/CombatLayerCollision.cs`
- **Causas Prováveis Identificadas:**
  1. **Overlay de debug sobreposto à barreira real:** `PlayerAbilityHandler` chama `_debugHost.ShowAbility(CoraBarrier, …)` com duração = `tierData.effectDuration`; `AbilityDebugVisualHost` instancia `SpriteRenderer` com `AbilityZoneFill` — segundo retângulo visual independente do `BoxCollider2D` da `CoraBarrier`.
  2. **Replicação remota duplica debug:** `NetworkPlayerAbilityRelay` também invoca `_debugHost.ShowAbility` no ClientRpc de habilidades — owner e peers podem exibir retângulo debug além do prefab spawnado via `NetworkAbilityObjectSpawner`.
  3. **Layer `Barrier` sem matriz completa em código:** `CoraBarrier` usa layer `Barrier` e comentário indica passagem de jogador/projétil via Physics Matrix, mas `CombatLayerCollision` só configura Player/Enemy/Projectile — se a matrix do Editor não ignorar Barrier×Player e Barrier×Projectile, jogadores podem colidir; se Barrier×Enemy não estiver bloqueando, inimigos atravessam.
- **Plano de Ação Recomendado:**
  1. Desativar `ShowAbility` para `CharacterAbilityType.CoraBarrier` em `PlayerAbilityHandler` e `NetworkPlayerAbilityRelay` (barreira física já é o feedback visual).
  2. Opcional: material/shader leve no prefab da barreira (reutilizar `TelegraphFill` como zona estática) em vez do debug host.
  3. Auditar **Edit → Project Settings → Physics 2D → Layer Collision Matrix**: Barrier×Enemy = colide; Barrier×Player e Barrier×Projectile = ignorado; validar tag/layer de projéteis aliados (`Projectile`).
  4. Teste MP: Cora usa Q — um retângulo, inimigo não passa, jogador e projétil aliado passam.
