# Carruagem (Fase 2)

Última revisão: 2026-07-18

## Comportamento

- Objeto com tag **Structure** e layer **Structure**, vida em `CarriageConfig.maxHealth`, movimento ao longo de `CarriagePath`.
- **Estados de rede** (`NetworkVariable<CarriageState>` no `CarriageController`):
  - `Idle` — nenhum jogador vivo no raio de presença → parado; label “Se aproximem da Carruagem”
  - `Moving` — ≥1 jogador com `CanFight` no raio (`Physics2D.OverlapCircle`) → avança no path; label “Protejam a Carruagem”
  - `Broken` — HP 0 (`NetworkCarriageHealth.IsBroken`) → movimento parado; label “Consertem…” / Press E / progresso
- **Aggro dos ratos** via `EnemyStats.aggroType` (`PlayersOnly` / `StructuresOnly` / `Dynamic`) — ver [guia Editor](../editor/guides/carriage-phase2-aggro-setup.md).
- Conserto cooperativo (mesmo padrão do selamento): **E** → zonas → progresso no servidor → restaura `repairRestoreHealthFraction` da vida.
  - Manager isolado: `NetworkCarriageRepairManager` (arquivo próprio — RPC NGO).
  - SFX: `Interacao.wav` ao iniciar (`GameplayInteractAudio.PlayConfirm`); `Reviver.wav` ao concluir (`PlayReviveComplete`).
  - Guia de correção do E: [carriage-repair-fix.md](../editor/guides/carriage-repair-fix.md)
- Chegada ao fim → `PhaseObjectiveManager.NotifyCarriageArrived()` + `GameEvents.OnCarriageArrived` → vitória Fase 2.
- **HUD:** `PhaseObjectiveHud` (Fase-2) — banner “Proteja a carruagem” + barra de trajeto (branco restante = `1 - PathProgress`) + ícone `Carriage_Reference` que acompanha o progresso.
- **Arte oficial:** hierarquia `VisualRoot` com 3 camadas (Body / Wheels / Back). Ver [prefabs/Carriage.md](../editor/prefabs/Carriage.md).
- **Placeholder legado:** só se `CarriageConfig.useOfficialArt = false`.

## Visual / rodas

| Peça | Responsável |
|------|-------------|
| Escala | `CarriageConfig.visualRootScale` aplicado em `VisualRoot` |
| Giro das rodas | `CarriageWheelSpinner` (local; pausa / quebrada / chegada param) |
| Raios | `frontWheelRadius` / `backWheelRadius` no config |
| Collider | `colliderSize` / `colliderOffset` no config |
| Label escolta/conserto | `repairLabelOffset` (base) + clearance acima da HP; com zonas ativas, `CooperativeZoneLabelPlacementUtility` coloca o texto acima/abaixo dos círculos |
| Área de presença (escolta) | `CarriagePresenceZoneVisual` — anel pastel (`SealZoneRingVisual`) com diâmetro `2 × playerPresenceRadius`; Idle mais legível, Moving mais suave; some em Broken/chegada |

Não sincronizar ângulo de roda na rede — deriva do progresso/movimento já replicado.

## Trajeto

Em `CarriageConfig`: `pathStartX` (-42), `pathEndX` (18), `moveSpeed`, `arrivalZoneRadius`, `playerPresenceRadius`.

1. **`PhaseGameplayContentInstaller`** — cria/atualiza `CarriagePath` em todos os peers.
2. **`CarriageSpawner`** (servidor) — instancia o prefab, `NetworkObject.Spawn()`.
3. **`CarriageController`** — servidor avança `_pathProgress` só em `Moving`; clientes seguem via `NetworkTransform`.

**Solo:** host inicia em Loading2 antes da fase. Não abrir `Fase-2` sem host.

## Configuração

- `Assets/Data/Gameplay/CarriageConfig.asset`
- Prefab: `Assets/Prefabs/Gameplay/Carriage.prefab`
- Sprites: `Assets/Art/Sprites/Carriage/`
- Catálogo: `GameplayPrefabCatalog.carriagePrefab`
- Setup pós-código: [guia Editor escolta/aggro](../editor/guides/carriage-phase2-aggro-setup.md)

Setup Editor: **MidnightMeow → Phases → Setup Active Phase Scene** (Fase-2).

## Código ativo

- `CarriageController`, `CarriageState`, `CarriageSpawner`, `CarriagePath`, `CarriageWheelSpinner`
- `CarriagePresenceZoneVisual` (anel de escolta)
- `NetworkCarriageHealth`, `NetworkCarriageRepairManager`, `CarriageDamageFilter`
- `CarriageRepairWorldUI`, `CarriageRepairZoneVisualHost`
- `PhaseGameplayContentInstaller.ConfigureCarriage()` / `EnsureCarriageSetup()`
- `EnemyTargetFinder` + `EnemyStats.AggroType` (busca de alvos)
