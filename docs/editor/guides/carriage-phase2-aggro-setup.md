# Guia Editor — Carruagem Fase 2 (escolta, aggro e telegraph)

Última revisão: 2026-07-18

Setup manual após a refatoração de estados da carruagem, `AggroType` nos ratos e dano de telegraph em `Structure`. **Não** use scripts de Editor para gravar Layers/UI — configure no Inspector.

## 1. Layer Setup (Carruagem)

1. Abra o prefab `Assets/Prefabs/Gameplay/Carriage.prefab`.
2. No root **Carriage**:
   - **Tag** = `Structure`
   - **Layer** = `Structure`
3. Confirme que o `BoxCollider2D` está no root (não só em filhos Untagged). O overlap de dano usa esse collider.
4. Filhos do `VisualRoot` podem herdar a layer `Structure` (já aplicado em runtime por `EnsureOfficialPresentation`), mas o collider de hit deve permanecer no root.
5. Em **Edit → Project Settings → Physics 2D → Layer Collision Matrix**, garanta que a layer do telegraph/overlap consegue detectar `Structure` (em geral `Default`/`Enemy` não precisam colidir rigidbody com a carruagem — o dano é via `OverlapBox`/`OverlapCircle` com LayerMask).

## 2. Telegraph Attack — LayerMask nos Patterns

O problema clássico: attacks não atingem a carruagem porque `damageLayers` nos SO só incluem **Player** (`m_Bits: 8`).

1. Abra cada asset em `Assets/Data/Combat/Patterns/` usado na Fase 2, por exemplo:
   - `Rato_Base_RangedCircle`
   - `Rato_Veloz_FastCircle`
   - `Rato_Resistente_Debris`
   - `Rato_Acido_Lane`
   - `Rato_Eletrico_MeleeCircle`
2. Em cada entrada de `strikes[]`, campo **Damage Layers**:
   - Marque **Player**
   - Marque **Structure**
3. Salve o asset (Ctrl+S).

Fallback de código: se a mask ficar `Nothing` (0), o runtime usa `Player | Structure`. Ainda assim configure os assets — o fallback é só rede de segurança.

## 3. EnemyStats — Aggro

1. Abra os SO em `Assets/Data/Stats/Enemies/` (ex.: `Rato_Padrao_Base`, `Rato_Padrao_Veloz`, …).
2. No header **Geral**:
   - **Aggro Type**:
     - `PlayersOnly` — só jogadores (padrão legado / boss).
     - `StructuresOnly` — só a carruagem (tag/layer Structure).
     - `Dynamic` — começa na estrutura; pode trocar para jogador.
3. Se **Dynamic**, configure o header **Aggro Dynamic**:
   - `Swap To Nearby Player` — se um jogador estiver mais próximo que a estrutura (ambos no range), trocar o alvo.
   - `Swap On Damage` — ao tomar dano de um jogador, focar nele até ele sair do range / morrer.
4. Sugestão Fase 2 (escolta):
   - Maioria dos ratos de onda: `Dynamic` com **ambas** flags ligadas.
   - Variante “foco na carroça”: `StructuresOnly`.
   - Rei Rato (Fase 3): manter `PlayersOnly`.

Campo legado `Target Priority` ainda existe: se `Aggro Type` = `PlayersOnly` e `Target Priority` = `Structure`, o runtime trata como `StructuresOnly` (compatibilidade de assets antigos).

## 4. UI Canvas / TextMeshPro flutuante

1. Em `Assets/Data/Gameplay/CarriageConfig.asset`:
   - **Player Presence Radius** — raio do gizmo ciano, do anel pastel in-game e da condição Idle→Moving (default ~8).
   - **Player Presence Layer Mask** — deixe vazio para usar automaticamente a layer `Player`, ou marque `Player`.
   - Header **Visual da área de presença (escolta)**:
     - `Show Player Presence Visual` → ligado (anel pastel no Play Mode).
     - Cores Idle/Moving em tons pastéis e alpha baixo (defaults já suaves).
     - `Presence Zone Sorting Order` → ~12 (abaixo do corpo da carruagem).
   - Textos de escolta:
     - `Escort Idle Text` → “Se aproximem da Carruagem”
     - `Escort Moving Text` → “Protejam a Carruagem”
     - `Escort Broken Text` → “Consertem a Carruagem”
   - Textos de conserto (quando Broken e perto): `Approach Text`, `Press E Text`, formato de progresso.
   - **Repair Prompt Prefab** — prefab world-space TMP (mesmo usado no revive).
2. No prefab Carriage, componente `CarriageRepairWorldUI`:
   - Se `Repair UI Prefab` estiver vazio, o script puxa de `CarriageConfig.repairPromptPrefab`.
   - Não precisa linkar o TMP manualmente: o runtime instancia o prefab e busca `TextMeshProUGUI` / `DownedReviveUILabelView`.
3. Comportamento esperado no Play Mode (cliente):
   - Longe / Idle → texto idle + anel um pouco mais visível
   - Jogador vivo no raio → “Protejam…” + anel mais suave
   - HP 0 → anel some; “Consertem…”; perto → “Aperte E…”; consertando → `%`

## 5. Visual de presença + gizmo de debug

**In-game (Play Mode):** o `CarriagePresenceZoneVisual` é anexado em runtime pelo `CarriageController` — **não** precisa adicionar no prefab. Diâmetro = `2 × Player Presence Radius`. Aparece em Idle/Moving; some em Broken e após a chegada.

**Editor (Scene View):** selecione a carruagem. O `CarriageController.OnDrawGizmos` desenha um wire sphere ciano com o mesmo raio (só debug de editor; não aparece no Game View sem Gizmos ligados).

Ajuste o raio e as cores pastéis no `CarriageConfig` se a escolta estiver larga/estreita ou muito chamativa.

## Checklist rápido

- [ ] Prefab Carriage: Tag + Layer `Structure`
- [ ] Patterns de telegraph: `damageLayers` = Player **e** Structure
- [ ] EnemyStats dos ratos da Fase 2: `AggroType` configurado
- [ ] `CarriageConfig`: raio de presença + textos de escolta + prompt prefab
- [ ] `CarriageConfig`: header de visual de presença ligado (cores pastéis)
- [ ] Play Mode host: anel pastel no chão; carruagem só anda com jogador vivo no raio; ratos Dynamic miram carroça/jogador; telegraph baixa HP da carroça

## Ver também

- [gameplay/carriage.md](../../gameplay/carriage.md)
- [prefabs/Carriage.md](../prefabs/Carriage.md)
- [enemy-telegraph-attacks.md](../../combat/enemy-telegraph-attacks.md)
