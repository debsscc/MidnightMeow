# Gameplay — Fases 1–3

Última revisão: 2026-06-25

## Status

| Fase | Contrato | Cena | Mecânicas |
|------|----------|------|-----------|
| 1 | 1 | `Fase-1` | Ondas + selamento de buracos |
| 2 | 2 | `Fase-2` | Ondas + selamento + carruagem horizontal |
| 3 | 3 | `Fase-3` | Boss (`Rato_Boss`) — sem selamento/carruagem |

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
