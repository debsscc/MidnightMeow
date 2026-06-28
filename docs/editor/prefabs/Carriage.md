# Carriage.prefab

Última revisão: 2026-06-28

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
├── HealthComponent (max 120, não destrói ao morrer)
├── NetworkCarriage → CarriageConfig, path (vazio OK — runtime)
├── BoxCollider2D
└── Visual
    └── SpriteRenderer (sem sprite — placeholder runtime)
```

## Notas para agentes

- **Não** atribuir sprites de menu/UI no `Visual` — `NetworkCarriage` força placeholder quadrado.
- `CarriageConfig.visualScale` padrão **1**; tamanho alvo ~2,4×1,6 uu.
- `path` no Inspector pode ficar vazio; `PhaseGameplayContentInstaller` + `NetworkCarriageSpawner` preenchem em runtime.
- Fase-2 pode ter instância in-scene **ou** spawn dinâmico pelo host.
- Setup opcional: **MidnightMeow → Phases → Setup Active Phase Scene** (cria `CarriagePath` + waypoints na cena).

## Ver também

- [gameplay/carriage.md](../../gameplay/carriage.md)
- [troubleshooting/common-errors.md](../../troubleshooting/common-errors.md) — secção carruagem
