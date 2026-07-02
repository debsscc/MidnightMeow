# Prefabs: UI diversos

Última revisão: 2026-05-22

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
| Botão continuar | |
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
| Menu2 | Abre via Opções → Controles (`UIActionBridge.OpenControlsFromSettings`); **não** é aba do `MenuTabController` |
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
