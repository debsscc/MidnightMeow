# Prefabs: UI diversos

Última revisão: 2026-07-18

## Defeat.prefab

**Caminho:** `Assets/Prefabs/UI/Defeat.prefab`

| Confirmar | Valor atual |
|-----------|-------------|
| Canvas modo | Screen Space |
| Botão reiniciar / menu | OnClick destino |
| Cena carregada por `SceneTransition` | |

---

## Victory_prefab.prefab

**Caminho:** `Assets/Prefabs/UI/Victory_prefab.prefab`

| Confirmar | Valor atual |
|-----------|-------------|
| Botão continuar | `Button_Prosseguir` — na vitória da Fase-3 o rótulo vira "Créditos" via `EndGameScreenController` |
| Botão créditos (meio) | `Button_Credits` — **oculto** só na vitória final (Fase-3), para não duplicar o Prosseguir→Créditos |
| Animações / TMP título | |

---

## Controls.prefab

**Caminho:** `Assets/Prefabs/UI/Controls.prefab`

| Confirmar | Valor atual |
|-----------|-------------|
| Script | `ControlsPanelController` (abas + Voltar) |
| Abas | `Tab_KeyboardMouse`, `Tab_Gamepad` |
| Painéis | `Panel_KeyboardMouse`, `Panel_Gamepad` |
| Labels | TMP + `LocalizeStringEvent` (tabela `UI`, chaves `controls.action.*`) |
| Rebuild | Menu Unity: **MidnightMeow → UI → Rebuild Controls Panel Prefab** |
| Menu2 | Abre via Opções → Controles (`UIActionBridge.OpenControlsFromSettings`) ou Saves (`OpenSaveFromSettings` → `ContinueSavePanelController.OpenFromSettings`). O `ControlsPanelController` fica no `UIManager` e só liga/desliga o prefab `Controls` (`panelRoot`); `MainMenuController` não desativa mais o `UIManager` nem o prefab por nome. |
| PauseMenu | `PauseMenuActions.ShowControls()` reutiliza o mesmo prefab |

---

## Shadow.prefab

**Caminho:** `Assets/Prefabs/UI/Shadow.prefab`

| Componente | Notas |
|------------|--------|
| `Solid` | Efeito ghosting/sombra (VFX) |

| Confirmar | Valor atual |
|-----------|-------------|
| Template do ghosting de dash (`Shadow.Sombra` em Cora/Nixie) | Sim |
| **Não** confundir com filho `Shadow` (elipse no chão, layer Shadow) | |
