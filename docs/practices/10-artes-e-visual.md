# Artes e pipeline visual (para artistas)

Última revisão: 2026-05-22

## Objetivo

Facilitar a implementação de artes no Unity: **pastas claras**, **importação consistente** e **campos acessíveis no Inspector** — sem depender de programador para cada sprite novo.

Este documento é para **artistas, game designers visuais e agentes de IA** que mexem em assets 2D do MidnightMeow.

---

## Onde colocar cada tipo de arte

Tudo visual fica em **`Assets/Art/`**:

```
Assets/Art/
├── Sprites/          # PNGs, spritesheets, animações
│   ├── NYXIE/        # Personagem jogador
│   ├── Rats/         # Inimigos
│   ├── Enviroment/   # Mapa, props, colisores visuais
│   ├── Drops/        # Coletáveis
│   ├── Animations/   # Clips .anim e controllers
│   └── id visual/UI/ # HUD, menus, ícones, vitória/derrota
├── Materials/        # Materiais 2D (ex.: Dissolve)
├── Shaders/          # Shaders custom (ex.: DissolveSprite)
├── Fonts/            # .ttf / SDF gerados no TMP
└── Models/           # 3D (se houver)
```

**Não colocar** arte nova solta em `Assets/Prefabs/` ou na raiz de `Assets/`. Prefabs **referenciam** sprites; eles ficam em `Assets/Prefabs/`.

---

## Convenção de nomes de arquivo

| Tipo | Padrão | Exemplo |
|------|--------|---------|
| Sprite único | `PascalCase` ou `snake_case` consistente na pasta | `Barra_Vida.png` |
| Spritesheet | sufixo ou pasta `Walk-rat` | `correspritesheetrato.png` |
| UI | dentro de `id visual/UI/{categoria}/` | `HUD/Barra_Magícula.png` |
| Variante de inimigo | prefixo `Rato_` alinhado ao prefab | mesma pasta `Rats/` |
| Placeholder | só em `Sprites/Placeholder/` | ícones genéricos |

Evite: espaços desnecessários, `Untitled_Artwork`, duplicata `image (27).png` em produção.

---

## Importação de sprites (Inspector)

Ao selecionar PNG em `Assets/Art/Sprites/`:

| Campo | Recomendação 2D pixel art |
|-------|----------------------------|
| **Texture Type** | Sprite (2D and UI) |
| **Sprite Mode** | Single ou Multiple (sheet) |
| **Pixels Per Unit (PPU)** | **100** no projeto (confirmar padrão da pasta pai) |
| **Filter Mode** | Point (no filter) para pixel art nítido |
| **Compression** | None ou baixa para pixel art; testar build |
| **Max Size** | Potência de 2 próxima (512, 1024…) |
| **Mesh Type** | Tight ou Full Rect conforme collider |

**Spritesheet:** abrir **Sprite Editor** → Slice (Grid By Cell Size ou Automatic) → nomes consistentes (`Anda_1`, `Anda_2`…).

**Regra:** depois de mudar PPU ou slice, verificar no **Scene view** se escala no prefab ainda está correta.

---

## Animações

| O quê | Onde |
|-------|------|
| `.anim` clips | `Art/Sprites/Animations/` ou junto do personagem (`NYXIE/`, `Rats/`) |
| Animator Controller | mesma pasta do personagem (ex.: `House.controller`) |

**Fluxo típico:**

1. Importar sheet → Slice.
2. Criar Animation Clip (arrastar frames).
3. Atualizar **Animator Controller** (parâmetros: `Speed`, `Attack`, `Death` — alinhar com programador).
4. No prefab (`Player`, `Enemy`), o campo **Controller** do `Animator` aponta para esse asset.

Artistas podem ajustar **timing** e **curves** no Animation window; mudanças de **parâmetros novos** combinar com dev.

---

## Materiais e shaders

| Asset | Uso |
|-------|-----|
| `Art/Shaders/DissolveSprite.shader` | Morte / desaparecer |
| `Art/Materials/DissolveSprite.mat` | Material de dissolve |

**No prefab:** `SpriteRenderer` → **Material** (não confundir com cor do sprite). Só trocar se o efeito já estiver integrado.

---

## Fontes e UI

- Fontes brutas: `Assets/Art/Fonts/` (ex.: FiraSans).
- TextMesh Pro SDF: gerados em `Assets/Fonts/` ou subpasta TMP — **não mover** pasta `TextMesh Pro/` do pacote.
- **Gameplay:** Fira Sans via `Assets/Resources/GameplayUiFontConfig.asset` (`GameplayUiFonts.Apply`). Prompts world-space (selar / reviver / consertar): `GameplayUiFonts.ApplyWorldInteraction` — tamanho `0.9`, canvas `6.5×1.1`, sorting `450`, opacidade ~0.78.
- **Tutorial:** `TutorialTipPanel` usa Fira Sans Medium (`TutorialUIController` aplica em Awake).
- **Cenas Fase-1/2/3:** textos TMP e UI.Text serializados usam Fira Sans Medium (mesmo asset do HUD "BLOQUEIE OS BURACOS").
- **Menu / títulos / créditos (título):** Inknut Antiqua Black. Corpo dos créditos: Fira Sans (mesmo SO `CreditsVisualConfig`).

**UI sprites:** `Art/Sprites/id visual/UI/` — HUD, Book/menu, Defeat&Victory, Controls, Upgrades.

**Prefab de UI:** `Assets/Prefabs/UI/` — artistas podem trocar **Source Image** nos componentes Image/TMP; evitar renomear GameObjects que scripts referenciam.

---

## Sorting e layers (visibilidade)

| Layer | Uso visual |
|-------|------------|
| Default | Cenário genérico |
| Player | Gato |
| Enemy | Ratos |
| Shadow | Sombra no chão |
| UI | Canvas/HUD |

**Order in Layer** no `SpriteRenderer`: sombra atrás, personagem na frente, VFX por cima — validar na cena `Fase-1` ou prefab `Player`.

**Sorting Layer:** manter lista curta no **Tags & Layers**; pedir ao dev antes de criar layer nova.

---

## Como ligar arte ao jogo (sem código)

1. **Prefab** — abrir em `Assets/Prefabs/...` (ex.: `Characters/Player.prefab`).
2. Selecionar objeto com `SpriteRenderer` / `Image`.
3. Arrastar sprite de `Art/Sprites/...` para **Sprite** ou **Source Image**.
4. Salvar prefab (Ctrl+S).

**Balanceamento visual** (tamanho hitbox, offset de tiro) muitas vezes está no prefab (`firePoint` Transform) — mover no Inspector, não no PNG.

**Dados de jogo** (vida, dano) ficam em `Assets/Data/` — artistas normalmente não editam.

---

## Movimentação de arquivos de arte

Sempre mover PNG + **`.meta` junto** (Unity gera GUID que prefabs usam).

- Preferir arrastar dentro do **Project** do Unity.
- Se mover no Explorer: fechar Unity ou usar reimport; nunca apagar `.meta`.

Ver [08-movimentacao-arquivos.md](08-movimentacao-arquivos.md).

---

## Boas práticas visuais no Editor

### Composição 2D

- Manter **PPU consistente** entre personagens e cenário da mesma fase.
- Props de colisão (`Enviroment/Colisores/`) alinhar pivô ao pé/base do objeto.
- UI exportar em resolução adequada (@1x ou @2x); evitar upscale excessivo no jogo.

### Cores e legibilidade

- HUD: contraste alto (barra vida/magia em `id visual/UI/HUD/`).
- Leribilidade em telas Defeat/Victory — testar sobre fundo escuro do jogo.

### Performance

- Spritesheets > dezenas de PNGs soltos para animação.
- Atlas manual ou Sprite Atlas (se o projeto adotar) para reduzir draw calls.
- Tamanho máximo só o necessário (não importar 4K para ícone 64px).

### Organização

- Uma pasta por **feature** (ex.: `Book/`, `Upgrades/`).
- Thumbnails e WIP em subpastas claras; não misturar com assets de build final.

---

## O que pedir ao programador

- Novo **Sorting Layer** ou **Layer** de física.
- Novo **parâmetro de Animator** usado em código.
- Sprite referenciado em **script** (raro) — preferir arrastar no prefab.
- Efeito shader novo além do Dissolve existente.
- Qualquer asset em `Resources/` ou Addressables (não é padrão atual).

---

## Checklist do artista (entrega)

- [ ] Arquivo na pasta correta em `Assets/Art/Sprites/...`
- [ ] Import settings: Sprite, PPU, Filter Point (se pixel art)
- [ ] Spritesheet fatiado e nomes claros
- [ ] Prefab ou cena atualizada com referência visual
- [ ] Sem `.meta` órfão / duplicata de GUID
- [ ] Testado na cena `Fase-1` ou `_Sandbox` (visual)

---

## Para agentes de IA

Ao adicionar arte: não alterar GUIDs; documentar prefab afetado em `docs/editor/prefabs/`. Se criar pasta nova em `Art/`, atualizar [STRUCTURE.md](../assets/STRUCTURE.md).
