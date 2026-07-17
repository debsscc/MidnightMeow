# Credits Overlay

Última revisão: 2026-07-16

## Resumo

Overlay global de créditos (singleton DDOL). **Não usa prefab** — a UI é criada em código pelo `CreditsOverlayController`, igual ao `TransitionFadeOverlay`.

## Texto dos créditos

Edite **`Assets/Resources/CreditsBody.txt`** (PT) e **`CreditsBody_en.txt`** (EN).

Trechos com `<size=…%>` (ex.: `120%`, `140%`) são tratados como **título** e usam a fonte de título do visual config.

## Visual (ScriptableObject)

Asset: **`Assets/Resources/CreditsVisualConfig.asset`**  
Menu: `Create → MidnightMeow/UI/Credits Visual Config`

| Campo | Uso |
|-------|-----|
| `titleFont` | Tipografia dos títulos (`<size=…%>` no txt) — padrão Inknut |
| `bodyFont` | Tipografia do restante — padrão Fira Sans Medium |
| `bodyFontSize` | Tamanho base do corpo (padrão 28) |
| `closeButtonSprite` | Fundo do botão **Fechar** |
| `closeButtonColor` | Cor do botão (com sprite, multiplica; sem sprite, cor sólida) |

O controller também aceita `visualConfig` no Inspector; se vazio, carrega o asset de Resources.

Botão **Fechar**: label em **preto**, **bold**, tipografia do corpo dos créditos (`bodyFont`); fundo/tamanho do botão inalterados. Recebe `UiButtonFeedbackUtility` (SFX + `Button_Juiceness` + tint).

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
| `visualConfig` | Override do SO de visual |
| `creditsMusic` | Trilha opcional (fallback: `Resources/CreditsMusicClip` → `Assets/Audio/Music/.../CreditsMusic.wav`) |

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
