# Gameplay — Fases 1–3

Última revisão: 2026-07-07

## Pendências (backlog)

- [ ] Glitch de UI na Fase 2: A tela de controles aparece incorretamente por alguns instantes antes da transição para a tela de vitória.
- [ ] Habilidade R (Poça da Cora): Shader dessincronizado. O tamanho visual no Cliente está minúsculo, não refletindo a área real de dano que o Host enxerga.
- [ ] Refinamento da Mecânica de Reviver.
- [ ] Implementação da Mecânica de Arrumar a Carruagem.
- [ ] Chefe/Inimigo: Rei Rato.

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

## [TASK CONCLUÍDA] Duplicação de áreas na Barreira da Cora

- **O que foi feito:** Removido `ShowAbility` para `CharacterAbilityType.CoraBarrier` em `PlayerAbilityHandler` e `NetworkPlayerAbilityRelay` — barreira física (`CoraBarrier` + prefab rede) é o único retângulo visual. `CombatLayerCollision` configura layer `Barrier`: ignora colisão com `Player` e `Projectile`; mantém colisão com `Enemy` e `ProjectileEnemy`. Scripts: `PlayerAbilityHandler.cs`, `NetworkPlayerAbilityRelay.cs`, `CombatLayerCollision.cs`.

- **Como testar (Singleplayer):** Fase com Cora → Q (barreira) → um único retângulo; jogador atravessa; inimigo não.

- **Como testar (Multiplayer/Netcode):** Host + Cliente → Cora usa Q → um retângulo em ambos; projétil aliado (`Projectile`) passa; inimigo bloqueado.

- **Resultado Esperado:** Sem overlay duplicado do `AbilityDebugVisualHost`; colisão 2D coerente com design da habilidade.

---

## Verificação manual

- [x] Cora barreira: um retângulo, colisão correta (código — validar em Play)
