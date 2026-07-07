# Carruagem (Fase 2)



Última revisão: 2026-07-05



## Comportamento



- Objeto com tag **Structure**, vida configurável, movimento ao longo de `CarriagePath`.

- Inimigos priorizam Player mas atacam Structure (`EnemyTargetFinder`); telegraphs aplicam dano via `PlayerCombatUtility`.

- Vida = 0 → para (`NetworkCarriage.IsBroken`); popup **"Aperte E para consertar"**.

- Conserto cooperativo (mesmo padrão do selamento).

- Chegada ao fim do trajeto → `PhaseObjectiveManager.NotifyCarriageArrived()` + `GameEvents.OnCarriageArrived` → vitória Fase 2; progresso em `OnCarriagePathProgressChanged` (replicado via `NetworkVariable` para clientes).

- **HUD Fase 2:** `PhaseObjectiveHud` mostra `Carruagem: X%` junto com buracos e inimigos.

- **Barra de vida:** `EnemyHealthBarDisplay` na carruagem; vida sincronizada por `NetworkCarriage`.

- **Placeholder visual:** quadrado marrom (~2,4×1,6 uu) gerado em runtime; **não** usar sprites de menu/UI no prefab.

- **Multiplicador opcional:** `CarriageConfig.visualScale` (padrão **1**). Só aumente quando houver arte final proporcional.



## Trajeto (configurável)



Em `CarriageConfig`:



| Campo | Padrão | Descrição |

|-------|--------|-----------|

| `pathStartX` | -42 | Waypoint inicial (X) |

| `pathEndX` | 18 | Waypoint final (X) |

| `useCustomPathY` | false | Usar `pathY` fixo em vez do centro do mapa |

| `pathY` | 0 | Y do trajeto quando `useCustomPathY` |



### Setup em runtime (importante)



1. **`PhaseGameplayContentInstaller`** (todos os peers): cria/atualiza `CarriagePath` com filhos `Waypoint_Start` / `Waypoint_End`, posiciona a carruagem no início e liga `NetworkCarriage.path`.

2. **`NetworkCarriageSpawner`** (servidor): instancia o prefab se a cena não tiver carruagem, faz `NetworkObject.Spawn()` e repete a configuração até path + spawn estarem prontos.

3. **`NetworkCarriage.OnNetworkSpawn`** (todos os peers): chama `PhaseGameplayContentInstaller.ConfigureCarriage` se `path` estiver vazio; retry local (~2s) até `CarriagePath` com ≥2 waypoints. Progresso HUD sincronizado via `_pathProgress` (`NetworkVariable`) + `GameEvents.OnCarriagePathProgressChanged`.

**Multiplayer:** `CarriagePath` **não** é objeto de rede — cada peer cria o trajeto localmente com os mesmos waypoints (`CarriageConfig` + bounds). **Posição visual no Cliente:** `NetworkTransform` (autoridade do servidor) replica `transform.position`; **HUD:** `NetworkVariable<float> _pathProgress` → `PathProgressChanged` / `PhaseObjectiveHud`. Não aplicar `ApplyPathPosition` no Cliente (evita conflito com `NetworkTransform`).

O array `waypoints` no Inspector de `CarriagePath` pode parecer vazio **fora do Play** — os waypoints são filhos criados em runtime. Durante o Play, veja a hierarquia `CarriagePath/Waypoint_*`.

**Movimento:** posição avança em direção ao waypoint final (`moveSpeed`); o progresso HUD (`0–100%`) vem da projeção real no segmento do path. Vitória dispara em `≥98%`, dentro de `arrivalZoneRadius`, ou ao encostar no fim. O fim X é limitado ao bounds do mapa (`max.x - 2`).



### Solo (single player)



O host local é iniciado em **Loading2** (`GameplaySessionStarter.EnsureReadyForGameplay`) **antes** de carregar a fase. A carruagem depende desse host — não teste Fase-2 abrindo a cena direto sem passar pelo fluxo Menu → Contrato → Loading2.



## Configuração



- `Assets/Data/Gameplay/CarriageConfig.asset`

- Prefab: `Assets/Prefabs/Gameplay/Carriage.prefab` (registrado em `DefaultNetworkPrefabs`)

- Catálogo runtime: `GameplayPrefabCatalog.carriagePrefab`



Setup de cena (Editor): **MidnightMeow → Phases → Setup Active Phase Scene** (Fase-2) — opcional; o runtime cobre path + posição.



## Prefab sugerido



| Componente | Notas |

|------------|--------|

| `NetworkObject` | Spawn pelo host (`NetworkCarriageSpawner` ou in-scene); `SynchronizeTransform = true` |
| `NetworkTransform` | Autoridade do servidor; sync só posição X/Y |

| `NetworkCarriage` | Referência a `CarriageConfig`; `path` pode ficar vazio (runtime preenche) |

| `HealthComponent` | **Um único** componente; `SetAllowDestroyOnDeath(false)` |

| `EnemyHealthBarDisplay` | Barra world-space acima do sprite |

| Collider2D | Para dano melee/projétil |

| Tag | `Structure` |

| Filho `Visual` | `SpriteRenderer` **sem sprite** — placeholder aplicado em runtime |



## Código



- `NetworkCarriage`, `CarriagePath`, `NetworkCarriageSpawner`

- `CarriageRepairPromptUI`, `CarriageRepairZoneVisual` (instalados no jogador via `PlayerGameplayModuleInstaller`)

- `PhaseGameplayContentInstaller.ConfigureCarriage()` / `EnsureCarriageSetup()`

