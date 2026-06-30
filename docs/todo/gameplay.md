# Gameplay — Fases 1–3

Última revisão: 2026-06-30

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
