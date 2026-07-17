# Remover tela de loading placeholder (softlock Cliente)

Última revisão: 2026-07-17

> Contexto: o Cliente travava em **4%** num painel DDOL gerado por `TransitionFadeOverlay` (`SetLoadingProgress(0.04f)`), enquanto o Host já chegava em **Preparation**. Também: fade preto eterno pós Vitória/Derrota se o fade-in abortava no meio do fade-out. O C# já não cria/ativa o painel de loading; este guia limpa a hierarquia no Editor.

## 1. Limpeza da Hierarquia

### 1.1 Menu2 — `FadeManager` (legado)

1. Abra `Assets/Scenes/UI/Menu2.unity`.
2. Na Hierarchy, busque **`FadeManager`** (Canvas com `SceneTransition` + `LoadingBar`).
3. Confirme no Inspector:
   - `SceneTransition.loadingScreen` → vazio (ou referência morta)
   - Componente `LoadingBar` (marcado `[Obsolete]` no código)
4. **Delete** o GameObject `FadeManager` (e filhos) permanentemente.
5. Salve a cena (`Ctrl+S`).

Esse objeto **não** é DontDestroyOnLoad; vivia só no Menu2. Em Play Mode, o overlay real é o singleton runtime `TransitionFadeOverlay` (criado em código — não aparece como prefab de cena).

### 1.2 Loading1 / Loading2 — UI oficial (manter)

Não delete a UI de progresso nestas cenas:

| Cena | Objetos oficiais |
|------|------------------|
| `Loading1` | `Canvas_UI` (ou Canvas da cena), `ProgressTrack` / `ProgressFill`, `Text_Loading`, `Character_loading` |
| `Loading2` | Mesma estrutura + `LoadingScreenController` |

O `LoadingScreenController` deve permanecer no bootstrap da cena (`---- ScreenFlow ----` ou equivalente).

### 1.3 Runtime DDOL (só verificação)

1. Entre em Play Mode a partir do Bootstrap/Menu.
2. No Hierarchy (com filtro *DontDestroyOnLoad*), localize `TransitionFadeOverlay`.
3. Sob `OverlayRoot` → `TransitionOverlay` deve existir **apenas** o filho **`Fade`** (preto).
4. Se ainda aparecer um painel filho **`Loading`** / texto "Carregando... 4%", você está em build antiga — recompile e reinicie o Play Mode. O código atual **não** cria mais esse painel.

Não use scripts de runtime para `Destroy` de UI; a limpeza é só Editor + recompile.

## 2. Atualização de Referências no Inspector

Após puxar o C# (campos removidos / stub):

### 2.1 `LoadingScreenController` (Loading1 e Loading2)

1. Selecione o objeto com `LoadingScreenController`.
2. Confirme refs preenchidas (não Missing):
   - `Status Text` → `Text_Loading`
   - `Progress Track` / `Progress Fill`
   - `Progress Follower` → `Character_loading` (se houver)
3. O campo antigo **`Build Placeholder If Missing`** foi **removido** do script. Se o Inspector mostrar "Missing script" ou property órfã:
   - Clique com o botão direito no componente → **Remove Component** e re-adicione `LoadingScreenController`, **ou**
   - Rode **Midnight Meow → Setup Loading1 UI** (só Loading1) e religue as refs.
4. Aplique overrides se for Prefab Variant e salve.

### 2.2 `SceneTransition` (se sobrar em alguma cena)

1. Busque globalmente: Hierarchy search `t:SceneTransition`.
2. Em cada instância:
   - `Loading Screen` → deixe **None** (campo ignorado pelo C#).
   - `Fade Image` pode permanecer se a cena ainda usa fade local; o overlay DDOL já cobre o fade global.
3. Prefira remover `SceneTransition` + `LoadingBar` onde só existiam para o placeholder (ex.: FadeManager do Menu2).

### 2.3 Prefabs / Missing Script

1. Abra `Assets/Prefabs` e cenas de fluxo (`Lobby`, `Preparation`, `Loading1`, `Loading2`).
2. Console: filtre por `Missing` / `The referenced script`.
3. Qualquer Missing no `LoadingScreenController` → reatribuir script Assembly-CSharp e refs da seção 2.1.
4. **Não** precisa de SerializeField novo no `TransitionFadeOverlay` — ele é 100% runtime.

## 3. Verificação do NetworkSceneManager

1. Abra o prefab/cena do **`NetworkManager`** (ex.: `Assets/Prefabs/Multiplayer/...` ou Bootstrap).
2. Em **NetworkManager → Scene Management**:
   - **Enable Scene Management** = ligado.
3. Em **File → Build Profiles / Build Settings**, confirme que estas cenas estão na lista (mesma ordem usada em rotas):
   - `Loading1`, `Preparation`, `Loading2`, `Fase-1` (e demais fases), `Lobby`, `Menu2`, etc.
4. Abra `Assets/Data/UI/ScreenFlow/Route_Loading1_Preparation.asset`:
   - `Scene Name` = `Preparation`
   - `Load Kind` = `NetcodeHost`
   - `Transition Mode` = `Fade` (não depende do painel placeholder)
5. Teste multiplayer:
   1. Host + Cliente → Lobby (2 jogadores) → **Loading1** (UI oficial anima).
   2. Cliente **não** deve ver tela azul/escura com "Carregando... 4%".
   3. Ambos chegam em **Preparation** com fade-in limpo.
6. Se o Cliente ficar só em "Aguardando host..." na Loading1 oficial: o Host ainda não disparou `loading1_preparation` — verifique sync de 2 jogadores no Lobby, não o overlay.

## Checklist rápido

- [ ] `FadeManager` removido de Menu2
- [ ] Loading1/2 com `LoadingScreenController` e barra oficial
- [ ] Play Mode: `TransitionFadeOverlay` só com `Fade` (sem painel `Loading`)
- [ ] NGO Scene Management ligado + cenas no Build
- [ ] Host e Cliente: Lobby → Loading1 → Preparation sem softlock
