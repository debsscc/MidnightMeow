# Carriage.prefab

Última revisão: 2026-07-08

| Campo | Valor |
|-------|--------|
| Caminho | `Assets/Prefabs/Gameplay/Carriage.prefab` |
| GUID | `c7eb2d7bdfd62084e9d7c4fbf793d8b9` |
| Rede | Registrado em `DefaultNetworkPrefabs.asset` |
| Catálogo | `GameplayPrefabCatalog.carriagePrefab` |

## Hierarquia

```
Carriage (tag Structure, layer Structure)
├── NetworkObject
├── NetworkTransform (AuthorityMode = Server)
├── HealthComponent (max 120, não destrói ao morrer)
├── CarriageController → CarriageConfig, path (vazio OK — runtime)
├── NetworkCarriageHealth
├── CarriageDamageFilter
├── NetworkCarriageRepairManager → CarriageConfig
├── CarriageRepairWorldUI
├── BoxCollider2D
└── Visual
    └── SpriteRenderer (placeholder marrom runtime)
```

## Notas para agentes

- **Não** atribuir sprites de menu/UI no `Visual` — `CarriageController` força placeholder quadrado.
- Progresso HUD: `NetworkVariable<float> _pathProgress` (servidor escreve; clientes leem via `PhaseObjectiveHud`).
- Posição replicada via `NetworkTransform` (servidor move; clientes não aplicam path localmente).
- `path` no Inspector pode ficar vazio; `PhaseGameplayContentInstaller` + `CarriageSpawner` preenchem em runtime.
- Conserto: `PlayerCarriageRepairInteraction` no jogador; label em `CarriageRepairWorldUI`; zonas em `CarriageRepairZoneVisualHost`.
- Fase-2: instância in-scene **ou** spawn dinâmico pelo host.
- Setup: **MidnightMeow → Phases → Setup Active Phase Scene**.

## Ver também

- [gameplay/carriage.md](../../gameplay/carriage.md)
- [troubleshooting/common-errors.md](../../troubleshooting/common-errors.md) — secção carruagem
