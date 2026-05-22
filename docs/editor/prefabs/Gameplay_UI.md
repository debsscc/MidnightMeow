# Prefab: Gameplay_UI

Última revisão: 2026-05-22  
**Caminho:** `Assets/Prefabs/UI/Gameplay_UI.prefab`

## Resumo

Canvas de HUD principal (vida, upgrades, ícones) durante gameplay single-player ou base.

## Componentes Unity

| Tipo | Presente |
|------|----------|
| `Canvas` + `CanvasScaler` + `GraphicRaycaster` | Sim |
| TMP / Image / Button | Sim |

## Scripts custom (confirmar no Hierarchy)

| Script | *(preencher após inspecionar)* |
|--------|--------------------------------|
| `healthBarUi`? | |
| `UpgradeController`? | |
| `ScienceIndicator`? | |

## Valores a confirmar no Editor

| Objeto | Campo | Valor atual |
|--------|-------|-------------|
| Canvas | Sort Order / Render Mode | |
| Barras de vida | Ref `HealthComponent` ou eventos `GameEvents` | |
| Botões de upgrade | Ref `UpgradeDefinition` assets | |
| EventSystem na cena | Não duplicar com Lobby | |
