# Credits Overlay

Última revisão: 2026-07-18

## Resumo

Overlay global de créditos (singleton DDOL). **Não usa prefab de UI** — a UI é criada em código pelo `CreditsOverlayController`. Ambiência visual (luz + partículas) reutiliza o mesmo setup do Menu2 via prefab `Resources/UI/MenuUiAmbience`.

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
| `bodyTextColor` | Cor do texto (padrão preto) |
| `bodyWidthNormalized` | Largura da coluna de texto (padrão 0.38 = 38% central) |
| `backgroundSprite` | Fundo do overlay — padrão `New_UI/Tela de Loading/Loading.png` |
| `litBackgroundMaterial` | **Sprite Lit Default** — necessário para Light2D da ambiência |
| `closeButtonSprite` | Fundo do botão **Fechar** |
| `closeButtonColor` | Cor do botão (com sprite, multiplica; sem sprite, cor sólida) |

O controller também aceita `visualConfig` no Inspector; se vazio, carrega o asset de Resources.

Botão **Fechar**: label em **preto**, **bold**, tipografia do corpo dos créditos (`bodyFont`); fundo/tamanho do botão inalterados. Recebe `UiButtonFeedbackUtility` (SFX + `Button_Juiceness` + tint).

## Ambiência (Light + ParticleSystem)

Igual ao Menu2: Canvas em **Screen Space – Camera** + fundo com **Sprite Lit Default**.

| Contexto | Comportamento |
|----------|----------------|
| Cena já tem `Light` + `ParticleSystem` na raiz (Menu2, Lobby, etc.) | Reutiliza a ambiência e a `Main Camera` da cena |
| Cena sem ambiência (Victory, Pause em gameplay, …) | Instancia `Resources/UI/MenuUiAmbience` + câmera dedicada |

As partículas do Menu2 usam `sortingOrder` 1 (acima do Canvas 0). O overlay de créditos usa Canvas **500**, então ao abrir os créditos o `sortingOrder` das partículas sobe para **510** (e volta ao original ao fechar) — senão o painel opaco cobre o efeito.

Gerar/atualizar o prefab (a partir da Menu2):

`MidnightMeow → UI → Build MenuUiAmbience Prefab from Menu2`

## Áudio

Ao abrir, toca `CREDITOS.mp3` via `Resources/CreditsMusicClip` → `MusicCrossfadeController.BeginExternalOverride` (para a trilha da cena, ex.: vitória). Ao fechar, restaura a música da cena ativa.

Na Victory/GameOver, créditos abrem com `ManualClose` (só fecham pelo botão Fechar).

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
| `creditsMusic` | Trilha opcional (fallback: `Resources/CreditsMusicClip` → `CREDITOS.mp3`) |

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
