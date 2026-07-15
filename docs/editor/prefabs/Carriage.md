# Carriage.prefab

Última revisão: 2026-07-14

| Campo | Valor |
|-------|--------|
| Caminho | `Assets/Prefabs/Gameplay/Carriage.prefab` |
| GUID | `c7eb2d7bdfd62084e9d7c4fbf793d8b9` |
| Rede | Registrado em `DefaultNetworkPrefabs.asset` |
| Catálogo | `GameplayPrefabCatalog.carriagePrefab` |
| Arte | `Assets/Art/Sprites/Carriage/` |

## Hierarquia

```
Carriage (tag Structure, layer Structure)
├── NetworkObject
├── NetworkTransform (AuthorityMode = Server)
├── HealthComponent
├── CarriageController → CarriageConfig
├── NetworkCarriageHealth
├── NetworkCarriageRepairManager → CarriageConfig
├── CarriageRepairWorldUI
├── CarriageWheelSpinner (giro local das rodas)
├── BoxCollider2D (tamanho via CarriageConfig)
└── VisualRoot                          ← escala uniforme (visualRootScale)
    ├── Layer_Body          (sorting 25 — atrás das rodas)
    │   └── Body            Carriage_Body
    ├── Layer_Back          (reservado)
    └── Layer_Wheels
        ├── Wheel_Back      ← pivot no eixo
        │   ├── Tire        sorting 26
        │   └── Spokes      sorting 27
        └── Wheel_Front
            ├── Tire        sorting 26
            └── Spokes      sorting 27
```

## Artes (import)

| Arquivo | Uso |
|---------|-----|
| `Carriage_Body.png` | Corpo (camada frente) |
| `Carriage_Tire_Front/Back.png` | Aro/pneu |
| `Carriage_Spokes_Front/Back.png` | Cruz/raios (gira com o pneu) |
| `Carriage_Reference.png` | Só referência — **não** usar no play |

Import: Sprite Single, **PPU 100**, Filter **Point**, pivot **centro** (0.5, 0.5).

## Notas para agentes

- `CarriageConfig.useOfficialArt = true` → **não** força placeholder marrom.
- Rodas giram em **todos os peers** via `CarriageWheelSpinner` (sem sync de ângulo).
- Progresso HUD: `NetworkVariable<float> _pathProgress`.
- Posição: `NetworkTransform` (servidor).
- Conserto: `PlayerCarriageRepairInteraction` + `CarriageRepairWorldUI` + zonas.
- Spawn: servidor (`CarriageSpawner`); não colocar carruagem fixa na cena Fase-2.

## Ver também

- [gameplay/carriage.md](../../gameplay/carriage.md)
- [troubleshooting/common-errors.md](../../troubleshooting/common-errors.md)
