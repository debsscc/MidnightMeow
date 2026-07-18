# Guia Editor — Tutorial dinâmico (dicas na HUD)

Última revisão: 2026-07-18

Setup manual do sistema de dicas. O código C# já está no projeto; este guia cobre **onde montar no Canvas das fases** (não use o prefab legado `Gameplay_UI.prefab`).

## Visão geral

| Peça | Script / asset | Papel |
|------|----------------|-------|
| Dados da dica | `TutorialTipSO` | Texto + gatilho (`Move` / `Shoot` / `SealHole`) |
| Sequência | `TutorialSequenceSO` | Ordem das dicas |
| Lógica | `TutorialManager` | Avança a sequência via `GameEvents` |
| Apresentação | `TutorialUIController` | Fade + `TextMeshProUGUI` |

Fluxo: gameplay invoca `GameEvents.InvokeTutorial*Executed` → `TutorialManager` avança → `OnTutorialTipChanged` → `TutorialUIController` atualiza o painel.

Multiplayer: cada cliente tem o próprio Manager/UI. Move e Shoot só disparam no jogador local; SealHole chega em todos via `PlayHoleSealedClientRpc`.

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
   - **Image** (fundo)
   - **Canvas Group**
   - **Tutorial UI Controller**
4. **Âncoras Middle Left** (Alt+Shift no preset para pivot):
   - `Pos X ≈ 24`, `Pos Y = 0`
   - `Width = 220`, `Height = 220`
5. **Image:** cor escura com alpha baixo, ex. `(0, 0, 0, 0.45)`; **Raycast Target** off.
6. Filho **TipText** → **TextMeshPro - Text (UI)**:
   - Stretch com margem ~12 px; alignment Center/Middle; wrapping on.
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

| Asset | Tip Text Pt | Tip Text En | Trigger |
|-------|------------|------------|---------|
| `Tip_Move` | Se movimente usando WASD | Move using WASD | **Move** |
| `Tip_Shoot` | Atire com o botão esquerdo do mouse | Shoot with the left mouse button | **Shoot** |
| `Tip_SealHole` | Sele um buraco ficando nas áreas indicadas | Seal a hole by standing in the marked areas | **SealHole** |

3. Create → MidnightMeow → Tutorial → **Sequence** (ex. `TutorialSequence_Fase1`):
   - **Tips:** Move → Shoot → SealHole (Fase-1).
   - Fase-2/3: outra Sequence sem `SealHole`, ou tips específicas (carruagem / boss) quando criar novos gatilhos.
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
| SealHole | `GameEvents.InvokeTutorialSealHoleExecuted()` | `NetworkRatHoleSealManager.cs` | `PlayHoleSealedClientRpc` |

UI (só Manager): `InvokeTutorialTipChanged` / `InvokeTutorialCompleted`.

Nova dica (ex. Dash): enum + evento em `GameEvents` + assinatura no Manager + `Invoke` no script local.

---

## 4. Checklist de teste

- [ ] Painel sob `---- UI ----` → Canvas da fase (não no prefab legado).
- [ ] SP Fase-1: dicas avançam com fade; última some.
- [ ] Skills (`PlayerAbilityHud`) e objetivo da fase continuam visíveis.
- [ ] MP: move/shoot independentes; selo avança SealHole em ambos.
- [ ] Troca pt/en atualiza o texto da dica atual.
- [ ] Fase-3: Canvas nomeado `Gameplay_UI` — mesmo setup de filho TutorialTipPanel.

## Notas

- Textos nos SOs (`tipTextPt` / `tipTextEn`), não Localization Tables.
- `TutorialUIController` exige `CanvasGroup`.
- Prefab legado: [`Gameplay_UI.md`](../prefabs/Gameplay_UI.md) (marcado como legado).
- Eventos: [`02-event-driven.md`](../../practices/02-event-driven.md).
