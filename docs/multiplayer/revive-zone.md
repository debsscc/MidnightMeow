# Reviver por zona (área)

Última revisão: 2026-05-22

## Comportamento

1. Jogador A cai **inconsciente** (sem controle).
2. Surge um **círculo verde** ao redor de A (`reviveZoneRadius` em `DownedPlayerConfig`).
3. Jogador B (vivo) entra no círculo e **permanece** → barra de progresso sobe.
4. B sai do círculo → progresso **decai** (`reviveZoneProgressDecayPerSecond`).
5. Timer de inconsciência **pausa** enquanto alguém está na zona.
6. Progresso = 100% → A revivido com fração de vida configurada.

## Configuração (`DownedPlayerConfig`)

| Campo | Padrão | Descrição |
|-------|--------|-----------|
| `reviveZoneRadius` | 2.2 | Raio da área |
| `reviveZoneFillDuration` | 3s | Tempo parado na zona para concluir |
| `reviveZoneProgressDecayPerSecond` | 0.75 | Queda do progresso ao sair |
| `reviveZone*Color` | Verde | Visual do círculo (shader TelegraphFill) |

## Código

- `DownedReviveZoneSystem` — tick único no servidor
- `DownedReviveZoneVisual` — círculo no jogador caído
- `RevivePromptWorldUI` — texto no aliado vivo
- `NetworkPlayerRevive` — flag local `IsContributingToRevive` (sem botão Interact)

## Teste em Fase-1

Host + cliente: derrubar um jogador, outro entra no círculo verde e aguarda ~3s.
