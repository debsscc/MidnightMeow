# Credits Overlay

Última revisão: 2026-06-18

## Resumo

Overlay global de créditos (singleton DDOL). **Não usa prefab** — a UI é criada em código pelo `CreditsOverlayController`, igual ao `TransitionFadeOverlay`.

## Texto dos créditos

Edite **`Assets/Resources/CreditsBody.txt`**.

O script carrega em runtime:

```csharp
Resources.Load<TextAsset>("CreditsBody");
```

## Abrir

| Origem | Chamada |
|--------|---------|
| Menu | `MainMenuController.OnCredits()` → `CreditsOverlayController.Open()` |
| Pause (solo/MP) | `PauseMenuActions.ShowCredits()` → `OpenFromPause()` |

## Inspector (opcional)

No objeto DDOL `CreditsOverlayController` (criado automaticamente ao rodar):

| Campo | Uso |
|-------|-----|
| `scrollSpeedPixelsPerSecond` | Velocidade da rolagem (~55) |
| `creditsMusic` | Trilha opcional dos créditos |

## Fim da rolagem

Comportamento configurável via **`CreditsPresentationConfig`** (sem acoplar menu/pause/fim de jogo):

| Modo | Comportamento |
|------|----------------|
| `HoldThenFadeClose` (padrão) | Para com o final visível → espera → escurece → fecha |
| `HoldUntilManualClose` | Para no final; usuário fecha com **Fechar** |

### Pause / multiplayer

```csharp
CreditsOverlayController.OpenFromPause();
```

- Esconde o menu de pause enquanto os créditos rolam
- **Não despausa** o jogo (`timeScale` continua 0)
- Ao fechar (Fechar ou fade automático), **restaura o menu de pause** se a partida ainda estiver pausada
### Chamadas

```csharp
// Menu
CreditsOverlayController.Open();

// Outro contexto — ex.: fim de jogo, sem auto-fechar
CreditsOverlayController.Open(CreditsPresentationConfig.ManualClose);
```

Campos padrão no Inspector: `defaultPresentation` (hold 3s, fade 1s).
