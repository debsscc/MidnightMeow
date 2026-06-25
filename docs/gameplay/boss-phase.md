# Fase 3 — Boss

Última revisão: 2026-06-25

## Comportamento

- Contrato 3 carrega `Fase-3.unity`
- Uma onda com um único `Rato_Boss` (200 HP, escala 2×)
- Sem selamento de buracos nem carruagem
- Vitória ao eliminar o boss (`GameEvents.OnNightEnded`)

## Prefab

| Campo | Valor |
|-------|-------|
| Caminho | `Assets/Prefabs/Enemies/Rato_Boss.prefab` |
| Base | `Rato_Padrao_Resistente` |
| HP | 200 |
| Componente extra | `BossEnemyMarker` (barra de vida sempre visível) |
| Rede | Registrado em `DefaultNetworkPrefabs.asset` |

## Dados

- `Assets/Data/Stats/Game/Fase3.asset` — 1 inimigo, 1 onda
- Catálogo: `Resources/PhaseWaveSettingsCatalog.asset` → entrada `Fase-3`

## Habilidades futuras

`BossEnemyMarker` e `EnemyTelegraphedAttacker` estão prontos para extensão; ataques especiais do boss ficam para iteração posterior.
