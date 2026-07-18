# Guia Editor — Tutorial dinâmico (dicas na HUD)

Última revisão: 2026-07-18

Setup manual do sistema de dicas. O código C# já está no projeto; este guia cobre **onde montar no Canvas das fases** (não use o prefab legado `Gameplay_UI.prefab`).

## Visão geral

| Peça | Script / asset | Papel |
|------|----------------|-------|
| Dados da dica | `TutorialTipSO` | Texto + gatilho + `requiredCount` (ex. 3 kills) |
| Sequência | `TutorialSequenceSO` | Ordem das dicas |
| Lógica | `TutorialManager` | Avança a sequência via `GameEvents` |
| Apresentação | `TutorialUIController` | Fade + contador + `TextMeshProUGUI` |

Fluxo: gameplay invoca `GameEvents.InvokeTutorial*Executed` → `TutorialManager` avança → `OnTutorialTipChanged` → `TutorialUIController` atualiza o painel.

Multiplayer: cada cliente tem o próprio Manager/UI. Move / Shoot / Ability / Dash só disparam no jogador local; KillEnemies e SealHole usam eventos compartilhados de gameplay.

---

## Sequência padrão (Fase-1)

Asset: `Assets/Data/Tutorial/TutorialSequence.asset`

| Ordem | Asset | Conclusão | Texto PT |
|-------|-------|-----------|----------|
| 1 | `Tip_Move` | WASD | Rápido! Movimente-se usando WASD |
| 2 | `Tip_Shoot` | Botão esquerdo | Agora faça uns ataques! |
| 3 | `Tip_Ability` | **Q e R** (ambos) | Muito bom, agora use suas habilidades! **Q R** (some a tecla usada) |
| 4 | `Tip_Dash` | Shift | Para desviar, use seu dash! |
| 5 | `Tip_KillRats` | 3 inimigos (`requiredCount: 3`) | Agora acabe com essa infestação **0/3** (atualiza) |
| 6 | `Tip_SealHole` | Selar buraco | Ótimo, só falta fechar por onde eles entram! Sele esses buracos! |

---

## Hierarquia real da UI nas fases

Em **Fase-1**, **Fase-2** e **Fase-3** a HUD de jogo fica sob o objeto raiz:

```
---- UI ----
├── UIManager          (CursorManager, overlays de pause/baú, etc.)
└── Canvas             (Fase-1 / Fase-2)
    └──  …             (Fase-3: o Canvas chama-se Gameplay_UI — mesmo papel)
        ├── Button
        ├── PlayerUI
        ├── HordeUI
        ├── HealthBar          ← vida do jogador local (healthBarUi)
        ├── AdrenalineBar      ← adrenalina / frenzy
        ├── Indicator
        │   └── ScienceIndicator   ← contador de magículas
        ├── HordeIndicator     ← wave (legado; em fases de objetivo fica off em runtime)
        └── Introduction       ← só Fase-2 (opcional)
```

**Nome do organizer:** `---- UI ----` (hífens + espaços). Não confundir com o prefab `Assets/Prefabs/UI/Gameplay_UI.prefab`, que é **legado e não é mais a fonte de verdade**.

### O que já está na cena (autorado)

| Widget | Função |
|--------|--------|
| `HealthBar` | Vida do jogador |
| `AdrenalineBar` | Adrenalina |
| `ScienceIndicator` | Magículas da fase |
| `HordeIndicator` | Contador de wave (desligado quando a fase usa objetivo, não wave) |

### O que o runtime cria no mesmo Canvas

`GameplaySceneBootstrap.TryEnsureGameplayHud` resolve o Canvas da fase (preferindo nome `Gameplay_UI`, senão o Canvas filho de um pai com `"UI"` no nome) e garante um `GameplayHudController`, que em `EnsureWidgets` cria/atualiza:

| Widget | Fase | Função |
|--------|------|--------|
| `PlayerAbilityHud` | 1 / 2 / 3 | Skills — canto **inferior esquerdo** |
| `PhaseObjectiveHud` | 1 | “Sele os buracos” + contador |
| `PhaseObjectiveHud` | 2 | “Proteja a carruagem” + barra de trajeto |
| `BossHealthBarHud` | 3 | Banner objetivo + vida do Rei Rato |
| `PlaytestFeedbackButton` | todas | Feedback (canto inferior direito) |

Camadas internas: `GameplayHudLayers` → `AbilityHudLayer`, `ObjectiveHudLayer`, `FeedbackHudLayer`, etc.

**Conclusão:** monte o painel do tutorial **no Canvas da cena** (`---- UI ----` → `Canvas` / `Gameplay_UI`), ao lado de HealthBar / Indicator — não no prefab legado.

---

## 1. Setup do painel (Middle-Left) — por fase

Repita em `Fase-1.unity`, `Fase-2.unity` e `Fase-3.unity` (ou só nas fases que tiverem tutorial).

1. Abra a cena da fase.
2. Hierarchy → `---- UI ----` → **Canvas** (Fase-3: objeto **Gameplay_UI**).
3. Crie um filho **TutorialTipPanel**:
   - **Canvas Group**
   - **Tutorial UI Controller**
   - Filhos **Background** (Image) + **TipText** (TMP)
4. **Âncoras Middle Right** (pivot à direita):
   - `Width = 320`, `Height = 221`; `Pos X = 20` (mais à direita)
5. Filho **Background** → **Image**:
   - Sprite **Pause 1**; `Rotation Z = 90`; tamanho local `221×320` (antes da rotação); cor `(1, 1, 1, 0.28)`; **Preserve Aspect** off (só comprime na altura); **Raycast Target** off.
   - Import da sprite: max size **4096**, sem compressão (Standalone/WebGL) para borda mais nítida.
6. Filho **TipText** → **TextMeshPro - Text (UI)**:
   - Stretch com inset (`SizeDelta` −80/−50); cor **preta**; **negrito**; fonte auto-size 14–22.
   - Fonte: **Fira Sans Medium SDF** (mesmo do gameplay — `TutorialUIController` também aplica em Awake).
7. No `TutorialUIController`:
   - **Tip Label** → TMP
   - **Canvas Group** → o do painel (ou vazio = mesmo GO)
8. No mesmo painel (ou empty `TutorialRoot` sob o Canvas), adicione **Tutorial Manager**.

Sugestão de sibling order: depois de `Indicator` / antes de overlays — o Manager não depende da ordem visual.

Não coloque o tutorial sob `UIManager` (esse objeto cuida de cursor/overlays, não do HUD de combate).

---

## 2. Criação dos Assets (ScriptableObjects)

Pasta: **`Assets/Data/Tutorial/`**

1. Create → Folder → `Assets/Data/Tutorial`.
2. Create → MidnightMeow → Tutorial → **Tip** (ex.):

| Asset | Tip Text Pt | Trigger | Required Count |
|-------|------------|---------|----------------|
| `Tip_Move` | Rápido! Movimente-se usando WASD | **Move** | 1 |
| `Tip_Shoot` | Agora faça uns ataques! | **Shoot** | 1 |
| `Tip_Ability` | Muito bom, agora use suas habilidades! | **UseAbility** | 1 (exige Q **e** R) |
| `Tip_Dash` | Para desviar, use seu dash! | **Dash** | 1 |
| `Tip_KillRats` | Agora acabe com essa infestação | **KillEnemies** | **3** (UI anexa `0/3`) |
| `Tip_SealHole` | Ótimo, só falta fechar… | **SealHole** | 1 |

3. Create → MidnightMeow → Tutorial → **Sequence** (ex. `TutorialSequence`):
   - **Tips:** Move → Shoot → Ability → Dash → KillRats → SealHole.
4. No **Tutorial Manager** daquela cena:
   - **Sequence** → o asset da fase
   - **Auto Start** → true
   - **Start Delay Seconds** → opcional (ex. 0.5)

Para desligar numa fase: desative o Manager, limpe a Sequence, ou remova o painel.

---

## 3. Injeção dos gatilhos (já no código)

| Gatilho | Invoke | Script | Momento |
|---------|--------|--------|---------|
| Move | `GameEvents.InvokeTutorialMoveExecuted()` | `PlayerMovement.cs` | idle → moving |
| Shoot | `GameEvents.InvokeTutorialShootExecuted()` | `PlayerShooting.cs` | tiro válido |
| Shoot (melee) | idem | `PlayerMeleeCombat.cs` | `PerformMeleeHit` (Nix) |
| UseAbility | `GameEvents.InvokeTutorialAbilityExecuted(slot)` | `PlayerAbilityHandler.cs` | Q / R ativados |
| Dash | `GameEvents.InvokeTutorialDashExecuted()` | `PlayerDash.cs` | dash iniciado |
| KillEnemies | `GameEvents.OnEnemyKilledByPlayer` | `TutorialManager` escuta | morte de inimigo |
| SealHole | `GameEvents.InvokeTutorialSealHoleExecuted()` | `NetworkRatHoleSealManager.cs` | `PlayHoleSealedClientRpc` |

UI (só Manager): `InvokeTutorialTipChanged(tip, current, required)` / `InvokeTutorialCompleted`.

`UseAbility` só avança depois de **Ability1 e Ability2** (Q e R). A UI anexa `Q R` e remove cada tecla ao usá-la (`TutorialTipDisplayFormatter.FormatAbilityKeys`). Contador `n/total` só aparece quando `requiredCount > 1` (kills).

---

## 4. Checklist de teste

- [ ] Painel sob `---- UI ----` → Canvas da fase (não no prefab legado).
- [ ] SP Fase-1: 6 dicas avançam na ordem; kills mostram `0/3` → `3/3`; última some após selar.
- [ ] Habilidades: só avança após usar Q **e** R (não basta repetir uma).
- [ ] Skills (`PlayerAbilityHud`) e objetivo da fase continuam visíveis.
- [ ] MP: move/shoot/ability/dash independentes; kill/selo avançam com eventos compartilhados.
- [ ] Troca pt/en atualiza o texto da dica atual (incluindo contador).
- [ ] Fase-3: Canvas nomeado `Gameplay_UI` — mesmo setup de filho TutorialTipPanel.

## Notas

- Textos nos SOs (`tipTextPt` / `tipTextEn`), não Localization Tables. Contador via `TutorialTipDisplayFormatter`.
- `TutorialUIController` exige `CanvasGroup`; atualiza progresso sem fade (só fade ao trocar de dica).
- Prefab legado: [`Gameplay_UI.md`](../prefabs/Gameplay_UI.md) (marcado como legado).
- Eventos: [`02-event-driven.md`](../../practices/02-event-driven.md).
