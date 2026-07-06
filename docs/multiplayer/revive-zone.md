# Reviver por zona (área cooperativa)

Última revisão: 2026-07-06

## Comportamento (transplante do selamento de buracos)

1. Jogador A cai **inconsciente**.
2. Aliado vivo se aproxima e vê **"Aperte E para começar a Reviver"** (`DownedPlayerWorldUI`).
3. Ao pressionar **E**, o servidor coloca 1–2 círculos cooperativos (`CooperativeZonePlacementUtility`, igual ao selamento).
4. Aliado(s) permanecem dentro do(s) círculo(s) → progresso sobe (`DownedReviveZoneSystem.TickSession`).
5. Ninguém nas áreas por `reviveAbandonTimeout` → sessão cancelada (igual `abandonTimeout` do selamento).
6. Progresso = 100% → `ServerReviveFromUnconscious()` no jogador caído.

## Configuração (`DownedPlayerConfig`)

Parâmetros espelhados de `RatHoleSealConfig` (ver asset em `Assets/Data/Multiplayer/DownedPlayerConfig.asset`).

## Código

| Script | Papel |
|--------|--------|
| `NetworkDownedReviveManager` | Sessões replicadas, RPC `RequestStartReviveRpc`, tick servidor |
| `DownedReviveZoneSystem` | Progresso/abandono (cópia de `RatHoleSealZoneSystem`) |
| `DownedReviveZoneVisualHost` | Círculos com `SealZoneRingVisual` |
| `PlayerDownedReviveInteraction` | Interact (E) — espelha `PlayerRatHoleSealInteraction` |
| `DownedPlayerWorldUI` | Prompt world-space no jogador caído |
| `RevivePromptWorldUI` | Texto/barra no aliado durante preenchimento |

## Edição visual do prompt (Editor)

O prompt usa **prefab world-space** (sem montagem procedural em código).

### 1. Criar o prefab (copiar estilo do selamento)

1. Na Hierarchy (fora de Play Mode), crie: **UI → Canvas**.
2. No Canvas: **Render Mode = World Space**.
3. Ajuste o **RectTransform** do Canvas (ex.: Width `4.8`, Height `0.22` — igual `RatHoleSealPromptUI`).
4. Defina **Canvas.sortingOrder** ≈ `115`.
5. Filho do Canvas: **UI → Text - TextMeshPro** chamado `Label`.
6. No **TextMeshProUGUI**:
   - Texto: `Aperte E para reviver`
   - Font Size ≈ `1.65`
   - Alignment: Center
   - Wrapping: Disabled
   - Cor ≈ `(0.85, 0.95, 1, 1)` (igual selamento)
7. Arraste o Canvas para `Assets/Prefabs/UI/World/DownedRevivePromptUI.prefab` e apague da cena.

### 2. Conectar ao jogo

**Opção A (recomendada — um lugar só):**

1. Abra `Assets/Data/Multiplayer/DownedPlayerConfig.asset`.
2. Campo **Revive Prompt Prefab** → arraste `DownedRevivePromptUI`.

**Opção B (por personagem):**

1. Abra `Assets/Prefabs/Characters/Nixie.prefab` e `Cora.prefab`.
2. No componente **Downed Player World UI** → **Revive UI Prefab** → arraste o prefab.

### 3. Ajuste fino

| Onde | O quê |
|------|--------|
| Prefab `DownedRevivePromptUI` | Fonte, tamanho, cor, RectTransform |
| Personagem → **Downed Player World UI** | **Offset** (altura acima do sprite) |

## Teste

Host + cliente na Fase-1: derrubar um jogador → E perto do caído → ficar no círculo verde até completar.
