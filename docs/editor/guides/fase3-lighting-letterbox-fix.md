# Fase-3 — Iluminação escura fora de 1920×1080 (letterbox / Canvas)

Última revisão: 2026-07-18

## Diagnóstico (por que só a Fase-3)

| | **Fase-1** | **Fase-3 (quebrado)** |
|--|------------|------------------------|
| Pai das luzes | `Lights` (Transform **world-space**) | `Enviroment` → **`Canvas` Overlay** (UI) |
| Global Light | Sob `Lights`, posição world ~`(-7.9, 4.3)` | Filho do **Canvas**, local ~`(-984, -537)` |
| Tipo real do “Global Light 2D” | Point Light com raio grande (mesmo nome) | **Point Light** (`m_LightType: 3`), **não** Global (`4`) |
| Texture / cookie | — | `Texture_Light` Sprite Light, `SizeDelta` **1920×1080**, `scale.z` era **0** |

**Causa:** Point Light + Sprite Light vivem sob um **Canvas Overlay** com `CanvasScaler` referência 1920×1080. Em Free Aspect / 2560×1440 o scaler muda a matriz do Canvas → a luz “ambiente” (Point) sai da área jogável e/ou o cookie Sprite fica desalinhado → **tela preta**. Nas outras fases as luzes estão em world-space.

Isso **não** vem do `AspectLetterboxController` (tarjas 16:9). O objeto que você chama de `letterbox` no Hierarchy corresponde a esse **Canvas de luzes** sob `Enviroment` (no YAML ainda se chama `Canvas`).

**Mitigação em código (já no projeto):**
- `PhaseLightingHierarchyFix` — ao entrar em cena de gameplay, move `Light2D` filhos de Canvas para `Enviroment/Lights` (ou `Lights` na raiz)
- `OrthographicSpriteLightFitter` — escala `Texture_Light` pelo `orthographicSize` × `aspect` da câmera
- Hook em `GameplaySceneBootstrap`

Ainda assim, **recomenda-se corrigir a hierarquia no Editor** (abaixo) para não depender só do runtime.

---

## Guia manual no Unity Editor (Fase-3)

### 1. Abrir a cena

1. Abra `Assets/Scenes/Fases/Fase-3.unity`
2. No Hierarchy, localize: `Enviroment` → `Canvas` (só com `Global Light 2D` e `Texture_Light`)  
   - Se você renomeou para `letterbox`, é esse objeto.

### 2. Criar pasta world-space de luzes (como a Fase-1)

1. Selecione `Enviroment`
2. Create Empty → renomeie para **`Lights`**
3. Reset Transform do `Lights` (Position 0,0,0 local; Scale 1,1,1)

### 3. Desacoplar as luzes do Canvas

1. Arraste **`Global Light 2D`** para dentro de `Enviroment/Lights`
2. Arraste **`Texture_Light`** para dentro de `Enviroment/Lights`
3. Em cada um:
   - Se aparecer aviso de `RectTransform` vs `Transform`: em `Texture_Light`, remova o **RectTransform** se o Unity permitir e mantenha só **Transform** + **Light 2D** (ou deixe o detacher/runtime cuidar; o ideal é Transform puro)
4. **`Global Light 2D`**
   - Position: `(0, 0, 0)` local sob `Lights` (ajuste fino depois se quiser, estilo Fase-1)
   - Scale: `(1, 1, 1)`
   - Confirme no Inspector: **Light Type** — se a intenção é luz global de verdade, mude para **Global**; se quiser manter o look atual, deixe **Point** com Outer Radius alto (~50–60) e Inner ~8
5. **`Texture_Light`**
   - Scale Z = **1** (nunca 0)
   - Remova dependência de SizeDelta 1920×1080 (isso é UI; luz Sprite usa o cookie + scale)
   - Add Component → **`OrthographicSpriteLightFitter`** (já no projeto)
   - Coverage Padding ≈ `1.08`, Follow Camera Center = on

### 4. Remover o Canvas vazio de luzes

1. Se o `Canvas` / `letterbox` sob `Enviroment` ficou **sem filhos**, delete-o  
   - Ele não deve ser o Canvas da HUD (`---- UI ----` / `Gameplay_UI`)
2. **Não** delete o Canvas de gameplay HUD

### 5. Conferir Light 2D (Inspector)

Para **ambos**:

| Campo | Valor sugerido |
|-------|----------------|
| Target Sorting Layers | Todas as layers de gameplay usadas na fase (Default / personagens / cenário) — **não** só UI |
| Blend Style | Index 0 (padrão do URP 2D Renderer) |
| Intensity | Global/Point ambiente: ~1.5–2; Texture_Light: manter look artístico (~16) e validar em Play |
| Shadow | Texture_Light: revisar se Shadow Intensity alto + cookie escuro não “tapar” a fase |

Para **Point “Global Light 2D”** (se não mudar para Global):

| Campo | Nota |
|-------|------|
| Outer Radius | Precisa cobrir o mapa do boss (~50+) |
| Inner Radius | ~8 como na cena atual |
| Position | Perto do centro da arena / sob `Lights` |

Para **Sprite `Texture_Light`**:

| Campo | Nota |
|-------|------|
| Light Type | Sprite |
| Cookie Sprite | Manter o sprite artístico |
| OrthographicSpriteLightFitter | Obrigatório se a luz deve acompanhar a câmera em qualquer aspect |

### 6. O que NÃO fazer

- Não colocar `Light 2D` como filho de **Canvas Overlay** / UI letterbox
- Não usar `SizeDelta` 1920×1080 para “tamanho de luz”
- Não deixar `scale.z = 0` em luzes
- Não confundir este Canvas de luzes com o `AspectLetterboxController` (tarjas pretas DDOL)

### 7. Validação

1. Game View → **1920×1080** → Play → iluminação ok  
2. Game View → **Free Aspect** bem largo → Play → **não** deve escurecer  
3. Game View → **2560×1440** → Play → ok  
4. Console: pode aparecer  
   `[PhaseLightingHierarchyFix] N Light2D movido(s) de Canvas → 'Lights'…`  
   (se ainda houver luz sob Canvas na cena)

### 8. Referência Fase-1

Hierarchy da Fase-1 (padrão correto):

```
Lights
├── Global Light 2D
├── Spot Light 2D
└── Sprite Light 2D (…)
```

Replique esse padrão na Fase-3 sob `Enviroment/Lights`.
