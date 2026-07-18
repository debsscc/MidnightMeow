# Carriage.prefab

Última revisão: 2026-07-18

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
├── HealthComponent (IDamageable)
├── CarriageController → CarriageConfig
│   └── NetworkVariable<CarriageState> Idle | Moving | Broken
├── CarriagePresenceZoneVisual   ← runtime (anel pastel do raio de presença)
│   └── PresenceZoneRing         ← SealZoneRingVisual
├── NetworkCarriageHealth
├── NetworkCarriageRepairManager → CarriageConfig
├── CarriageRepairWorldUI (labels escolta + conserto)
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
| `Carriage_Reference.png` | Ícone follower do HUD Fase-2 (`PhaseObjectiveHudVisuals`) |

Import: Sprite Single, **PPU 100**, Filter **Point**, pivot **centro** (0.5, 0.5).

## Notas para agentes

- `CarriageConfig.useOfficialArt = true` → **não** força placeholder marrom.
- `CarriageConfig.visualRootScale = 0.9` → arte oficial em escala 3×; collider e regras de gameplay permanecem inalterados.
- Label world-space: `worldLabelFontSize = 0.9`, sorting `450`, opacidade ~0.78 — mesmo padrão de selar/reviver (`GameplayUiFonts.ApplyWorldInteraction`). Offset vertical da carruagem: `repairLabelOffset.y = 4.6` (acima da barra de HP; runtime ainda garante clearance). Durante conserto ativo, o texto fica **acima ou abaixo** dos círculos (`CooperativeZoneLabelPlacementUtility`), sem sobrepor a área.
- Rodas giram em **todos os peers** via `CarriageWheelSpinner` (sem sync de ângulo).
- Progresso HUD: `NetworkVariable<float> _pathProgress`.
- Estado de escolta: `NetworkVariable<CarriageState> _carriageState` (servidor).
- Presença de jogadores: `Physics2D.OverlapCircle` + `playerPresenceRadius`.
- Visual de presença: `CarriagePresenceZoneVisual` cria anel pastel (`SealZoneRingVisual`) no mesmo raio; Idle um pouco mais visível, Moving mais suave; oculto em Broken / chegada. Campos no `CarriageConfig` (header **Visual da área de presença**).
- Gizmo editor: `CarriageController.OnDrawGizmos` (wire sphere ciano) — só Scene View.
- Posição: `NetworkTransform` (servidor).
- Conserto: `PlayerCarriageRepairInteraction` + `NetworkCarriageRepairManager` (arquivo próprio) + `CarriageRepairWorldUI` + zonas `SealZoneRingVisual`.
- Fix Input E: [guides/carriage-repair-fix.md](../guides/carriage-repair-fix.md).
- Spawn: servidor (`CarriageSpawner`); não colocar carruagem fixa na cena Fase-2.
- Setup Inspector: [guia escolta/aggro/telegraph](../guides/carriage-phase2-aggro-setup.md).
- Conserto E / zonas: [guia repair fix](../guides/carriage-repair-fix.md).

## Ver também

- [gameplay/carriage.md](../../gameplay/carriage.md)
- [troubleshooting/common-errors.md](../../troubleshooting/common-errors.md)
