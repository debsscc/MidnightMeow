# Estrutura de Assets

Última revisão: 2026-07-10

## Visão geral

```
Assets/
├── Art/                 # Sprites, Materials, Shaders, Fonts, Models
├── Audio/               # Música e SFX (clips)
├── Resources/
│   └── MidnightMeowAudioMixer.mixer  # Mixer único (Master / Music / SFX)
├── Data/                # Instâncias ScriptableObject (balanceamento)
│   ├── Characters/      # GameplayProfile + AnimationProfile por personagem
│   ├── Abilities/
│   ├── Combat/
│   ├── Contracts/
│   ├── Multiplayer/
│   ├── Stats/           # Player, Enemies, Game, Projectiles
│   ├── Tutorial/        # TutorialTipSO + TutorialSequenceSO (dicas HUD)
│   └── UI/ScreenFlow/
├── Prefabs/
│   ├── Characters/
│   ├── Enemies/
│   ├── Combat/
│   ├── Environment/
│   ├── UI/
│   ├── Multiplayer/
│   └── _Legacy/         # Prefabs antigos (`_Legacy/oLD/`)
├── Scenes/
│   ├── BootstrapScene/
│   ├── Fases/
│   └── UI/
├── Scripts/             # C# (antigo _Scripts)
├── Settings/            # URP, Build Profiles
├── Resources/           # Assets carregados por Resources.Load
├── NavMeshComponents/   # Extensão 2D NavMesh (terceiros)
├── Plugins/             # DOTween, etc.
├── Samples/             # Amostras Unity (AI Navigation)
├── TextMesh Pro/        # Pacote TMP (não mover)
├── _Sandbox/            # Cenas e testes (não build release)
└── Unity.VisualScripting.Generated/
```

## Scripts (`Assets/Scripts/`)

| Pasta | Conteúdo |
|-------|----------|
| `Core/` | Bootstrap, GameFlow, ServiceLocator, GameEvents |
| `Systems/` | Day/Night, Waves, Tutorial (`TutorialManager`) |
| `Components/` | Player, Enemy, Projectile, Collectibles, Health |
| `Multiplayer/` | NGO, lobby, câmera, wave rede |
| `ScriptableObjects/` | Definições de SO (código), incl. `Tutorial/` |
| `UI/` | HUD, menus, botões (`TutorialUIController`) |
| `VFX/` | Efeitos visuais |
| `Audio/` | Mixer settings, buses (`UiSfxPlayer`, `EnemySfxBus`), configs SO |

**Removido:** `_Scripts/Scriptables/` (pasta vazia duplicada de `ScriptableObjects`).

## Data (`Assets/Data/`)

Somente **assets** (.asset), não código. Organizar por domínio: `Characters/`, `Stats/Player`, `Stats/Enemies`, etc.

### Limpeza 2026-06

Removidos órfãos legados: `PlayersSkiils/`, `Enemy_FastStats`, `AimVisualizerSettings`, `DefaultEnemyTelegraphVisual`.

Renomeados para clareza: `PlayerCoreStats`, `NixieMeleeCombatStats`, `EnemyProjectileStats`, `DefaultGameConfig`, `Fase1`/`Fase2`, `Rato_Padrao_Veloz`.

## Prefabs — convenção de nomes

- `PascalCase` ou padrão já usado (`Rato_Padrao_Base`)
- Um prefab por arquivo; variantes Unity permitidas para inimigos

## Áudio (dívida técnica)

Existem pastas legadas `Audio/Music & Sound/` e duplicatas entre `Music/` e `Music/Musica atualizada/`.  
**Não mover clips em massa** sem plano de substituição de referências. Meta futura:

```
Audio/
├── Music/
└── SFX/
    ├── Player/
    ├── Enemy/
    └── Environment/
```

## O que não reorganizar sem necessidade

- `TextMesh Pro/`, `Samples/`, `Plugins/Demigiant/` — pacotes externos
- `Unity.VisualScripting.Generated/` — gerado automaticamente

## Artes (`Assets/Art/`)

Guia completo para artistas: [docs/practices/10-artes-e-visual.md](../practices/10-artes-e-visual.md).

## Para agentes

Ao adicionar asset: coloque na pasta da tabela acima e atualize este arquivo se criar categoria nova.
